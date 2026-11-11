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

// Server-free coverage of the slicing that bounds phase 2's _id $in filter: the functional tests cannot reach the real 50,000 ceiling.

#if !EF8

using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.UnitTests.Storage;

public static class BulkIdBatchingTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(7, 3)]
    public static void Splits_into_the_expected_number_of_batches_in_order(int idCount, int expectedBatches)
    {
        var ids = Enumerable.Range(0, idCount).Select(i => (BsonValue)i).ToList();

        var batches = MongoBulkOperationExecutor.BatchIds(ids, batchSize: 3).ToList();

        Assert.Equal(expectedBatches, batches.Count);
        Assert.Equal(ids, batches.SelectMany(b => b).ToList());
        Assert.All(batches, b => Assert.InRange(b.Count, 1, 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public static void A_non_positive_batch_size_is_rejected_rather_than_looping_forever(int batchSize)
        => Assert.Throws<ArgumentOutOfRangeException>(() => MongoBulkOperationExecutor.BatchIds([], batchSize).ToList());

    [Fact]
    public static void The_override_is_restored_on_dispose()
    {
        using (MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(2))
        {
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => MongoBulkOperationExecutor.OverrideMaxIdsPerBulkCommand(0));
    }
}

#endif
