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
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Types;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Types;

public enum TypeTestEnum
{
    One = 1,
    Two = 2,
    Three = 3
}

/// <summary>
/// Runs the upstream <see cref="TypeTestBase{T,TFixture}"/> once per CLR type. The upstream package ships no concrete
/// per-type classes, so each provider supplies its own set of types.
/// </summary>
public abstract class MongoTypeTest<T, TFixture>(TFixture fixture) : TypeTestBase<T, TFixture>(fixture)
    where T : notnull
    where TFixture : MongoTypeFixture<T>
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(typeof(MongoTypeTest<T, TFixture>));

#if EF10
    public override Task Equality_in_query()
        => base.Equality_in_query();
#else
    public override Task Equality_in_query_with_parameter()
        => base.Equality_in_query_with_parameter();

    public override Task Equality_in_query_with_constant()
        => base.Equality_in_query_with_constant();

    public override Task Primitive_collection_in_query()
        => base.Primitive_collection_in_query();

    public override Task SaveChanges()
        => base.SaveChanges();
#endif
}

public abstract class MongoTypeFixture<T> : TypeFixtureBase<T>
{
    private ITestStoreFactory? _testStoreFactory;

    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("TypeTest");

    public TestServer TestServer { get; private set; } = null!;

    protected override ITestStoreFactory TestStoreFactory
        => _testStoreFactory!;

    public override async LifetimeTask InitializeAsync()
    {
        TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

        await base.InitializeAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
    {
        base.OnModelCreating(modelBuilder, context);

        MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: false);
    }
}

public class BoolTypeMongoTest(BoolTypeMongoTest.BoolTypeMongoFixture fixture)
    : MongoTypeTest<bool, BoolTypeMongoTest.BoolTypeMongoFixture>(fixture)
{
    public class BoolTypeMongoFixture : MongoTypeFixture<bool>
    {
        public override bool Value { get; } = true;

        public override bool OtherValue { get; } = false;
    }
}

public class ByteTypeMongoTest(ByteTypeMongoTest.ByteTypeMongoFixture fixture)
    : MongoTypeTest<byte, ByteTypeMongoTest.ByteTypeMongoFixture>(fixture)
{
    public class ByteTypeMongoFixture : MongoTypeFixture<byte>
    {
        public override byte Value { get; } = (byte)8;

        public override byte OtherValue { get; } = (byte)9;
    }

#if EF11
    // Fails: A byte[] primitive collection (byte array) cannot be queried as an array: ByteArraySerializer is not an IBsonArraySerializer EF-X815
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "must implement IBsonArraySerializer");
#endif
}

