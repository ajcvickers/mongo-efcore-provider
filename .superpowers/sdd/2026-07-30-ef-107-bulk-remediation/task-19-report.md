# Task 19 report — Cache the compiled setter value and the aggregate renderer

**Status: DONE**
**Commit: `df9c21f` — perf: cache the compiled setter fallback delegate and the aggregate-expression renderer**

Branch `EF-361`, based on `812ad18`. The brief's file list was correct: everything landed in
`Query/Expressions/MongoNonQueryExpression.cs` and `Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs`,
plus one new functional test.

---

## 1. The staleness invariant I established

**Invariant: what is cached is the compiled *delegate*, never the evaluated *value*, and a delegate compiled from
an immutable expression tree is behaviour-identical to any other delegate compiled from that same tree. Therefore
re-using it is safe unconditionally — no property of the expression (parameter-independence in particular) is
required.**

Reasoning, in order:

1. `setter.ValueExpression` is an ordinary LINQ expression tree. Expression nodes are immutable, so the tree that
   the delegate is built from cannot change between executions.
2. `Expression.Lambda<Func<object?>>(Convert(expr)).Compile()` is a pure function of that tree. Two delegates built
   from it at different times are interchangeable.
3. The emitted code reads all external state at **invoke** time, not at compile time. I verified this empirically
   rather than assuming it (scratch probe on net10.0): with a `MemberExpression(Constant(obj), field)` tree, the
   *same* compiled delegate returned `1` then `42` across a mutation of `obj.Value`; likewise for a property read
   (`420` → `70`), an instance method call (`1007` → `1099`) and a static field read (`5` → `6`). There is no
   constant folding of reads through a baked-in `ConstantExpression`.
4. Combining 1–3: invoking the cached delegate on execution *N* returns exactly what compiling a fresh delegate and
   invoking it on execution *N* would have returned. The cache is behaviour-preserving by construction, so there is
   nothing to go stale.

This is a strictly stronger and simpler story than the brief's. It is also the *only* correct one, because —

**the brief's invariant is empirically false.** I disproved it. A value lambda
`o => capturedPrefix + Guid.NewGuid()` **does** reach the fallback with an extracted query parameter still embedded
in the tree, on both EF9 and EF10:

```
EF10: Add MethodBinaryExpression :: ([Microsoft.EntityFrameworkCore.Query.QueryParameterExpression] + Convert(NewGuid(), Object))
EF9 : Add MethodBinaryExpression :: (__prefix_0 + Convert(NewGuid(), Object))
```

So "the fallback branch is reached solely for expressions with no query-parameter dependency" is simply not true,
and any design resting on it would have been resting on nothing.

