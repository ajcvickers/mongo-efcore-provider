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

using MongoDB.EntityFrameworkCore.Query.Layout;
using Xunit;

namespace MongoDB.EntityFrameworkCore.UnitTests.Query.Layout;

public static class DocumentLayoutTests
{
    [Fact]
    public static void Root_entity_absolute_path_is_empty_by_default()
    {
        var root = DocumentLayout.ForEntity(relativePath: "");
        Assert.Equal("", root.GetAbsolutePath());
    }

    [Fact]
    public static void Child_absolute_path_composes_relative_paths_down_the_tree()
    {
        var root = DocumentLayout.ForEntity(relativePath: "");
        var nav = root.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Customer"));
        var thenNav = nav.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Region"));

        Assert.Equal("_lookup_Customer._lookup_Region", thenNav.GetAbsolutePath());
    }

    [Fact]
    public static void Absolute_path_override_short_circuits_ancestor_composition()
    {
        // The _outer/_inner sibling case: root is "_outer" but the joined reference is a SIBLING
        // under "_inner", not nested under "_outer". The override pins the absolute path.
        var root = DocumentLayout.ForEntity(relativePath: "_outer");
        var nav = root.AddChild(DocumentLayout.ForNavigation(relativePath: "ignored"));
        nav.SetAbsolutePathOverride("_inner");
        var thenNav = nav.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Region"));

        Assert.Equal("_inner", nav.GetAbsolutePath());
        Assert.Equal("_inner._lookup_Region", thenNav.GetAbsolutePath());
    }

    [Fact]
    public static void Finalize_driver_join_mode_pins_root_to_outer_and_lone_reference_to_inner()
    {
        var root = DocumentLayout.ForEntity(relativePath: "");
        var nav = root.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Customer"));

        DocumentLayout.FinalizeDriverJoinMode(root, loneReference: nav);

        Assert.Equal("_outer", root.GetAbsolutePath());
        Assert.Equal("_inner", nav.GetAbsolutePath());
    }

    [Fact]
    public static void Driver_join_then_include_reference_composes_under_inner()
    {
        // The case the structural-resolution follow-up must preserve: a ThenInclude reference under the
        // driver-join lone reference reads from "_inner._lookup_<ThenNav>", composing through the pinned parent.
        var root = DocumentLayout.ForEntity(relativePath: "");
        var inner = root.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Customer"));
        var thenInclude = inner.AddChild(DocumentLayout.ForNavigation(relativePath: "_lookup_Region"));

        DocumentLayout.FinalizeDriverJoinMode(root, loneReference: inner);

        Assert.Equal("_inner._lookup_Region", thenInclude.GetAbsolutePath());
    }

    [Fact]
    public static void ForEntity_sets_entity_kind()
        => Assert.Equal(DocumentLayoutKind.Entity, DocumentLayout.ForEntity("").Kind);

    [Fact]
    public static void ForNavigation_sets_navigation_kind()
        => Assert.Equal(DocumentLayoutKind.Navigation, DocumentLayout.ForNavigation("_lookup_X").Kind);

    [Fact]
    public static void ForCollection_sets_collection_kind()
        => Assert.Equal(DocumentLayoutKind.Collection, DocumentLayout.ForCollection("_lookup_X").Kind);

    [Fact]
    public static void AddChild_links_parent_and_returns_child()
    {
        var root = DocumentLayout.ForEntity("");
        var child = DocumentLayout.ForCollection("_lookup_Orders");

        var returned = root.AddChild(child);

        Assert.Same(child, returned);
        Assert.Same(root, child.Parent);
        Assert.Contains(child, root.Children);
        Assert.Null(root.Parent);
    }
}
