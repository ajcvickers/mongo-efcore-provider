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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Mapping;

public class JsonTypesMongoTest
#if EF8
    : JsonTypesTestBase
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMongoDB("mongodb://localhost:27017", "JsonTypes");
#elif EF9
    : JsonTypesTestBase
{
    public JsonTypesMongoTest()
    {
    }
#else
    : JsonTypesTestBase
{
    public JsonTypesMongoTest(NonSharedFixture fixture)
        : base(fixture)
    {
    }
#endif

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

#if EF11
    // Fails: new in the EF11 upstream package; asserts any failure until triaged. EF-X605
    public override Task Can_read_write_collection_of_TimeOnly_JSON_values(string expected)
        => SpecFails.ThrowsAsync(() => base.Can_read_write_collection_of_TimeOnly_JSON_values(expected));

    public override Task Can_read_write_collection_of_TimeSpan_JSON_values(string expected)
        => SpecFails.ThrowsAsync(() => base.Can_read_write_collection_of_TimeSpan_JSON_values(expected));

    public override Task Can_read_write_collection_of_nullable_TimeOnly_JSON_values(string expected)
        => SpecFails.ThrowsAsync(() => base.Can_read_write_collection_of_nullable_TimeOnly_JSON_values(expected));

    public override Task Can_read_write_collection_of_nullable_TimeSpan_JSON_values(string expected)
        => SpecFails.ThrowsAsync(() => base.Can_read_write_collection_of_nullable_TimeSpan_JSON_values(expected));
#endif

    // <generated>
#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_DateOnly_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_DateOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_DateOnly_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_DateOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_DateTimeOffset_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_DateTimeOffset_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_DateTimeOffset_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_DateTimeOffset_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_DateTime_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_DateTime_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_DateTime_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_DateTime_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_GUID_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_GUID_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_GUID_JSON_values(global::System.Guid value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_GUID_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_IP_address_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_IP_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_IP_address_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_IP_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_TimeOnly_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_TimeOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_TimeOnly_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_TimeOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_TimeSpan_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_TimeSpan_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_TimeSpan_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_TimeSpan_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_URI_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_URI_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_URI_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_URI_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_array_of_array_of_int_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_array_of_array_of_int_JSON_values(), "The 'int[][][]' property 'Int32ArrayArrayArrayType.Prop' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_GUID_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_GUID_JSON_values(expected), "The 'ICollection<Guid>[]' property 'GuidListArrayType.Prop' could not ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_IPAddress_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_IPAddress_JSON_values(), "The 'Collection<IPAddress>[]' property 'IpAddressListArrayType.Prop' c");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_array_of_IPAddress_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_array_of_IPAddress_JSON_values(), "The 'List<IPAddress[]>[]' property 'IpAddressArrayListArrayType.Prop' ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_array_of_string_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_array_of_string_JSON_values(), "The 'List<string[]>[]' property 'StringArrayListArrayType.Prop' could ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_binary_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_binary_JSON_values(expected), "The 'List<byte[]>[]' property 'BinaryListArrayType.Prop' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_int_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_int_JSON_values(), "The 'IList<int>[]' property 'Int32ListArrayType.Prop' could not be map");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_string_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_string_JSON_values(), "The 'List<string>[]' property 'StringListArrayType.Prop' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_array_of_list_of_ulong_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_array_of_list_of_ulong_JSON_values(), "The 'List<ulong>[]' property 'ULongListArrayType.Prop' could not be ma");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_binary_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_binary_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_binary_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_binary_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_binary_as_collection()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_binary_as_collection(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_binary_as_collection()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_binary_as_collection(), "The 'byte[]' property 'BinaryAsJsonType.BinaryAsJson' could not be map");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_bool_JSON_values(bool value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_bool_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_bool_JSON_values(bool value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_bool_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_byte_JSON_values(byte value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_byte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_byte_JSON_values(byte value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_byte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_byte_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU8 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_byte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_byte_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU8 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_byte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_char_JSON_values(global::System.Char value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_char_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_char_JSON_values(global::System.Char value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_char_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_DateOnly_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_DateOnly_JSON_values(), "The 'IList<DateOnly>' property 'DateOnlyCollectionType.DateOnly' could");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_DateOnly_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_DateOnly_JSON_values(), "The 'IList<DateOnly>' property 'DateOnlyCollectionType.DateOnly' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_DateOnly_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_DateOnly_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_DateOnly_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_DateOnly_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_DateTimeOffset_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_DateTimeOffset_JSON_values(expected), "The 'IList<DateTimeOffset>' property 'DateTimeOffsetCollectionType.Dat");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_DateTimeOffset_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_DateTimeOffset_JSON_values(), "The 'IList<DateTimeOffset>' property 'DateTimeOffsetCollectionType.Dat");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_DateTime_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_DateTime_JSON_values(expected), "The 'IList<DateTime>' property 'DateTimeCollectionType.DateTime' could");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_DateTime_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_DateTime_JSON_values(), "The 'IList<DateTime>' property 'DateTimeCollectionType.DateTime' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_DateTime_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_DateTime_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_DateTime_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_DateTime_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_GUID_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_GUID_JSON_values(expected), "The 'IList<Guid>' property 'GuidCollectionType.Guid' could not be mapp");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_GUID_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_GUID_JSON_values(), "The 'IList<Guid>' property 'GuidCollectionType.Guid' could not be mapp");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_Guid_converted_to_bytes_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_Guid_converted_to_bytes_JSON_values(expected), "The 'IList<Guid>' property 'GuidCollectionType.Guid' could not be mapp");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_Guid_converted_to_bytes_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_Guid_converted_to_bytes_JSON_values(), "The 'IList<Guid>' property 'GuidCollectionType.Guid' could not be mapp");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_IP_address_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_IP_address_JSON_values(), "The 'ReadOnlyCollection<IPAddress>' property 'IpAddressCollectionType.");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_IP_address_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_IP_address_JSON_values(), "The 'List<IPAddress>' property 'IpAddressCollectionType.IpAddress' cou");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_TimeOnly_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_TimeOnly_JSON_values(), "The 'IReadOnlyCollection<TimeOnly>' property 'TimeOnlyCollectionType.T");
#endif
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_TimeOnly_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_TimeOnly_JSON_values(), "The 'IList<TimeOnly>' property 'TimeOnlyCollectionType.TimeOnly' could");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_TimeSpan_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_TimeSpan_JSON_values(), "The 'IList<TimeSpan>' property 'TimeSpanCollectionType.TimeSpan' could");
#endif
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_TimeSpan_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_TimeSpan_JSON_values(), "The 'IList<TimeSpan>' property 'TimeSpanCollectionType.TimeSpan' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_URI_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_URI_JSON_values(), "The 'List<Uri>' property 'UriCollectionType.Uri' could not be mapped b");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_URI_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_URI_JSON_values(), "The 'List<Uri>' property 'UriCollectionType.Uri' could not be mapped b");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_binary_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_binary_JSON_values(expected), "The 'IList<byte[]>' property 'BytesCollectionType.Bytes' could not be ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_binary_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_binary_JSON_values(), "The 'IList<byte[]>' property 'BytesCollectionType.Bytes' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_binary_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_binary_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_binary_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_binary_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_bool_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_bool_JSON_values(), "The 'IList<bool>' property 'BooleanCollectionType.Boolean' could not b");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_bool_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_bool_JSON_values(), "The 'IList<bool>' property 'BooleanCollectionType.Boolean' could not b");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_bool_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_bool_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_bool_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_bool_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_byte_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_byte_JSON_values(), "The 'List<byte>' property 'UInt8CollectionType.UInt8' could not be map");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_byte_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_byte_JSON_values(), "The 'List<byte>' property 'UInt8CollectionType.UInt8' could not be map");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_byte_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_byte_enum_JSON_values(), "The 'IList<EnumU8>' property 'EnumU8CollectionType.EnumU8' could not b");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_byte_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_byte_enum_JSON_values(), "The 'IList<EnumU8>' property 'EnumU8CollectionType.EnumU8' could not b");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_char_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_char_JSON_values(), "The 'IList<char>' property 'CharacterCollectionType.Character' could n");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_char_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_char_JSON_values(), "The 'IList<char>' property 'CharacterCollectionType.Character' could n");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_char_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_char_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_char_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_char_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_decimal_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_decimal_JSON_values(expected), "The 'decimal[]' property 'DecimalCollectionType.Decimal' could not be ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_decimal_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_decimal_JSON_values(), "The 'decimal[]' property 'DecimalCollectionType.Decimal' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_decimal_with_precision_and_scale_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_decimal_with_precision_and_scale_JSON_values(expected), "The 'decimal[]' property 'DecimalCollectionType.Decimal' could not be ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_decimal_with_precision_and_scale_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_decimal_with_precision_and_scale_JSON_values(), "The 'decimal[]' property 'DecimalCollectionType.Decimal' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_double_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_double_JSON_values(), "The 'double[]' property 'DoubleCollectionType.Double' could not be map");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_double_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_double_JSON_values(), "The 'double[]' property 'DoubleCollectionType.Double' could not be map");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_double_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_double_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_double_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_double_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_float_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_float_JSON_values(), "The 'float[]' property 'FloatCollectionType.Float' could not be mapped");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_float_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_float_JSON_values(), "The 'float[]' property 'FloatCollectionType.Float' could not be mapped");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_int_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_int_JSON_values(), "The 'ReadOnlyCollection<int>' property 'Int32CollectionType.Int32' cou");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_int_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_int_JSON_values(), "The 'List<int>' property 'Int32CollectionType.Int32' could not be mapp");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_int_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_int_enum_JSON_values(), "The 'List<Enum32>' property 'Enum32CollectionType.Enum32' could not be");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_int_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_int_enum_JSON_values(), "The 'List<Enum32>' property 'Enum32CollectionType.Enum32' could not be");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_int_enum_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_int_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_int_enum_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_int_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_int_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_int_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_int_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_int_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_int_with_converter_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_int_with_converter_JSON_values(), "The 'IList<DddId>' property 'DddIdCollectionType.DddId' could not be m");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_int_with_converter_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_int_with_converter_JSON_values(), "The 'IList<DddId>' property 'DddIdCollectionType.DddId' could not be m");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_long_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_long_JSON_values(), "The 'IList<long>' property 'Int64CollectionType.Int64' could not be ma");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_long_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_long_JSON_values(), "The 'IList<long>' property 'Int64CollectionType.Int64' could not be ma");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_long_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_long_enum_JSON_values(), "The 'List<Enum64>' property 'Enum64CollectionType.Enum64' could not be");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_long_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_long_enum_JSON_values(), "The 'List<Enum64>' property 'Enum64CollectionType.Enum64' could not be");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_DateOnly_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_DateOnly_JSON_values(), "The 'IList<DateOnly?>' property 'NullableDateOnlyCollectionType.DateOn");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_DateOnly_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_DateOnly_JSON_values(), "The 'IList<DateOnly?>' property 'NullableDateOnlyCollectionType.DateOn");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_DateOnly_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_DateOnly_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_DateOnly_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_DateOnly_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_DateTimeOffset_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_DateTimeOffset_JSON_values(expected), "The 'IReadOnlyList<DateTimeOffset?>' property 'NullableDateTimeOffsetC");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_DateTimeOffset_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_DateTimeOffset_JSON_values(), "The 'IList<DateTimeOffset?>' property 'NullableDateTimeOffsetCollectio");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_DateTime_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_DateTime_JSON_values(expected), "The 'IList<DateTime?>' property 'NullableDateTimeCollectionType.DateTi");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_DateTime_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_DateTime_JSON_values(), "The 'IList<DateTime?>' property 'NullableDateTimeCollectionType.DateTi");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_DateTime_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_DateTime_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_DateTime_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_DateTime_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_GUID_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_GUID_JSON_values(expected), "The 'IList<Guid?>' property 'NullableGuidCollectionType.Guid' could no");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_GUID_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_GUID_JSON_values(), "The 'IList<Guid?>' property 'NullableGuidCollectionType.Guid' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_IP_address_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_IP_address_JSON_values(), "The 'List<IPAddress>' property 'NullableIpAddressCollectionType.IpAddr");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_IP_address_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_IP_address_JSON_values(), "The 'List<IPAddress>' property 'NullableIpAddressCollectionType.IpAddr");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_TimeOnly_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_TimeOnly_JSON_values(), "The 'IReadOnlyList<TimeOnly?>' property 'NullableTimeOnlyCollectionTyp");
#endif
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_TimeOnly_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_TimeOnly_JSON_values(), "The 'IList<TimeOnly?>' property 'NullableTimeOnlyCollectionType.TimeOn");
#endif

