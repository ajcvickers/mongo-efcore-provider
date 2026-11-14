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

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Update;

namespace MongoDB.EntityFrameworkCore.Storage;

/// <summary>
/// Orders the updates of one collection so that an update that moves a unique-indexed value away from a document comes
/// before the update that gives another document that value (#102). Without it a chain such as "document 1 changes from
/// A to B, document 2 changes from B to C" succeeds or fails with a duplicate key error depending on the order the
/// documents were tracked in.
/// </summary>
/// <remarks>
/// This is the part of <c>CommandBatchPreparer.TopologicalSort</c> in the relational providers that orders a modified row
/// that frees a unique value before the modified row that takes it, done on the values of unique indexes in the model. It is
/// best effort: values are compared as EF Core compares keys, so what MongoDB treats as equal but EF Core does not (a
/// case-insensitive collation, an array that holds the value) is not seen, and the order is then as before. Keys with a
/// <see langword="null"/> are skipped, because the provider relaxes unique indexes for <see langword="null"/>. A cycle,
/// such as swapping two values, cannot be ordered, so its updates keep the order they had and the duplicate key error is
/// reported by the server.
/// </remarks>
internal static class MongoUniqueValueOrderer
{
    public static List<MongoUpdate> Order(List<MongoUpdate> updates)
    {
        if (updates.Count < 2)
        {
            return updates;
        }

        var comparer = new IndexKeyComparer();
        Dictionary<IndexKey, List<int>>? frees = null;
        Dictionary<IndexKey, List<int>>? takes = null;

        for (var i = 0; i < updates.Count; i++)
        {
            var entry = updates[i].Entry;
            if (updates[i].Replaced is { } removed)
            {
                AddReplacement(entry, removed, i, comparer, ref frees, ref takes);
                continue;
            }

            if (entry.EntityState != EntityState.Modified)
            {
                continue;
            }

            foreach (var index in entry.EntityType.GetIndexes())
            {
                if (!index.IsUnique || !TryGetKeys(entry, index, out var original, out var current))
                {
                    continue;
                }

                if (original != null)
                {
                    Add(ref frees, new IndexKey(index, original), i, comparer);
                }

                if (current != null)
                {
                    Add(ref takes, new IndexKey(index, current), i, comparer);
                }
            }
        }

        if (frees == null || takes == null)
        {
            return updates;
        }

        // An edge from the update that frees a value to the update that takes it.
        HashSet<(int From, int To)>? edges = null;
        foreach (var (key, takers) in takes)
        {
            if (!frees.TryGetValue(key, out var givers))
            {
                continue;
            }

            foreach (var giver in givers)
            {
                foreach (var taker in takers)
                {
                    if (giver != taker)
                    {
                        (edges ??= []).Add((giver, taker));
                    }
                }
            }
        }

        return edges == null ? updates : Sort(updates, edges);
    }

    /// <summary>
    /// A document replaced (#109) gives up the unique values the removed entity had and takes those of the entity added, so
    /// like a modified one it must come after the update that frees a value it takes, and before the one that takes a value it
    /// frees. A value it keeps is neither.
    /// </summary>
    private static void AddReplacement(
        IUpdateEntry added,
        IUpdateEntry removed,
        int position,
        IndexKeyComparer comparer,
        ref Dictionary<IndexKey, List<int>>? frees,
        ref Dictionary<IndexKey, List<int>>? takes)
    {
        var originals = new List<IndexKey>();
        foreach (var index in removed.EntityType.GetIndexes())
        {
            if (index.IsUnique && TryGetValues(removed, index, original: true) is { } values)
            {
                originals.Add(new IndexKey(index, values));
            }
        }

        foreach (var index in added.EntityType.GetIndexes())
        {
            if (!index.IsUnique || TryGetValues(added, index, original: false) is not { } values)
            {
                continue;
            }

            var key = new IndexKey(index, values);
            if (originals.RemoveAll(o => comparer.Equals(o, key)) == 0)
            {
                Add(ref takes, key, position, comparer);
            }
        }

        foreach (var key in originals)
        {
            Add(ref frees, key, position, comparer);
        }
    }

    /// <summary>
    /// The values of <paramref name="index"/> of <paramref name="entry"/>, or <see langword="null"/> if there is no value to
    /// conflict on (a <see langword="null"/> in the key, a member of a complex type, or no original values).
    /// </summary>
    private static object?[]? TryGetValues(IUpdateEntry entry, IIndex index, bool original)
    {
        var values = new object?[index.Properties.Count];
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (AsProperty(index.Properties[i]) is not { } property)
                {
                    return null;
                }