public class SByteTypeMongoTest(SByteTypeMongoTest.SByteTypeMongoFixture fixture)
    : MongoTypeTest<sbyte, SByteTypeMongoTest.SByteTypeMongoFixture>(fixture)
{
    public class SByteTypeMongoFixture : MongoTypeFixture<sbyte>
    {
        public override sbyte Value { get; } = (sbyte)-8;

        public override sbyte OtherValue { get; } = (sbyte)9;
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class ShortTypeMongoTest(ShortTypeMongoTest.ShortTypeMongoFixture fixture)
    : MongoTypeTest<short, ShortTypeMongoTest.ShortTypeMongoFixture>(fixture)
{
    public class ShortTypeMongoFixture : MongoTypeFixture<short>
    {
        public override short Value { get; } = (short)-1234;

        public override short OtherValue { get; } = (short)4321;
    }
}

public class UShortTypeMongoTest(UShortTypeMongoTest.UShortTypeMongoFixture fixture)
    : MongoTypeTest<ushort, UShortTypeMongoTest.UShortTypeMongoFixture>(fixture)
{
    public class UShortTypeMongoFixture : MongoTypeFixture<ushort>
    {
        public override ushort Value { get; } = (ushort)1234;

        public override ushort OtherValue { get; } = (ushort)4321;
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class IntTypeMongoTest(IntTypeMongoTest.IntTypeMongoFixture fixture)
    : MongoTypeTest<int, IntTypeMongoTest.IntTypeMongoFixture>(fixture)
{
    public class IntTypeMongoFixture : MongoTypeFixture<int>
    {
        public override int Value { get; } = -123456;

        public override int OtherValue { get; } = 654321;
    }
}

public class UIntTypeMongoTest(UIntTypeMongoTest.UIntTypeMongoFixture fixture)
    : MongoTypeTest<uint, UIntTypeMongoTest.UIntTypeMongoFixture>(fixture)
{
    public class UIntTypeMongoFixture : MongoTypeFixture<uint>
    {
        public override uint Value { get; } = 123456u;

        public override uint OtherValue { get; } = 654321u;
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class LongTypeMongoTest(LongTypeMongoTest.LongTypeMongoFixture fixture)
    : MongoTypeTest<long, LongTypeMongoTest.LongTypeMongoFixture>(fixture)
{
    public class LongTypeMongoFixture : MongoTypeFixture<long>
    {
        public override long Value { get; } = -1234567890123L;

        public override long OtherValue { get; } = 1234567890123L;
    }
}

public class ULongTypeMongoTest(ULongTypeMongoTest.ULongTypeMongoFixture fixture)
    : MongoTypeTest<ulong, ULongTypeMongoTest.ULongTypeMongoFixture>(fixture)
{
    public class ULongTypeMongoFixture : MongoTypeFixture<ulong>
    {
        public override ulong Value { get; } = 1234567890123uL;

        public override ulong OtherValue { get; } = 2234567890123uL;
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class FloatTypeMongoTest(FloatTypeMongoTest.FloatTypeMongoFixture fixture)
    : MongoTypeTest<float, FloatTypeMongoTest.FloatTypeMongoFixture>(fixture)
{
    public class FloatTypeMongoFixture : MongoTypeFixture<float>
    {
        public override float Value { get; } = 3.5f;

        public override float OtherValue { get; } = -7.25f;
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class DoubleTypeMongoTest(DoubleTypeMongoTest.DoubleTypeMongoFixture fixture)
    : MongoTypeTest<double, DoubleTypeMongoTest.DoubleTypeMongoFixture>(fixture)
{
    public class DoubleTypeMongoFixture : MongoTypeFixture<double>
    {
        public override double Value { get; } = 3.141592653589793;

        public override double OtherValue { get; } = -2.718281828459045;
    }
}

public class DecimalTypeMongoTest(DecimalTypeMongoTest.DecimalTypeMongoFixture fixture)
    : MongoTypeTest<decimal, DecimalTypeMongoTest.DecimalTypeMongoFixture>(fixture)
{
    public class DecimalTypeMongoFixture : MongoTypeFixture<decimal>
    {
        public override decimal Value { get; } = 123.456m;

        public override decimal OtherValue { get; } = -654.321m;
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class StringTypeMongoTest(StringTypeMongoTest.StringTypeMongoFixture fixture)
    : MongoTypeTest<string, StringTypeMongoTest.StringTypeMongoFixture>(fixture)
{
    public class StringTypeMongoFixture : MongoTypeFixture<string>
    {
        public override string Value { get; } = "foo";

        public override string OtherValue { get; } = "bar";
    }
}

public class CharTypeMongoTest(CharTypeMongoTest.CharTypeMongoFixture fixture)
    : MongoTypeTest<char, CharTypeMongoTest.CharTypeMongoFixture>(fixture)
{
    public class CharTypeMongoFixture : MongoTypeFixture<char>
    {
        public override char Value { get; } = 'a';

        public override char OtherValue { get; } = 'b';
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class GuidTypeMongoTest(GuidTypeMongoTest.GuidTypeMongoFixture fixture)
    : MongoTypeTest<Guid, GuidTypeMongoTest.GuidTypeMongoFixture>(fixture)
{
    public class GuidTypeMongoFixture : MongoTypeFixture<Guid>
    {
        public override Guid Value { get; } = new Guid("8f1a7ec9-6f9b-4a0c-8f70-5c1d2a3b4c5d");

        public override Guid OtherValue { get; } = new Guid("0c2d4e6f-1a3b-4c5d-9e7f-8a9b0c1d2e3f");
    }

#if EF11
    // Fails: Guid array elements in a primitive collection query cannot be serialized (GuidRepresentation Unspecified) EF-X814
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<MongoDB.Bson.BsonSerializationException>(() => base.Primitive_collection_in_query(), "GuidRepresentation is Unspecified");
#endif
}

public class DateTimeTypeMongoTest(DateTimeTypeMongoTest.DateTimeTypeMongoFixture fixture)
    : MongoTypeTest<DateTime, DateTimeTypeMongoTest.DateTimeTypeMongoFixture>(fixture)
{
    public class DateTimeTypeMongoFixture : MongoTypeFixture<DateTime>
    {
        public override DateTime Value { get; } = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        public override DateTime OtherValue { get; } = new DateTime(2021, 6, 7, 8, 9, 10, DateTimeKind.Utc);
    }
}

public class DateTimeOffsetTypeMongoTest(DateTimeOffsetTypeMongoTest.DateTimeOffsetTypeMongoFixture fixture)
    : MongoTypeTest<DateTimeOffset, DateTimeOffsetTypeMongoTest.DateTimeOffsetTypeMongoFixture>(fixture)
{
    public class DateTimeOffsetTypeMongoFixture : MongoTypeFixture<DateTimeOffset>
    {
        public override DateTimeOffset Value { get; } = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);

        public override DateTimeOffset OtherValue { get; } = new DateTimeOffset(2021, 6, 7, 8, 9, 10, TimeSpan.Zero);
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class DateOnlyTypeMongoTest(DateOnlyTypeMongoTest.DateOnlyTypeMongoFixture fixture)
    : MongoTypeTest<DateOnly, DateOnlyTypeMongoTest.DateOnlyTypeMongoFixture>(fixture)
{
    public class DateOnlyTypeMongoFixture : MongoTypeFixture<DateOnly>
    {
        public override DateOnly Value { get; } = new DateOnly(2020, 1, 2);

        public override DateOnly OtherValue { get; } = new DateOnly(2021, 6, 7);
    }

#if EF11
    // Fails: DateOnly/TimeOnly array elements in a primitive collection query are serialized through the object serializer and rejected EF-X813
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<MongoDB.Bson.BsonSerializationException>(() => base.Primitive_collection_in_query(), "is not configured as a type that is allowed to be serialized");
#endif
}

public class TimeOnlyTypeMongoTest(TimeOnlyTypeMongoTest.TimeOnlyTypeMongoFixture fixture)
    : MongoTypeTest<TimeOnly, TimeOnlyTypeMongoTest.TimeOnlyTypeMongoFixture>(fixture)
{
    public class TimeOnlyTypeMongoFixture : MongoTypeFixture<TimeOnly>
    {
        public override TimeOnly Value { get; } = new TimeOnly(3, 4, 5);

        public override TimeOnly OtherValue { get; } = new TimeOnly(8, 9, 10);
    }

#if EF11
    // Fails: DateOnly/TimeOnly array elements in a primitive collection query are serialized through the object serializer and rejected EF-X813
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<MongoDB.Bson.BsonSerializationException>(() => base.Primitive_collection_in_query(), "is not configured as a type that is allowed to be serialized");
#endif
}

public class TimeSpanTypeMongoTest(TimeSpanTypeMongoTest.TimeSpanTypeMongoFixture fixture)
    : MongoTypeTest<TimeSpan, TimeSpanTypeMongoTest.TimeSpanTypeMongoFixture>(fixture)
{
    public class TimeSpanTypeMongoFixture : MongoTypeFixture<TimeSpan>
    {
        public override TimeSpan Value { get; } = new TimeSpan(1, 2, 3);

        public override TimeSpan OtherValue { get; } = new TimeSpan(4, 5, 6);
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class ByteArrayTypeMongoTest(ByteArrayTypeMongoTest.ByteArrayTypeMongoFixture fixture)
    : MongoTypeTest<byte[], ByteArrayTypeMongoTest.ByteArrayTypeMongoFixture>(fixture)
{
    public class ByteArrayTypeMongoFixture : MongoTypeFixture<byte[]>
    {
        public override byte[] Value { get; } = [1, 2, 3];

        public override byte[] OtherValue { get; } = [4, 5, 6];

    public override Func<byte[], byte[], bool> Comparer { get; } = (a, b) => a.AsSpan().SequenceEqual(b);
    }

#if EF11
    // Fails: A primitive collection query (array.Count(a => a.Equals(value)) == 2) silently matches no document for this element type: wrong results EF-X812
    public override Task Primitive_collection_in_query()
        => SpecFails.ThrowsAsync<System.InvalidOperationException>(() => base.Primitive_collection_in_query(), "Sequence contains no elements");
#endif
}

public class EnumTypeMongoTest(EnumTypeMongoTest.EnumTypeMongoFixture fixture)
    : MongoTypeTest<TypeTestEnum, EnumTypeMongoTest.EnumTypeMongoFixture>(fixture)
{
    public class EnumTypeMongoFixture : MongoTypeFixture<TypeTestEnum>
    {
        public override TypeTestEnum Value { get; } = TypeTestEnum.Two;

        public override TypeTestEnum OtherValue { get; } = TypeTestEnum.Three;
    }
}

#endif
