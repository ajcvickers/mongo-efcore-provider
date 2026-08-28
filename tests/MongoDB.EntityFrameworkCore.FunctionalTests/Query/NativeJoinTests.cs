/* Copyright 2023-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Diagnostics;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Query.NativeTranslation;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// EF-392 (native-join-translation plan).
/// <para>
/// <b>History, so the earlier framing here is not re-derived.</b> The first attempt at this feature concluded
/// a genuine two-sided <c>Join</c>/<c>LeftJoin</c> was PERMANENTLY out of scope (EF-439): EF Core's
/// <c>NavigationExpandingExpressionVisitor</c> normalizes such a join into the same shape as an
/// Include-generated one, so the two cannot be told apart at bind time, and a provider-side disambiguation
/// mechanism was ruled out. That premise was true but the conclusion drawn from it was too strong — the two
/// shapes do not NEED distinguishing, because for the same navigation they emit the same <c>$lookup</c> and
/// produce the same rows. The v2 plan therefore keeps the "indistinguishable" finding and drops the "always
/// declines" conclusion: a join records a <see cref="MongoDB.EntityFrameworkCore.Query.Expressions.MongoJoinScope"/>
/// (pure metadata, registering nothing), and the operator that CONSUMES it — a <c>Where</c> over the outer
/// side, a bare whole-entity <c>Select</c>, or a scalar-only wrapped <c>Select</c> — confirms it and registers
/// the <c>$lookup</c> at that point.
/// </para>
/// <para>
/// These tests pin both sides of that line: the shapes that now go native (asserted under
/// <see cref="MongoQueryMode.NativeOnly"/>, the only mode that can prove nativeness), and the shapes that
/// still decline gracefully — a chained second join, a query-filtered inner, a key-equality join with no
/// matching navigation, and any projection carrying a whole-entity leaf (deferred: there is no native shaper
/// for an entity-typed projection leaf anywhere in this provider).
/// </para>
/// </summary>
public class NativeJoinTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    [Fact]
    public void Recording_join_scope_does_not_change_driver_LINQ_fallback_MQL()
    {
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.DriverLinq,
            nameof(Recording_join_scope_does_not_change_driver_LINQ_fallback_MQL), out var spyLogger);

        // EF-392 Task 5 update: the query must be one NO Select arm confirms, or the invariant under test is
        // not the one being measured. `new { o, r }` has two whole-entity leaves, which
        // NativeJoinScopeProjectionBinder declines outright (Task 5b), so nothing registers the $lookup and
        // the driver-LINQ shape is decided purely by the join-scope RECORDING — which is what this test is
        // about. (The original `new { o.Name, r.Total }` body is now confirmed and registered by that binder,
        // at translation time and therefore in every MongoQueryMode, so its driver-LINQ document shape
        // legitimately flips to the flat one — exactly as a confirmed reference Include's already does. See
        // Confirmed_join_projection_uses_the_flat_lookup_shape_under_DriverLinq_too below.)
        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .AsEnumerable()
            .OrderBy(x => x.o.Name).ThenBy(x => x.r.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .OrderBy(x => x.o.Name).ThenBy(x => x.r.Total)
            .ToList();

        Assert.Equal(
            expected.Select(x => (x.o.Name, x.r.Total)),
            result.Select(x => (x.o.Name, x.r.Total)));

        // The driver-LINQ shape for a SINGLE, UNCONFIRMED join must stay the classic "_outer"/"_inner" shape —
        // NOT flip to the flat "_lookup_<Navigation>" shape that a premature AddLookup would force. This is
        // the guard against the exact regression this task's design doc flags: recording a MongoJoinScope is
        // pure metadata and must register nothing on its own.
        var message = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.DoesNotContain("_lookup_Order", message);
    }

    [Fact]
    public void Confirmed_join_projection_uses_the_flat_lookup_shape_under_DriverLinq_too()
    {
        // The other half of the pair above. Once a Select arm CONFIRMS the join, the $lookup is registered —
        // and that happens at TRANSLATION time, before MongoQueryMode is ever read, so the flat
        // "_lookup_<Navigation>" document shape applies in every mode including explicit DriverLinq. This is
        // not a regression and not mode-specific leakage: it is exactly how a confirmed reference Include
        // already behaves, MongoQueryExpression.UsesDriverJoinFields is COMPUTED from the registered lookups
        // so the pipeline and the shaper can never disagree, and the results are unchanged either way (the
        // oracle below is the proof).
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.DriverLinq,
            nameof(Confirmed_join_projection_uses_the_flat_lookup_shape_under_DriverLinq_too), out var spyLogger);

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        Assert.Equal(expected, result);

        var message = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.Contains("_lookup_Orders", message);
    }

    [Fact]
    public void Genuine_two_sided_join_returns_correct_results_via_fallback()
    {
        // EF-392 Task 5 update: under the default Native mode this shape now goes NATIVE rather than falling
        // back (Wrapped_scalar_only_join_projection_goes_native proves that separately, under NativeOnly).
        // The test is kept, and kept under Native, precisely because the value it adds is mode-independent:
        // whichever path Native picks, the answer must equal the in-memory oracle.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.Native,
            nameof(Genuine_two_sided_join_returns_correct_results_via_fallback));

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Whole_entity_leaf_projection_after_join_declines_cleanly_under_NativeOnly()
    {
        // EF-392 Task 5 update. The original framing here — "a genuine two-sided join can NEVER be
        // distinguished from an Include-generated one, so it always declines" (EF-439) — turned out to be
        // stronger than necessary. The two are indeed indistinguishable at bind time, but they no longer need
        // distinguishing for the shapes this slice covers: an Include-generated join and a user-written join
        // over the same navigation emit the SAME $lookup and produce the same rows, so confirming the join
        // from the Select side is correct for both. What still declines is a shape no Select arm can confirm —
        // here, a projection with a whole-entity leaf, which has no native shaper anywhere (Task 5b). See
        // Wrapped_scalar_only_join_projection_goes_native for the shape that now succeeds.
        //
        // The name was narrowed for exactly that reason (final-review finding 8): it used to claim "a genuine
        // two-sided join declines", which is no longer true in general — only this whole-entity-leaf shape does.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Whole_entity_leaf_projection_after_join_declines_cleanly_under_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r.Total })
                .AsEnumerable()
                .ToList());
    }

    [Fact]
    public void GroupJoin_array_result_shape_still_declines_cleanly_in_NativeOnly()
    {
        // Owned by EF-436, not this ticket — must stay declining, not silently "fixed" by this work.
        // Raw GroupJoin with collection result shape is unsupported outright, in every mode (Native/Fallback/DriverLinq).
        // This decline is NOT NativeOnly-specific: it fails the same way in all modes because raw GroupJoin's
        // collection-result shape isn't supported by the projection binder — when rs gets substituted with a
        // single-entity shaper in TranslateJoinCore, there's a type mismatch that MongoExpressionTranslator
        // can't handle in any routing path.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(GroupJoin_array_result_shape_still_declines_cleanly_in_NativeOnly));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            db.Owners
                .GroupJoin(db.Orders, o => o.Id, r => r.OwnerId, (o, rs) => new { o.Name, Orders = rs })
                .ToList());

        // GroupJoin's collection result shape can't be bound; this throws during projection binding in all modes
        Assert.Contains("could not be translated", ex.Message);
    }

    [Fact]
    public void Chained_second_join_still_declines_cleanly_in_NativeOnly()
    {
        var seed = SeedOwnersOrdersAndLines();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Chained_second_join_still_declines_cleanly_in_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Join(db.OrderLines, x => x.r.Id, l => l.OrderId, (x, l) => new { x.o.Name, l.Sku })
                .AsEnumerable()
                .ToList());

        // Final-review finding 4: the routing assertion above (and its unit-test twin,
        // NativeJoinScopeProjectionBinderTests.Declines_a_second_chained_join_rather_than_reusing_the_first_joins_scope)
        // is not enough on its own for the INTERMEDIATE-Select spelling below. There, the intermediate
        // `Select(x => x.o)` hits the bare-whole-entity-leaf confirm arm while Joins.Count is still 1 — so
        // AddLookup FIRES and MongoQueryExpression.UsesDriverJoinFields flips — and only THEN does the second
        // join push the overall query to Fallback. That is precisely the ordering that produced a real
        // wrong-DATA failure elsewhere on this plan (NorthwindJoinQueryMongoTest.GroupJoin_Where), so the
        // driver-LINQ fallback for this shape needs a RESULT check, not just a route check.
        using var dbNative = CreateContext(seed, MongoQueryMode.Native,
            nameof(Chained_second_join_still_declines_cleanly_in_NativeOnly) + "_fallback");

        var result = dbNative.Owners
            .Join(dbNative.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.o)
            .Join(dbNative.Orders, o => o.Id, r2 => r2.OwnerId, (o, r2) => new { o.Name, r2.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.o)
            .Join(seed.Orders, o => o.Id, r2 => r2.OwnerId, (o, r2) => new { o.Name, r2.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Take_or_Skip_after_a_confirmed_join_declines_cleanly_under_NativeOnly()
    {
        // FINAL-REVIEW CRITICAL 1. A Take/Skip composed onto a join was recorded into PipelineOps, which
        // MongoSelectLowerer emits BEFORE the $lookup + $unwind. Over a COLLECTION navigation that $unwind is
        // 1:N, so the paging applied to the un-joined OWNER rows, not to the joined result rows LINQ pages.
        //
        // The guard that catches it is the HasPaging conjunct in IsSingleEligibleNativeJoinScope, i.e. the join
        // is never CONFIRMED once a $skip/$limit is already recorded — not the post-confirmation
        // HasConfirmedJoinLookup signal. MEASURED: EF Core's nav-expansion defers a join's result selector as a
        // PENDING SELECTOR applied LAST, so the Take is translated BEFORE the confirming Select, and a
        // post-confirmation gate alone would never see it. Verified by mutation (2026-08-27): disabling that
        // conjunct makes the NativeOnly assertions below fail with "No exception was thrown" and the Native
        // half fail with 3 rows where LINQ specifies 2 (and 0 where LINQ specifies 1).
        //
        // The seed is what makes this a wrong-DATA pin: Alice has two Orders and Bob one, so the join has three
        // rows over two owner documents. `Take(2)` must return exactly TWO joined rows; the un-gated native
        // pipeline emitted {$limit: 2}, {$lookup}, {$unwind} — limiting to two OWNERS and then expanding them
        // into all THREE joined rows. Likewise `Skip(2)` must drop two joined rows and return one, where
        // skipping two owner documents leaves nothing at all.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Take_or_Skip_after_a_confirmed_join_declines_cleanly_under_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
                .Take(2)
                .ToList());

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
                .Skip(2)
                .ToList());

        using var dbNative = CreateContext(seed, MongoQueryMode.Native,
            nameof(Take_or_Skip_after_a_confirmed_join_declines_cleanly_under_NativeOnly) + "_fallback");

        // Differential oracle. Take/Skip over an unordered source has no defined row IDENTITY in LINQ, so what
        // is pinned is the row COUNT (which is fully defined — and is exactly the quantity the un-gated native
        // pipeline got wrong: 3 instead of 2, and 0 instead of 1) plus membership of every returned row in the
        // full joined set.
        var allJoined = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .ToList();
        Assert.Equal(3, allJoined.Count);

        var taken = dbNative.Owners
            .Join(dbNative.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .Take(2)
            .ToList();
        Assert.Equal(2, taken.Count);
        Assert.All(taken, row => Assert.Contains(row, allJoined));

        var skipped = dbNative.Owners
            .Join(dbNative.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .Skip(2)
            .ToList();
        Assert.Single(skipped);
        Assert.All(skipped, row => Assert.Contains(row, allJoined));
    }

    [Fact]
    public void First_after_a_confirmed_join_declines_cleanly_under_NativeOnly()
    {
        // FINAL-REVIEW CRITICAL 1, reducer half. NativeCardinalityBinder.TryBindReducer synthesizes its
        // $limit into PipelineOps, i.e. ahead of the $lookup/$unwind — so an un-gated First() reduced the
        // OUTER rows and only then joined.
        //
        // This is the ONE shape measured to reach the post-confirmation MongoSelectDefinition
        // .HasConfirmedJoinLookup signal (instrumented across the functional + both spec runs: two hits, both
        // from this test). Unlike Take/Skip, a reducer is NOT hoisted ahead of the join's pending result
        // selector, so it arrives after the confirming Select — which is exactly why the forward-ordering
        // conjuncts in IsSingleEligibleNativeJoinScope cannot cover it and TryBindReducer needs its own gate.
        //
        // The seed puts an owner with NO orders FIRST in the collection, which is what turns this from a
        // "maybe the wrong row" hazard into a hard, observable failure: {$limit: 1} keeps that owner, the
        // $unwind (preserveNullAndEmptyArrays: false, per the collection-nav arm) then drops it, and First()
        // returns nothing at all where LINQ answers with the OTHER owner's joined row.
        //
        // FUTURE EDITORS — the WHOLE-ENTITY bare-leaf spelling (`Select(x => x.r)`) is load-bearing and must
        // not be "simplified" to the scalar `new { o.Name, r.Total }` projection the Take/Skip test above uses.
        // MEASURED (2026-08-27): a reducer over a wrapped scalar projection declines earlier and for an
        // unrelated reason ("Query projects a non-entity result…"), so that spelling stays green with this
        // test's guard deleted — i.e. it would pin nothing. The whole-entity spelling reaches the bare-leaf
        // confirm arm with a reducer's $limit already recorded, which is the case actually under test.
        var seed = SeedOrderlessOwnerFirst();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(First_after_a_confirmed_join_declines_cleanly_under_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Select(x => x.r)
                .First());

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Select(x => x.r)
                .FirstOrDefault());

        // NO Native-mode correctness half here, and that omission is deliberate + measured, not an oversight.
        // `Join(...).Select(x => x.r).FirstOrDefault()` returns NULL — where LINQ's answer is the one joined
        // Order — on the DRIVER-LINQ path as well, in explicit DriverLinq mode, and with this guard both
        // enabled AND disabled (so also at the pre-fix-wave branch tip 706154b, and, since the guard prevents
        // the $lookup from ever being registered for this shape, on a pipeline identical to the pre-EF-392
        // one). That is a PRE-EXISTING fallback defect in the reducer-after-a-whole-entity-join-projection
        // shape, entirely independent of this fix wave, so there is no correct result for this test to assert
        // under Native. What this test pins is the thing this fix wave OWNS: the shape declines cleanly instead
        // of silently going native (before the guard, NativeOnly returned wrong data — null, or a
        // "Sequence contains more than one element" throw depending on the seed — rather than throwing
        // NativeTranslationNotSupportedException). Verified by mutation (2026-08-27): disabling
        // TryBindReducer's HasConfirmedJoinLookup gate makes both assertions above fail with "No exception was
        // thrown", and the query then returns null / throws "Sequence contains more than one element" natively
        // depending on the seed.
    }

    [Fact]
    public void Where_after_a_bare_whole_entity_leaf_select_declines_cleanly_under_NativeOnly()
    {
        // FINAL-REVIEW CRITICAL 2, and the mechanism below is NOT the one the review hypothesised — this
        // comment records what was actually measured, because the difference decides which guard may be
        // deleted later.
        //
        // The hypothesis was: `Select(x => x.r)` is confirmed by the bare whole-entity-leaf arm and the shaper
        // then yields ORDER entities, while MongoQueryExpression.CollectionExpression.EntityType is still OWNER
        // (nothing updates it) — and that is what NativeSlotPopulator builds its single-scope
        // MongoExpressionTranslator from. Since MongoExpressionTranslator.Members resolves a member off a
        // ParameterExpression by NAME only, "Id" would resolve against OWNER and emit a $match on the ROOT
        // document's _id: OWNERS filtered by an ORDER id, before the $lookup.
        //
        // MEASURED (2026-08-27, via the real preprocessor + QMTEV): that ordering never occurs. EF Core's
        // nav-expansion HOISTS the trailing Where ahead of the join's pending result selector, so this query
        // preprocesses to `Where(ti => ti.Inner.Id == k).Select(ti => ti.Inner)` — the predicate is still
        // expressed against the transparent identifier's INNER side, which NativeSlotPopulator's Where arm
        // declines outright (NativeJoinScopeTranslator.ReferencesInnerScope: a $match lowers before the
        // $lookup). That decline marks the query non-native, which in turn blocks confirmation through
        // IsSingleEligibleNativeJoinScope's HasUnsupportedOperator conjunct. Both guards are pre-existing; the
        // post-confirmation HasConfirmedJoinLookup signal added by this fix wave is NOT what saves this shape
        // (it is defence-in-depth for the reversed ordering — see MongoSelectDefinition.HasConfirmedJoinLookup).
        //
        // The test is kept because the SHAPE is what matters: `Id`/`_id` is a near-universal property name, and
        // this pins that the join instance of the "single-scope resolution is by name only" hazard declines and
        // returns correct rows rather than silently filtering the wrong collection. The general, join-
        // independent form of that hazard is pre-existing and deliberately out of scope here.
        var seed = SeedOwnersAndOrders();
        var targetOrderId = seed.Orders[2].Id;

        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Where_after_a_bare_whole_entity_leaf_select_declines_cleanly_under_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Select(x => x.r)
                .Where(r => r.Id == targetOrderId)
                .ToList());

        using var dbNative = CreateContext(seed, MongoQueryMode.Native,
            nameof(Where_after_a_bare_whole_entity_leaf_select_declines_cleanly_under_NativeOnly) + "_fallback");

        var result = dbNative.Owners
            .Join(dbNative.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.r)
            .Where(r => r.Id == targetOrderId)
            .ToList();

        // Exactly one Order carries that id, and it belongs to a matched Owner — so the correct answer is that
        // one Order. Matching the same id against OWNER._id (the mis-resolution above) matches no owner at all
        // and returns nothing.
        Assert.Equal([targetOrderId], result.Select(r => r.Id));
        Assert.Equal(30m, Assert.Single(result).Total);
    }

    [Fact]
    public void Where_on_the_outer_side_then_a_wrapped_scalar_projection_goes_native()
    {
        // FINAL-REVIEW IMPORTANT 3: the full composed pipeline through the wrapped-scalar-projection arm —
        // Task 4's Where-over-the-OUTER-side capability feeding Task 5's wrapped scalar-only Select. Every
        // other positive test for that binder projects straight off the join with no Where in between, so the
        // two capabilities had never been exercised together (the Where records a $match conjunct BEFORE the
        // Select confirms and registers the $lookup, and the shared gate's !HasUnsupportedOperator conjunct
        // means a Where that failed to translate would silently prevent confirmation).
        //
        // NativeOnly + an in-memory differential oracle: succeeding here is the "went native" proof, and the
        // oracle is what makes it a correctness check rather than a "doesn't throw" check.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Where_on_the_outer_side_then_a_wrapped_scalar_projection_goes_native));

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Where(x => x.o.Name == "Alice")
            .Select(x => new { x.o.Name, x.r.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Where(x => x.o.Name == "Alice")
            .Select(x => new { x.o.Name, x.r.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        Assert.Equal(expected, result);
        Assert.Equal(2, result.Count);
    }

#if !EF8 && !EF9
    [Fact]
    public void LeftJoin_Where_on_the_outer_side_then_a_wrapped_scalar_projection_goes_native()
    {
        // The LeftJoin spelling of the composition above (final-review finding 3's second half): MongoJoinScope
        // .IsLeftOuter is live for this shape and was untested in combination with a preceding Where.
        //
        // Driven from the DEPENDENT side (Orders → Owners) deliberately: that resolves Order.Owner, a REFERENCE
        // navigation, which is the arm that threads requiredness through properly. The principal-side spelling
        // resolves a COLLECTION navigation and is declined outright by the left-outer conjunct — see
        // LeftJoin_over_a_collection_navigation_still_declines_under_NativeOnly.
        //
        // The seed has no dangling FK, so every Order has an Owner and `x.o.Name` is never a null dereference;
        // left-outer ROW PRESERVATION is pinned by that other test, not this one — what this pins is the
        // Where-then-wrapped-Select composition staying native for the left-outer scope.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(LeftJoin_Where_on_the_outer_side_then_a_wrapped_scalar_projection_goes_native));

        var result = db.Orders
            .LeftJoin(db.Owners, r => r.OwnerId, o => o.Id, (r, o) => new { r, o })
            .Where(x => x.r.Total > 10m)
            .Select(x => new { x.o.Name, x.r.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Orders
            .LeftJoin(seed.Owners, r => r.OwnerId, o => o.Id, (r, o) => new { r, o })
            .Where(x => x.r.Total > 10m)
            .Select(x => new { x.o.Name, x.r.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        Assert.Equal(expected, result);
        Assert.Equal(2, result.Count);
    }
#endif

    [Fact]
    public void Query_filtered_join_target_still_declines_cleanly_in_NativeOnly()
    {
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Query_filtered_join_target_still_declines_cleanly_in_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders.Where(r => r.Total > 0), o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
                .AsEnumerable()
                .ToList());
    }

    [Fact]
    public void Navigation_less_key_equality_join_still_declines_cleanly_in_NativeOnly()
    {
        // FUTURE EDITORS — the fixture's Owner.Orders / Order.Owner navigations are what make this test mean
        // something; do NOT "simplify" them away. (An earlier version of this comment claimed there was "no
        // corresponding model navigation between the two entity types", which was simply inaccurate for this
        // fixture and would have invited exactly that simplification.)
        //
        // What is actually pinned: a Join on Region/Region, i.e. NON-key properties, between two types that DO
        // have a navigation. RebindInnerShaperToOuterQuery's navigation resolution ends in a loose
        // `FirstOrDefault(n => n.TargetEntityType == innerEntityType)` fallback, so it happily resolves
        // Owner.Orders here and builds a $lookup joining on _id/OwnerId — a completely different join condition
        // from the one written. That is harmless while the lookup is never emitted, but the moment a Select arm
        // confirms the join it would emit natively and return silently wrong rows. TranslateJoinCore's
        // JoinLookupImplementsKeySelectors conjunct is what declines it, by requiring the emitted $lookup's
        // localField/foreignField to be exactly the element names of the join's own key properties. Remove the
        // navigations from the fixture and this test still passes — for the wrong reason (no navigation, so no
        // JoinScope at all), silently defanging that pin.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Navigation_less_key_equality_join_still_declines_cleanly_in_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Region, r => r.Region, (o, r) => new { o.Name, r.Total })
                .AsEnumerable()
                .ToList());
    }

    [Fact]
    public void Where_after_join_still_declines_under_NativeOnly_pending_the_Select_side_binder()
    {
        // NOTE (review-round honesty fix): this test's name and the exception it asserts CANNOT distinguish
        // "the Where arm translated x.o.Name natively and the trailing Select(x => x.r.Total) declined" from
        // "the Where arm itself declined and nothing downstream ever got a chance" — both raise the exact
        // same NativeTranslationNotSupportedException with the exact same message under NativeOnly, because
        // EF Core's own pipeline always appends a final Select (explicit here, but even an implicit identity
        // one for a bare `.Where(...).ToList()` would do the same) and the Select-side join-scope binder
        // doesn't exist yet (Task 5). So this test currently only pins "still declines gracefully, doesn't
        // crash, doesn't silently return wrong data" — it does NOT prove the Where arm works.
        //
        // EF-392 Task 5 update: the Select-side binder now exists, and this shape STILL declines — correctly.
        // The trailing `Select(x => x.r.Total)` is a BARE (unwrapped) scalar leaf, which is neither of Task 5's
        // two shapes: not a bare whole-ENTITY leaf, and not a `new {...}` wrapper. A bare scalar leaf needs the
        // reserved-alias machinery NativeProjectionBinder carries for its own bare bodies
        // (ProjectionAliasTier/`_v`), which has no join-scope analogue yet. So this test keeps its original
        // meaning unchanged; the caveat below about what it can and cannot distinguish still applies.
        //
        // The actual proof that NativeSlotPopulator's Where arm resolves a join-scope predicate against a
        // REAL EF-generated TransparentIdentifier lives in a fast, deterministic unit test instead:
        // JoinScopeWhereSlotPopulationTests.Where_reading_outer_scope_after_join_populates_predicate_natively
        // (tests/.../UnitTests/Query/NativeTranslation/), which asserts directly on the populated
        // MongoSelectDefinition.PipelineOps rather than relying on an end-to-end result that Task 5 hasn't
        // made reachable yet. Once Task 5 lands the Select-side binder, THIS test should be revisited: either
        // extended to assert NativeOnly success (proving the whole chain), or left as the "still declines for
        // shapes Task 5 doesn't cover" pin — whichever is accurate at that point.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Where_after_join_still_declines_under_NativeOnly_pending_the_Select_side_binder));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
                .Where(x => x.o.Name == seed.Owners[0].Name)
                .Select(x => x.r.Total)
                .ToList());

        // Correctness is still proven under the default Native mode (graceful fallback), exactly like the
        // other genuine-join tests in this file.
        using var dbNative = CreateContext(seed, MongoQueryMode.Native,
            nameof(Where_after_join_still_declines_under_NativeOnly_pending_the_Select_side_binder) + "_fallback");

        var result = dbNative.Owners
            .Join(dbNative.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Where(x => x.o.Name == seed.Owners[0].Name)
            .Select(x => x.r.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Where(x => x.o.Name == seed.Owners[0].Name)
            .Select(x => x.r.Total)
            .ToList();

        Assert.Equal(expected.OrderBy(x => x), result.OrderBy(x => x));
    }

    [Fact]
    public void Bare_whole_inner_entity_select_after_join_goes_native()
    {
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Bare_whole_inner_entity_select_after_join_goes_native));

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.r)
            .AsEnumerable()
            .OrderBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.r)
            .OrderBy(x => x.Total)
            .ToList();

        Assert.Equal(expected.Select(x => x.Id), result.Select(x => x.Id));
    }

    [Fact]
    public void Bare_whole_outer_entity_select_after_join_goes_native()
    {
        // The whole-OUTER spelling of the same shape. Inner-join semantics still apply — an Owner with no
        // Order drops out, and an Owner with two Orders appears twice — so this is not the same query as a
        // bare `db.Owners.ToList()`; the oracle below encodes that.
        var seed = SeedOwnersAndOrdersWithUnmatchedRows();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Bare_whole_outer_entity_select_after_join_goes_native));

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.o)
            .AsEnumerable()
            .OrderBy(x => x.Name)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o, r })
            .Select(x => x.o)
            .OrderBy(x => x.Name)
            .ToList();

        Assert.Equal(expected.Select(x => x.Id), result.Select(x => x.Id));
    }

    [Fact]
    public void Wrapped_scalar_only_join_projection_goes_native()
    {
        // EF-392 Task 6: this is the flattened-scalar-join smoke test the Task 6 brief describes
        // (Flattened_scalar_join_goes_native_with_correct_results_and_mql) — it already existed from Task 5,
        // so rather than duplicate it, it's extended here with the MQL assertion Task 6 needs: proof that
        // AppendLookupStages emits both $lookup and $unwind for this confirmed join, with no lowerer change
        // required (outcome A from the brief — the pre-existing IsReference && !HasPipeline branch, built via
        // the same LookupExpression(navigation, forceUnwind: true) construction the reference-Include path
        // uses, already handles it).
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Wrapped_scalar_only_join_projection_goes_native), out var spyLogger);

        var result = db.Owners
            .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .AsEnumerable()
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        var expected = seed.Owners
            .Join(seed.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })
            .OrderBy(x => x.Name).ThenBy(x => x.Total)
            .ToList();

        // Succeeding under NativeOnly is itself the "went native" signal (Query/AGENTS.md).
        Assert.Equal(expected, result);

        var message = spyLogger.GetLogMessageByEventId(MongoEventId.ExecutedMqlQuery);
        Assert.Contains("\"$lookup\"", message);
        Assert.Contains("\"$unwind\"", message);
    }

    [Fact]
    public void Wrapped_projection_with_a_whole_entity_leaf_still_declines_under_NativeOnly()
    {
        // Task 5b's deferred territory: a whole-entity projection leaf has no native shaper anywhere in this
        // codebase. The binder must decline the WHOLE projection rather than half-support it.
        //
        // The INNER-side spelling, deliberately — Genuine_two_sided_join_declines_cleanly_under_NativeOnly
        // above covers the OUTER-side one (`new { o, r.Total }`). The check
        // (IsTransparentIdentifierOuterOrInnerAccess) is position-agnostic, but the two leaves would be shaped
        // by different machinery if either were ever admitted (the outer entity reads off the root document,
        // the inner one out of the $lookup's unwound alias), so both positions are worth pinning rather than
        // asserting the same shape twice.
        var seed = SeedOwnersAndOrders();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Wrapped_projection_with_a_whole_entity_leaf_still_declines_under_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .Join(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r })
                .AsEnumerable()
                .ToList());
    }

#if !EF8 && !EF9
    [Fact]
    public void LeftJoin_over_a_collection_navigation_still_declines_under_NativeOnly()
    {
        // Pins the left-outer conjunct of IsSingleEligibleNativeJoinScope. Driving the join from the PRINCIPAL
        // side (Owners → Orders) resolves Owner.Orders, a COLLECTION navigation, and MongoSelectLowerer's
        // collection-ForceUnwind arm hard-codes preserveNullAndEmptyArrays: false — correct for an inner Join,
        // silently wrong for a LeftJoin (an Owner with no Order is DROPPED instead of kept with nulls).
        //
        // FUTURE EDITORS — the projection body here is load-bearing and must stay ALL-OUTER and fully
        // translatable. An earlier version of this test used `Total = r == null ? (decimal?)null : r.Total`,
        // whose ConditionalExpression MongoExpressionTranslator has no support for at all — so
        // TryBindProjection declined on the LEAF, for a reason entirely unrelated to left-outer-ness, and the
        // conjunct under test was never reached. That version stayed green with the conjunct deleted, i.e. it
        // pinned nothing (the same dead-guard failure mode this feature has hit before — see
        // NativeJoinScopeTranslator's field-vs-property note). `new { o.Name }` translates cleanly and
        // therefore reaches, and is stopped by, the left-outer conjunct itself.
        //
        // Verified by mutation (2026-08-27), disabling ONLY that conjunct: the NativeOnly assertion below fails
        // with "No exception was thrown", and the Native assertion fails with ["Alice"] instead of
        // ["Alice", "Bob"] — i.e. the mutation reproduces the hazard live, it is not merely theorized.
        var seed = SeedOwnersAndOrdersWithUnmatchedRows();
        using var db = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(LeftJoin_over_a_collection_navigation_still_declines_under_NativeOnly));

        Assert.Throws<NativeTranslationNotSupportedException>(() =>
            db.Owners
                .LeftJoin(db.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name })
                .AsEnumerable()
                .ToList());

        // The second half is what makes this a WRONG-DATA pin rather than a routing pin: the seed has an Owner
        // with no Order at all, so a correct LeftJoin returns BOTH owners. Were the conjunct removed, this
        // would go native, the collection-ForceUnwind arm would emit preserveNullAndEmptyArrays: false, and
        // the Order-less owner would silently vanish — one row instead of two.
        using var dbNative = CreateContext(seed, MongoQueryMode.Native,
            nameof(LeftJoin_over_a_collection_navigation_still_declines_under_NativeOnly) + "_fallback");

        var result = dbNative.Owners
            .LeftJoin(dbNative.Orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name })
            .AsEnumerable()
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToList();

        // Spelled out rather than computed from an in-memory LINQ oracle: SeedOwnersAndOrdersWithUnmatchedRows
        // gives Alice exactly one Order and Bob none, so a correct LeftJoin is exactly one row each. Writing
        // it literally keeps the "Bob must survive" point visible instead of hiding it behind a mirror query.
        Assert.Equal(["Alice", "Bob"], result);
    }
#endif

    public static IEnumerable<object[]> JoinOracleCases()
    {
        // Each case is a Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<...>> so the SAME expression
        // tree runs against both the native-backed DbSet and the in-memory seed collection, following the
        // NativeOwnedCollectionAllTests.cs oracle pattern exactly. The trailing bool is `goesNativeUnderNativeOnly`:
        // whether THIS case is expected to succeed under MongoQueryMode.NativeOnly (proving it actually goes
        // native, per Task 5/6) or is expected to still decline there (falling back correctly under the
        // default Native mode only). See the theory body below for how each half is asserted.
        yield return
        [
            (Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<object>>)((owners, orders) =>
                owners.Join(orders, o => o.Id, r => r.OwnerId, (o, r) => new { o.Name, r.Total })),
            true
        ];
#if !EF8 && !EF9
        // Queryable.LeftJoin itself only dispatches on EF10 (EF-X020, per RequiredNavigationUnwindTests.cs) —
        // on EF8/EF9, EF Core's OWN NavigationExpandingExpressionVisitor throws "could not be translated"
        // for ANY LeftJoin call, before the expression ever reaches this (or any) provider. That is an EF
        // Core-version limitation, not a Mongo provider gap, so this case is compiled out rather than run
        // and expected to fail on EF8/EF9.
        //
        // This case's projection leaf is `Total = r == null ? (decimal?)null : r.Total` — a ConditionalExpression,
        // which MongoExpressionTranslator has no support for at all (confirmed during Task 5's review). So this
        // case still declines under NativeOnly, correctly, and `goesNativeUnderNativeOnly` is false: it is
        // verified via the graceful-decline + Native-mode-fallback-correctness path, not native success.
        yield return
        [
            (Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<object>>)((owners, orders) =>
                owners.LeftJoin(orders, o => o.Id, r => r.OwnerId,
                    (o, r) => new { o.Name, Total = r == null ? (decimal?)null : r.Total })),
            false
        ];
#endif
    }

    [Theory]
    [MemberData(nameof(JoinOracleCases))]
    public void Join_result_matches_in_memory_oracle_including_unmatched_rows(
        Func<IQueryable<Owner>, IQueryable<Order>, IQueryable<object>> query, bool goesNativeUnderNativeOnly)
    {
        // Seed must include: an Owner with a matching Order, an Owner with NO Order (left-join/unmatched
        // case), and an Order whose OwnerId matches nothing (dangling FK, dropped by inner Join).
        //
        // EF-392 Task 6 update: the stale premise here — "Do NOT use MongoQueryMode.NativeOnly, it would
        // always throw for this shape, by design" — no longer holds for every case in JoinOracleCases. After
        // Task 5, the plain Join case DOES go native under NativeOnly; only the LeftJoin case (a conditional
        // projection leaf MongoExpressionTranslator can't translate at all) still declines. So this test now
        // asserts the oracle match under the default Native mode UNCONDITIONALLY (proving whichever path
        // Native picks - fallback or native - is correct), and ADDITIONALLY, under NativeOnly, either the
        // oracle match again (when goesNativeUnderNativeOnly, proving native itself, since NativeOnly can only
        // succeed by translating natively - there is no silent fallback in that mode) or the expected clean
        // decline (when not).
        var seed = SeedOwnersAndOrdersWithUnmatchedRows();

        // dynamic is used below (rather than a typed lambda) because this method's result shape is erased to
        // IQueryable<object> by JoinOracleCases' shared delegate signature.
        var oracleResult = query(seed.Owners.AsQueryable(), seed.Orders.AsQueryable()).AsEnumerable()
            .OrderBy(x => ((dynamic)x).Name).ThenBy(x => ((dynamic)x).Total)
            .ToList();

        using var dbNative = CreateContext(seed, MongoQueryMode.Native,
            nameof(Join_result_matches_in_memory_oracle_including_unmatched_rows) + "_native");

        // Ordered by a stable key before comparing — an unordered List<object> equality check would only
        // pass by coincidence of seed/pipeline ordering. Mirrors the OrderBy(x => x.Name).ThenBy(x => x.Total)
        // pattern used on both sides three methods up (Genuine_two_sided_join_returns_correct_results_via_fallback).
        var nativeModeResult = query(dbNative.Owners, dbNative.Orders).AsEnumerable()
            .OrderBy(x => ((dynamic)x).Name).ThenBy(x => ((dynamic)x).Total)
            .ToList();
        Assert.Equal(oracleResult, nativeModeResult);

        using var dbNativeOnly = CreateContext(seed, MongoQueryMode.NativeOnly,
            nameof(Join_result_matches_in_memory_oracle_including_unmatched_rows) + "_nativeOnly");

        if (goesNativeUnderNativeOnly)
        {
            // NativeOnly never falls back silently — it either translates natively or throws. So a passing
            // result here is itself the proof this case reaches and completes native translation, not some
            // other decline path (see Task 6 brief's rigor requirement re: guard/test bugs earlier in this
            // ticket family).
            var nativeOnlyResult = query(dbNativeOnly.Owners, dbNativeOnly.Orders).AsEnumerable()
                .OrderBy(x => ((dynamic)x).Name).ThenBy(x => ((dynamic)x).Total)
                .ToList();
            Assert.Equal(oracleResult, nativeOnlyResult);
        }
        else
        {
            Assert.Throws<NativeTranslationNotSupportedException>(() =>
                query(dbNativeOnly.Owners, dbNativeOnly.Orders).AsEnumerable().ToList());
        }
    }

    private sealed record Seed(Owner[] Owners, Order[] Orders, OrderLine[] OrderLines = default!);

    private static Seed SeedOwnersAndOrders()
    {
        var ownerA = new Owner { Id = ObjectId.GenerateNewId(), Name = "Alice", Region = "North" };
        var ownerB = new Owner { Id = ObjectId.GenerateNewId(), Name = "Bob", Region = "South" };
        var owners = new[] { ownerA, ownerB };

        var orders = new[]
        {
            new Order { Id = ObjectId.GenerateNewId(), OwnerId = ownerA.Id, Total = 10m, Region = "North" },
            new Order { Id = ObjectId.GenerateNewId(), OwnerId = ownerA.Id, Total = 20m, Region = "North" },
            new Order { Id = ObjectId.GenerateNewId(), OwnerId = ownerB.Id, Total = 30m, Region = "South" },
        };

        return new Seed(owners, orders, []);
    }

    // Covers, in one fixture, the three row shapes a Join/LeftJoin differential must exercise: an Owner
    // with a matching Order (both cases include it), an Owner with NO Order (unmatched — dropped by inner
    // Join, kept with a null Total by LeftJoin), and an Order whose OwnerId matches no seeded Owner (a
    // dangling FK — dropped by inner Join and, since LeftJoin here is driven from the Owner side, also
    // absent from the LeftJoin result).
    private static Seed SeedOwnersAndOrdersWithUnmatchedRows()
    {
        var ownerWithOrder = new Owner { Id = ObjectId.GenerateNewId(), Name = "Alice", Region = "North" };
        var ownerWithoutOrder = new Owner { Id = ObjectId.GenerateNewId(), Name = "Bob", Region = "South" };
        var owners = new[] { ownerWithOrder, ownerWithoutOrder };

        var orders = new[]
        {
            new Order { Id = ObjectId.GenerateNewId(), OwnerId = ownerWithOrder.Id, Total = 10m, Region = "North" },
            new Order { Id = ObjectId.GenerateNewId(), OwnerId = ObjectId.GenerateNewId(), Total = 99m, Region = "East" },
        };

        return new Seed(owners, orders, []);
    }

    // An Owner with NO Orders inserted FIRST, followed by an Owner with exactly one — the ordering is
    // load-bearing for First_after_a_confirmed_join_declines_cleanly_under_NativeOnly: it makes an un-gated
    // reducer's pre-$lookup {$limit: 1} keep the order-less owner, which the 1:N $unwind then drops entirely,
    // turning a wrong-row hazard into an observable empty result.
    private static Seed SeedOrderlessOwnerFirst()
    {
        var orderless = new Owner { Id = ObjectId.GenerateNewId(), Name = "Aaron", Region = "North" };
        var withOrder = new Owner { Id = ObjectId.GenerateNewId(), Name = "Zoe", Region = "South" };

        return new Seed(
            [orderless, withOrder],
            [new Order { Id = ObjectId.GenerateNewId(), OwnerId = withOrder.Id, Total = 5m, Region = "South" }],
            []);
    }

    private static Seed SeedOwnersOrdersAndLines()
    {
        var ownerA = new Owner { Id = ObjectId.GenerateNewId(), Name = "Alice", Region = "North" };
        var ownerB = new Owner { Id = ObjectId.GenerateNewId(), Name = "Bob", Region = "South" };
        var owners = new[] { ownerA, ownerB };

        var order1 = new Order { Id = ObjectId.GenerateNewId(), OwnerId = ownerA.Id, Total = 10m, Region = "North" };
        var order2 = new Order { Id = ObjectId.GenerateNewId(), OwnerId = ownerA.Id, Total = 20m, Region = "North" };
        var order3 = new Order { Id = ObjectId.GenerateNewId(), OwnerId = ownerB.Id, Total = 30m, Region = "South" };
        var orders = new[] { order1, order2, order3 };

        var lines = new[]
        {
            new OrderLine { Id = ObjectId.GenerateNewId(), OrderId = order1.Id, Sku = "SKU-1" },
            new OrderLine { Id = ObjectId.GenerateNewId(), OrderId = order1.Id, Sku = "SKU-2" },
            new OrderLine { Id = ObjectId.GenerateNewId(), OrderId = order2.Id, Sku = "SKU-3" },
            new OrderLine { Id = ObjectId.GenerateNewId(), OrderId = order3.Id, Sku = "SKU-4" },
        };

        return new Seed(owners, orders, lines);
    }

    public class Owner
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = "";
        public string Region { get; set; } = "";
        public List<Order> Orders { get; set; } = [];
    }

    public class Order
    {
        public ObjectId Id { get; set; }
        public ObjectId OwnerId { get; set; }
        public Owner? Owner { get; set; }
        public decimal Total { get; set; }
        public string Region { get; set; } = "";
        public List<OrderLine> OrderLines { get; set; } = [];
    }

    public class OrderLine
    {
        public ObjectId Id { get; set; }
        public ObjectId OrderId { get; set; }
        public Order? Order { get; set; }
        public string Sku { get; set; } = "";
    }

    private JoinTestDbContext CreateContext(Seed seed, MongoQueryMode mode, string name)
        => CreateContext(seed, mode, name, loggerFactory: null);

    private JoinTestDbContext CreateContext(
        Seed seed, MongoQueryMode mode, string name, out SpyLoggerProvider spyLogger)
    {
        var (loggerFactory, provider) = SpyLoggerProvider.Create();
        spyLogger = provider;
        return CreateContext(seed, mode, name, loggerFactory);
    }

    private JoinTestDbContext CreateContext(Seed seed, MongoQueryMode mode, string name, ILoggerFactory? loggerFactory)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ownersName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "Owners" + suffix;
        var ordersName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "Orders" + suffix;
        var linesName = TemporaryDatabaseFixtureBase.CreateCollectionName(name) + "OrderLines" + suffix;

        database.MongoDatabase.GetCollection<Owner>(ownersName).InsertMany(seed.Owners);
        database.MongoDatabase.GetCollection<Order>(ordersName).InsertMany(seed.Orders);
        if (seed.OrderLines.Length > 0)
            database.MongoDatabase.GetCollection<OrderLine>(linesName).InsertMany(seed.OrderLines);

        return new JoinTestDbContext(database, ownersName, ordersName, linesName, mode, loggerFactory);
    }

    private sealed class JoinTestDbContext : DbContext
    {
        private readonly string _ownersCollection;
        private readonly string _ordersCollection;
        private readonly string _linesCollection;

        public DbSet<Owner> Owners { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderLine> OrderLines { get; set; } = null!;

        public JoinTestDbContext(
            TemporaryDatabaseFixture database, string ownersCollection, string ordersCollection, string linesCollection,
            MongoQueryMode mode, ILoggerFactory? loggerFactory = null)
            : base(BuildOptions(database, mode, loggerFactory))
        {
            _ownersCollection = ownersCollection;
            _ordersCollection = ordersCollection;
            _linesCollection = linesCollection;
        }

        private static DbContextOptions BuildOptions(
            TemporaryDatabaseFixture database, MongoQueryMode mode, ILoggerFactory? loggerFactory)
        {
            var optionsBuilder = new DbContextOptionsBuilder<JoinTestDbContext>()
                .UseMongoDB(database.Client, database.MongoDatabase.DatabaseNamespace.DatabaseName)
                .ReplaceService<IModelCacheKeyFactory, IgnoreCacheKeyFactory>()
                .ConfigureWarnings(x => x.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(optionsBuilder).UseQueryMode(mode);
            if (loggerFactory != null)
            {
                optionsBuilder = optionsBuilder.UseLoggerFactory(loggerFactory).EnableSensitiveDataLogging();
            }

            return optionsBuilder.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Owner>(b =>
            {
                b.ToCollection(_ownersCollection);
                b.HasMany(o => o.Orders).WithOne(r => r.Owner).HasForeignKey(r => r.OwnerId);
            });
            modelBuilder.Entity<Order>(b =>
            {
                b.ToCollection(_ordersCollection);
                b.HasMany(o => o.OrderLines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId);
            });
            modelBuilder.Entity<OrderLine>(b => b.ToCollection(_linesCollection));
        }

        private sealed class IgnoreCacheKeyFactory : IModelCacheKeyFactory
        {
            private static int _count;
            public object Create(DbContext context, bool designTime) => Interlocked.Increment(ref _count);
        }
    }
}
