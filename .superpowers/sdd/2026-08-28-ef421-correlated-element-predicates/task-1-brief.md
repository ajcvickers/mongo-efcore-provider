### Task 1: `_selfParam` capture on `MongoExpressionTranslator` + `FreeParameterVisitor` reports which parameter it found

**Files:**
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs:59-79` (constructors)
- Modify: `src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.MethodCalls.cs:238-294` (`ReferencesEnclosingScope`/`FreeParameterVisitor`)
- Test: `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/MongoExpressionTranslatorMethodCallsTests.cs` (create if it does not already exist — check first with `Glob` for any existing `*MethodCalls*Tests.cs` under that directory; if one exists, add to it instead)

**Interfaces:**
- Produces: `MongoExpressionTranslator.SelfParam` (internal, settable, nullable `ParameterExpression`) — the root parameter this single-scope translator was built for, or `null` if none was supplied / this is a two-scope translator.
- Produces: a private static `FreeParameterVisitor` that now exposes `FoundParameter` (the actual free `ParameterExpression`, or `null`), in addition to the existing `FoundFreeParameter` bool.
- Produces: `ReferencesEnclosingScope(Expression body, ParameterExpression elementParameter, out ParameterExpression? found)` — same bool return, plus the found parameter.

Since `FreeParameterVisitor` and `ReferencesEnclosingScope` are `private` to the `partial class MongoExpressionTranslator`, they aren't independently unit-testable through a public surface. Write the test against the existing internal test hook instead: check whether `tests/MongoDB.EntityFrameworkCore.UnitTests/Query/NativeTranslation/` already has a test file that constructs a `MongoExpressionTranslator` directly and calls `TryTranslate` on hand-built expression trees (this is the established pattern for this class elsewhere in the test suite — search for `new MongoExpressionTranslator(` in that directory). If no test can reach `ReferencesEnclosingScope` without a behavior change (which arrives in Task 3/5), skip the isolated unit test for this task and rely on Task 3/5's differential tests to exercise it — note this explicitly in the commit message rather than fabricating a test that doesn't test real behavior.

- [ ] **Step 1: Check for an existing reachable test surface**

Run: `grep -rn "new MongoExpressionTranslator(" tests/MongoDB.EntityFrameworkCore.UnitTests/`

If this returns a test file with direct access to translator internals (e.g. via `InternalsVisibleTo`), read it to confirm the pattern. If it returns nothing, this class is only exercised via the FunctionalTests end-to-end path — proceed to Step 2 without a Task-1-local unit test; Task 3 and Task 5's differential FunctionalTests are what prove this step's correctness.

- [ ] **Step 2: Modify `FreeParameterVisitor` to report the found parameter**

In `MongoExpressionTranslator.MethodCalls.cs`, replace:

```csharp
    private static bool ReferencesEnclosingScope(Expression body, ParameterExpression elementParameter)
    {
        var visitor = new FreeParameterVisitor(elementParameter);
        visitor.Visit(body);
        return visitor.FoundFreeParameter;
    }

    /// <summary>
    /// Finds a reference to a <see cref="ParameterExpression"/> that is free in the visited expression — i.e.
    /// neither the element parameter it was constructed with, nor bound by a <see cref="LambdaExpression"/>
    /// encountered while descending, nor an EF query parameter. See
    /// <see cref="ReferencesEnclosingScope"/> for why this distinction matters.
    /// </summary>
    private sealed class FreeParameterVisitor(ParameterExpression elementParameter) : ExpressionVisitor
    {
        private readonly List<ParameterExpression> _bound = [elementParameter];

        public bool FoundFreeParameter { get; private set; }
```

with:

```csharp
    /// <summary>
    /// As the single-bool overload, but also reports WHICH parameter was found free — used by the
    /// <c>Count(pred)</c>/<c>Any</c>/<c>All</c> call sites to check, by <see cref="ReferenceEquals"/>, whether
    /// the free parameter is the immediate enclosing translator's own root parameter
    /// (<see cref="MongoExpressionTranslator.SelfParam"/>). A match upgrades the shape from "decline" to
    /// "build a two-scope child translator"; anything else (no match — correlation reaches past the
    /// immediate root) still declines exactly as before.
    /// </summary>
    private static bool ReferencesEnclosingScope(
        Expression body, ParameterExpression elementParameter, out ParameterExpression? found)
    {
        var visitor = new FreeParameterVisitor(elementParameter);
        visitor.Visit(body);
        found = visitor.FoundParameter;
        return visitor.FoundFreeParameter;
    }

    /// <summary>
    /// Finds a reference to a <see cref="ParameterExpression"/> that is free in the visited expression — i.e.
    /// neither the element parameter it was constructed with, nor bound by a <see cref="LambdaExpression"/>
    /// encountered while descending, nor an EF query parameter. See
    /// <see cref="ReferencesEnclosingScope"/> for why this distinction matters.
    /// </summary>
    private sealed class FreeParameterVisitor(ParameterExpression elementParameter) : ExpressionVisitor
    {
        private readonly List<ParameterExpression> _bound = [elementParameter];

        public bool FoundFreeParameter { get; private set; }

        /// <summary>
        /// The first free parameter found, or <see langword="null"/> if none has been found yet. Only the
        /// FIRST one is recorded: a body with two distinct free parameters is already declined by the caller
        /// regardless of which one is reported (neither can be confirmed as the sole enclosing scope), so
        /// there is no need to track more than one.
        /// </summary>
        public ParameterExpression? FoundParameter { get; private set; }
```

Then update `VisitParameter` in the same class:

```csharp
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!ContainsByIdentity(node) && !NativeQueryParameter.TryGetQueryParameterName(node, out _))
            {
                FoundFreeParameter = true;
                FoundParameter ??= node;
            }

            return node;
        }
```

- [ ] **Step 3: Add `SelfParam` to `MongoExpressionTranslator`**

In `MongoExpressionTranslator.cs`, replace:

```csharp
    private readonly IEntityType _entityType;
    private readonly ParameterExpression? _outerParam;
    private readonly IEntityType? _outerEntityType;
    private readonly string? _innerPrefix;

    /// <summary>
    /// Creates a single-scope <see cref="MongoExpressionTranslator"/> for the given entity type.
    /// </summary>
    /// <param name="entityType">The entity type whose properties and element names are used during translation.</param>
    public MongoExpressionTranslator(IEntityType entityType)
    {
        _entityType = entityType;
    }
```

with:

```csharp
    private readonly IEntityType _entityType;
    private readonly ParameterExpression? _outerParam;
    private readonly IEntityType? _outerEntityType;
    private readonly string? _innerPrefix;

    /// <summary>
    /// Creates a single-scope <see cref="MongoExpressionTranslator"/> for the given entity type.
    /// </summary>
    /// <param name="entityType">The entity type whose properties and element names are used during translation.</param>
    /// <param name="selfParam">
    /// The root <see cref="ParameterExpression"/> this translator was built for (a <c>Where</c> predicate's,
    /// an <c>OrderBy</c> key selector's, a <c>Select</c> projection's, etc.), or <see langword="null"/> when
    /// none is available. Used ONLY to detect a single-level CORRELATED owned-collection element predicate
    /// (<c>Count(pred)</c>/<c>Any</c>/<c>All</c>) nested inside this translator's own tree: when such a
    /// predicate's free parameter is <see cref="ReferenceEquals"/>-identical to <see cref="SelfParam"/>, the
    /// quantifier/count call sites build a two-scope child translator instead of declining. See EF-421.
    /// </param>
    public MongoExpressionTranslator(IEntityType entityType, ParameterExpression? selfParam = null)
    {
        _entityType = entityType;
        SelfParam = selfParam;
    }

    /// <summary>
    /// The root <see cref="ParameterExpression"/> this SINGLE-SCOPE translator was built for — see the
    /// constructor parameter of the same name. SETTABLE (not constructor-only) because
    /// <see cref="NativeSlotPopulator.PopulateNativeSlots"/> reuses one translator instance across several
    /// slot operators (<c>Where</c>/<c>OrderBy</c>/<c>ThenBy</c>/etc.), each with its own lambda parameter;
    /// that call site sets this field for the duration of each arm rather than constructing a fresh
    /// translator per arm. Always <see langword="null"/> on a two-scope translator (the constructor below
    /// never sets it) — a correlation nested two-or-more scopes deep is out of EF-421's scope and must keep
    /// declining, which requires that a two-scope child never itself exposes a further <see cref="SelfParam"/>.
    /// </summary>
    internal ParameterExpression? SelfParam { get; set; }
```

- [ ] **Step 4: Build**

Run: `dotnet build MongoDB.EFCoreProvider.sln -c "Debug EF10"`
Expected: builds clean (no callers of `ReferencesEnclosingScope`'s old 2-arg overload exist yet outside this file — the two real call sites are updated in Tasks 3 and 5 — but confirm there is no other caller by running `grep -rn "ReferencesEnclosingScope(" src/`).

If the grep finds the two call sites in `MongoExpressionTranslator.cs` (the quantifier arm and the `Count(pred)` arm) still using the old 2-arg form, the build will fail. Fix those two call sites minimally for now (just add a discard, `out _`) so the build passes — Tasks 3 and 5 will replace the discard with the real branch:

```csharp
if (ReferencesEnclosingScope(elementLambda.Body, elementLambda.Parameters[0], out _))
    return null;
```

and

```csharp
if (ReferencesEnclosingScope(countPredicate.Body, countPredicate.Parameters[0], out _))
    return null;
```

- [ ] **Step 5: Run the full existing native-translation test suite to confirm no regression**

Run: `dotnet test MongoDB.EFCoreProvider.sln -c "Debug EF10" --no-build --filter "FullyQualifiedName~NativeOwnedCollection"`
Expected: PASS (identical to before this task — pure plumbing, no behavior change yet).

- [ ] **Step 6: Commit**

```bash
git add src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.cs \
        src/MongoDB.EntityFrameworkCore/Query/NativeTranslation/MongoExpressionTranslator.MethodCalls.cs
git commit -m "EF-421: capture root parameter identity on MongoExpressionTranslator"
```

---

