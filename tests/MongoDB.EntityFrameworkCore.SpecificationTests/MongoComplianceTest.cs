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
        // GraphUpdatesMongoTest and ProxyGraphUpdatesMongoTest exist but are abstract (not run) until contexts can enlist in a rolled-back
        // transaction, because the per-test drop and reseed costs minutes per EF version; see
        // https://github.com/ajcvickers/mongo-efcore-provider/issues/77. The compliance test only counts concrete derived classes.
        typeof(GraphUpdatesTestBase<>),
        typeof(ProxyGraphUpdatesTestBase<>),

        // Query test bases (present across EF8/EF9/EF10).
        typeof(ComplexTypeQueryTestBase<>), // Complex types: covered by the separate complex type work.
        typeof(FilteredQueryTestBase<>), // No tests of its own; covered by the ported NorthwindQueryFiltersQueryTestBase<>.
#if EF11
        // Complex type inheritance is not supported; tracked in https://github.com/ajcvickers/mongo-efcore-provider/issues/22
        typeof(InheritanceComplexTypesQueryTestBase<>),
#endif
        typeof(QueryTestBase<>),
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
        typeof(Microsoft.EntityFrameworkCore.BulkUpdates.BulkUpdatesTestBase<>), // Abstract infrastructure of the bulk-update suites: assert helpers only, no tests of its own.
        // NorthwindBulkUpdatesTestBase is implemented by NorthwindBulkUpdatesMongoTest: the supported
        // single-collection Where-scoped scalar cases, as well as OrderBy/Skip/Take/Distinct-scoped cases
        // (executed via the two-phase _id-projection path), call base and pass; everything outside that
        // subset (joins, set ops, GroupBy, SelectMany, navigations, non-entity projections,
        // multiple-collection updates) is not skipped there — each runs and asserts its actual failure
        // mode (translation failure or cross-DbSet rejection), tagged with a // Fails: <reason> comment.
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
        typeof(Microsoft.EntityFrameworkCore.ModelBuilding.ModelBuilderTest.ComplexCollectionTestBase), // Complex types: covered by the separate complex type work.
        // Associations (complex properties, navigations, owned navigations) and the remaining Translations bases
        // (Operators.*, Miscellaneous) are not yet ported.
        // abstract: covered by the derived Navigations/OwnedNavigations/ComplexProperties ports
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
#endif
    ];

    protected override Assembly TargetAssembly
        => typeof(MongoComplianceTest).Assembly;
}