#if (EF10 || EF11) || EF9
#if !EF11
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_TimeSpan_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_TimeSpan_JSON_values(), "The 'IList<TimeSpan?>' property 'NullableTimeSpanCollectionType.TimeSp");
#endif
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_TimeSpan_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_TimeSpan_JSON_values(), "The 'IList<TimeSpan?>' property 'NullableTimeSpanCollectionType.TimeSp");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_URI_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_URI_JSON_values(), "The 'List<Uri>' property 'NullableUriCollectionType.Uri' could not be ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_URI_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_URI_JSON_values(), "The 'List<Uri>' property 'NullableUriCollectionType.Uri' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_binary_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_binary_JSON_values(expected), "The 'IList<byte[]>' property 'NullableBytesCollectionType.Bytes' could");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_binary_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_binary_JSON_values(), "The 'IList<byte[]>' property 'NullableBytesCollectionType.Bytes' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_binary_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_binary_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_binary_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_binary_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_bool_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_bool_JSON_values(), "The 'ReadOnlyCollection<bool?>' property 'NullableBooleanCollectionTyp");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_bool_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_bool_JSON_values(), "The 'IList<bool?>' property 'NullableBooleanCollectionType.Boolean' co");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_bool_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_bool_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_bool_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_bool_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_byte_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_byte_JSON_values(), "The 'List<byte?>' property 'NullableUInt8CollectionType.UInt8' could n");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_byte_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_byte_JSON_values(), "The 'List<byte?>' property 'NullableUInt8CollectionType.UInt8' could n");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_byte_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_byte_enum_JSON_values(), "The 'IList<EnumU8?>' property 'NullableEnumU8CollectionType.EnumU8' co");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_byte_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_byte_enum_JSON_values(), "The 'IList<EnumU8?>' property 'NullableEnumU8CollectionType.EnumU8' co");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_char_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_char_JSON_values(), "The 'IList<char?>' property 'NullableCharacterCollectionType.Character");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_char_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_char_JSON_values(), "The 'IList<char?>' property 'NullableCharacterCollectionType.Character");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_char_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_char_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_char_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_char_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_decimal_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_decimal_JSON_values(expected), "The 'decimal?[]' property 'NullableDecimalCollectionType.Decimal' coul");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_decimal_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_decimal_JSON_values(), "The 'decimal?[]' property 'NullableDecimalCollectionType.Decimal' coul");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_double_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_double_JSON_values(), "The 'double?[]' property 'NullableDoubleCollectionType.Double' could n");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_double_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_double_JSON_values(), "The 'double?[]' property 'NullableDoubleCollectionType.Double' could n");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_double_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_double_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_double_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_double_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_float_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_float_JSON_values(), "The 'float?[]' property 'NullableFloatCollectionType.Float' could not ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_float_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_float_JSON_values(), "The 'float?[]' property 'NullableFloatCollectionType.Float' could not ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_int_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_int_JSON_values(), "The 'List<int?>' property 'NullableInt32CollectionType.Int32' could no");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_int_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_int_JSON_values(), "The 'List<int?>' property 'NullableInt32CollectionType.Int32' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_int_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_int_enum_JSON_values(), "The 'List<Enum32?>' property 'NullableEnum32CollectionType.Enum32' cou");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_int_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_int_enum_JSON_values(), "The 'List<Enum32?>' property 'NullableEnum32CollectionType.Enum32' cou");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_int_enum_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_int_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_int_enum_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_int_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_int_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_int_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_int_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_int_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_int_with_converter_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_int_with_converter_JSON_values(), "The 'IList<DddId?>' property 'NullableDddIdCollectionType.DddId' could");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_int_with_converter_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_int_with_converter_JSON_values(), "The 'IList<DddId?>' property 'NullableDddIdCollectionType.DddId' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_long_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_long_JSON_values(), "The 'ReadOnlyCollection<long?>' property 'NullableInt64CollectionType.");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_long_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_long_JSON_values(), "The 'IList<long?>' property 'NullableInt64CollectionType.Int64' could ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_long_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_long_enum_JSON_values(), "The 'List<Enum64?>' property 'NullableEnum64CollectionType.Enum64' cou");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_long_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_long_enum_JSON_values(), "The 'List<Enum64?>' property 'NullableEnum64CollectionType.Enum64' cou");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_physical_address_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_physical_address_JSON_values(), "The 'List<PhysicalAddress>' property 'NullablePhysicalAddressCollectio");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_physical_address_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_physical_address_JSON_values(), "The 'List<PhysicalAddress>' property 'NullablePhysicalAddressCollectio");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_sbyte_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_sbyte_JSON_values(), "The 'sbyte?[]' property 'NullableInt8CollectionType.Int8' could not be");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_sbyte_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_sbyte_JSON_values(), "The 'sbyte?[]' property 'NullableInt8CollectionType.Int8' could not be");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_sbyte_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_sbyte_enum_JSON_values(), "The 'List<Enum8?>' property 'NullableEnum8CollectionType.Enum8' could ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_sbyte_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_sbyte_enum_JSON_values(), "The 'List<Enum8?>' property 'NullableEnum8CollectionType.Enum8' could ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_sbyte_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_sbyte_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_sbyte_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_sbyte_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_short_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_short_JSON_values(), "The 'IReadOnlyCollection<short?>' property 'NullableInt16CollectionTyp");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_short_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_short_JSON_values(), "The 'IList<short?>' property 'NullableInt16CollectionType.Int16' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_short_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_short_enum_JSON_values(), "The 'List<Enum16?>' property 'NullableEnum16CollectionType.Enum16' cou");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_short_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_short_enum_JSON_values(), "The 'List<Enum16?>' property 'NullableEnum16CollectionType.Enum16' cou");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_string_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_string_JSON_values(), "The 'IList<string>' property 'NullableStringCollectionType.String' cou");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_string_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_string_JSON_values(), "The 'IList<string>' property 'NullableStringCollectionType.String' cou");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_string_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_string_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_string_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_string_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_uint_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_uint_JSON_values(), "The 'List<uint?>' property 'NullableUInt32CollectionType.UInt32' could");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_uint_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_uint_JSON_values(), "The 'List<uint?>' property 'NullableUInt32CollectionType.UInt32' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_uint_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_uint_enum_JSON_values(), "The 'IList<EnumU32?>' property 'NullableEnumU32CollectionType.EnumU32'");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_uint_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_uint_enum_JSON_values(), "The 'IList<EnumU32?>' property 'NullableEnumU32CollectionType.EnumU32'");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_ulong_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ulong_JSON_values(), "The 'ObservableCollection<ulong?>' property 'NullableUInt64CollectionT");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_ulong_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ulong_JSON_values(), "The 'ObservableCollection<ulong?>' property 'NullableUInt64CollectionT");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_ulong_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ulong_enum_JSON_values(), "The 'IList<EnumU64?>' property 'NullableEnumU64CollectionType.EnumU64'");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_ulong_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ulong_enum_JSON_values(), "The 'IList<EnumU64?>' property 'NullableEnumU64CollectionType.EnumU64'");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_ulong_enum_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_ulong_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_ulong_enum_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_ulong_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_nullable_ulong_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_ulong_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_nullable_ulong_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_nullable_ulong_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_ushort_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ushort_JSON_values(), "The 'Collection<ushort?>' property 'NullableUInt16CollectionType.UInt1");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_ushort_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ushort_JSON_values(), "The 'Collection<ushort?>' property 'NullableUInt16CollectionType.UInt1");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_nullable_ushort_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ushort_enum_JSON_values(), "The 'IList<EnumU16?>' property 'NullableEnumU16CollectionType.EnumU16'");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_nullable_ushort_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_nullable_ushort_enum_JSON_values(), "The 'IList<EnumU16?>' property 'NullableEnumU16CollectionType.EnumU16'");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_physical_address_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_physical_address_JSON_values(), "The 'List<PhysicalAddress>' property 'PhysicalAddressCollectionType.Ph");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_physical_address_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_physical_address_JSON_values(), "The 'List<PhysicalAddress>' property 'PhysicalAddressCollectionType.Ph");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_sbyte_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_sbyte_JSON_values(), "The 'sbyte[]' property 'Int8CollectionType.Int8' could not be mapped b");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_sbyte_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_sbyte_JSON_values(), "The 'sbyte[]' property 'Int8CollectionType.Int8' could not be mapped b");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_sbyte_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_sbyte_enum_JSON_values(), "The 'List<Enum8>' property 'Enum8CollectionType.Enum8' could not be ma");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_sbyte_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_sbyte_enum_JSON_values(), "The 'List<Enum8>' property 'Enum8CollectionType.Enum8' could not be ma");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_sbyte_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_sbyte_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_sbyte_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_sbyte_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_short_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_short_JSON_values(), "The 'IReadOnlyCollection<short>' property 'Int16CollectionType.Int16' ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_short_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_short_JSON_values(), "The 'IList<short>' property 'Int16CollectionType.Int16' could not be m");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_short_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_short_enum_JSON_values(), "The 'List<Enum16>' property 'Enum16CollectionType.Enum16' could not be");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_short_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_short_enum_JSON_values(), "The 'List<Enum16>' property 'Enum16CollectionType.Enum16' could not be");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_string_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_string_JSON_values(), "The 'IReadOnlyCollection<string>' property 'StringCollectionType.Strin");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_string_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_string_JSON_values(), "The 'IList<string>' property 'StringCollectionType.String' could not b");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_string_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_string_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_string_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_string_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_uint_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_uint_JSON_values(), "The 'List<uint>' property 'UInt32CollectionType.UInt32' could not be m");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_uint_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_uint_JSON_values(), "The 'List<uint>' property 'UInt32CollectionType.UInt32' could not be m");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_uint_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_uint_enum_JSON_values(), "The 'IList<EnumU32>' property 'EnumU32CollectionType.EnumU32' could no");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_uint_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_uint_enum_JSON_values(), "The 'IList<EnumU32>' property 'EnumU32CollectionType.EnumU32' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_ulong_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ulong_JSON_values(), "The 'ObservableCollection<ulong>' property 'UInt64CollectionType.UInt6");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_ulong_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ulong_JSON_values(), "The 'ObservableCollection<ulong>' property 'UInt64CollectionType.UInt6");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_ulong_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ulong_enum_JSON_values(), "The 'IList<EnumU64>' property 'EnumU64CollectionType.EnumU64' could no");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_ulong_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ulong_enum_JSON_values(), "The 'IList<EnumU64>' property 'EnumU64CollectionType.EnumU64' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_ulong_enum_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_ulong_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_ulong_enum_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_ulong_enum_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_collection_of_ulong_values_with_converter_as_JSON_string()
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_ulong_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_collection_of_ulong_values_with_converter_as_JSON_string()
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_collection_of_ulong_values_with_converter_as_JSON_string(), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_ushort_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ushort_JSON_values(), "The 'Collection<ushort>' property 'UInt16CollectionType.UInt16' could ");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_ushort_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ushort_JSON_values(), "The 'Collection<ushort>' property 'UInt16CollectionType.UInt16' could ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_collection_of_ushort_enum_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ushort_enum_JSON_values(), "The 'IList<EnumU16>' property 'EnumU16CollectionType.EnumU16' could no");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_collection_of_ushort_enum_JSON_values()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_collection_of_ushort_enum_JSON_values(), "The 'IList<EnumU16>' property 'EnumU16CollectionType.EnumU16' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_converted_type_JSON_values(int value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_converted_type_JSON_values(int value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_custom_converted_type_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_custom_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_custom_converted_type_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_custom_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_decimal_JSON_values(decimal value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_decimal_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_decimal_JSON_values(decimal value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_decimal_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_double_JSON_values(double value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_double_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_double_JSON_values(double value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_double_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_enum_char_converted_type_JSON_values(int value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_enum_char_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_enum_char_converted_type_JSON_values(int value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_enum_char_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_float_JSON_values(float value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_float_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_float_JSON_values(float value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_float_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_int_JSON_values(int value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_int_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_int_JSON_values(int value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_int_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_int_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum32 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_int_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_int_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum32 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_int_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_line_string()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_line_string(), "The 'LineString' property 'LineStringType.LineString' could not be map");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_line_string()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_line_string(), "The 'LineString' property 'LineStringType.LineString' could not be map");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_line_string_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_line_string_as_GeoJson(), "The 'LineString' property 'LineStringType.LineString' could not be map");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_line_string_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_line_string_as_GeoJson(), "The 'LineString' property 'LineStringType.LineString' could not be map");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_GUID_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_GUID_JSON_values(expected), "The 'List<Guid[]>' property 'GuidArrayListType.Prop' could not be mapp");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_IPAddress_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_IPAddress_JSON_values(), "The 'ObservableCollection<IPAddress[]>' property 'IpAddressArrayListTy");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_binary_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_binary_JSON_values(expected), "The 'IEnumerable<byte[][]>' property 'BinaryArrayListType.Prop' could ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_int_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_int_JSON_values(), "The 'List<int[]>' property 'Int32ArrayListType.Prop' could not be mapp");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_list_of_IPAddress_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_list_of_IPAddress_JSON_values(), "The 'List<List<IPAddress>[]>' property 'IpAddressListArrayListType.Pro");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_list_of_array_of_binary_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_list_of_array_of_binary_JSON_values(expected), "The 'List<List<byte[][]>[]>' property 'BinaryListArrayArrayListType.Pr");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_list_of_string_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_list_of_string_JSON_values(), "The 'List<List<string>[]>' property 'StringListArrayListType.Prop' cou");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_list_of_ulong_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_list_of_ulong_JSON_values(), "The 'List<List<ulong[]>>' property 'ULongListArrayListType.Prop' could");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_nullable_GUID_JSON_values(string expected)
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_nullable_GUID_JSON_values(expected), "The 'List<Guid?[]>' property 'NullableGuidArrayListType.Prop' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_nullable_int_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_nullable_int_JSON_values(), "The 'List<int?[]>' property 'NullableInt32ArrayListType.Prop' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_nullable_ulong_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_nullable_ulong_JSON_values(), "The 'List<ulong?[]>' property 'NullableULongArrayListType.Prop' could ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_string_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_string_JSON_values(), "The 'List<string[]>' property 'StringArrayListType.Prop' could not be ");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_array_of_ulong_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_array_of_ulong_JSON_values(), "The 'List<ulong[]>' property 'ULongArrayListType.Prop' could not be ma");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_list_of_list_of_list_of_int_JSON_values()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_list_of_list_of_list_of_int_JSON_values(), "The 'List<List<List<int>>>' property 'Int32ListListListType.Prop' coul");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_long_JSON_values(long value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_long_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_long_JSON_values(long value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_long_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_long_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum64 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_long_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_long_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum64 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_long_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_multi_line_string()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_multi_line_string(), "The 'MultiLineString' property 'MultiLineStringType.MultiLineString' c");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_multi_line_string()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_multi_line_string(), "The 'MultiLineString' property 'MultiLineStringType.MultiLineString' c");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_multi_line_string_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_multi_line_string_as_GeoJson(), "The 'MultiLineString' property 'MultiLineStringType.MultiLineString' c");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_multi_line_string_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_multi_line_string_as_GeoJson(), "The 'MultiLineString' property 'MultiLineStringType.MultiLineString' c");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_DateOnly_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_DateOnly_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_DateOnly_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateOnly_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_DateOnly_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateOnly_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_DateTimeOffset_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTimeOffset_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_DateTimeOffset_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTimeOffset_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_DateTimeOffset_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTimeOffset_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_DateTimeOffset_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTimeOffset_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_DateTime_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTime_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_DateTime_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTime_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_DateTime_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTime_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_DateTime_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_DateTime_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_GUID_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_GUID_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_GUID_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_GUID_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_IP_address_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_IP_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_IP_address_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_IP_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_IP_address_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_IP_address_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_IP_address_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_IP_address_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_TimeOnly_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_TimeOnly_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeOnly_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_TimeOnly_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeOnly_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_TimeOnly_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeOnly_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_TimeSpan_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeSpan_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_TimeSpan_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeSpan_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_TimeSpan_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeSpan_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_TimeSpan_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_TimeSpan_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_URI_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_URI_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_URI_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_URI_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_URI_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_URI_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_URI_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_URI_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_as_string_GUID_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_as_string_GUID_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_as_string_GUID_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_as_string_GUID_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_binary_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_binary_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_binary_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_binary_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_binary_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_binary_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_binary_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_binary_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_bool_JSON_values(bool? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_bool_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_bool_JSON_values(bool? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_bool_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_bool_as_string_JSON_values(bool? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_bool_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_bool_as_string_JSON_values(bool? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_bool_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_byte_JSON_values(byte? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_byte_JSON_values(byte? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_byte_as_string_JSON_values(byte? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_byte_as_string_JSON_values(byte? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_byte_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_byte_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_byte_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_byte_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_byte_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_char_JSON_values(global::System.Char? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_char_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_char_JSON_values(global::System.Char? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_char_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_char_as_string_JSON_values(global::System.Char? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_char_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_char_as_string_JSON_values(global::System.Char? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_char_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_converted_type_JSON_values(int? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_converted_type_JSON_values(int? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_converted_type_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_decimal_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_decimal_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_decimal_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_decimal_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_decimal_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_decimal_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_decimal_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_decimal_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_double_JSON_values(double? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_double_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_double_JSON_values(double? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_double_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_double_as_string_JSON_values(double? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_double_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_double_as_string_JSON_values(double? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_double_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_float_JSON_values(float? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_float_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_float_JSON_values(float? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_float_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_float_as_string_JSON_values(float? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_float_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_float_as_string_JSON_values(float? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_float_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_int_JSON_values(int? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_int_JSON_values(int? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_int_as_string_JSON_values(int? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_int_as_string_JSON_values(int? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_int_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_int_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_int_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_int_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_int_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_line_string()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_line_string(), "The 'LineString' property 'NullableLineStringType.LineString' could no");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_line_string()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_line_string(), "The 'LineString' property 'NullableLineStringType.LineString' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_line_string_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_line_string_as_GeoJson(), "The 'LineString' property 'NullableLineStringType.LineString' could no");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_line_string_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_line_string_as_GeoJson(), "The 'LineString' property 'NullableLineStringType.LineString' could no");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_long_JSON_values(long? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_long_JSON_values(long? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_long_as_string_JSON_values(long? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_long_as_string_JSON_values(long? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_long_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_long_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_long_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_long_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_long_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_multi_line_string()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_multi_line_string(), "The 'MultiLineString' property 'NullableMultiLineStringType.MultiLineS");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_multi_line_string()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_multi_line_string(), "The 'MultiLineString' property 'NullableMultiLineStringType.MultiLineS");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_multi_line_string_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_multi_line_string_as_GeoJson(), "The 'MultiLineString' property 'NullableMultiLineStringType.MultiLineS");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_multi_line_string_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_multi_line_string_as_GeoJson(), "The 'MultiLineString' property 'NullableMultiLineStringType.MultiLineS");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_physical_address_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_physical_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_physical_address_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_physical_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_physical_address_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_physical_address_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_physical_address_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_physical_address_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_point()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_point(), "The 'Point' property 'NullablePointType.Point' could not be mapped bec");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_point()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_point(), "The 'Point' property 'NullablePointType.Point' could not be mapped bec");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_point_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_point_as_GeoJson(), "The 'Point' property 'NullablePointType.Point' could not be mapped bec");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_point_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_point_as_GeoJson(), "The 'Point' property 'NullablePointType.Point' could not be mapped bec");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_polygon()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_polygon(), "The 'Polygon' property 'NullablePolygonType.Polygon' could not be mapp");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_polygon()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_polygon(), "The 'Polygon' property 'NullablePolygonType.Polygon' could not be mapp");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_nullable_polygon_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_polygon_as_GeoJson(), "The 'Polygon' property 'NullablePolygonType.Polygon' could not be mapp");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_nullable_polygon_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_nullable_polygon_as_GeoJson(), "The 'Polygon' property 'NullablePolygonType.Polygon' could not be mapp");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_sbyte_JSON_values(global::System.SByte? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_sbyte_JSON_values(global::System.SByte? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_sbyte_as_string_JSON_values(global::System.SByte? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_sbyte_as_string_JSON_values(global::System.SByte? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_sbyte_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_sbyte_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_sbyte_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_sbyte_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_sbyte_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_short_JSON_values(short? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_short_JSON_values(short? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_short_as_string_JSON_values(short? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_short_as_string_JSON_values(short? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_short_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_short_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_short_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_short_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_short_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_string_as_string_JSON_values(string value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_string_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_string_as_string_JSON_values(string value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_string_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_uint_JSON_values(global::System.UInt32? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_uint_JSON_values(global::System.UInt32? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_uint_as_string_JSON_values(global::System.UInt32? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_uint_as_string_JSON_values(global::System.UInt32? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_uint_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_uint_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_uint_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_uint_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_uint_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ulong_JSON_values(global::System.UInt64? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ulong_JSON_values(global::System.UInt64? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ulong_as_string_JSON_values(global::System.UInt64? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ulong_as_string_JSON_values(global::System.UInt64? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ulong_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ulong_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ulong_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ulong_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ulong_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ushort_JSON_values(global::System.UInt16? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ushort_JSON_values(global::System.UInt16? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ushort_as_string_JSON_values(global::System.UInt16? value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ushort_as_string_JSON_values(global::System.UInt16? value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ushort_enum_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ushort_enum_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_nullable_ushort_enum_as_string_JSON_values(object value, string json)
        => SpecFails.AsyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails (some data rows): NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_nullable_ushort_enum_as_string_JSON_values(object value, string json)
        => SpecFails.SyncSomeRows<global::System.NullReferenceException>(() => base.Can_read_write_nullable_ushort_enum_as_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_physical_address_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_physical_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_physical_address_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_physical_address_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point(), "The 'Point' property 'PointType.Point' could not be mapped because the");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point(), "The 'Point' property 'PointType.Point' could not be mapped because the");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point_as_GeoJson(), "The 'Point' property 'PointType.Point' could not be mapped because the");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point_as_GeoJson(), "The 'Point' property 'PointType.Point' could not be mapped because the");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point_with_M()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_M(), "The 'Point' property 'PointMType.PointM' could not be mapped because t");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point_with_M()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_M(), "The 'Point' property 'PointMType.PointM' could not be mapped because t");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point_with_M_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_M_as_GeoJson(), "The 'Point' property 'PointMType.PointM' could not be mapped because t");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point_with_M_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_M_as_GeoJson(), "The 'Point' property 'PointMType.PointM' could not be mapped because t");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point_with_Z()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z(), "The 'Point' property 'PointZType.PointZ' could not be mapped because t");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point_with_Z()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z(), "The 'Point' property 'PointZType.PointZ' could not be mapped because t");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point_with_Z_and_M()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z_and_M(), "The 'Point' property 'PointZMType.PointZM' could not be mapped because");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point_with_Z_and_M()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z_and_M(), "The 'Point' property 'PointZMType.PointZM' could not be mapped because");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point_with_Z_and_M_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z_and_M_as_GeoJson(), "The 'Point' property 'PointZMType.PointZM' could not be mapped because");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point_with_Z_and_M_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z_and_M_as_GeoJson(), "The 'Point' property 'PointZMType.PointZM' could not be mapped because");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_point_with_Z_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z_as_GeoJson(), "The 'Point' property 'PointZType.PointZ' could not be mapped because t");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_point_with_Z_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_point_with_Z_as_GeoJson(), "The 'Point' property 'PointZType.PointZ' could not be mapped because t");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_polygon()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_polygon(), "The 'Polygon' property 'PolygonType.Polygon' could not be mapped becau");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_polygon()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_polygon(), "The 'Polygon' property 'PolygonType.Polygon' could not be mapped becau");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_polygon_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_as_GeoJson(), "The 'Polygon' property 'PolygonType.Polygon' could not be mapped becau");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_polygon_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_as_GeoJson(), "The 'Polygon' property 'PolygonType.Polygon' could not be mapped becau");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_polygon_typed_as_geometry()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_geometry(), "The 'Geometry' property 'GeometryType.Geometry' could not be mapped be");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_polygon_typed_as_geometry()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_geometry(), "The 'Geometry' property 'GeometryType.Geometry' could not be mapped be");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_polygon_typed_as_geometry_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_geometry_as_GeoJson(), "The 'Geometry' property 'GeometryType.Geometry' could not be mapped be");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_polygon_typed_as_geometry_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_geometry_as_GeoJson(), "The 'Geometry' property 'GeometryType.Geometry' could not be mapped be");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_polygon_typed_as_nullable_geometry()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_nullable_geometry(), "The 'Geometry' property 'NullableGeometryType.Geometry' could not be m");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_polygon_typed_as_nullable_geometry()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_nullable_geometry(), "The 'Geometry' property 'NullableGeometryType.Geometry' could not be m");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override Task Can_read_write_polygon_typed_as_nullable_geometry_as_GeoJson()
        => SpecFails.Async<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_nullable_geometry_as_GeoJson(), "The 'Geometry' property 'NullableGeometryType.Geometry' could not be m");
