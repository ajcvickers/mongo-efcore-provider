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

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
#if EF11
using Microsoft.EntityFrameworkCore.Query.Inheritance;
#endif

namespace MongoDB.EntityFrameworkCore.SpecificationTests;

public class MongoComplianceTest : ComplianceTestBase
{
    // Test bases from Microsoft.EntityFrameworkCore.Specification.Tests that have not yet been
    // overridden for the MongoDB provider. As tests are implemented they should be removed from
    // this list so that the compliance test enforces continued coverage of newly added
    // specification tests.
    protected override ICollection<Type> IgnoredTestBases { get; } =
    [
        // Provider-wide non-query test bases (present across EF8/EF9/EF10).
        typeof(ComplexTypesTrackingTestBase<>), // Complex types: covered by the separate complex type work.

        // Query test bases (present across EF8/EF9/EF10).
        typeof(ComplexTypeQueryTestBase<>), // Complex types: covered by the separate complex type work.
        typeof(FilteredQueryTestBase<>), // No tests of its own; covered by the ported NorthwindQueryFiltersQueryTestBase<>.
        typeof(InheritanceRelationshipsQueryTestBase<>), // Blocked, EF-X538: model validation fails in the fixture, so no test body runs.
#if EF11
        // Complex type inheritance is not supported; tracked in https://github.com/ajcvickers/mongo-efcore-provider/issues/22
        typeof(InheritanceComplexTypesQueryTestBase<>),
#endif
        typeof(ManyToManyNoTrackingQueryTestBase<>), // Blocked, EF-X539: the join entity is configured as owned.
        typeof(ManyToManyQueryTestBase<>), // Blocked, EF-X539: the join entity is configured as owned.
        typeof(QueryTestBase<>),
        typeof(SpatialQueryTestBase<>),
#if !EF11
        typeof(AdHocComplexTypeQueryTestBase), // Complex types: covered by the separate complex type work.
#endif

#if EF8
        // EF8-only test bases.
        typeof(ManyToManyHeterogeneousQueryTestBase),
        typeof(SimpleQueryTestBase),
#endif

#if !EF8
        // Test bases added in EF9+.
        // Bulk ExecuteUpdate/ExecuteDelete ARE supported (EF9+), but only for single-collection,
        // Where-scoped queries with scalar setters. These EF conformance suites target owned-collection,
        // table-sharing, cross-collection/navigation, and join scenarios outside that supported subset,
        // so they remain ignored here; behavioral coverage lives in
        // FunctionalTests/Query/ExecuteDeleteTests.cs and ExecuteUpdateTests.cs.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.BulkUpdatesTestBase<>),
#if EF11
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.Inheritance.FiltersInheritanceBulkUpdatesTestBase<>),
#else
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.FiltersInheritanceBulkUpdatesTestBase<>),
#endif
#if EF11
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.Inheritance.InheritanceBulkUpdatesTestBase<>),
#else
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.InheritanceBulkUpdatesTestBase<>),
#endif
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.NonSharedModelBulkUpdatesTestBase),
        // NorthwindBulkUpdatesTestBase is implemented by NorthwindBulkUpdatesMongoTest: the supported
        // single-collection Where-scoped scalar cases, as well as OrderBy/Skip/Take/Distinct-scoped cases
        // (executed via the two-phase _id-projection path), call base and pass; everything outside that
        // subset (joins, set ops, GroupBy, SelectMany, navigations, non-entity projections,
        // multiple-collection updates) is not skipped there — each runs and asserts its actual failure
        // mode (translation failure or cross-DbSet rejection), tagged with a // Fails: <reason> comment.
        typeof(JsonQueryTestBase<>), // Blocked, EF-X548: seeding throws OverflowException, so every test fails in the fixture.
#if EF11
        typeof(Microsoft.EntityFrameworkCore.Scaffolding.CompiledModelTestBase), // Blocked on EF11, EF-X710: the generated compiled model crashes the test host.
#endif
        typeof(Microsoft.EntityFrameworkCore.ModelBuilding.ModelBuilderTest.ComplexTypeTestBase), // Complex types: covered by the separate complex type work.
#endif

#if EF9
        // Test bases that existed only in EF9.
        // Bulk ExecuteUpdate/ExecuteDelete ARE supported (EF9+), but only for single-collection,
        // Where-scoped queries with scalar setters. This complex-type conformance suite targets
        // scenarios outside that supported subset, so it remains ignored here; behavioral coverage
        // lives in FunctionalTests/Query/ExecuteDeleteTests.cs and ExecuteUpdateTests.cs.
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.ComplexTypeBulkUpdatesTestBase<>), // Complex types: covered by the separate complex type work.
#endif

#if !EF8 && !EF9
        // Test bases added in EF10+.
#if !EF11
        typeof(AdHocJsonQueryTestBase),
#endif
        // NorthwindFunctionsQueryMongoTest exists only for EF8 and EF9 — the EF10 build deliberately omits it.
        typeof(NorthwindFunctionsQueryTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.ModelBuilding.ModelBuilderTest.ComplexCollectionTestBase), // Complex types: covered by the separate complex type work.
        // Associations (complex properties, navigations, owned navigations) and the remaining Translations bases
        // (Operators.*, Miscellaneous) are not yet ported.
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.AssociationsBulkUpdateTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.AssociationsCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.AssociationsMiscellaneousTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.AssociationsPrimitiveCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.AssociationsProjectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.AssociationsSetOperationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.AssociationsStructuralEqualityTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties.ComplexPropertiesBulkUpdateTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties.ComplexPropertiesCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties.ComplexPropertiesMiscellaneousTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties.ComplexPropertiesPrimitiveCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties.ComplexPropertiesProjectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties.ComplexPropertiesSetOperationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.ComplexProperties.ComplexPropertiesStructuralEqualityTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.Navigations.NavigationsCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.Navigations.NavigationsIncludeTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.Navigations.NavigationsMiscellaneousTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.Navigations.NavigationsPrimitiveCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.Navigations.NavigationsProjectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.Navigations.NavigationsSetOperationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.Navigations.NavigationsStructuralEqualityTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.OwnedNavigations.OwnedNavigationsCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.OwnedNavigations.OwnedNavigationsMiscellaneousTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.OwnedNavigations.OwnedNavigationsPrimitiveCollectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.OwnedNavigations.OwnedNavigationsProjectionTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.OwnedNavigations.OwnedNavigationsSetOperationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Associations.OwnedNavigations.OwnedNavigationsStructuralEqualityTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Translations.MiscellaneousTranslationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Translations.Operators.ArithmeticOperatorTranslationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Translations.Operators.BitwiseOperatorTranslationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Translations.Operators.ComparisonOperatorTranslationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Translations.Operators.LogicalOperatorTranslationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Query.Translations.Operators.MiscellaneousOperatorTranslationsTestBase<>),
        typeof(Microsoft.EntityFrameworkCore.Types.TypeTestBase<,>),
#endif
    ];

    protected override Assembly TargetAssembly
        => typeof(MongoComplianceTest).Assembly;
}
