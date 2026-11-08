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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Loading;

public class LoadMongoTest(LoadMongoTest.LoadMongoFixture fixture)
    : LoadTestBase<LoadMongoTest.LoadMongoFixture>(fixture)
{
    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override Task Lazy_load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_collection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_many_to_one_reference_to_principal(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_dependent(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_dependent(state, queryTrackingBehavior, async);

    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent(state, queryTrackingBehavior);

    public override Task Lazy_load_many_to_one_reference_to_principal_null_FK(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal_null_FK(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK(state, queryTrackingBehavior, async);

    public override Task Lazy_load_collection_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_collection_not_found(state, queryTrackingBehavior, async);

    public override Task Lazy_load_many_to_one_reference_to_principal_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal_not_found(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal_not_found(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_dependent_not_found(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_dependent_not_found(state, queryTrackingBehavior, async);

    public override Task Lazy_load_collection_already_loaded(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_collection_already_loaded(state, deleteOrphansTiming, queryTrackingBehavior, async);

    public override Task Lazy_load_collection_already_partially_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_collection_already_partially_loaded(state, queryTrackingBehavior, async);

    public override Task Lazy_load_many_to_one_reference_to_principal_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal_already_loaded(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal_already_loaded(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_dependent_already_loaded(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded(state, deleteOrphansTiming, queryTrackingBehavior, async);

    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_alternate_key(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_alternate_key(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_alternate_key(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_alternate_key(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_alternate_key(state, queryTrackingBehavior);

    public override void Lazy_load_collection_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_shadow_fk(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_shadow_fk(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_shadow_fk(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_shadow_fk(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_shadow_fk(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_shadow_fk(state, queryTrackingBehavior);

    public override void Lazy_load_collection_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_composite_key(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_composite_key(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_composite_key(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_composite_key(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_composite_key(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_composite_key(state, queryTrackingBehavior);

    public override Task Lazy_load_collection_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_collection_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_many_to_one_reference_to_principal_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_dependent_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_dependent_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_many_to_one_reference_to_principal_null_FK_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal_null_FK_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_collection_not_found_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_collection_not_found_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_many_to_one_reference_to_principal_not_found_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal_not_found_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal_not_found_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal_not_found_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_dependent_not_found_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_dependent_not_found_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_collection_already_loaded_full_loader_constructor_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_collection_already_loaded_full_loader_constructor_injection(state, deleteOrphansTiming, queryTrackingBehavior, async);

    public override Task Lazy_load_many_to_one_reference_to_principal_already_loaded_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_many_to_one_reference_to_principal_already_loaded_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_principal_already_loaded_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_principal_already_loaded_full_loader_constructor_injection(state, queryTrackingBehavior, async);

    public override Task Lazy_load_one_to_one_reference_to_dependent_already_loaded_full_loader_constructor_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded_full_loader_constructor_injection(state, deleteOrphansTiming, queryTrackingBehavior, async);

    public override void Lazy_load_collection_already_partially_loaded_full_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_partially_loaded_full_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_not_found_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_not_found_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_not_found_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_not_found_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_not_found_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_not_found_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_not_found_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_not_found_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_already_loaded_delegate_loader_constructor_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_loaded_delegate_loader_constructor_injection(state, deleteOrphansTiming, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_already_loaded_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_already_loaded_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_already_loaded_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_already_loaded_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_already_loaded_delegate_loader_constructor_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded_delegate_loader_constructor_injection(state, deleteOrphansTiming, queryTrackingBehavior);

    public override void Lazy_load_collection_already_partially_loaded_delegate_loader_constructor_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_partially_loaded_delegate_loader_constructor_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_not_found_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_not_found_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_not_found_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_not_found_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_not_found_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_not_found_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_not_found_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_not_found_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_already_loaded_delegate_loader_property_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_loaded_delegate_loader_property_injection(state, deleteOrphansTiming, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_already_loaded_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_already_loaded_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_already_loaded_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_already_loaded_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_already_loaded_delegate_loader_property_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded_delegate_loader_property_injection(state, deleteOrphansTiming, queryTrackingBehavior);

    public override void Lazy_load_collection_already_partially_loaded_delegate_loader_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_partially_loaded_delegate_loader_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_null_FK_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_null_FK_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_null_FK_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_null_FK_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_not_found_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_not_found_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_not_found_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_not_found_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_not_found_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_not_found_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_not_found_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_not_found_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_collection_already_loaded_delegate_loader_with_state_property_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_loaded_delegate_loader_with_state_property_injection(state, deleteOrphansTiming, queryTrackingBehavior);

    public override void Lazy_load_many_to_one_reference_to_principal_already_loaded_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_many_to_one_reference_to_principal_already_loaded_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_principal_already_loaded_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_principal_already_loaded_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_load_one_to_one_reference_to_dependent_already_loaded_delegate_loader_with_state_property_injection(EntityState state, CascadeTiming deleteOrphansTiming, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_one_to_one_reference_to_dependent_already_loaded_delegate_loader_with_state_property_injection(state, deleteOrphansTiming, queryTrackingBehavior);

    public override void Lazy_load_collection_already_partially_loaded_delegate_loader_with_state_property_injection(EntityState state, QueryTrackingBehavior queryTrackingBehavior)
        => base.Lazy_load_collection_already_partially_loaded_delegate_loader_with_state_property_injection(state, queryTrackingBehavior);

    public override void Lazy_loading_uses_field_access_when_abstract_base_class_navigation()
        => base.Lazy_loading_uses_field_access_when_abstract_base_class_navigation();

    public override void Attached_references_to_principal_are_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_references_to_principal_are_marked_as_loaded(state, lazy);

    public override void Attached_references_to_dependents_are_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_references_to_dependents_are_marked_as_loaded(state, lazy);

    public override void Attached_collections_are_not_marked_as_loaded(EntityState state, bool lazy)
        => base.Attached_collections_are_not_marked_as_loaded(state, lazy);

    public override Task Load_collection(EntityState state, QueryTrackingBehavior queryTrackingBehavior, bool async)
        => base.Load_collection(state, queryTrackingBehavior, async);

    public override Task Load_many_to_one_reference_to_principal(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal(state, async);

    public override Task Load_one_to_one_reference_to_principal(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal(state, async);

    public override Task Load_one_to_one_reference_to_principal_when_NoTracking_behavior(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_when_NoTracking_behavior(state, async);

    public override Task Load_one_to_one_reference_to_dependent(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent(state, async);

    public override Task Load_one_to_one_PK_to_PK_reference_to_principal(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_principal(state, async);

    public override Task Load_one_to_one_PK_to_PK_reference_to_dependent(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_dependent(state, async);

    public override Task Load_collection_using_Query(EntityState state, bool async)
        => base.Load_collection_using_Query(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query(state, async);

    public override Task Load_one_to_one_PK_to_PK_reference_to_principal_using_Query(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_principal_using_Query(state, async);

    public override Task Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query(state, async);

    public override Task Load_many_to_one_reference_to_principal_null_FK(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_null_FK(state, async);

    public override Task Load_one_to_one_reference_to_principal_null_FK(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_null_FK(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_null_FK(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_null_FK(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_null_FK(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_null_FK(state, async);

    public override Task Load_collection_not_found(EntityState state, bool async)
        => base.Load_collection_not_found(state, async);

    public override Task Load_many_to_one_reference_to_principal_not_found(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_not_found(state, async);

    public override Task Load_one_to_one_reference_to_principal_not_found(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_not_found(state, async);

    public override Task Load_one_to_one_reference_to_dependent_not_found(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_not_found(state, async);

    public override Task Load_collection_using_Query_not_found(EntityState state, bool async)
        => base.Load_collection_using_Query_not_found(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_not_found(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_not_found(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_not_found(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_not_found(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_not_found(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query_not_found(state, async);

    public override Task Load_collection_already_loaded(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_collection_already_loaded(state, async, deleteOrphansTiming);

    public override Task Load_many_to_one_reference_to_principal_already_loaded(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_already_loaded(state, async);

    public override Task Load_one_to_one_reference_to_principal_already_loaded(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_one_to_one_reference_to_principal_already_loaded(state, async, deleteOrphansTiming);

    public override Task Load_one_to_one_reference_to_dependent_already_loaded(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_one_to_one_reference_to_dependent_already_loaded(state, async, deleteOrphansTiming);

    public override Task Load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_principal_already_loaded(state, async);

    public override Task Load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_dependent_already_loaded(state, async);

    public override Task Load_collection_using_Query_already_loaded(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_collection_using_Query_already_loaded(state, async, deleteOrphansTiming);

    public override Task Load_many_to_one_reference_to_principal_using_Query_already_loaded(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_already_loaded(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_already_loaded(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_already_loaded(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_already_loaded(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_one_to_one_reference_to_dependent_using_Query_already_loaded(state, async, deleteOrphansTiming);

    public override Task Load_one_to_one_PK_to_PK_reference_to_principal_using_Query_already_loaded(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_principal_using_Query_already_loaded(state, async);

    public override Task Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query_already_loaded(EntityState state, bool async)
        => base.Load_one_to_one_PK_to_PK_reference_to_dependent_using_Query_already_loaded(state, async);

    public override Task Load_collection_untyped(EntityState state, bool async)
        => base.Load_collection_untyped(state, async);

    public override Task Load_many_to_one_reference_to_principal_untyped(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_untyped(state, async);

    public override Task Load_one_to_one_reference_to_principal_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_untyped(state, async);

    public override Task Load_one_to_one_reference_to_dependent_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_untyped(state, async);

    public override Task Load_collection_using_Query_untyped(EntityState state, bool async)
        => base.Load_collection_using_Query_untyped(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_untyped(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_untyped(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_untyped(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query_untyped(state, async);

    public override Task Load_collection_not_found_untyped(EntityState state, bool async)
        => base.Load_collection_not_found_untyped(state, async);

    public override Task Load_many_to_one_reference_to_principal_not_found_untyped(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_not_found_untyped(state, async);

    public override Task Load_one_to_one_reference_to_principal_not_found_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_not_found_untyped(state, async);

    public override Task Load_one_to_one_reference_to_dependent_not_found_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_not_found_untyped(state, async);

    public override Task Load_collection_using_Query_not_found_untyped(EntityState state, bool async)
        => base.Load_collection_using_Query_not_found_untyped(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_not_found_untyped(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_not_found_untyped(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_not_found_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_not_found_untyped(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_not_found_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query_not_found_untyped(state, async);

    public override Task Load_collection_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_collection_already_loaded_untyped(state, async, deleteOrphansTiming);

    public override Task Load_many_to_one_reference_to_principal_already_loaded_untyped(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_already_loaded_untyped(state, async);

    public override Task Load_one_to_one_reference_to_principal_already_loaded_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_already_loaded_untyped(state, async);

    public override Task Load_one_to_one_reference_to_dependent_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_one_to_one_reference_to_dependent_already_loaded_untyped(state, async, deleteOrphansTiming);

    public override Task Load_collection_using_Query_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_collection_using_Query_already_loaded_untyped(state, async, deleteOrphansTiming);

    public override Task Load_many_to_one_reference_to_principal_using_Query_already_loaded_untyped(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_already_loaded_untyped(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_already_loaded_untyped(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_already_loaded_untyped(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_already_loaded_untyped(EntityState state, bool async, CascadeTiming deleteOrphansTiming)
        => base.Load_one_to_one_reference_to_dependent_using_Query_already_loaded_untyped(state, async, deleteOrphansTiming);

    public override Task Load_collection_alternate_key(EntityState state, bool async)
        => base.Load_collection_alternate_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_alternate_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_alternate_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_alternate_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_alternate_key(state, async);

    public override Task Load_one_to_one_reference_to_dependent_alternate_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_alternate_key(state, async);

    public override Task Load_collection_using_Query_alternate_key(EntityState state, bool async)
        => base.Load_collection_using_Query_alternate_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_alternate_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_alternate_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_alternate_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_alternate_key(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_alternate_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query_alternate_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_null_FK_alternate_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_null_FK_alternate_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_null_FK_alternate_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_null_FK_alternate_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_null_FK_alternate_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_null_FK_alternate_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_null_FK_alternate_key(state, async);

    public override Task Load_collection_shadow_fk(EntityState state, bool async)
        => base.Load_collection_shadow_fk(state, async);

    public override Task Load_many_to_one_reference_to_principal_shadow_fk(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_shadow_fk(state, async);

    public override Task Load_one_to_one_reference_to_principal_shadow_fk(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_shadow_fk(state, async);

    public override Task Load_one_to_one_reference_to_dependent_shadow_fk(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_shadow_fk(state, async);

    public override Task Load_collection_using_Query_shadow_fk(EntityState state, bool async)
        => base.Load_collection_using_Query_shadow_fk(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_shadow_fk(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_shadow_fk(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_shadow_fk(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_shadow_fk(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_shadow_fk(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query_shadow_fk(state, async);

    public override Task Load_many_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_null_FK_shadow_fk(state, async);

    public override Task Load_one_to_one_reference_to_principal_null_FK_shadow_fk(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_null_FK_shadow_fk(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_null_FK_shadow_fk(state, async);

    public override Task Load_collection_composite_key(EntityState state, bool async)
        => base.Load_collection_composite_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_composite_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_composite_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_composite_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_composite_key(state, async);

    public override Task Load_one_to_one_reference_to_dependent_composite_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_composite_key(state, async);

    public override Task Load_collection_using_Query_composite_key(EntityState state, bool async)
        => base.Load_collection_using_Query_composite_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_composite_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_composite_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_composite_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_composite_key(state, async);

    public override Task Load_one_to_one_reference_to_dependent_using_Query_composite_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_dependent_using_Query_composite_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_null_FK_composite_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_null_FK_composite_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_null_FK_composite_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_null_FK_composite_key(state, async);

    public override Task Load_many_to_one_reference_to_principal_using_Query_null_FK_composite_key(EntityState state, bool async)
        => base.Load_many_to_one_reference_to_principal_using_Query_null_FK_composite_key(state, async);

    public override Task Load_one_to_one_reference_to_principal_using_Query_null_FK_composite_key(EntityState state, bool async)
        => base.Load_one_to_one_reference_to_principal_using_Query_null_FK_composite_key(state, async);

    public override void Can_change_IsLoaded_flag_for_collection()
        => base.Can_change_IsLoaded_flag_for_collection();

    public override void Can_change_IsLoaded_flag_for_reference_only_if_null()
        => base.Can_change_IsLoaded_flag_for_reference_only_if_null();

    public override void Fixup_reference_after_FK_change_without_DetectChanges()
        => base.Fixup_reference_after_FK_change_without_DetectChanges();

    public override void Fixup_one_to_one_reference_after_FK_change_without_DetectChanges()
        => base.Fixup_one_to_one_reference_after_FK_change_without_DetectChanges();

    public override void Setting_navigation_to_null_is_detected_by_local_DetectChanges()
        => base.Setting_navigation_to_null_is_detected_by_local_DetectChanges();

#if (EF10 || EF11)
    public override Task Lazy_loading_is_thread_safe(bool noTracking, bool async)
        => base.Lazy_loading_is_thread_safe(noTracking, async);
#endif

    public class LoadMongoFixture : LoadFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("LoadTest");

        public TestServer TestServer { get; private set; }

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        public override async LifetimeTask InitializeAsync()
        {
            TestServer = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(TestServer);

            await base.InitializeAsync();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.MapSinglePropertyKeysToElementId(generateIntKeys: true); // EF-X201, EF-X205
        }
    }
}
