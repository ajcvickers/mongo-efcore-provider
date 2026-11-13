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
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Infrastructure;
using MongoDB.EntityFrameworkCore.Storage;

namespace MongoDB.EntityFrameworkCore.UnitTests.Storage;

public static class MongoRetryingExecutionStrategyTests
{
    private static MongoException WithLabel(string label)
    {
        var exception = new MongoException("failure");
        exception.AddErrorLabel(label);
        return exception;
    }

    [Fact]
    public static void A_TransientTransactionError_is_retryable()
        => Assert.True(MongoRetryingExecutionStrategy.IsTransientTransactionError(WithLabel("TransientTransactionError")));

    [Fact]
    public static void A_wrapped_TransientTransactionError_is_retryable()
        => Assert.True(
            MongoRetryingExecutionStrategy.IsTransientTransactionError(
                new DbUpdateException("save failed", WithLabel("TransientTransactionError"))));

    [Theory]
    [InlineData("UnknownTransactionCommitResult")] // retried by the commit itself, never by re-running the unit of work
    [InlineData("RetryableWriteError")]
    [InlineData("SomethingElse")]
    public static void Other_labels_are_not_retryable(string label)
        => Assert.False(MongoRetryingExecutionStrategy.IsTransientTransactionError(WithLabel(label)));

    [Fact]
    public static void Errors_without_a_label_are_not_retryable()
    {
        Assert.False(MongoRetryingExecutionStrategy.IsTransientTransactionError(new MongoException("failure")));
        Assert.False(MongoRetryingExecutionStrategy.IsTransientTransactionError(new InvalidOperationException("failure")));
        Assert.False(MongoRetryingExecutionStrategy.IsTransientTransactionError(new DbUpdateConcurrencyException("conflict")));
        Assert.False(MongoRetryingExecutionStrategy.IsTransientTransactionError(new OperationCanceledException()));
    }

    private static IExecutionStrategy CreateStrategy(Action<MongoDbContextOptionsBuilder>? configure)
    {
        var builder = new DbContextOptionsBuilder()
            .UseMongoDB("mongodb://localhost:27017", "db", configure)
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
        using var context = new DbContext(builder.Options);
        return context.Database.CreateExecutionStrategy();
    }

    [Fact]
    public static void Retrying_is_off_by_default()
    {
        var strategy = CreateStrategy(null);

        Assert.IsType<NonRetryingExecutionStrategy>(strategy);
        Assert.False(strategy.RetriesOnFailure);
    }

    [Fact]
    public static void EnableRetryOnFailure_registers_the_retrying_strategy()
    {
        var strategy = CreateStrategy(o => o.EnableRetryOnFailure());

        Assert.IsType<MongoRetryingExecutionStrategy>(strategy);
        Assert.True(strategy.RetriesOnFailure);
    }

    [Fact]
    public static void EnableRetryOnFailure_with_zero_retries_turns_retrying_off()
        => Assert.IsType<NonRetryingExecutionStrategy>(CreateStrategy(o => o.EnableRetryOnFailure(0)));

    [Fact]
    public static void EnableRetryOnFailure_has_relational_defaults()
    {
        var extension = new MongoOptionsExtension().WithRetryOnFailure(6, TimeSpan.FromSeconds(30));

        Assert.Equal(6, extension.MaxRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(30), extension.MaxRetryDelay);
        Assert.Null(new MongoOptionsExtension().MaxRetryCount);
    }

    [Fact]
    public static void EnableRetryOnFailure_validates_its_arguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MongoOptionsExtension().WithRetryOnFailure(-1, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MongoOptionsExtension().WithRetryOnFailure(1, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public static void The_retry_settings_survive_cloning_with_other_options()
    {
        var extension = new MongoOptionsExtension()
            .WithRetryOnFailure(3, TimeSpan.FromSeconds(2))
            .WithQueryMode(MongoQueryMode.Native)
            .WithDatabaseName("db");

        Assert.Equal(3, extension.MaxRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(2), extension.MaxRetryDelay);
    }
}