That shape is harmless anyway, and for a reason worth writing down: such a tree **cannot be compiled at all**.
`Compile()` throws (`ArgumentException: must be reducible node` on EF10 for `QueryParameterExpression`; the
equivalent unbound-variable failure on EF9 for a free `ParameterExpression`), which `TranslateBulkOrThrow` converts
into EF's canonical non-query translation failure. Because the cache slot is only populated *after* `Compile()`
returns, a shape that throws goes on throwing identically on every execution — the cache cannot convert a hard
failure into a silent stale write. Verified: the probe test failed with EF's translation-failure message and a
stack ending in `CompileAndEvaluate` → `Expression`1.Compile()`.

## 2. What I did about the `Debug.Assert` / detector, and `QueryParameterExpression`'s base type

**I shipped neither the assert nor the detector, and no runtime guard at all.** Justification: by §1 the cache needs
no invariant, so there is nothing for a guard to protect. A guard asserting a false proposition (§1) would fire on
legitimate input.

On the detector specifically, both of your suspicions are confirmed:

- **`QueryParameterExpression` does not derive from `ParameterExpression`.** Probed against
  `Microsoft.EntityFrameworkCore 10.0.8`: the base chain is
  `Microsoft.EntityFrameworkCore.Query.QueryParameterExpression` → `System.Linq.Expressions.Expression` →
  `System.Object`, and `IsSubclassOf(typeof(ParameterExpression)) == False`. A `VisitParameter` override therefore
  never fires on EF10 — dead code exactly where it matters.
- `Debug.Assert` compiles out of Release, so it would have given production nothing.

## 3. Which `Setter` option I chose, and why

**Option 1: `Setter` is now a `sealed class`** with three get-only properties and the cache field.

- Your Correction 2 is right and the brief's rationale was backwards: C# record equality compares *all* instance
  fields including ones declared in the record body, so `internal Func<object?>? CompiledValue;` inside
  `public sealed record Setter(...)` **would** participate in `Equals`/`GetHashCode`.
- I verified your claim that no record feature is used. Grepped every `Setter` reference across `src` and `tests`:
  construction is positional only (`new MongoNonQueryExpression.Setter(property, value, isSelfReferencing)` and
  `new(property, Expression.Constant(5), false)` in `MongoNonQueryExpressionTests`); reads are `Property`,
  `ValueExpression`, `IsSelfReferencing`; the only near-equality consumer, `RejectDuplicateSetters`, compares
  `setter.Property`, not setters. No `with`, no deconstruction, no `ToString()`/interpolation of a `Setter`, no
  dictionary/set keyed on one.
- I preferred this to option 3 (parallel slot array on `MongoNonQueryExpression`) because option 3 buys immutability
  at the cost of threading an index from `BuildUpdate` through `SerializeConstant` into the evaluator and lazily
  allocating an array sized to a list that is empty for deletes — more moving parts guarding a value that is not
  externally observable. Option 2 (keep the record, correct the comment) was my last choice: it leaves a mutable
  field silently inside record equality, which is precisely the thing most likely to bite a later reader.
- The doc comment now states the true reason for not being a record, rather than the false one.

**Concurrency detail I added beyond the brief:** the field is `volatile`. Two threads racing to populate the slot is
benign (§1: the delegates are interchangeable; reference assignment is atomic; worst case one is discarded). A
thread observing the *reference* published ahead of the delegate object's own field initialization would **not** be
benign — invoking a half-published delegate is an access violation, not a wrong value. Non-volatile publication is
not ordered on a weakly-ordered architecture (and this repo is developed on arm64), so `volatile` closes that at the
cost of one acquire load on a path that immediately does a reflective-free delegate invoke. I kept the brief's
read-once-into-a-local shape as you asked, so a concurrent write cannot be observed twice within one call.

## 4. The two `CompileAndEvaluate` call sites (Correction 1)

`CompileAndEvaluate` is **deleted**; there is exactly one cached fallback and no uncached path.

The `_ =>` arm of the `member.Member` switch now falls through to the query-parameter checks and then the cached
fallback. **This is not a behaviour change, because that arm is unreachable by construction:** a
`MemberExpression`'s `Member` is always a `FieldInfo` or a `PropertyInfo`. I verified rather than assumed —
`Expression.MakeMemberAccess` rejects every other `MemberInfo` kind:

```
MakeMemberAccess(method) threw: ArgumentException: Member 'Void M()' not field or property (Parameter 'member')
MakeMemberAccess(event)  threw: ArgumentException: Member 'System.Action E' not field or property (Parameter 'member')
```

and `MemberExpression`'s constructor is internal, so the factories are the only way to build one. I rewrote the
`switch` expression as a `switch` statement with only the two reachable cases and a comment recording that they are
exhaustive. So the answer to "which behaviour did you ship" is: the one that is observationally identical to the old
one, with the dead arm removed instead of silently relocated.

## 5. Perf claims (Correction 5)

No ranking is asserted anywhere. The comments state only what is certain and mechanical:

- fallback: "without the cache a lambda is rebuilt and compiled on every execution of an already-compiled query".
- renderer: the `MakeGenericMethod` construction is no longer repeated per self-referencing setter per execution —
  and I explicitly say **the reflective `Invoke` still happens per setter per execution**, since that is what
  actually renders the value. (The brief's framing, and the task description's "`MakeGenericMethod` + `Invoke` …
  is eliminated", overstate this: only the generic-method construction is cached.)

No benchmark was run and none is claimed.

## 6. The Correction 6 test, and the failure mode I observed

New test: `ExecuteUpdateTests.ExecuteUpdate_re_evaluates_a_non_parameterizable_setter_value_on_every_execution`.

**Does the brief's nominated test reach the cached fallback? No.** You were right that it proves nothing, and it is
worse than that — it never touches the fallback. I instrumented `EvaluateToConstant` to log the shape of every
setter value it sees, and ran `ExecuteUpdateTests`, `BulkOperations*` and `NorthwindBulkUpdatesMongoTest` on EF10:
84 evaluations, **80 `QueryParameterExpression` and 4 `ConstantExpression`, zero fallback hits**. A separate
instrumentation pass on `CompileAndEvaluate` alone produced no log file at all. So the entire pre-existing bulk
suite — `ExecuteUpdate_with_captured_variable_set_value` included — leaves both the fallback *and* the
`MemberExpression`-on-constant branch completely uncovered.

**Finding the shape that does reach it.** I probed seven candidate shapes (temporary throwaway test class, not
committed) on both EF9 and EF10:

| shape | EF10 | EF9 |
|---|---|---|
| `SetProperty(o => o.Status, Guid.NewGuid().ToString())` | query parameter | (n/a, see below) |
| `SetProperty(o => o.Status, DateTime.UtcNow.ToString("O"))` | query parameter | **fallback** |
| `SetProperty(o => o.Quantity, new Random().Next(100))` | query parameter | **fallback** |
| `SetProperty(o => o.Status, o => Guid.NewGuid().ToString())` | **fallback** | **fallback** |
| `SetProperty(o => o.Status, o => capturedPrefix + Guid.NewGuid())` | **fallback**, then throws | **fallback**, then throws |
| `SetProperty(o => o.Status, o => capturedVariable)` | query parameter | query parameter |
| `SetProperty(o => o.Status, o => capturedVariable.ToUpperInvariant())` | query parameter | query parameter |

Two things fell out of this that are worth recording:

- EF10 parameterizes non-lambda setter values **even when the subtree is on EF's own non-evaluatable list**
  (`EvaluatableExpressionFilter`'s statics are `DateTimeNow/UtcNow/Today`, `DateTimeOffsetNow/UtcNow`, `GuidNewGuid`,
  `RandomNextNoArgs/OneArg/TwoArgs` — I enumerated them off the 10.0.8 assembly). EF9 does not. So the fallback is
  materially more reachable on EF9 than on EF10.
- Inside a **value lambda**, both versions leave the non-evaluatable call in the tree. That is the one shape that
  reaches the fallback identically on EF9 and EF10, so that is what the test uses.

**The test.** Two `ExecuteUpdate` calls through the same local function (identical trees → same EF cache key → same
compiled query → same `MongoNonQueryExpression.Setter` instance), each writing `Guid.NewGuid().ToString()` to the
same single document, reading back with `AsNoTracking()` between them, asserting both values parse as Guids and
differ.

I confirmed it genuinely hits the cache slot twice, not just twice through the method, by logging
`RuntimeHelpers.GetHashCode(expression)` — the same reference identity both times, on both configurations:

```
Debug EF10: FALLBACK: exprId=3685159  Call :: NewGuid().ToString()   (x2, identical id)
Debug EF9 : FALLBACK: exprId=61401293 Call :: NewGuid().ToString()   (x2, identical id)
```

**Failure mode observed.** I deliberately broke the implementation to cache the *value* rather than the delegate
(`var once = …Compile()(); compiled = () => once;`) and ran the test on both configurations:

```
Debug EF10  Assert.NotEqual() Failure: Strings are equal
            Expected: Not "f378a110-57a9-4723-9d83-a27a3616e4b8"
            Actual:       "f378a110-57a9-4723-9d83-a27a3616e4b8"
