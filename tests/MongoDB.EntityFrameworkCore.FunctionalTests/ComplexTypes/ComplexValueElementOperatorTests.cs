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

using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Infrastructure;
using static MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes.CompositionAssert;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// <c>ElementAt</c>, <c>ElementAtOrDefault</c> and <c>DefaultIfEmpty</c> read no projected VALUE, but after a projected whole
/// complex value they stay refused with the R7 message in every mode (documented as not supported): none of them has a native
/// translation even for a scalar projection (measured: <c>Select(c =&gt; c.Name).ElementAt(1)</c> runs only on driver-LINQ,
/// <c>ElementAtOrDefault</c> fails there, <c>DefaultIfEmpty</c> is untranslatable), and the driver-LINQ fallback cannot read
/// a complex value back, so admitting them would only trade the clear refusal for a less clear failure. Never wrong rows.
/// </summary>
[XUnitCollection("QueryTests")]
public class ComplexValueElementOperatorTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    private const string Refusal = "A projected whole complex value cannot be the operand of '";

    private Func<MongoQueryMode, List<string>> Run(Func<Composition.ShopContext, IEnumerable<string>> query,
        [System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var collections = Composition.Seed(database, name);
        return mode =>
        {
            using var db = Composition.Create(database, collections, mode);
            return query(db).ToList();
        };
    }

    [Fact]
    public void ElementAt_ElementAtOrDefault_and_DefaultIfEmpty_after_a_whole_complex_value_are_refused_in_every_mode()
    {
        foreach (var op in new[] { "ElementAt", "ElementAtOrDefault", "DefaultIfEmpty" })
        {
            var outcome = Throws<NotSupportedException>(Refusal + op + "'");
#if EF8 || EF9
            // EF8/EF9 reject DefaultIfEmpty over this projection before the provider sees it (EF's own message).
            if (op == "DefaultIfEmpty")
            {
                outcome = Throws<InvalidOperationException>("could not be translated");
            }
#endif
            var run = Run(db => op switch
            {
                "ElementAt" => [Composition.Fmt(db.Clients.OrderBy(c => c.Name).Select(c => c.Billing).ElementAt(1))],
                "ElementAtOrDefault" => [db.Clients.OrderBy(c => c.Name).Select(c => c.Billing).ElementAtOrDefault(1) is { } a ? Composition.Fmt(a) : "<null>"],
                _ => [.. db.Clients.Where(c => c.Name == "none").Select(c => c.Billing).DefaultIfEmpty().ToList().Select(a => a is null ? "<null>" : Composition.Fmt(a))]
            }, "ElementOps" + op);
            PerMode(run, [], outcome, outcome, outcome);
        }
    }
}
