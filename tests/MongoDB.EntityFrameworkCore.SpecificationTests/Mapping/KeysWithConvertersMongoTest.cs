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

public class KeysWithConvertersMongoTest(KeysWithConvertersMongoTest.KeysWithConvertersMongoFixture fixture)
    : KeysWithConvertersTestBase<KeysWithConvertersMongoTest.KeysWithConvertersMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // <generated>
#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_bare_class_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_bare_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_bare_class_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_bare_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_bare_class_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_bare_class_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_bare_class_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_bare_class_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_class_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_class_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_class_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_class_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_class_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_class_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_class_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_comparable_struct_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_enumerable_class_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_enumerable_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_enumerable_class_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_enumerable_class_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_generic_comparable_struct_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_binary_key_and_required_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_binary_key_and_required_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_key_and_required_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_key_and_required_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_struct_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_struct_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_struct_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_optional_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override Task Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Async<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#elif EF8
    // Fails: Inserting entities with value-converted keys raises a duplicate _id error EF-X306
    public override void Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents_with_shadow_FK()
        => SpecFails.Sync<global::MongoDB.Driver.MongoBulkWriteException>(() => base.Can_insert_and_read_back_with_structural_struct_binary_key_and_required_dependents_with_shadow_FK(), "A bulk write operation resulted in one or more errors");
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_binary_struct_key()
        => base.Can_query_and_update_owned_entity_with_binary_struct_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_binary_struct_key()
        => base.Can_query_and_update_owned_entity_with_binary_struct_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_comparable_bytes_struct_key()
        => base.Can_query_and_update_owned_entity_with_comparable_bytes_struct_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_comparable_bytes_struct_key()
        => base.Can_query_and_update_owned_entity_with_comparable_bytes_struct_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_comparable_int_class_key()
        => base.Can_query_and_update_owned_entity_with_comparable_int_class_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_comparable_int_class_key()
        => base.Can_query_and_update_owned_entity_with_comparable_int_class_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_comparable_int_struct_key()
        => base.Can_query_and_update_owned_entity_with_comparable_int_struct_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_comparable_int_struct_key()
        => base.Can_query_and_update_owned_entity_with_comparable_int_struct_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_generic_comparable_bytes_struct_key()
        => base.Can_query_and_update_owned_entity_with_generic_comparable_bytes_struct_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_generic_comparable_bytes_struct_key()
        => base.Can_query_and_update_owned_entity_with_generic_comparable_bytes_struct_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_generic_comparable_int_class_key()
        => base.Can_query_and_update_owned_entity_with_generic_comparable_int_class_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_generic_comparable_int_class_key()
        => base.Can_query_and_update_owned_entity_with_generic_comparable_int_class_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_generic_comparable_int_struct_key()
        => base.Can_query_and_update_owned_entity_with_generic_comparable_int_struct_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_generic_comparable_int_struct_key()
        => base.Can_query_and_update_owned_entity_with_generic_comparable_int_struct_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_int_bare_class_key()
        => base.Can_query_and_update_owned_entity_with_int_bare_class_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_int_bare_class_key()
        => base.Can_query_and_update_owned_entity_with_int_bare_class_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_int_class_key()
        => base.Can_query_and_update_owned_entity_with_int_class_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_int_class_key()
        => base.Can_query_and_update_owned_entity_with_int_class_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_int_struct_key()
        => base.Can_query_and_update_owned_entity_with_int_struct_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_int_struct_key()
        => base.Can_query_and_update_owned_entity_with_int_struct_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_structural_generic_comparable_bytes_struct_key()
        => base.Can_query_and_update_owned_entity_with_structural_generic_comparable_bytes_struct_key();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_structural_generic_comparable_bytes_struct_key()
        => base.Can_query_and_update_owned_entity_with_structural_generic_comparable_bytes_struct_key();
#endif

#if (EF10 || EF11) || EF9
    public override Task Can_query_and_update_owned_entity_with_value_converter()
        => base.Can_query_and_update_owned_entity_with_value_converter();
#elif EF8
    public override void Can_query_and_update_owned_entity_with_value_converter()
        => base.Can_query_and_update_owned_entity_with_value_converter();
#endif
    // </generated>

    public class KeysWithConvertersMongoFixture : KeysWithConvertersFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("KeysWithConverters");

        public TestServer TestServer { get; private set; }

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);
            MongoSpecModelHelpers.MapSingleKeysToId(modelBuilder, generateIntegerKeys: true);
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder.ConfigureWarnings(w => w.Ignore(CoreEventId.CollectionWithoutComparer)));

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;
    }
}
