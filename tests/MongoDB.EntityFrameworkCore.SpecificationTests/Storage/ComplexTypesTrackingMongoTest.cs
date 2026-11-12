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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Storage;

// EF11 only: the shared-type property-bag entity in the EF11 tracking model has a List<string> property that the provider cannot
// map, so the fixture fails to initialize (see #22).
#if !EF11
public class ComplexTypesTrackingMongoTest(ComplexTypesTrackingMongoFixture fixture)
    : ComplexTypesTrackingTestBase<ComplexTypesTrackingMongoFixture>(fixture)
{
    // EF's ExecuteWithStrategyInTransaction begins a transaction on one context and passes it to a
    // *second* context via UseTransaction; this hook enlists that second context in the same MongoDB
    // transaction so its saves participate in, and are rolled back with, the outer transaction. The
    // second context only participates, so disposing it does not end the outer context's transaction.
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction);

#if EF10
    public override async Task Can_change_state_from_Deleted_with_complex_collection(EntityState newState, bool async)
    {
        await base.Can_change_state_from_Deleted_with_complex_collection(newState, async);
    }

    public override async Task Can_change_state_from_Deleted_with_complex_record_collection(EntityState newState, bool async)
    {
        await base.Can_change_state_from_Deleted_with_complex_record_collection(newState, async);
    }

    public override async Task Can_change_state_from_Deleted_with_complex_field_collection(EntityState newState, bool async)
    {
        await base.Can_change_state_from_Deleted_with_complex_field_collection(newState, async);
    }

    public override async Task Can_change_state_from_Deleted_with_complex_field_record_collection(EntityState newState, bool async)
    {
        await base.Can_change_state_from_Deleted_with_complex_field_record_collection(newState, async);
    }

    public override async Task Can_save_default_values_in_optional_complex_property_with_multiple_properties(bool async)
    {
        await base.Can_save_default_values_in_optional_complex_property_with_multiple_properties(async);
    }

    public override async Task Can_null_complex_property_with_default_values_and_multiple_properties(bool async)
    {
        await base.Can_null_complex_property_with_default_values_and_multiple_properties(async);
    }
#endif
}
#endif
