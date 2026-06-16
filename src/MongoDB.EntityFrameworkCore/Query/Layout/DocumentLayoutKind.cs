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

namespace MongoDB.EntityFrameworkCore.Query.Layout;

/// <summary>
/// The role a <see cref="DocumentLayout"/> node plays in the shaped result. Phase 1 uses
/// <see cref="Entity"/>, <see cref="Navigation"/>, and <see cref="Collection"/>; further kinds
/// (e.g. Grouping) are added when new query-feature shapes are implemented.
/// </summary>
internal enum DocumentLayoutKind
{
    /// <summary>The query-root entity.</summary>
    Entity,

    /// <summary>A reference (singleton) navigation joined into the document.</summary>
    Navigation,

    /// <summary>A collection navigation joined into the document.</summary>
    Collection
}