Debug EF9   Assert.NotEqual() Failure: Strings are equal
            Expected: Not "86c93ca5-5ea8-4faf-935d-d437a398cf82"
            Actual:       "86c93ca5-5ea8-4faf-935d-d437a398cf82"
```

Then restored the correct implementation (byte-identical to the pre-experiment file, diffed against a backup) and
re-ran green. So this is a test whose failure mode I have watched, not one that merely passes.

The test's comment records how the shape was chosen, that it stays non-self-referencing (the value lambda ignores
its parameter, so the value is evaluated once per execution rather than per document — a pre-existing semantic the
test deliberately does not pin, hence targeting exactly one document), and why `AsNoTracking()` is needed.

## 7. TDD evidence

1. Instrumented the fallback → ran the bulk suites → **no log file**: proved the fallback was untested before any
   production change (and that the brief's nominated test does not reach it).
2. Instrumented every branch → ran the bulk suites → the 80/4/0 distribution above.
3. Probed seven shapes to find one that reaches the fallback on both EF9 and EF10.
4. Wrote the test against the **unmodified** production code; it passed and the instrumentation showed two fallback
   hits with identical expression identity — i.e. the test provably exercises the slot before the cache existed.
5. Implemented the cache; test still green.
6. Broke the cache (value instead of delegate); watched the test fail on both configurations with the message above.
7. Restored; verified the restored file was byte-identical to the pre-break backup; re-ran green.

## 8. Verification

Builds — `dotnet build MongoDB.EFCoreProvider.sln -c "<cfg>"`, all after the final `volatile` edit:

| configuration | result |
|---|---|
| Debug EF8 | Build succeeded, 0 warnings |
| Debug EF9 | Build succeeded, 0 warnings |
| Debug EF10 | Build succeeded, 0 warnings |

Northwind (the counts you asked for):

| suite | result | baseline |
|---|---|---|
| `NorthwindBulkUpdatesMongoTest`, Debug EF10 | **181 / 181 passed** | 181/181 ✓ |
| `NorthwindBulkUpdatesMongoTest`, Debug EF9 | **169 / 169 passed** | 169/169 ✓ |

Targeted bulk suites (`ExecuteUpdateTests`, `ExecuteDeleteTests`, `BulkOperations*`, `MongoNonQueryExpressionTests`,
`BulkSetterRebindingTests`):

| configuration | result |
|---|---|
| Debug EF10 | 11 unit + 92 functional passed, 1 skipped (pre-existing `ExecuteDelete_two_phase_on_standalone_throws_actionable_error`) |
| Debug EF9 | 11 unit + 93 functional passed, 1 skipped (same) |

Unit tests, all three configurations: EF8 283/283, EF9 304/304, EF10 297/297.

Full solution, all three configurations (`MONGODB_URI`/`ATLAS_URI` unset, TestContainers atlas-local):

| configuration | UnitTests | SpecificationTests | FunctionalTests |
|---|---|---|---|
| Debug EF10 | 297 passed | 4571 passed, 28 skipped | 1803 passed, 52 skipped |
| Debug EF9 | 304 passed | 4999 passed, 25 skipped | 1781 passed, 52 skipped |
| Debug EF8 | 283 passed | 4686 passed, 25 skipped | 1684 passed, 51 skipped |

Zero failures anywhere. Test output is pristine — the new test writes nothing to console, and all instrumentation
and the throwaway probe test class were removed before committing (grepped `src`/`tests` for the scratchpad path:
no hits).

## 9. Constraints checked

- **Three configurations.** All bulk code stays inside `#if !EF8`; the new `using System.Collections.Concurrent;` is
  itself `#if !EF8`-guarded to match the file's existing convention for the bulk-only imports, so EF8 gets no unused
  using. `EvaluateToConstant`'s `#if EF8 || EF9` / `#else` split is untouched and both arms still compile and run.
