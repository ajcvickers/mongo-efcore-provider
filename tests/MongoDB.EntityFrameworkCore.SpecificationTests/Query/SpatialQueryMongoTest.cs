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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.TestModels.SpatialModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using MongoDB.EntityFrameworkCore.FunctionalTests.Utilities;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace MongoDB.EntityFrameworkCore.SpecificationTests.Query;

public class SpatialQueryMongoTest : SpatialQueryTestBase<SpatialQueryMongoTest.SpatialQueryMongoFixture>
{
    public SpatialQueryMongoTest(SpatialQueryMongoFixture fixture)
        : base(fixture)
        => Fixture.TestMqlLoggerFactory.Clear();

    [ConditionalFact]
    public virtual void Check_all_tests_overridden()
        => TestHelpers.AssertAllMethodsOverridden(GetType());

    public override async Task SimpleSelect(bool async)
    {
        await base.SimpleSelect(async);

        AssertMql(
            """
PointEntity.
""",
            //
            """
LineStringEntity.
""",
            //
            """
PolygonEntity.
""",
            //
            """
MultiLineStringEntity.
""");
    }

    public override async Task WithConversion(bool async)
    {
        await base.WithConversion(async);

        AssertMql(
            """
GeoPointEntity.
""");
    }

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Area(bool async)
        => SpecFails.ThrowsAsync(() => base.Area(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task AsBinary(bool async)
        => SpecFails.ThrowsAsync(() => base.AsBinary(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task AsBinary_with_null_check(bool async)
        => SpecFails.ThrowsAsync(() => base.AsBinary_with_null_check(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task AsText(bool async)
        => SpecFails.ThrowsAsync(() => base.AsText(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Boundary(bool async)
        => SpecFails.ThrowsAsync(() => base.Boundary(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Buffer(bool async)
        => SpecFails.ThrowsAsync(() => base.Buffer(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Buffer_quadrantSegments(bool async)
        => SpecFails.ThrowsAsync(() => base.Buffer_quadrantSegments(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Centroid(bool async)
        => SpecFails.ThrowsAsync(() => base.Centroid(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Combine_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.Combine_aggregate(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task EnvelopeCombine_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.EnvelopeCombine_aggregate(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Contains(bool async)
        => SpecFails.ThrowsAsync(() => base.Contains(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task ConvexHull(bool async)
        => SpecFails.ThrowsAsync(() => base.ConvexHull(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task ConvexHull_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.ConvexHull_aggregate(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IGeometryCollection_Count(bool async)
        => SpecFails.ThrowsAsync(() => base.IGeometryCollection_Count(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task LineString_Count(bool async)
        => SpecFails.ThrowsAsync(() => base.LineString_Count(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task CoveredBy(bool async)
        => SpecFails.ThrowsAsync(() => base.CoveredBy(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Covers(bool async)
        => SpecFails.ThrowsAsync(() => base.Covers(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Crosses(bool async)
        => SpecFails.ThrowsAsync(() => base.Crosses(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Difference(bool async)
        => SpecFails.ThrowsAsync(() => base.Difference(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Dimension(bool async)
        => SpecFails.ThrowsAsync(() => base.Dimension(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Disjoint_with_cast_to_nullable(bool async)
        => SpecFails.ThrowsAsync(() => base.Disjoint_with_cast_to_nullable(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Disjoint_with_null_check(bool async)
        => SpecFails.ThrowsAsync(() => base.Disjoint_with_null_check(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_with_null_check(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_with_null_check(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_with_cast_to_nullable(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_with_cast_to_nullable(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_geometry(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_geometry(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_constant(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_constant(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_constant_srid_4326(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_constant_srid_4326(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_constant_lhs(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_constant_lhs(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_on_converted_geometry_type(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_on_converted_geometry_type_lhs(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type_lhs(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_on_converted_geometry_type_constant(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type_constant(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Distance_on_converted_geometry_type_constant_lhs(bool async)
        => SpecFails.ThrowsAsync(() => base.Distance_on_converted_geometry_type_constant_lhs(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task EndPoint(bool async)
        => SpecFails.ThrowsAsync(() => base.EndPoint(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Envelope(bool async)
        => SpecFails.ThrowsAsync(() => base.Envelope(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task EqualsTopologically(bool async)
        => SpecFails.ThrowsAsync(() => base.EqualsTopologically(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task ExteriorRing(bool async)
        => SpecFails.ThrowsAsync(() => base.ExteriorRing(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task GeometryType(bool async)
        => SpecFails.ThrowsAsync(() => base.GeometryType(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task GetGeometryN(bool async)
        => SpecFails.ThrowsAsync(() => base.GetGeometryN(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task GetGeometryN_with_null_argument(bool async)
        => SpecFails.ThrowsAsync(() => base.GetGeometryN_with_null_argument(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task GetInteriorRingN(bool async)
        => SpecFails.ThrowsAsync(() => base.GetInteriorRingN(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task GetPointN(bool async)
        => SpecFails.ThrowsAsync(() => base.GetPointN(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task InteriorPoint(bool async)
        => SpecFails.ThrowsAsync(() => base.InteriorPoint(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Intersection(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersection(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Intersects(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersects(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task ICurve_IsClosed(bool async)
        => SpecFails.ThrowsAsync(() => base.ICurve_IsClosed(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IMultiCurve_IsClosed(bool async)
        => SpecFails.ThrowsAsync(() => base.IMultiCurve_IsClosed(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IsEmpty(bool async)
        => SpecFails.ThrowsAsync(() => base.IsEmpty(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IsRing(bool async)
        => SpecFails.ThrowsAsync(() => base.IsRing(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IsSimple(bool async)
        => SpecFails.ThrowsAsync(() => base.IsSimple(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IsValid(bool async)
        => SpecFails.ThrowsAsync(() => base.IsValid(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IsWithinDistance(bool async)
        => SpecFails.ThrowsAsync(() => base.IsWithinDistance(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Item(bool async)
        => SpecFails.ThrowsAsync(() => base.Item(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Length(bool async)
        => SpecFails.ThrowsAsync(() => base.Length(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task M(bool async)
        => SpecFails.ThrowsAsync(() => base.M(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Normalized(bool async)
        => SpecFails.ThrowsAsync(() => base.Normalized(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task NumGeometries(bool async)
        => SpecFails.ThrowsAsync(() => base.NumGeometries(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task NumInteriorRings(bool async)
        => SpecFails.ThrowsAsync(() => base.NumInteriorRings(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task NumPoints(bool async)
        => SpecFails.ThrowsAsync(() => base.NumPoints(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task OgcGeometryType(bool async)
        => SpecFails.ThrowsAsync(() => base.OgcGeometryType(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Overlaps(bool async)
        => SpecFails.ThrowsAsync(() => base.Overlaps(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task PointOnSurface(bool async)
        => SpecFails.ThrowsAsync(() => base.PointOnSurface(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Relate(bool async)
        => SpecFails.ThrowsAsync(() => base.Relate(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Reverse(bool async)
        => SpecFails.ThrowsAsync(() => base.Reverse(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task SRID(bool async)
        => SpecFails.ThrowsAsync(() => base.SRID(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task SRID_geometry(bool async)
        => SpecFails.ThrowsAsync(() => base.SRID_geometry(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task StartPoint(bool async)
        => SpecFails.ThrowsAsync(() => base.StartPoint(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task SymmetricDifference(bool async)
        => SpecFails.ThrowsAsync(() => base.SymmetricDifference(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task ToBinary(bool async)
        => SpecFails.ThrowsAsync(() => base.ToBinary(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task ToText(bool async)
        => SpecFails.ThrowsAsync(() => base.ToText(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Touches(bool async)
        => SpecFails.ThrowsAsync(() => base.Touches(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Union(bool async)
        => SpecFails.ThrowsAsync(() => base.Union(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Union_aggregate(bool async)
        => SpecFails.ThrowsAsync(() => base.Union_aggregate(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Union_void(bool async)
        => SpecFails.ThrowsAsync(() => base.Union_void(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Within(bool async)
        => SpecFails.ThrowsAsync(() => base.Within(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task X(bool async)
        => SpecFails.ThrowsAsync(() => base.X(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Y(bool async)
        => SpecFails.ThrowsAsync(() => base.Y(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Z(bool async)
        => SpecFails.ThrowsAsync(() => base.Z(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task XY_with_collection_join(bool async)
        => SpecFails.ThrowsAsync(() => base.XY_with_collection_join(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IsEmpty_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.IsEmpty_equal_to_null(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task IsEmpty_not_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.IsEmpty_not_equal_to_null(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Intersects_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersects_equal_to_null(async));

    // Fails: NetTopologySuite types are stored as well-known binary through a value converter, so the spatial operation is not translated EF-X320
    public override Task Intersects_not_equal_to_null(bool async)
        => SpecFails.ThrowsAsync(() => base.Intersects_not_equal_to_null(async));

    private void AssertMql(params string[] expected)
        => Fixture.TestMqlLoggerFactory.AssertBaseline(expected);

    public class SpatialQueryMongoFixture : SpatialQueryFixtureBase
    {
        private ITestStoreFactory? _testStoreFactory;

        protected override ITestStoreFactory TestStoreFactory
            => _testStoreFactory!;

        protected override string StoreName { get; } = TestDatabaseNamer.GetUniqueDatabaseName("SpatialQ");

        public TestMqlLoggerFactory TestMqlLoggerFactory
            => (TestMqlLoggerFactory)ListLoggerFactory;

        public override async LifetimeTask InitializeAsync()
        {
            var server = await TestServer.GetOrInitializeTestServerAsync(MongoCondition.None);
            _testStoreFactory = new MongoTestStoreFactory(server, pinDefaultQueryMode: true);

            await base.InitializeAsync();
        }

        // EF-X320: the provider has no spatial support and would own the NetTopologySuite types, so they are stored as well-known binary.
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Properties<Geometry>().HaveConversion<WkbConverter<Geometry>, GeometryComparer<Geometry>>();
            configurationBuilder.Properties<Point>().HaveConversion<WkbConverter<Point>, GeometryComparer<Point>>();
            configurationBuilder.Properties<LineString>().HaveConversion<WkbConverter<LineString>, GeometryComparer<LineString>>();
            configurationBuilder.Properties<Polygon>().HaveConversion<WkbConverter<Polygon>, GeometryComparer<Polygon>>();
            configurationBuilder.Properties<MultiLineString>().HaveConversion<WkbConverter<MultiLineString>, GeometryComparer<MultiLineString>>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // The upstream converter yields a Point, which the provider cannot store: append the well-known binary conversion.
            modelBuilder.Entity<GeoPointEntity>().Property(e => e.Location)
                .HasConversion(new GeoPointConverter().ComposeWith(new WkbConverter<Point>()));
        }

        private sealed class GeometryComparer<TGeometry>()
            : ValueComparer<TGeometry>(
                (a, b) => a == null ? b == null : b != null && a.EqualsExact(b),
                g => g.GetHashCode(),
                g => (TGeometry)g.Copy())
            where TGeometry : Geometry;

        private sealed class WkbConverter<TGeometry>()
            : ValueConverter<TGeometry, byte[]>(
                g => new WKBWriter(ByteOrder.LittleEndian, true, true).Write(g),
                b => (TGeometry)new WKBReader().Read(b))
            where TGeometry : Geometry;

    }
}
