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

#if !EF8 && !EF9

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Translations;
using Microsoft.EntityFrameworkCore.TestUtilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query.Translations;

public class MiscellaneousTranslationsMongoTest : MiscellaneousTranslationsTestBase<MongoBasicTypesQueryFixture>
{
    public MiscellaneousTranslationsMongoTest(MongoBasicTypesQueryFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF11
    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: Parse(ToString(b.Int EF-X800
    public override Task Byte_Parse()
        => SpecFails.ThrowsAsync(() => base.Byte_Parse());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: Parse(ToString(b.Int EF-X800
    public override Task Decimal_Parse()
        => SpecFails.ThrowsAsync(() => base.Decimal_Parse());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: Parse(ToString(b.Int EF-X800
    public override Task Double_Parse()
        => SpecFails.ThrowsAsync(() => base.Double_Parse());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: Parse(ToString(b.Int EF-X800
    public override Task Short_Parse()
        => SpecFails.ThrowsAsync(() => base.Short_Parse());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: Parse(ToString(b.Int EF-X800
    public override Task Int_Parse()
        => SpecFails.ThrowsAsync(() => base.Int_Parse());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: Parse(ToString(b.Int EF-X800
    public override Task Long_Parse()
        => SpecFails.ThrowsAsync(() => base.Long_Parse());

#endif

    // Fails: System.InvalidOperationException : The LINQ expression contains a call to 'X', which cannot be evalu EF-X801
    public override Task Random_on_EF_Functions()
        => SpecFails.ThrowsAsync(() => base.Random_on_EF_Functions());

    // Fails: System.InvalidOperationException : The LINQ expression contains a call to 'X', which cannot be evalu EF-X801
    public override Task Random_Shared_Next_with_no_args()
        => SpecFails.ThrowsAsync(() => base.Random_Shared_Next_with_no_args());

    // Fails: System.InvalidOperationException : The LINQ expression contains a call to 'X', which cannot be evalu EF-X801
    public override Task Random_Shared_Next_with_one_arg()
        => SpecFails.ThrowsAsync(() => base.Random_Shared_Next_with_one_arg());

    // Fails: System.InvalidOperationException : The LINQ expression contains a call to 'X', which cannot be evalu EF-X801
    public override Task Random_Shared_Next_with_two_args()
        => SpecFails.ThrowsAsync(() => base.Random_Shared_Next_with_two_args());

    // Fails: System.InvalidOperationException : The LINQ expression contains a call to 'X', which cannot be evalu EF-X801
    public override Task Random_new_Next_with_no_args()
        => SpecFails.ThrowsAsync(() => base.Random_new_Next_with_no_args());

    // Fails: System.InvalidOperationException : The LINQ expression contains a call to 'X', which cannot be evalu EF-X801
    public override Task Random_new_Next_with_one_arg()
        => SpecFails.ThrowsAsync(() => base.Random_new_Next_with_one_arg());

    // Fails: System.InvalidOperationException : The LINQ expression contains a call to 'X', which cannot be evalu EF-X801
    public override Task Random_new_Next_with_two_args()
        => SpecFails.ThrowsAsync(() => base.Random_new_Next_with_two_args());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToBoolean(b.Bool). EF-X802
    public override Task Convert_ToBoolean()
        => SpecFails.ThrowsAsync(() => base.Convert_ToBoolean());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToByte(b.Bool). EF-X803
    public override Task Convert_ToByte()
        => SpecFails.ThrowsAsync(() => base.Convert_ToByte());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToDecimal(b.Bool). EF-X804
    public override Task Convert_ToDecimal()
        => SpecFails.ThrowsAsync(() => base.Convert_ToDecimal());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToDouble(b.Bool). EF-X805
    public override Task Convert_ToDouble()
        => SpecFails.ThrowsAsync(() => base.Convert_ToDouble());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToIntN(b.Bool). EF-X806
    public override Task Convert_ToInt16()
        => SpecFails.ThrowsAsync(() => base.Convert_ToInt16());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToIntN(b.Bool). EF-X806
    public override Task Convert_ToInt32()
        => SpecFails.ThrowsAsync(() => base.Convert_ToInt32());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToIntN(b.Bool). EF-X806
    public override Task Convert_ToInt64()
        => SpecFails.ThrowsAsync(() => base.Convert_ToInt64());

    // Fails: MongoDB.Driver.Linq.ExpressionNotSupportedException : Expression not supported: ToString(b.Bool). EF-X807
    public override Task Convert_ToString()
        => SpecFails.ThrowsAsync(() => base.Convert_ToString());

    public override Task Int_Compare_to_simple_zero()
        => base.Int_Compare_to_simple_zero();

    // Fails: DateTime.Compare/CompareTo against an Unspecified-Kind DateTime returns different rows than client evaluation (local-time shift) EF-X808
    // The test's DateTime literal has Kind Unspecified and the driver treats it as local time, so the comparison
    // is shifted by the local UTC offset and matches nothing; it matches (and passes) only when that offset is zero.
    public override Task DateTime_Compare_to_simple_zero(bool compareTo)
        => TimeZoneInfo.Local.GetUtcOffset(new DateTime(1998, 5, 4, 15, 30, 10)) == TimeSpan.Zero
            ? base.DateTime_Compare_to_simple_zero(compareTo)
            : SpecFails.ThrowsAsync(() => base.DateTime_Compare_to_simple_zero(compareTo));

    public override Task TimeSpan_Compare_to_simple_zero(bool compareTo)
        => base.TimeSpan_Compare_to_simple_zero(compareTo);

    // <generated>
    // </generated>

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);
}

#endif
