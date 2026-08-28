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
using System.Linq.Expressions;
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.Query.NativeTranslation;

/// <summary>
/// Attempts to populate the native <c>$project</c> slot for a wrapped <c>new {...}</c>/<c>MemberInit</c>
/// <c>Select</c> composed immediately after an eligible single-level <c>Join</c>/<c>LeftJoin</c>
/// (<see cref="MongoSelectDefinition.JoinScope"/>), where EVERY leaf is a scalar value
/// <see cref="NativeJoinScopeTranslator"/> can translate — <c>x.Outer.Foo</c>, <c>x.Inner.Foo</c>, or a mixed
/// computed expression combining both.
/// </summary>
/// <remarks>
/// <para>
/// Declines the WHOLE projection — no partial commit — if any leaf is a whole-entity reference
/// (<c>x.Outer</c>/<c>x.Inner</c> verbatim). That shape has no native shaper support anywhere in this codebase
/// today (not for joins, not for any other native projection: <c>NativeProjectionBinder</c> has no
/// whole-entity-leaf branch at all, and the existing mechanism for "a projection contains entity references",
/// <c>MongoMixedProjectionBindingRemovingExpressionVisitor</c>, is a client-side-over-whole-documents shaper
/// that <c>NativeOnly</c> forbids). It is deliberately left on the driver-LINQ fallback rather than
/// half-supported here — see the plan's Task 5b.
/// </para>
/// <para>
/// On success: stages every leaf, then commits in one block — <c>Select.Projection</c> entries, the join's
/// already-built <see cref="JoinInfo.Lookup"/> (<c>AddLookup</c>), and
/// <see cref="MongoSelectDefinition.MarkReferenceIncludeConfirmed"/>. Nothing is mutated on any decline path,
/// so a rejected leaf can never leave a half-registered <c>$lookup</c> or a stray projection entry behind.
/// </para>
/// <para>
/// <b>Alias space.</b> Each leaf is emitted under its own member name, which is exactly the
/// <c>ProjectionMember</c> the shaper side derives for the same leaf
/// (<c>MongoProjectionBindingExpressionVisitor.VisitNew</c> pushes <c>newExpression.Members[i]</c>), so the
/// emit side and the read side agree by construction — the same rule
/// <c>NativeProjectionBinder</c>'s wrapped-leaf path follows. Aliases are deduped case-INSENSITIVELY, because
/// <c>MongoQueryExpression.AddToProjection</c> disambiguates them that way: two members differing only by case
/// would have the shaper read a disambiguated alias the <c>$project</c> never emitted.
/// </para>
/// <para>
/// <b>Non-default serialization needs no guard here.</b>
/// <see cref="NativeJoinScopeTranslator.TryTranslateValue"/> routes through
/// <c>MongoExpressionTranslator.TryTranslateValue</c>, which already applies
/// <c>AllFieldsDefaultSerialized</c> to the whole translated subtree — a value-converted or
/// non-default-<c>BsonRepresentation</c> field declines there, before this binder sees it.
/// </para>
/// <para>
/// <b>On <see cref="NativeJoinScopeTranslator"/>'s documented RESIDUAL GAP.</b> That comment warns that the
/// first caller to translate the Inner side WITHOUT the <c>Where</c> arm's blanket
/// <c>ReferencesInnerScope</c> block — i.e. this binder — must either add a per-join identity check or
/// re-validate the scope against the actual join being bound. It is closed here structurally, by this binder's
/// call-site gate (<c>MongoQueryableMethodTranslatingExpressionVisitor.IsSingleEligibleNativeJoinScope</c>)
/// requiring <c>Joins.Count == 1</c>: a <c>JoinScope</c> is recorded only for the FIRST join on a select, and
/// every later join appends to that same <c>Joins</c> list on the same <c>MongoQueryExpression</c>, so "exactly
/// one join has ever been recorded here" and "the recorded scope describes that join" are one fact, making
/// <c>scope.InnerPrefix == joinInfo.Alias</c> hold by construction. The gap's own worked example —
/// <c>Join(a, b, …).Select(x =&gt; x.Outer).Join(c, d, …)</c> — reaches the trailing <c>Select</c> with
/// <c>Joins.Count == 2</c> and is declined by that gate. See the gate's own remarks for the full derivation;
/// it is stated in one place so the two arms cannot drift.
/// </para>
/// </remarks>
internal static class NativeJoinScopeProjectionBinder
{
    internal static bool TryBindProjection(
        MongoQueryExpression mongoQ, LambdaExpression selector, JoinInfo joinInfo)
    {
        if (mongoQ.Select.JoinScope is not { } scope
            || joinInfo.Lookup is not { } lookup
            || mongoQ.Select.Projection.Count > 0
            || selector.Parameters.Count != 1)
        {
            return false;
        }

        var rootParam = selector.Parameters[0];

        if (!TryReadMembers(selector.Body, out var members))
        {
            return false;
        }

        var staged = new List<MongoProjection>();

        // Seeded with the aliases ALREADY on the query expression, not just the ones staged here. A join query
        // never reaches this binder with an empty MongoQueryExpression.Projection: RebindInnerShaperToOuterQuery
        // registered the inner entity's own EntityProjectionExpression at join time, under an alias derived from
        // the "_lookup_<Nav>" access-expression name. AddToProjection uniquifies case-INSENSITIVELY by appending
        // a counter, so a user member literally named to collide with that internal alias would have the SHAPER
        // read the renamed alias while the emitted $project still carries the original — a silent dropped value,
        // not an error. Declining the whole projection is the only safe answer: the emitted alias is fixed by
        // the member name (that agreement is what makes the shaper correct at all), so there is nothing to
        // renegotiate. Narrow by construction — it takes a DTO/anonymous member spelled exactly like the
        // provider's internal lookup alias — but free to close, and the failure mode if left open is silent.
        var seenAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in mongoQ.Projection)
        {
            if (existing.Alias is { } existingAlias)
            {
                seenAliases.Add(existingAlias);
            }
        }