                values[i] = original ? entry.GetOriginalValue(property) : entry.GetCurrentValue(property);
            }
        }
        catch (InvalidOperationException)
        {
            // The original values are not available, see TryGetKeys. This is only an ordering aid, so go without.
            return null;
        }

        return Array.IndexOf(values, null) >= 0 ? null : values;
    }

    private static void Add(ref Dictionary<IndexKey, List<int>>? map, IndexKey key, int position, IndexKeyComparer comparer)
    {
        map ??= new Dictionary<IndexKey, List<int>>(comparer);
        if (!map.TryGetValue(key, out var positions))
        {
            map.Add(key, positions = []);
        }

        positions.Add(position);
    }

    /// <summary>
    /// Kahn's algorithm that takes the earliest position that is ready, so updates that are not ordered by an edge keep
    /// their order. Updates that are never ready, because they are in a cycle, are added last in their original order.
    /// </summary>
    private static List<MongoUpdate> Sort(List<MongoUpdate> updates, HashSet<(int From, int To)> edges)
    {
        var predecessors = new int[updates.Count];
        var successors = new List<int>?[updates.Count];
        foreach (var (from, to) in edges)
        {
            predecessors[to]++;
            (successors[from] ??= []).Add(to);
        }

        var ready = new PriorityQueue<int, int>();
        for (var i = 0; i < updates.Count; i++)
        {
            if (predecessors[i] == 0)
            {
                ready.Enqueue(i, i);
            }
        }

        var ordered = new List<MongoUpdate>(updates.Count);
        var placed = new bool[updates.Count];
        while (ready.TryDequeue(out var position, out _))
        {
            ordered.Add(updates[position]);
            placed[position] = true;

            if (successors[position] is { } next)
            {
                foreach (var successor in next)
                {
                    if (--predecessors[successor] == 0)
                    {
                        ready.Enqueue(successor, successor);
                    }
                }
            }
        }

        if (ordered.Count < updates.Count)
        {
            for (var i = 0; i < updates.Count; i++)
            {
                if (!placed[i])
                {
                    ordered.Add(updates[i]);
                }
            }
        }

        return ordered;
    }

    /// <summary>
    /// Gets the values of <paramref name="index"/> that <paramref name="entry"/> had and has now, when they are different.
    /// Either is <see langword="null"/> if it has no value to conflict on (a <see langword="null"/> in the key).
    /// </summary>
    private static bool TryGetKeys(IUpdateEntry entry, IIndex index, out object?[]? original, out object?[]? current)
    {
        original = null;
        current = null;

        var properties = index.Properties;
        var comparers = new ValueComparer[properties.Count];
        var anyModified = false;
        for (var i = 0; i < properties.Count; i++)
        {
            // An index can include a member of a complex type (EF11), which has no key comparer; leave such an index alone.
            if (AsProperty(properties[i]) is not { } property)
            {
                return false;
            }

            comparers[i] = property.GetKeyValueComparer();
            anyModified |= entry.IsModified(property);
        }

        if (!anyModified)
        {
            return false;
        }

        try
        {
            var originalValues = new object?[properties.Count];
            var currentValues = new object?[properties.Count];
            var changed = false;
            for (var i = 0; i < properties.Count; i++)
            {
                originalValues[i] = entry.GetOriginalValue(properties[i]);
                currentValues[i] = entry.GetCurrentValue(properties[i]);
                changed |= !comparers[i].Equals(originalValues[i], currentValues[i]);
            }

            if (!changed)
            {
                return false;
            }

            original = Array.IndexOf(originalValues, null) >= 0 ? null : originalValues;
            current = Array.IndexOf(currentValues, null) >= 0 ? null : currentValues;
            return original != null || current != null;
        }
        catch (InvalidOperationException)
        {
            // The original values are not available (some change-tracking proxies do not keep them for every property).
            // This is only an ordering aid, so go without.
            return false;
        }
    }

    private static IProperty? AsProperty(IPropertyBase property)
        => property as IProperty;

    // Only keys of indexes whose members are all properties are ever stored (see TryGetKeys).
    private static ValueComparer KeyComparerFor(IPropertyBase property)
        => ((IProperty)property).GetKeyValueComparer();

    private readonly record struct IndexKey(IIndex Index, object?[] Values);

    private sealed class IndexKeyComparer : IEqualityComparer<IndexKey>
    {
        public bool Equals(IndexKey x, IndexKey y)
        {
            if (!ReferenceEquals(x.Index, y.Index) || x.Values.Length != y.Values.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Values.Length; i++)
            {
                if (!KeyComparerFor(x.Index.Properties[i]).Equals(x.Values[i], y.Values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(IndexKey key)
        {
            var hash = new HashCode();
            hash.Add(key.Index);
            for (var i = 0; i < key.Values.Length; i++)
            {
                hash.Add(KeyComparerFor(key.Index.Properties[i]).GetHashCode(key.Values[i]!));
            }

            return hash.ToHashCode();
        }
    }
}
