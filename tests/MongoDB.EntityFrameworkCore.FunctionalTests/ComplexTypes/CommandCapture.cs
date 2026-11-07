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

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

/// <summary>
/// A client on the test server that records every command it sends, so tests can inspect the exact
/// <c>update</c>/<c>delete</c> commands SaveChanges issues.
/// </summary>
internal sealed class CommandCapture : IDisposable
{
    private readonly List<BsonDocument> _commands = [];
    private readonly MongoClient _client;

    public CommandCapture(TemporaryDatabaseFixture database)
    {
        var settings = MongoClientSettings.FromConnectionString(database.TestServer.ConnectionString);
        settings.ClusterConfigurator = cb => cb.Subscribe<CommandStartedEvent>(e =>
        {
            lock (_commands)
            {
                _commands.Add(new BsonDocument("name", e.CommandName).Add("command", e.Command.DeepClone()));
            }
        });
        _client = new MongoClient(settings);
    }

    public IMongoCollection<T> Collection<T>(IMongoCollection<T> collection)
        => _client.GetDatabase(collection.CollectionNamespace.DatabaseNamespace.DatabaseName)
            .GetCollection<T>(collection.CollectionNamespace.CollectionName);

    public void Clear()
    {
        lock (_commands)
        {
            _commands.Clear();
        }
    }

    public IReadOnlyList<BsonDocument> Named(string name)
    {
        lock (_commands)
        {
            return _commands.Where(c => c["name"] == name).Select(c => c["command"].AsBsonDocument).ToList();
        }
    }

    /// <summary>The <c>$set</c> document of the single update statement sent since the last <see cref="Clear"/>.</summary>
    public BsonDocument SingleSet()
    {
        var update = Assert.Single(Named("update"));
        var statement = Assert.Single(update["updates"].AsBsonArray).AsBsonDocument;
        var u = statement["u"].AsBsonDocument;
        Assert.Equal(new[] { "$set" }, u.Names.ToArray());
        return u["$set"].AsBsonDocument;
    }

    public void Dispose()
        => _client.Dispose();
}
