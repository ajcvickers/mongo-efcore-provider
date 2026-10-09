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
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query;

public class ExpressionShapePrinterTests
{
    private class Row
    {
        public string Name { get; set; } = "";
        public int Rank { get; set; }
        public string? Note { get; set; }
    }

    private static string Print(Expression<Func<Row, bool>> lambda)
        => ExpressionShapePrinter.Print(lambda);

    [Fact]
    public void Inlined_constants_are_replaced_by_a_placeholder()
    {
        var printed = Print(r => r.Name == "top-secret" && r.Rank > 42);

        Assert.DoesNotContain("top-secret", printed);
        Assert.DoesNotContain("42", printed);
        Assert.Equal("r => ((r.Name == ?) AndAlso (r.Rank > ?))", printed);
    }

    [Fact]
    public void Null_constants_member_names_and_EF_Property_names_are_kept()
    {
        Assert.Equal("r => (r.Note == null)", Print(r => r.Note == null));
        Assert.Equal("r => (Property(r, \"Name\") == ?)", Print(r => EF.Property<string>(r, "Name") == "x"));
    }

    [Fact]
    public void Captured_variables_print_as_closure_members_not_values()
    {
        var secret = "captured-secret";
        var printed = Print(r => r.Name == secret);

        Assert.DoesNotContain("captured-secret", printed);
        Assert.Contains(".secret", printed);
    }
}
