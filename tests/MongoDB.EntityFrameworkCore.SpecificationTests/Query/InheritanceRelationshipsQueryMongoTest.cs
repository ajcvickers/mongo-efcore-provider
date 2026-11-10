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
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.EntityFrameworkCore.SpecificationTests.Utilities;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class InheritanceRelationshipsQueryMongoTest : InheritanceRelationshipsQueryTestBase<InheritanceRelationshipsQueryMongoTest.InheritanceRelationshipsQueryMongoFixture>
{
    public InheritanceRelationshipsQueryMongoTest(InheritanceRelationshipsQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override void Changes_in_derived_related_entities_are_detected()
    {
        base.Changes_in_derived_related_entities_are_detected();

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase_BaseParentId__id" } }, { "$match" : { "Name" : "Derived1(4)" } }, { "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase" } }, { "$limit" : 2 }
""");
    }

    public override void Entity_can_make_separate_relationships_with_base_type_and_derived_type_both()
    {
        base.Entity_can_make_separate_relationships_with_base_type_and_derived_type_both();

        AssertMql();
    }

    public override async Task Include_reference_with_inheritance(bool async)
    {
        await base.Include_reference_with_inheritance(async);

        AssertMql(
            """
BaseEntities.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseReferencesOnBase", "localField" : "_outer._id", "foreignField" : "BaseParentId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_with_inheritance_reverse(bool async)
    {
        await base.Include_reference_with_inheritance_reverse(async);

        AssertMql(
            """
BaseReferencesOnBase.{ "$lookup" : { "from" : "BaseEntities", "localField" : "BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    public override async Task Include_self_reference_with_inheritance(bool async)
    {
        await base.Include_self_reference_with_inheritance(async);

        AssertMql(
            """
BaseEntities.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "_outer._id", "foreignField" : "BaseId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_self_reference_with_inheritance_reverse(bool async)
    {
        await base.Include_self_reference_with_inheritance_reverse(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "_outer.BaseId", "foreignField" : "_id", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_with_inheritance_with_filter(bool async)
    {
        await base.Include_reference_with_inheritance_with_filter(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseReferencesOnBase", "localField" : "_outer._id", "foreignField" : "BaseParentId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_with_inheritance_with_filter_reverse(bool async)
    {
        await base.Include_reference_with_inheritance_with_filter_reverse(async);

        AssertMql(
            """
BaseReferencesOnBase.{ "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    public override async Task Include_reference_without_inheritance(bool async)
    {
        await base.Include_reference_without_inheritance(async);

        AssertMql(
            """
BaseEntities.{ "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ReferencesOnBase", "localField" : "_outer._id", "foreignField" : "ParentId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_without_inheritance_reverse(bool async)
    {
        await base.Include_reference_without_inheritance_reverse(async);

        AssertMql(
            """
ReferencesOnBase.{ "$lookup" : { "from" : "BaseEntities", "localField" : "ParentId", "foreignField" : "_id", "as" : "_lookup_Parent" } }, { "$set" : { "_lookup_Parent" : { "$arrayElemAt" : ["$_lookup_Parent", 0] } } }
""");
    }

    public override async Task Include_reference_without_inheritance_with_filter(bool async)
    {
        await base.Include_reference_without_inheritance_with_filter(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ReferencesOnBase", "localField" : "_outer._id", "foreignField" : "ParentId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_without_inheritance_with_filter_reverse(bool async)
    {
        await base.Include_reference_without_inheritance_with_filter_reverse(async);

        AssertMql(
            """
ReferencesOnBase.{ "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "ParentId", "foreignField" : "_id", "as" : "_lookup_Parent" } }, { "$set" : { "_lookup_Parent" : { "$arrayElemAt" : ["$_lookup_Parent", 0] } } }
""");
    }

    public override async Task Include_collection_with_inheritance(bool async)
    {
        await base.Include_collection_with_inheritance(async);

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase_BaseParentId__id" } }, { "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase" } }
""");
    }

    public override async Task Include_collection_with_inheritance_reverse(bool async)
    {
        await base.Include_collection_with_inheritance_reverse(async);

        AssertMql(
            """
BaseCollectionsOnBase.{ "$lookup" : { "from" : "BaseEntities", "localField" : "BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    public override async Task Include_collection_with_inheritance_with_filter(bool async)
    {
        await base.Include_collection_with_inheritance_with_filter(async);

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase_BaseParentId__id" } }, { "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase" } }
""");
    }

    public override async Task Include_collection_with_inheritance_with_filter_reverse(bool async)
    {
        await base.Include_collection_with_inheritance_with_filter_reverse(async);

        AssertMql(
            """
BaseCollectionsOnBase.{ "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    public override async Task Include_collection_without_inheritance(bool async)
    {
        await base.Include_collection_without_inheritance(async);

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "CollectionsOnBase", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_CollectionOnBase_ParentId__id" } }, { "$lookup" : { "from" : "CollectionsOnBase", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_CollectionOnBase" } }
""");
    }

    public override async Task Include_collection_without_inheritance_reverse(bool async)
    {
        await base.Include_collection_without_inheritance_reverse(async);

        AssertMql(
            """
CollectionsOnBase.{ "$lookup" : { "from" : "BaseEntities", "localField" : "ParentId", "foreignField" : "_id", "as" : "_lookup_Parent" } }, { "$set" : { "_lookup_Parent" : { "$arrayElemAt" : ["$_lookup_Parent", 0] } } }
""");
    }

    public override async Task Include_collection_without_inheritance_with_filter(bool async)
    {
        await base.Include_collection_without_inheritance_with_filter(async);

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "CollectionsOnBase", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_CollectionOnBase_ParentId__id" } }, { "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "CollectionsOnBase", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_CollectionOnBase" } }
""");
    }

    public override async Task Include_collection_without_inheritance_with_filter_reverse(bool async)
    {
        await base.Include_collection_without_inheritance_with_filter_reverse(async);

        AssertMql(
            """
CollectionsOnBase.{ "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "ParentId", "foreignField" : "_id", "as" : "_lookup_Parent" } }, { "$set" : { "_lookup_Parent" : { "$arrayElemAt" : ["$_lookup_Parent", 0] } } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived1(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived1(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$lookup" : { "from" : "BaseReferencesOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseReferenceOnBase" } }, { "$unwind" : { "path" : "$_lookup_BaseReferenceOnBase", "preserveNullAndEmptyArrays" : true } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived2(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived2(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseReferencesOnDerived", "localField" : "_outer._id", "foreignField" : "BaseParentId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived4(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived4(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseReferencesOnDerived", "localField" : "_outer._id", "foreignField" : "DerivedInheritanceRelationshipEntityId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived_reverse(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived_reverse(async);

        AssertMql(
            """
BaseReferencesOnDerived.{ "$lookup" : { "from" : "BaseEntities", "let" : { "localField" : "$BaseParentId" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$_id", "$$localField"] } } }, { "$match" : { "_t" : { "$in" : ["DerivedInheritanceRelationshipEntity"] } } }], "as" : "_lookup_BaseParent" } }, { "$unwind" : { "path" : "$_lookup_BaseParent", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_BaseParent" : { "$ifNull" : ["$_lookup_BaseParent", null] } } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived_with_filter1(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived_with_filter1(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity", "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "BaseReferencesOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseReferenceOnBase" } }, { "$unwind" : { "path" : "$_lookup_BaseReferenceOnBase", "preserveNullAndEmptyArrays" : true } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived_with_filter2(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived_with_filter2(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseReferencesOnDerived", "localField" : "_outer._id", "foreignField" : "BaseParentId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived_with_filter4(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived_with_filter4(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "BaseReferencesOnDerived", "localField" : "_outer._id", "foreignField" : "DerivedInheritanceRelationshipEntityId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_with_inheritance_on_derived_with_filter_reverse(bool async)
    {
        await base.Include_reference_with_inheritance_on_derived_with_filter_reverse(async);

        AssertMql(
            """
BaseReferencesOnDerived.{ "$match" : { "Name" : { "$ne" : "Bar" } } }, { "$lookup" : { "from" : "BaseEntities", "let" : { "localField" : "$BaseParentId" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$_id", "$$localField"] } } }, { "$match" : { "_t" : { "$in" : ["DerivedInheritanceRelationshipEntity"] } } }], "as" : "_lookup_BaseParent" } }, { "$unwind" : { "path" : "$_lookup_BaseParent", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_BaseParent" : { "$ifNull" : ["$_lookup_BaseParent", null] } } }
""");
    }

    public override async Task Include_reference_without_inheritance_on_derived1(bool async)
    {
        await base.Include_reference_without_inheritance_on_derived1(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$lookup" : { "from" : "ReferencesOnBase", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_ReferenceOnBase" } }, { "$unwind" : { "path" : "$_lookup_ReferenceOnBase", "preserveNullAndEmptyArrays" : true } }
""");
    }

    public override async Task Include_reference_without_inheritance_on_derived2(bool async)
    {
        await base.Include_reference_without_inheritance_on_derived2(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$project" : { "_outer" : "$$ROOT", "_id" : 0 } }, { "$lookup" : { "from" : "ReferencesOnDerived", "localField" : "_outer._id", "foreignField" : "ParentId", "as" : "_inner" } }, { "$unwind" : { "path" : "$_inner", "preserveNullAndEmptyArrays" : true } }, { "$project" : { "_outer" : "$_outer", "_inner" : "$_inner", "_id" : 0 } }
""");
    }

    public override async Task Include_reference_without_inheritance_on_derived_reverse(bool async)
    {
        await base.Include_reference_without_inheritance_on_derived_reverse(async);

        AssertMql(
            """
ReferencesOnDerived.{ "$lookup" : { "from" : "BaseEntities", "let" : { "localField" : "$ParentId" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$_id", "$$localField"] } } }, { "$match" : { "_t" : { "$in" : ["DerivedInheritanceRelationshipEntity"] } } }], "as" : "_lookup_Parent" } }, { "$unwind" : { "path" : "$_lookup_Parent", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_Parent" : { "$ifNull" : ["$_lookup_Parent", null] } } }
""");
    }

    public override async Task Include_collection_with_inheritance_on_derived1(bool async)
    {
        await base.Include_collection_with_inheritance_on_derived1(async);

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase_BaseParentId__id" } }, { "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase" } }
""");
    }

    public override async Task Include_collection_with_inheritance_on_derived2(bool async)
    {
        await base.Include_collection_with_inheritance_on_derived2(async);

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "BaseCollectionsOnDerived", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_BaseCollectionOnDerived_ParentId__id" } }, { "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$lookup" : { "from" : "BaseCollectionsOnDerived", "localField" : "_id", "foreignField" : "ParentId", "as" : "_lookup_BaseCollectionOnDerived" } }
""");
    }

    public override async Task Include_collection_with_inheritance_on_derived3(bool async)
    {
        await base.Include_collection_with_inheritance_on_derived3(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$lookup" : { "from" : "BaseCollectionsOnDerived", "let" : { "localField" : "$_id" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$DerivedInheritanceRelationshipEntityId", "$$localField"] } } }, { "$match" : { "_t" : { "$in" : ["DerivedCollectionOnDerived"] } } }], "as" : "_lookup_DerivedCollectionOnDerived" } }
""");
    }

    public override async Task Include_collection_with_inheritance_on_derived_reverse(bool async)
    {
        await base.Include_collection_with_inheritance_on_derived_reverse(async);

        AssertMql(
            """
BaseCollectionsOnDerived.{ "$lookup" : { "from" : "BaseEntities", "let" : { "localField" : "$ParentId" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$_id", "$$localField"] } } }, { "$match" : { "_t" : { "$in" : ["DerivedInheritanceRelationshipEntity"] } } }], "as" : "_lookup_BaseParent" } }, { "$unwind" : { "path" : "$_lookup_BaseParent", "preserveNullAndEmptyArrays" : true } }, { "$set" : { "_lookup_BaseParent" : { "$ifNull" : ["$_lookup_BaseParent", null] } } }
""");
    }

    // Fails: wrong results: the query returns a different number of rows than expected (not yet run in isolation) EF-X530
    public override Task Nested_include_with_inheritance_reference_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_reference(async));

    // Fails: wrong results: the query returns a different number of rows than expected (not yet run in isolation) EF-X530
    public override Task Nested_include_with_inheritance_reference_reference_on_base(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_reference_on_base(async));

    public override async Task Nested_include_with_inheritance_reference_reference_reverse(bool async)
    {
        await base.Nested_include_with_inheritance_reference_reference_reverse(async);

        AssertMql(
            """
NestedReferences.{ "$lookup" : { "from" : "BaseReferencesOnBase", "localField" : "ParentReferenceId", "foreignField" : "_id", "as" : "_lookup_ParentReference" } }, { "$set" : { "_lookup_ParentReference" : { "$arrayElemAt" : ["$_lookup_ParentReference", 0] } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "_lookup_ParentReference.BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    // Fails: document element missing for a required non-nullable property EF-X212
    public override Task Nested_include_with_inheritance_reference_collection(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_reference_collection(async), "Document element is missing");

    public override async Task Nested_include_with_inheritance_reference_collection_on_base(bool async)
    {
        await base.Nested_include_with_inheritance_reference_collection_on_base(async);

        AssertMql(
            """
BaseEntities.{ "$match" : { "_t" : "DerivedInheritanceRelationshipEntity" } }, { "$lookup" : { "from" : "BaseReferencesOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseReferenceOnBase" } }, { "$unwind" : { "path" : "$_lookup_BaseReferenceOnBase", "preserveNullAndEmptyArrays" : true } }, { "$lookup" : { "from" : "NestedCollections", "localField" : "_lookup_BaseReferenceOnBase._id", "foreignField" : "ParentReferenceId", "as" : "_lookup_BaseReferenceOnBase._lookup_NestedCollection" } }
""");
    }

    public override async Task Nested_include_with_inheritance_reference_collection_reverse(bool async)
    {
        await base.Nested_include_with_inheritance_reference_collection_reverse(async);

        AssertMql(
            """
NestedCollections.{ "$lookup" : { "from" : "BaseReferencesOnBase", "localField" : "ParentReferenceId", "foreignField" : "_id", "as" : "_lookup_ParentReference" } }, { "$set" : { "_lookup_ParentReference" : { "$arrayElemAt" : ["$_lookup_ParentReference", 0] } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "_lookup_ParentReference.BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    // Fails: hierarchy materialization fails: no discriminator matches the stored value EF-X519
    public override Task Nested_include_with_inheritance_collection_reference(bool async)
        => SpecFails.ThrowsAsync(() => base.Nested_include_with_inheritance_collection_reference(async), "No discriminators matched");

    public override async Task Nested_include_with_inheritance_collection_reference_reverse(bool async)
    {
        await base.Nested_include_with_inheritance_collection_reference_reverse(async);

        AssertMql(
            """
NestedReferences.{ "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "ParentCollectionId", "foreignField" : "_id", "as" : "_lookup_ParentCollection" } }, { "$set" : { "_lookup_ParentCollection" : { "$arrayElemAt" : ["$_lookup_ParentCollection", 0] } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "_lookup_ParentCollection.BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    public override async Task Nested_include_with_inheritance_collection_collection(bool async)
    {
        await base.Nested_include_with_inheritance_collection_collection(async);

        AssertMql(
            """
BaseEntities.{ "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "_id", "foreignField" : "BaseParentId", "as" : "_lookup_BaseCollectionOnBase_BaseParentId__id" } }, { "$lookup" : { "from" : "BaseCollectionsOnBase", "let" : { "localField" : "$_id" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$BaseParentId", "$$localField"] } } }, { "$lookup" : { "from" : "NestedCollections", "localField" : "_id", "foreignField" : "ParentCollectionId", "as" : "_lookup_NestedCollection" } }], "as" : "_lookup_BaseCollectionOnBase" } }
""");
    }

    public override async Task Nested_include_with_inheritance_collection_collection_reverse(bool async)
    {
        await base.Nested_include_with_inheritance_collection_collection_reverse(async);

        AssertMql(
            """
NestedCollections.{ "$lookup" : { "from" : "BaseCollectionsOnBase", "localField" : "ParentCollectionId", "foreignField" : "_id", "as" : "_lookup_ParentCollection" } }, { "$set" : { "_lookup_ParentCollection" : { "$arrayElemAt" : ["$_lookup_ParentCollection", 0] } } }, { "$lookup" : { "from" : "BaseEntities", "localField" : "_lookup_ParentCollection.BaseParentId", "foreignField" : "_id", "as" : "_lookup_BaseParent" } }, { "$set" : { "_lookup_BaseParent" : { "$arrayElemAt" : ["$_lookup_BaseParent", 0] } } }
""");
    }

    public override async Task Nested_include_collection_reference_on_non_entity_base(bool async)
    {
        await base.Nested_include_collection_reference_on_non_entity_base(async);

        AssertMql(
            """
ReferencedEntities.{ "$lookup" : { "from" : "PrincipalEntities", "let" : { "localField" : "$_id" }, "pipeline" : [{ "$match" : { "$expr" : { "$eq" : ["$ReferencedEntityId", "$$localField"] } } }, { "$lookup" : { "from" : "ReferencedEntities", "localField" : "ReferenceId", "foreignField" : "_id", "as" : "_lookup_Reference" } }, { "$unwind" : { "path" : "$_lookup_Reference", "preserveNullAndEmptyArrays" : true } }], "as" : "_lookup_Principals" } }
""");
    }

    // Fails: the query is not translated EF-X526
    public override Task Collection_projection_on_base_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Collection_projection_on_base_type(async), "could not be translated");

    // Fails: the query is not translated EF-X526
    public override Task Include_on_derived_type_with_queryable_Cast(bool async)
        => SpecFails.ThrowsAsync(() => base.Include_on_derived_type_with_queryable_Cast(async), "could not be translated");

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    public class InheritanceRelationshipsQueryMongoFixture : InheritanceRelationshipsQueryFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("InhRel");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // A type deriving from an entity type that is reached through a navigation before it is in the model is owned by default, which
            // breaks the explicit one-to-one between BaseInheritanceRelationshipEntity and its derived type (EF-X538).
            configurationBuilder.DoNotOwnTypes((type, model) => model.GetEntityTypes().Any(e => e.FindOwnership() == null && type.IsSubclassOf(e.ClrType)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // EF-X207: a unique index on a nullable foreign key treats every null as a duplicate.
            modelBuilder.RelaxUniqueForeignKeyIndexes();
        }

        public override Microsoft.EntityFrameworkCore.TestModels.InheritanceRelationshipsModel.InheritanceRelationshipsContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }
    }
}