- **Redaction / `MongoEventId` / Query↔Storage bulk seam.** Not touched. `CreateBulkPlan` and `MongoBulkPlan` are
  unchanged; the caches live entirely on the Query side of the seam.
- **Public API.** Confirmed myself: `MongoNonQueryExpression` is `internal sealed`, so the nested `public
  Setter`'s accessibility domain is the assembly. Per `AGENTS.md` ("Changes to anything `internal` — regardless of
  `InternalsVisibleTo` … Internal code is never part of the public surface"), the record→class change and the new
  field are not public-API breaks. **No `BREAKING-CHANGES.md` entry.**
- **BOMs.** Both `src/` files still begin `EF BB BF`. `ExecuteUpdateTests.cs` had no BOM at `HEAD` (verified with
  `git show HEAD:… | head -c 3` → `2f2a20`) and still has none.
- **Nullable.** `internal volatile Func<object?>? CompiledValue;` is correctly annotated.
- **Env vars.** Every run used `env -u MONGODB_URI -u ATLAS_URI`; neither was set.

## 10. Files changed

- `src/MongoDB.EntityFrameworkCore/Query/Expressions/MongoNonQueryExpression.cs` — `Setter` record → sealed class
  with get-only properties and a `volatile` internal cache slot; doc comment stating the true reason it is not a
  record.
