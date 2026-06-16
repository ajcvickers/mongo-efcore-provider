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

#if DEBUG
using System.Collections.Concurrent;

namespace MongoDB.EntityFrameworkCore.Query.Layout;

/// <summary>
/// DEBUG-only diagnostic sink for Phase 1a of the shaper layout rework: records every place the authored
/// DocumentLayout's resolved field disagrees with the legacy field-location heuristic, so a single test
/// run yields a complete divergence census. Removed when the heuristic is deleted in Phase 1b.
/// </summary>
internal static class LayoutHeuristicDivergence
{
    public static readonly ConcurrentQueue<string> Divergences = new();

    public static void Record(string navigation, string layoutLeaf, string heuristicField)
        => Divergences.Enqueue($"nav='{navigation}' layoutLeaf='{layoutLeaf}' heuristic='{heuristicField}'");
}
#endif