#elif EF8
    // Fails: Property type (collection of non-primitive/exotic element, geometry) cannot be mapped by the provider EF-X316
    public override void Can_read_write_polygon_typed_as_nullable_geometry_as_GeoJson()
        => SpecFails.Sync<global::System.InvalidOperationException>(() => base.Can_read_write_polygon_typed_as_nullable_geometry_as_GeoJson(), "The 'Geometry' property 'NullableGeometryType.Geometry' could not be m");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_sbyte_JSON_values(global::System.SByte value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_sbyte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_sbyte_JSON_values(global::System.SByte value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_sbyte_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_sbyte_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum8 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_sbyte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_sbyte_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum8 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_sbyte_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_short_JSON_values(short value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_short_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_short_JSON_values(short value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_short_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_short_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum16 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_short_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_short_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.Enum16 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_short_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_string_JSON_values(string value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_string_JSON_values(string value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_string_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_uint_JSON_values(global::System.UInt32 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_uint_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_uint_JSON_values(global::System.UInt32 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_uint_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_uint_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU32 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_uint_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_uint_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU32 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_uint_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_ulong_JSON_values(global::System.UInt64 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_ulong_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_ulong_JSON_values(global::System.UInt64 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_ulong_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_ulong_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU64 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_ulong_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_ulong_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU64 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_ulong_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_ushort_JSON_values(global::System.UInt16 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_ushort_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_ushort_JSON_values(global::System.UInt16 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_ushort_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif

#if (EF10 || EF11) || EF9
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override Task Can_read_write_ushort_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU16 value, string json)
        => SpecFails.Async<global::System.NullReferenceException>(() => base.Can_read_write_ushort_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#elif EF8
    // Fails: NullReferenceException in JSON type round-trip (not triaged) EF-X317
    public override void Can_read_write_ushort_enum_JSON_values(global::Microsoft.EntityFrameworkCore.JsonTypesTestBase.EnumU16 value, string json)
        => SpecFails.Sync<global::System.NullReferenceException>(() => base.Can_read_write_ushort_enum_JSON_values(value, json), "Object reference not set to an instance of an object.");
#endif
    // </generated>

#if !EF8
    private ITestStoreFactory? _testStoreFactory;

#if EF11
    protected override ITestStoreFactory NonSharedTestStoreFactory
#else
    protected override ITestStoreFactory TestStoreFactory
#endif
        => _testStoreFactory!;

#if EF11
    protected override string NonSharedStoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("JsonTypes");
#else
    protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("JsonTypes");
#endif

    public override async LifetimeTask InitializeAsync()
    {
        var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
        _testStoreFactory = new MongoTestStoreFactory(server);

        await base.InitializeAsync();
    }
#endif
}
