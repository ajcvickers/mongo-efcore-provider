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

#if !EF8 && !EF9
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace MongoDB.EntityFrameworkCore.UnitTests.Infrastructure;

// Complex collection CLR types (EF10), measured end to end (read, save, replace, in-place mutation, query in every mode;
// ComplexCollectionClrTypeTests in the functional tests): EF accepts T[] and ReadOnlyCollection<T> in the model, but
// cannot materialize or save them (EF's "Collection navigations cannot be arrays" / "Collection was of a fixed size" /
// "not possible to create a concrete instance"), so MongoModelValidator refuses them up front. EF itself rejects the
// types that do not implement IList<T>.
public static class ComplexCollectionClrTypeValidationTests
{
    [Theory]
    [InlineData(typeof(List<Tag>))]
    [InlineData(typeof(IList<Tag>))]
    [InlineData(typeof(ObservableCollection<Tag>))]
    [InlineData(typeof(Collection<Tag>))]
    [InlineData(typeof(TagList))]
    public static void Usable_collection_types_build(Type collectionType)
        => Assert.Null(Run(nameof(Build), collectionType));

    [Theory]
    [InlineData(typeof(Tag[]), "Tag[]")]
    [InlineData(typeof(ReadOnlyCollection<Tag>), "ReadOnlyCollection<Tag>")]
    public static void Fixed_size_or_unconstructible_collection_types_are_refused(Type collectionType, string displayName)
    {
        var ex = Run(nameof(Build), collectionType);
        Assert.IsType<NotSupportedException>(ex);
        Assert.Equal(
            $"Complex property 'Items' on entity type 'Holder<{displayName}>' is a complex collection of CLR type '{displayName}',"
            + " which EF Core cannot materialize or save: arrays and collection types without a public parameterless constructor"
            + " are not supported. Declare it as List<T>, IList<T>, or another mutable collection type with a public"
            + " parameterless constructor.",
            ex!.Message);
    }

    [Fact]
    public static void Array_typed_collection_nested_in_a_complex_property_is_refused()
    {
        var ex = Assert.Throws<NotSupportedException>(() =>
        {
            using var db = SingleEntityDbContext.Create<NestedHolder>(mb =>
                mb.Entity<NestedHolder>().ComplexProperty(h => h.Box, b => b.ComplexCollection(x => x.Items)));
            _ = db.Model;
        });
        Assert.StartsWith("Complex property 'Items' on complex type 'NestedHolder.Box#Box' is a complex collection of CLR type 'Tag[]'", ex.Message);
    }

    [Fact]
    public static void Array_typed_collection_nested_in_a_complex_collection_element_is_refused()
    {
        var ex = Assert.Throws<NotSupportedException>(() =>
        {
            using var db = SingleEntityDbContext.Create<NestedCollectionHolder>(mb =>
                mb.Entity<NestedCollectionHolder>().ComplexCollection(h => h.Boxes, b => b.ComplexCollection(x => x.Items)));
            _ = db.Model;
        });
        Assert.Contains("is a complex collection of CLR type 'Tag[]'", ex.Message);
    }

    // EF's own refusal comes first for types that are not IList<T> (unchanged, not the provider's).
    [Theory]
    [InlineData(typeof(ICollection<Tag>))]
    [InlineData(typeof(IReadOnlyList<Tag>))]
    [InlineData(typeof(IReadOnlyCollection<Tag>))]
    [InlineData(typeof(IEnumerable<Tag>))]
    [InlineData(typeof(HashSet<Tag>))]
    public static void Non_IList_collection_types_are_rejected_by_EF(Type collectionType)
    {
        var ex = Run(nameof(Build), collectionType);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("which does not implement 'IList<Tag>'", ex!.Message);
    }

    // A primitive array (not a complex collection) is unaffected.
    [Fact]
    public static void Primitive_array_property_is_unaffected()
    {
        using var db = SingleEntityDbContext.Create<PrimitiveArrayHolder>();
        Assert.NotNull(db.Model.FindEntityType(typeof(PrimitiveArrayHolder))!.FindProperty(nameof(PrimitiveArrayHolder.Names)));
    }

    static Exception? Run(string builder, Type collectionType)
    {
        try
        {
            typeof(ComplexCollectionClrTypeValidationTests)
                .GetMethod(builder, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(collectionType)
                .Invoke(null, null);
            return null;
        }
        catch (TargetInvocationException e)
        {
            return e.InnerException;
        }
    }

    static void Build<TC>() where TC : class, IEnumerable<Tag>
    {
        using var db = SingleEntityDbContext.Create<Holder<TC>>(mb => mb.Entity<Holder<TC>>().ComplexCollection(h => h.Items));
        _ = db.Model;
    }

    public class Tag { public string Label { get; set; } = ""; }
    public class TagList : List<Tag>;
    public class Holder<TC> where TC : class { public int _id { get; set; } public TC Items { get; set; } = null!; }
    public class Box { public string Name { get; set; } = ""; public Tag[] Items { get; set; } = []; }
    public class NestedHolder { public int _id { get; set; } public Box Box { get; set; } = null!; }
    public class NestedCollectionHolder { public int _id { get; set; } public List<Box> Boxes { get; set; } = []; }
    public class PrimitiveArrayHolder { public int _id { get; set; } public string[] Names { get; set; } = []; }
}
#endif
