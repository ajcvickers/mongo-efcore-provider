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

// The operator set shared by the bulk-source classifier and the single-command filter builder. It is matched by canonical method
// definition, so overloads that change what is selected or how it compares are not treated as ordering or Distinct no-ops. EF rejects
// those overloads before provider code runs today, so this pins the guard rather than a reachable behaviour.

#if !EF8

using System.Reflection;
using MongoDB.EntityFrameworkCore.Query.Expressions;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Expressions;

public static class AffectedSetNeutralDefinitionsTests
{
    private static MethodInfo Closed(string name, int parameterCount, Func<MethodInfo, bool>? filter = null)
        => typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == name && m.GetParameters().Length == parameterCount && (filter?.Invoke(m) ?? true))
            .Single()
            .MakeGenericMethod(TypeArguments(name));

    // OrderBy/ThenBy and friends are generic over (TSource, TKey); the rest over TSource alone.
    private static Type[] TypeArguments(string name)
        => name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenBy)
            or nameof(Queryable.ThenByDescending)
            ? [typeof(int), typeof(string)]
            : [typeof(int)];

    [Theory]
    [InlineData(nameof(Queryable.OrderBy))]
    [InlineData(nameof(Queryable.OrderByDescending))]
    [InlineData(nameof(Queryable.ThenBy))]
    [InlineData(nameof(Queryable.ThenByDescending))]
    public static void The_canonical_ordering_overloads_are_neutral(string name)
    {
        var method = Closed(name, 2);

        Assert.True(MongoNonQueryExpression.IsAffectedSetNeutral(method));
        Assert.False(MongoNonQueryExpression.IsPaging(method));
        Assert.False(MongoNonQueryExpression.IsWhere(method));
    }

    [Fact]
    public static void The_canonical_Distinct_is_neutral()
        => Assert.True(MongoNonQueryExpression.IsAffectedSetNeutral(Closed(nameof(Queryable.Distinct), 1)));

    [Theory]
    [InlineData(nameof(Queryable.OrderBy))]
    [InlineData(nameof(Queryable.OrderByDescending))]
    [InlineData(nameof(Queryable.ThenBy))]
    [InlineData(nameof(Queryable.ThenByDescending))]
    public static void Ordering_with_a_comparer_is_not_neutral(string name)
        => Assert.False(MongoNonQueryExpression.IsAffectedSetNeutral(Closed(name, 3)));

    [Fact]
    public static void Distinct_with_a_comparer_is_not_neutral()
        => Assert.False(MongoNonQueryExpression.IsAffectedSetNeutral(Closed(nameof(Queryable.Distinct), 2)));

    [Fact]
    public static void Where_is_only_the_non_indexed_overload()
    {
        var plain = Closed(nameof(Queryable.Where), 2, m => m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);
        var indexed = Closed(nameof(Queryable.Where), 2, m => m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 3);

        Assert.True(MongoNonQueryExpression.IsWhere(plain));
        Assert.False(MongoNonQueryExpression.IsWhere(indexed));
        Assert.False(MongoNonQueryExpression.IsAffectedSetNeutral(plain));
    }

    [Theory]
    [InlineData(nameof(Queryable.Skip))]
    [InlineData(nameof(Queryable.Take))]
    public static void Skip_and_Take_are_paging_and_never_neutral(string name)
    {
        var method = Closed(name, 2, m => m.GetParameters()[1].ParameterType == typeof(int));

        Assert.True(MongoNonQueryExpression.IsPaging(method));
        Assert.False(MongoNonQueryExpression.IsAffectedSetNeutral(method));
    }

    [Fact]
    public static void Take_with_a_range_is_not_paging()
        => Assert.False(MongoNonQueryExpression.IsPaging(Closed(nameof(Queryable.Take), 2, m => m.GetParameters()[1].ParameterType == typeof(Range))));

    [Theory]
    [InlineData(nameof(Queryable.OfType))]
    [InlineData(nameof(Queryable.Cast))]
    public static void OfType_and_Cast_narrow_the_affected_set_so_are_never_neutral(string name)
    {
        // The directive for the classification work: stepping over either in the filter builder would delete documents of the wrong
        // subtype. Neither belongs in the neutral set.
        var method = typeof(Queryable).GetMethod(name)!.MakeGenericMethod(typeof(int));

        Assert.False(MongoNonQueryExpression.IsAffectedSetNeutral(method));
        Assert.False(MongoNonQueryExpression.IsPaging(method));
    }
}

#endif