        foreach (var (alias, leafBody) in members)
        {
            // A whole-entity leaf (`x.Outer`/`x.Inner` verbatim) declines the WHOLE projection — Task 5b's
            // deferred territory, not something to half-support with a wrong/missing shaper. Recognized by
            // the member's DECLARING TYPE (IsTransparentIdentifierOuterOrInnerAccess), never by member name
            // alone, so a joined entity that happens to declare its own real "Outer"/"Inner" property is not
            // mistaken for join-chain plumbing — the same rule NativeJoinScopeTranslator's own splitter and
            // ExpressionExtensionMethods document every caller must stay in agreement on.
            //
            // NativeJoinScopeTranslator would decline this leaf anyway (a bare scope leaf rewrites to the
            // synthetic scope parameter, which resolves to no field), so this check is about being explicit
            // and stable rather than about reachability.
            if (leafBody is MemberExpression member
                && ReferenceEquals(member.Expression, rootParam)
                && member.IsTransparentIdentifierOuterOrInnerAccess())
            {
                return false;
            }

            if (!NativeJoinScopeTranslator.TryTranslateValue(scope, rootParam, leafBody, out var computedLeaf))
            {
                return false; // one untranslatable leaf declines the whole projection — no partial commit
            }

            if (!seenAliases.Add(alias))
            {
                return false;
            }

            staged.Add(new MongoProjection(alias, computedLeaf));
        }

        foreach (var projection in staged)
        {
            mongoQ.Select.AddProjection(projection);
        }

        mongoQ.AddLookup(lookup);
        mongoQ.Select.MarkReferenceIncludeConfirmed();
        return true;
    }

    /// <summary>
    /// Splits an anonymous-type / DTO construction into its (member name, value expression) pairs.
    /// </summary>
    /// <remarks>
    /// Deliberately a local copy of the same two-case shape <c>NativeProjectionBinder</c> and
    /// <c>NativeGroupByBinder</c> each parse: theirs are inlined into their own leaf-translation loops
    /// (interleaved with alias derivation, pending-lookup staging and array-leaf bookkeeping this binder has no
    /// analogue for), so there is no existing accessible helper to call. Kept to the same two admitted shapes —
    /// a <see cref="NewExpression"/> carrying <c>Members</c>, or a <see cref="MemberInitExpression"/> over a
    /// parameterless constructor with <see cref="MemberAssignment"/> bindings only.
    /// </remarks>
    private static bool TryReadMembers(
        Expression body, out IReadOnlyList<(string Alias, Expression Leaf)> members)
    {
        var list = new List<(string, Expression)>();
        members = list;

        switch (body)
        {
            case NewExpression newExpression
                when newExpression.Members != null
                     && newExpression.Members.Count == newExpression.Arguments.Count
                     && newExpression.Arguments.Count > 0:
                for (var i = 0; i < newExpression.Arguments.Count; i++)
                {
                    list.Add((newExpression.Members[i].Name, newExpression.Arguments[i]));
                }

                return true;

            case MemberInitExpression memberInit
                when memberInit.NewExpression.Arguments.Count == 0
                     && memberInit.Bindings.Count > 0:
                foreach (var binding in memberInit.Bindings)
                {
                    if (binding is not MemberAssignment assignment)
                    {
                        return false;
                    }

                    list.Add((binding.Member.Name, assignment.Expression));
                }

                return true;

            default:
                return false;
        }
    }
}
