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
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class SpatialQueryMongoTest : SpatialQueryTestBase<SpatialQueryMongoTest.SpatialQueryMongoFixture>
{
    public SpatialQueryMongoTest(SpatialQueryMongoFixture fixture)
        : base(fixture)
    {
    }

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task SimpleSelect(bool async)
        => SpecFails.ThrowsAsync(() => base.SimpleSelect(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task WithConversion(bool async)
        => SpecFails.ThrowsAsync(() => base.WithConversion(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Area(bool async)
        => SpecFails.ThrowsAsync(() => base.Area(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task AsBinary(bool async)
        => SpecFails.ThrowsAsync(() => base.AsBinary(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task AsBinary_with_null_check(bool async)
        => SpecFails.ThrowsAsync(() => base.AsBinary_with_null_check(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task AsText(bool async)
        => SpecFails.ThrowsAsync(() => base.AsText(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Boundary(bool async)
        => SpecFails.ThrowsAsync(() => base.Boundary(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Buffer(bool async)
        => SpecFails.ThrowsAsync(() => base.Buffer(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Buffer_quadrantSegments(bool async)
        => SpecFails.ThrowsAsync(() => base.Buffer_quadrantSegments(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Centroid(bool async)
        => SpecFails.ThrowsAsync(() => base.Centroid(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Combine_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.Combine_aggregate(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task EnvelopeCombine_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.EnvelopeCombine_aggregate(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Contains(bool async)
        => SpecFails.ThrowsAsync(() => base.Contains(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task ConvexHull(bool async)
        => SpecFails.ThrowsAsync(() => base.ConvexHull(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task ConvexHull_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.ConvexHull_aggregate(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IGeometryCollection_Count(bool async)
        => SpecFails.ThrowsAsync(() => base.IGeometryCollection_Count(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task LineString_Count(bool async)
        => SpecFails.ThrowsAsync(() => base.LineString_Count(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task CoveredBy(bool async)
        => SpecFails.ThrowsAsync(() => base.CoveredBy(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Covers(bool async)
        => SpecFails.ThrowsAsync(() => base.Covers(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Crosses(bool async)
        => SpecFails.ThrowsAsync(() => base.Crosses(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Difference(bool async)
        => SpecFails.ThrowsAsync(() => base.Difference(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Dimension(bool async)
        => SpecFails.ThrowsAsync(() => base.Dimension(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Disjoint_with_cast_to_nullable(bool async)
        => SpecFails.ThrowsAsync(() => base.Disjoint_with_cast_to_nullable(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Disjoint_with_null_check(bool async)
        => SpecFails.ThrowsAsync(() => base.Disjoint_with_null_check(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_with_null_check(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_with_null_check(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_with_cast_to_nullable(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_with_cast_to_nullable(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_geometry(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_geometry(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_constant(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_constant(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_constant_srid_4326(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_constant_srid_4326(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_constant_lhs(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_constant_lhs(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_on_converted_geometry_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_on_converted_geometry_type_lhs(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type_lhs(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_on_converted_geometry_type_constant(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type_constant(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Distance_on_converted_geometry_type_constant_lhs(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type_constant_lhs(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task EndPoint(bool async)
        => SpecFails.ThrowsAsync(() => base.EndPoint(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Envelope(bool async)
        => SpecFails.ThrowsAsync(() => base.Envelope(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task EqualsTopologically(bool async)
        => SpecFails.ThrowsAsync(() => base.EqualsTopologically(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task ExteriorRing(bool async)
        => SpecFails.ThrowsAsync(() => base.ExteriorRing(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task GeometryType(bool async)
        => SpecFails.ThrowsAsync(() => base.GeometryType(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task GetGeometryN(bool async)
        => SpecFails.ThrowsAsync(() => base.GetGeometryN(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task GetGeometryN_with_null_argument(bool async)
        => SpecFails.ThrowsAsync(() => base.GetGeometryN_with_null_argument(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task GetInteriorRingN(bool async)
        => SpecFails.ThrowsAsync(() => base.GetInteriorRingN(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task GetPointN(bool async)
        => SpecFails.ThrowsAsync(() => base.GetPointN(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task InteriorPoint(bool async)
        => SpecFails.ThrowsAsync(() => base.InteriorPoint(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Intersection(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersection(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Intersects(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersects(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task ICurve_IsClosed(bool async)
        => SpecFails.ThrowsAsync(() => base.ICurve_IsClosed(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IMultiCurve_IsClosed(bool async)
        => SpecFails.ThrowsAsync(() => base.IMultiCurve_IsClosed(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IsEmpty(bool async)
        => SpecFails.ThrowsAsync(() => base.IsEmpty(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IsRing(bool async)
        => SpecFails.ThrowsAsync(() => base.IsRing(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IsSimple(bool async)
        => SpecFails.ThrowsAsync(() => base.IsSimple(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IsValid(bool async)
        => SpecFails.ThrowsAsync(() => base.IsValid(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IsWithinDistance(bool async)
        => SpecFails.ThrowsAsync(() => base.IsWithinDistance(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Item(bool async)
        => SpecFails.ThrowsAsync(() => base.Item(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Length(bool async)
        => SpecFails.ThrowsAsync(() => base.Length(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task M(bool async)
        => SpecFails.ThrowsAsync(() => base.M(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Normalized(bool async)
        => SpecFails.ThrowsAsync(() => base.Normalized(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task NumGeometries(bool async)
        => SpecFails.ThrowsAsync(() => base.NumGeometries(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task NumInteriorRings(bool async)
        => SpecFails.ThrowsAsync(() => base.NumInteriorRings(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task NumPoints(bool async)
        => SpecFails.ThrowsAsync(() => base.NumPoints(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task OgcGeometryType(bool async)
        => SpecFails.ThrowsAsync(() => base.OgcGeometryType(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Overlaps(bool async)
        => SpecFails.ThrowsAsync(() => base.Overlaps(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task PointOnSurface(bool async)
        => SpecFails.ThrowsAsync(() => base.PointOnSurface(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Relate(bool async)
        => SpecFails.ThrowsAsync(() => base.Relate(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Reverse(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task SRID(bool async)
        => SpecFails.ThrowsAsync(() => base.SRID(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task SRID_geometry(bool async)
        => SpecFails.ThrowsAsync(() => base.SRID_geometry(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task StartPoint(bool async)
        => SpecFails.ThrowsAsync(() => base.StartPoint(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task SymmetricDifference(bool async)
        => SpecFails.ThrowsAsync(() => base.SymmetricDifference(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task ToBinary(bool async)
        => SpecFails.ThrowsAsync(() => base.ToBinary(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task ToText(bool async)
        => SpecFails.ThrowsAsync(() => base.ToText(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Touches(bool async)
        => SpecFails.ThrowsAsync(() => base.Touches(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Union(bool async)
        => SpecFails.ThrowsAsync(() => base.Union(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Union_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.Union_aggregate(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Union_void(bool async)
        => SpecFails.ThrowsAsync(() => base.Union_void(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Within(bool async)
        => SpecFails.ThrowsAsync(() => base.Within(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task X(bool async)
        => SpecFails.ThrowsAsync(() => base.X(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Y(bool async)
        => SpecFails.ThrowsAsync(() => base.Y(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Z(bool async)
        => SpecFails.ThrowsAsync(() => base.Z(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task XY_with_collection_join(bool async)
        => SpecFails.ThrowsAsync(() => base.XY_with_collection_join(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IsEmpty_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.IsEmpty_equal_to_null(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task IsEmpty_not_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.IsEmpty_not_equal_to_null(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Intersects_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersects_equal_to_null(async));

    // Fails: NetTopologySuite spatial types are not supported (discovered as entity types) EF-X320
    public override Task Intersects_not_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersects_not_equal_to_null(async));

    public class SpatialQueryMongoFixture : SpatialQueryFixtureBase
    {
        private readonly InitFailureGuard _initFailure = new();
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("SpatialQ");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server);

            await _initFailure.RunAsync(base.InitializeAsync);
        }

        public override Microsoft.EntityFrameworkCore.TestModels.SpatialModel.SpatialContext CreateContext()
        {
            _initFailure.ThrowIfFailed();
            return base.CreateContext();
        }
    }
}