- `src/MongoDB.EntityFrameworkCore/Query/Visitors/MongoShapedQueryCompilingExpressionVisitor.cs` —
  `EvaluateToConstant` takes the `Setter` and consults/populates the delegate cache; `CompileAndEvaluate` deleted;
  the `member.Member` switch reduced to its two reachable cases with a comment on exhaustiveness;
  `RenderAggregateExpressionCache` + `GetAggregateRenderer` added and used by `RenderSelfReferencingValue`;
  `#if !EF8`-guarded `using System.Collections.Concurrent;`.
- `tests/MongoDB.EntityFrameworkCore.FunctionalTests/Query/ExecuteUpdateTests.cs` — new test
  `ExecuteUpdate_re_evaluates_a_non_parameterizable_setter_value_on_every_execution`.

## 11. Self-review findings

- *Correct under repeated execution?* Yes, and now covered by a test that provably hits the same cache slot twice
  and fails if the cached thing goes stale.
- *Correct under concurrency?* Yes; the benign-race argument is written down, the slot is read once into a local,
  and I hardened publication with `volatile` after concluding the "benign" argument covers duplicate *construction*
  but not *half-publication*.
- *Is every comment true?* I checked each factual claim by execution or probe rather than by memory: the
  invoke-time-read behaviour of compiled delegates, `MemberExpression.Member`'s field/property-only constraint,
  `QueryParameterExpression`'s base chain, EF's non-evaluatable list, the "no shape in the bulk suite reaches the
  fallback" claim, the "same Setter instance twice" claim, and the "`Invoke` still happens per execution"
  qualification. No comment asserts an unmeasured performance ranking.
- *Does the new test fail if the cache goes stale?* Yes — observed on EF9 and EF10.
- *Test output pristine?* Yes; instrumentation and probe class removed, no console writes, no new skips.
- Untouched pre-existing gap I noticed but did not act on: the `MemberExpression { Expression: ConstantExpression }`
  branch of `EvaluateToConstant` is also completely uncovered by the suite (0 hits across all bulk tests on EF10). It
  is not in this task's scope and I did not change its behaviour, but if you want it pinned it would need its own
  shape hunt.

## 12. Concerns

1. **The renderer cache is an unbounded static `Type`-keyed dictionary.** Keys are `(TSource, property.ClrType)`,
   bounded by the model, and `Type` keys root their assemblies — so with collectible/dynamically-emitted entity
   types this retains them for process lifetime. This is the same trade-off EF Core itself makes in its own static
   reflection caches, and the brief specified this shape, so I shipped it; flagging it only so the choice is on the
   record.
2. **A latent semantic I noticed and deliberately did not touch:** a *value lambda* whose body does not reference
   the entity (e.g. `SetProperty(o => o.Status, o => Guid.NewGuid().ToString())`) is classified
   non-self-referencing and evaluated **once per execution**, so every matched document gets the *same* Guid — a
   user could reasonably expect one per document. That is pre-existing behaviour, unaffected by this change, and my
   test avoids pinning it by targeting a single document. Worth a separate ticket if you agree it is surprising.
3. **The fallback's reachability differs by EF version** (EF9 reaches it from plain `DateTime.UtcNow` /
   `new Random().Next(...)` setter values; EF10 parameterizes those). Not a problem for the cache — which is safe on
   both — but it means the EF9 bulk path exercises this code far more than EF10 does, which is worth knowing if
   anyone reasons about coverage here later.
