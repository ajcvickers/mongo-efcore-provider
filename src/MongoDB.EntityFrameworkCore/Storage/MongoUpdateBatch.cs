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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace MongoDB.EntityFrameworkCore.Storage;

internal class MongoUpdateBatch(string collectionName, List<MongoUpdate> updates)
{
    public string CollectionName { get => collectionName; }
    public List<MongoUpdate> Updates { get => updates; }

    public long Modified => updates.Count(u => u.Model.ModelType == WriteModelType.UpdateOne);
    public long Inserts => updates.Count(u => u.Model.ModelType == WriteModelType.InsertOne);
    public long Deletes => updates.Count(u => u.Model.ModelType == WriteModelType.DeleteOne);

    /// <summary>
    /// Groups <paramref name="updates"/> into one batch per collection, in the order the collections first appear, and
    /// orders the writes within each batch: deletes, then updates, then inserts. The order within each kind is kept.
    /// </summary>
    /// <remarks>
    /// EF Core supplies the entries Added, then Modified, then Deleted (<c>StateManager.GetEntriesToSave</c>), whatever
    /// order they were tracked in, so left alone an insert reaches the server before a delete that frees the unique-indexed
    /// value it takes, and before an update that moves such a value away. The relational providers sort their commands for
    /// the same reason, see <c>CommandBatchPreparer.TopologicalSort</c>. Deletes first, then updates, then inserts is the
    /// simple form of that. It covers a delete that frees a value for an insert or an update, and an update that moves a
    /// value away for an insert. An update that moves a value away for another update is ordered by
    /// <see cref="MongoUniqueValueOrderer"/>.
    /// The grouping must cover the whole list, not just consecutive updates for a collection, because EF Core's order
    /// can separate a collection's deletes from its inserts by writes to other collections.
    /// </remarks>
    public static IReadOnlyList<MongoUpdateBatch> CreateBatches(IEnumerable<MongoUpdate> updates)
    {
        var collectionNames = new List<string>();
        var writesByCollection = new Dictionary<string, (List<MongoUpdate> Deletes, List<MongoUpdate> Updates, List<MongoUpdate> Inserts)>();

        foreach (var update in updates)
        {
            var collectionName = update.Entry.EntityType.GetCollectionName();
            if (!writesByCollection.TryGetValue(collectionName, out var writes))
            {
                writes = ([], [], []);
                writesByCollection.Add(collectionName, writes);
                collectionNames.Add(collectionName);
            }

            switch (update.Model.ModelType)
            {
                case WriteModelType.DeleteOne:
                    writes.Deletes.Add(update);
                    break;
                case WriteModelType.InsertOne:
                    writes.Inserts.Add(update);
                    break;
                default:
                    writes.Updates.Add(update);
                    break;
            }
        }

        var batches = new List<MongoUpdateBatch>(collectionNames.Count);
        foreach (var collectionName in collectionNames)
        {
            var (deletes, modifications, inserts) = writesByCollection[collectionName];
            deletes.AddRange(MongoUniqueValueOrderer.Order(modifications));
            deletes.AddRange(inserts);
            batches.Add(new MongoUpdateBatch(collectionName, deletes));
        }

        return batches;
    }
}
