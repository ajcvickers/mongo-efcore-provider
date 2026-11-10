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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Infrastructure;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Query;

/// <summary>
/// EF-X702: <c>DateTimeOffset.TimeOfDay</c> must keep the sub-millisecond ticks. The stored <c>DateTime</c> sub-field is a BSON
/// date (whole milliseconds, and a pre-1970 value is rounded the wrong way), so the time of day is computed from the stored <c>Ticks</c>.
/// </summary>
public class DateTimeOffsetTimeOfDayTests(TemporaryDatabaseFixture database)
    : IClassFixture<TemporaryDatabaseFixture>
{
    public static TheoryData<DateTimeOffset> Values => new()
    {
        // Before the epoch, where the millisecond date used to round up to the next millisecond.
        new DateTimeOffset(11, 5, 3, 12, 0, 0, 0, 200, default).Add(TimeSpan.FromTicks(4)),
        new DateTimeOffset(1998, 5, 4, 15, 30, 10, 123, 456, new TimeSpan(1, 30, 0)).Add(TimeSpan.FromTicks(4)),
        new DateTimeOffset(2024, 3, 15, 13, 45, 30, 250, TimeSpan.FromHours(-5)).Add(TimeSpan.FromTicks(7)),
        new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2000, 1, 1, 23, 59, 59, 999, 999, TimeSpan.FromHours(14)).Add(TimeSpan.FromTicks(9))
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void TimeOfDay_keeps_every_tick_native(DateTimeOffset value)
        => AssertTimeOfDay(value, MongoQueryMode.Native);

    [Theory]
    [MemberData(nameof(Values))]
    public void TimeOfDay_keeps_every_tick_native_only(DateTimeOffset value)
        => AssertTimeOfDay(value, MongoQueryMode.NativeOnly);

    [Fact]
    public void Optional_TimeOfDay_is_exact()
    {
        var value = new DateTimeOffset(11, 5, 3, 12, 0, 0, 0, 200, default).Add(TimeSpan.FromTicks(4));
        var collection = database.CreateCollection<Entity>();
        collection.InsertOne(new Entity { Id = 1, Value = value, Optional = value });

        using var db = CreateContext(collection, MongoQueryMode.NativeOnly);
        Assert.Equal(value.TimeOfDay, db.Entities.Select(e => e.Optional!.Value.TimeOfDay).Single());
    }

    private void AssertTimeOfDay(DateTimeOffset value, MongoQueryMode mode)
    {
        var collection = database.CreateCollection<Entity>(name: $"{mode}_{value.Ticks}");
        collection.InsertOne(new Entity { Id = 1, Value = value });

        using var db = CreateContext(collection, mode);
        Assert.Equal(value.TimeOfDay, db.Entities.Select(e => e.Value.TimeOfDay).Single());
    }

    private static SingleEntityDbContext<Entity> CreateContext(IMongoCollection<Entity> collection, MongoQueryMode mode)
        => SingleEntityDbContext.Create(collection, optionsBuilderAction: b =>
        {
            b.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning));
            new MongoDbContextOptionsBuilder(b).UseQueryMode(mode);
        });

    public class Entity
    {
        public int Id { get; set; }
        public DateTimeOffset Value { get; set; }
        public DateTimeOffset? Optional { get; set; }
    }
}
