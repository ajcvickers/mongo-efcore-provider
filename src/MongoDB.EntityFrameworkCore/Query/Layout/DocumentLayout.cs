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
using Microsoft.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.Query.Layout;

/// <summary>
/// A node in the document-layout tree authored by the query translator. Each node records WHERE in the
/// returned <c>BsonDocument</c> a shaped slot's value physically lives, so the shaper reads a recorded
/// path instead of re-deriving it. Paths are stored relative to the parent; <see cref="GetAbsolutePath"/>
/// composes them down the tree, unless a node carries an <see cref="SetAbsolutePathOverride"/> (used for
/// the driver-native LeftJoin <c>_outer</c>/<c>_inner</c> sibling layout, where a child is NOT nested
/// under its parent's path).
/// </summary>
internal sealed class DocumentLayout
{
    private readonly List<DocumentLayout> _children = [];
    private string? _absolutePathOverride;

    private DocumentLayout(DocumentLayoutKind kind, string relativePath)
    {
        Kind = kind;
        RelativePath = relativePath;
    }

    public DocumentLayoutKind Kind { get; }

    /// <summary>The BSON path of this node relative to its parent ("" means same document as parent).</summary>
    public string RelativePath { get; }

    public DocumentLayout? Parent { get; private set; }

    public IReadOnlyList<DocumentLayout> Children => _children;

    public INavigation? Navigation { get; private set; }

    public static DocumentLayout ForEntity(string relativePath)
        => new(DocumentLayoutKind.Entity, relativePath);

    public static DocumentLayout ForNavigation(string relativePath)
        => new(DocumentLayoutKind.Navigation, relativePath);

    public static DocumentLayout ForCollection(string relativePath)
        => new(DocumentLayoutKind.Collection, relativePath);

    public DocumentLayout WithNavigation(INavigation navigation)
    {
        Navigation = navigation;
        return this;
    }

    public DocumentLayout AddChild(DocumentLayout child)
    {
        child.Parent = this;
        _children.Add(child);
        return child;
    }

    /// <summary>
    /// Pin this node's absolute path, short-circuiting ancestor composition. Used for the driver-native
    /// LeftJoin layout where the joined reference is a sibling (<c>_inner</c>) of the root (<c>_outer</c>),
    /// not nested beneath it.
    /// </summary>
    public void SetAbsolutePathOverride(string absolutePath)
        => _absolutePathOverride = absolutePath;

    /// <summary>
    /// The absolute BSON path to this node, composed from ancestors' relative paths unless an override is set.
    /// </summary>
    public string GetAbsolutePath()
    {
        if (_absolutePathOverride != null)
        {
            return _absolutePathOverride;
        }

        if (Parent == null)
        {
            return RelativePath;
        }

        var parentPath = Parent.GetAbsolutePath();
        if (RelativePath.Length == 0)
        {
            return parentPath;
        }

        return parentPath.Length == 0 ? RelativePath : $"{parentPath}.{RelativePath}";
    }
}
