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

// Server-free coverage for the transaction-unsupported classification used by two-phase bulk operations. ExecuteDeleteTests skips the
// end-to-end standalone-deployment test because CI runs a replica set; this predicate is a pure function and has no such constraint.

#if !EF8

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.UnitTests.Storage;

public static class BulkTransactionErrorClassificationTests
{
    // A real MongoCommandException whose Code is read from the result document by the driver, as it would be from a server reply.
    private static MongoCommandException CommandException(int code, string message)
    {
        var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        var command = new BsonDocument("find", "c");
        var result = new BsonDocument { { "ok", 0 }, { "code", code }, { "errmsg", message } };
        return new MongoCommandException(connectionId, message, command, result);
    }

    [Fact]
    public static void Classifies_NotSupportedException_from_provider_transaction_startup()
        => Assert.True(
            MongoBulkOperationExecutor.IsTransactionsUnsupported(
                new NotSupportedException("The current MongoDB deployment does not support transactions.")));

    [Fact]
    public static void Classifies_the_transaction_numbers_message_on_a_MongoCommandException()
    {
        var exception = CommandException(20, "Transaction numbers are only allowed on a replica set member or mongos");

        Assert.Equal(20, exception.Code);
        Assert.True(MongoBulkOperationExecutor.IsTransactionsUnsupported(exception));
    }

    [Fact]
    public static void Classifies_the_transaction_numbers_message_on_any_MongoException()
        => Assert.True(
            MongoBulkOperationExecutor.IsTransactionsUnsupported(
                new MongoException("Transaction numbers are only allowed on a replica set member or mongos")));

    [Fact]
    public static void Does_not_classify_an_unrelated_IllegalOperation()
    {
        // Code 20 is IllegalOperation in general. An unrelated illegal operation inside a transaction must not be relabelled as
        // "your deployment does not support transactions".
        var exception = CommandException(20, "Stage not supported inside of a multi-document transaction: $unionWith");

        Assert.Equal(20, exception.Code);
        Assert.False(MongoBulkOperationExecutor.IsTransactionsUnsupported(exception));
    }

    [Fact]
    public static void Does_not_classify_an_unrelated_NotSupportedException()
        => Assert.False(MongoBulkOperationExecutor.IsTransactionsUnsupported(new NotSupportedException("something else entirely")));

    [Fact]
    public static void Does_not_classify_an_unrelated_exception()
        => Assert.False(MongoBulkOperationExecutor.IsTransactionsUnsupported(new InvalidOperationException("something else entirely")));
}

#endif
