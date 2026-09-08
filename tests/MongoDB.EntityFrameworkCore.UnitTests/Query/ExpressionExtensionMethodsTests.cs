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

using System.Linq.Expressions;
using System.Reflection;
using MongoDB.EntityFrameworkCore;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public class ExpressionExtensionMethodsTests
{
    private class OneArgDto
    {
        public string Value { get; }
        public OneArgDto(string value) => Value = value;
    }

    private class TwoArgDto
    {
        public string A { get; }
        public int B { get; }
        public TwoArgDto(string a, int b) { A = a; B = b; }
    }

    private static ConstructorInfo Ctor<T>(int argCount)
        => typeof(T).GetConstructors().Single(c => c.GetParameters().Length == argCount);

    [Fact]
    public void Ctor_only_new_expression_declines_by_default()
    {
        var arg = Expression.Constant("hello");
        var newExpr = Expression.New(Ctor<OneArgDto>(1), arg);

        var result = newExpr.TryGetProjectionMembers(out var members);

        Assert.False(result);
        Assert.Empty(members);
    }

    [Fact]
    public void Ctor_only_new_expression_declines_when_flag_explicitly_false()
    {
        var arg = Expression.Constant("hello");
        var newExpr = Expression.New(Ctor<OneArgDto>(1), arg);

        var result = newExpr.TryGetProjectionMembers(out var members, allowPositionalConstructorArguments: false);

        Assert.False(result);
        Assert.Empty(members);
    }

    [Fact]
    public void Ctor_only_new_expression_admits_one_positional_argument_when_flag_true()
    {
        var arg = Expression.Constant("hello");
        var newExpr = Expression.New(Ctor<OneArgDto>(1), arg);

        var result = newExpr.TryGetProjectionMembers(out var members, allowPositionalConstructorArguments: true);

        Assert.True(result);
        var member = Assert.Single(members);
        Assert.Equal(ExpressionExtensionMethods.PositionalConstructorArgumentAliasPrefix + "0", member.MemberName);
        Assert.Same(arg, member.Value);
    }

    [Fact]
    public void Ctor_only_new_expression_admits_multiple_positional_arguments_in_order_when_flag_true()
    {
        var argA = Expression.Constant("hello");
        var argB = Expression.Constant(42);
        var newExpr = Expression.New(Ctor<TwoArgDto>(2), argA, argB);

        var result = newExpr.TryGetProjectionMembers(out var members, allowPositionalConstructorArguments: true);

        Assert.True(result);
        Assert.Equal(2, members.Count);
        Assert.Equal(ExpressionExtensionMethods.PositionalConstructorArgumentAliasPrefix + "0", members[0].MemberName);
        Assert.Same(argA, members[0].Value);
        Assert.Equal(ExpressionExtensionMethods.PositionalConstructorArgumentAliasPrefix + "1", members[1].MemberName);
        Assert.Same(argB, members[1].Value);
    }

    [Fact]
    public void Named_members_new_expression_is_unaffected_by_the_new_flag()
    {
        // An anonymous type: NewExpression.Members IS populated by the compiler. Passing
        // allowPositionalConstructorArguments: true must not change this arm's behavior at all.
        Expression<System.Func<string, int, object>> lambda = (a, b) => new { A = a, B = b };
        var newExpr = (NewExpression)lambda.Body;

        var result = newExpr.TryGetProjectionMembers(out var members, allowPositionalConstructorArguments: true);

        Assert.True(result);
        Assert.Equal(2, members.Count);
        Assert.Equal("A", members[0].MemberName);
        Assert.Equal("B", members[1].MemberName);
    }

    [Fact]
    public void Ctor_only_new_expression_with_zero_arguments_declines_even_when_flag_true()
    {
        var newExpr = Expression.New(typeof(object));

        var result = newExpr.TryGetProjectionMembers(out var members, allowPositionalConstructorArguments: true);

        Assert.False(result);
        Assert.Empty(members);
    }
}
