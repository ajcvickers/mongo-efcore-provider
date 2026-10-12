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

using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.EntityFrameworkCore.UnitTests.Infrastructure;

// The member/class/constructor/method attributes the provider does not support are rejected inside complex types exactly
// as they are inside owned types (MongoModelValidator.ValidateNoUnsupportedAttributesOrAnnotations): a complex type's
// members are serialized by the provider, so the attribute would otherwise be silently ignored. The owned rows are the
// controls: their messages are pinned verbatim so the shared code path is proven unchanged for them.
public static class ComplexTypeUnsupportedAttributeValidationTests
{
    // Every attribute recorded as Mongo:NotSupportedAttributes by a NotSupportedPropertyAttributeConvention<T>.
    public static readonly TheoryData<Type, string> MemberAttributeCases = new()
    {
        { typeof(LSerializer), nameof(BsonSerializerAttribute) },
        { typeof(LExtraElements), nameof(BsonExtraElementsAttribute) },
        { typeof(LDefaultValue), nameof(BsonDefaultValueAttribute) },
        { typeof(LGuidRepresentation), nameof(BsonGuidRepresentationAttribute) },
        { typeof(LTimeSpanOptions), nameof(BsonTimeSpanOptionsAttribute) },
        { typeof(LDictionaryOptions), nameof(BsonDictionaryOptionsAttribute) },
        { typeof(LIgnoreIfDefault), nameof(BsonIgnoreIfDefaultAttribute) },
        { typeof(LIgnoreIfNull), nameof(BsonIgnoreIfNullAttribute) },
    };

    // MongoModelValidator.UnsupportedClassAttributes that can be applied to a class, plus the constructor and method checks.
    public static readonly TheoryData<Type, string, string> TypeAttributeCases = new()
    {
        { typeof(CDiscriminator), nameof(BsonDiscriminatorAttribute), "is annotated with" },
        { typeof(CKnownTypes), nameof(BsonKnownTypesAttribute), "is annotated with" },
        { typeof(CMemberMapUsage), nameof(BsonMemberMapAttributeUsageAttribute), "is annotated with" },
        { typeof(CNoId), nameof(BsonNoIdAttribute), "is annotated with" },
        { typeof(CSerializer), nameof(BsonSerializerAttribute), "is annotated with" },
        { typeof(CConstructor), nameof(BsonConstructorAttribute), "has a constructor annotated with" },
        { typeof(CFactoryMethod), nameof(BsonFactoryMethodAttribute), "is annotated with" },
    };

    [Theory]
    [MemberData(nameof(MemberAttributeCases))]
    public static void Unsupported_member_attribute_on_complex_leaf_is_rejected(Type leafType, string attribute)
        => AssertMemberRejected(Run(nameof(BuildComplex), leafType), leafType, attribute);

    [Theory]
    [MemberData(nameof(MemberAttributeCases))]
    public static void Unsupported_member_attribute_on_nested_complex_leaf_is_rejected(Type leafType, string attribute)
        => AssertMemberRejected(Run(nameof(BuildNested), leafType), leafType, attribute);

    // Home ignores the attributed member, Work maps it: the type is shared, so the check must reach every path.
    [Theory]
    [MemberData(nameof(MemberAttributeCases))]
    public static void Unsupported_member_attribute_on_shared_complex_type_is_rejected_at_the_path_that_maps_it(
        Type leafType, string attribute)
        => AssertMemberRejected(Run(nameof(BuildShared), leafType), leafType, attribute);

#if !EF8 && !EF9
    [Theory]
    [MemberData(nameof(MemberAttributeCases))]
    public static void Unsupported_member_attribute_on_complex_collection_element_is_rejected(Type leafType, string attribute)
        => AssertMemberRejected(Run(nameof(BuildCollection), leafType), leafType, attribute);

    [Theory]
    [MemberData(nameof(TypeAttributeCases))]
    public static void Unsupported_type_attribute_on_complex_collection_element_is_rejected(Type type, string attribute, string verb)
        => AssertTypeRejected(Run(nameof(BuildCollection), type), attribute, verb);
#endif

    [Theory]
    [MemberData(nameof(MemberAttributeCases))]
    public static void Owned_control_unsupported_member_attribute_message_is_unchanged(Type leafType, string attribute)
    {
        var ex = Run(nameof(BuildOwned), leafType);
        Assert.NotNull(ex);
        Assert.Equal(
            $"Property '{leafType.Name}.{nameof(LDefaultValue.Value)}' is annotated with unsupported attribute '{attribute}'.",
            ex!.Message);
    }

    [Theory]
    [MemberData(nameof(TypeAttributeCases))]
    public static void Unsupported_type_attribute_on_complex_type_is_rejected(Type type, string attribute, string verb)
        => AssertTypeRejected(Run(nameof(BuildComplex), type), attribute, verb);

    [Theory]
    [MemberData(nameof(TypeAttributeCases))]
    public static void Unsupported_type_attribute_on_nested_complex_type_is_rejected(Type type, string attribute, string verb)
        => AssertTypeRejected(Run(nameof(BuildNested), type), attribute, verb);

    [Theory]
    [MemberData(nameof(TypeAttributeCases))]
    public static void Owned_control_unsupported_type_attribute_message_is_unchanged(Type type, string attribute, string verb)
    {
        var ex = Run(nameof(BuildOwned), type);
        Assert.NotNull(ex);
        var expected = type == typeof(CFactoryMethod)
            ? $"Method '{type.Name}.{nameof(CFactoryMethod.Create)}' is annotated with unsupported attribute '{attribute}'."
            : $"Entity '{type.Name}' {verb} unsupported attribute '{attribute}'.";
        Assert.Equal(expected, ex!.Message);
    }

    // The same complex types without the attribute build (the rejection is the attribute, not the shape).
    [Fact]
    public static void Complex_shapes_without_unsupported_attributes_build()
    {
        Assert.Null(Run(nameof(BuildComplex), typeof(LPlain)));
        Assert.Null(Run(nameof(BuildNested), typeof(LPlain)));
        Assert.Null(Run(nameof(BuildShared), typeof(LPlain)));
#if !EF8 && !EF9
        Assert.Null(Run(nameof(BuildCollection), typeof(LPlain)));
#endif
    }

    static void AssertMemberRejected(NotSupportedException? ex, Type leafType, string attribute)
    {
        Assert.NotNull(ex);
        Assert.Matches(
            "^Property '[^']*" + Regex.Escape(leafType.Name) + "\\." + nameof(LDefaultValue.Value)
            + "' is annotated with unsupported attribute '" + attribute + "'\\.$",
            ex!.Message);
    }

    static void AssertTypeRejected(NotSupportedException? ex, string attribute, string verb)
    {
        Assert.NotNull(ex);
        Assert.Contains($"{verb} unsupported attribute '{attribute}'.", ex!.Message);
        Assert.DoesNotContain("Entity '", ex.Message);
    }

    static NotSupportedException? Run(string builder, Type leafType)
    {
        try
        {
            typeof(ComplexTypeUnsupportedAttributeValidationTests)
                .GetMethod(builder, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(leafType)
                .Invoke(null, null);
            return null;
        }
        catch (TargetInvocationException e) when (e.InnerException is NotSupportedException nse)
        {
            return nse;
        }
    }

    static void BuildComplex<T>() where T : class, new()
    {
        using var db = SingleEntityDbContext.Create<Holder<T>>(mb => mb.Entity<Holder<T>>().ComplexProperty(h => h.Home));
        _ = db.Model;
    }

    static void BuildNested<T>() where T : class, new()
    {
        using var db = SingleEntityDbContext.Create<NestedHolder<T>>(mb =>
            mb.Entity<NestedHolder<T>>().ComplexProperty(h => h.Home, b => b.ComplexProperty(o => o.Inner)));
        _ = db.Model;
    }

    static void BuildShared<T>() where T : class, new()
    {
        using var db = SingleEntityDbContext.Create<SharedHolder<T>>(mb =>
        {
            var entity = mb.Entity<SharedHolder<T>>();
            var member = typeof(T).GetProperty(nameof(LDefaultValue.Value));
            entity.ComplexProperty(h => h.Home, b =>
            {
                if (member != null) b.Ignore(member.Name);
            });
            entity.ComplexProperty(h => h.Work);
        });
        _ = db.Model;
    }

#if !EF8 && !EF9
    static void BuildCollection<T>() where T : class, new()
    {
        using var db = SingleEntityDbContext.Create<CollectionHolder<T>>(mb =>
            mb.Entity<CollectionHolder<T>>().ComplexCollection(h => h.Items));
        _ = db.Model;
    }
#endif

    static void BuildOwned<T>() where T : class, new()
    {
        using var db = SingleEntityDbContext.Create<Holder<T>>(mb => mb.Entity<Holder<T>>().OwnsOne(h => h.Home));
        _ = db.Model;
    }

    class Holder<T> where T : class
    {
        public int _id { get; set; }
        public T Home { get; set; } = null!;
    }

    class Outer<T> where T : class
    {
        public string Name { get; set; } = "";
        public T Inner { get; set; } = null!;
    }

    class NestedHolder<T> where T : class
    {
        public int _id { get; set; }
        public Outer<T> Home { get; set; } = null!;
    }

    class SharedHolder<T> where T : class
    {
        public int _id { get; set; }
        public T Home { get; set; } = null!;
        public T Work { get; set; } = null!;
    }

    class CollectionHolder<T> where T : class
    {
        public int _id { get; set; }
        public List<T> Items { get; set; } = [];
    }

    public class LPlain { public string City { get; set; } = ""; public string? Value { get; set; } }
    public class LSerializer { public string City { get; set; } = ""; [BsonSerializer] public string? Value { get; set; } }
    public class LExtraElements { public string City { get; set; } = ""; [BsonExtraElements] public string? Value { get; set; } }
    public class LDefaultValue { public string City { get; set; } = ""; [BsonDefaultValue("dflt")] public string? Value { get; set; } }
    public class LGuidRepresentation { public string City { get; set; } = ""; [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)] public string? Value { get; set; } }
    public class LTimeSpanOptions { public string City { get; set; } = ""; [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Seconds)] public string? Value { get; set; } }
    public class LDictionaryOptions { public string City { get; set; } = ""; [BsonDictionaryOptions] public string? Value { get; set; } }
    public class LIgnoreIfDefault { public string City { get; set; } = ""; [BsonIgnoreIfDefault] public string? Value { get; set; } }
    public class LIgnoreIfNull { public string City { get; set; } = ""; [BsonIgnoreIfNull] public string? Value { get; set; } }

    [BsonDiscriminator] public class CDiscriminator { public string City { get; set; } = ""; }
    [BsonKnownTypes] public class CKnownTypes { public string City { get; set; } = ""; }
    [BsonMemberMapAttributeUsage] public class CMemberMapUsage { public string City { get; set; } = ""; }
    [BsonNoId] public class CNoId { public string City { get; set; } = ""; }
    [BsonSerializer] public class CSerializer { public string City { get; set; } = ""; }

    public class CConstructor
    {
        public CConstructor() { }
        [BsonConstructor] public CConstructor(string city) => City = city;
        public string City { get; set; } = "";
    }

    public class CFactoryMethod
    {
        public string City { get; set; } = "";
        [BsonFactoryMethod] public static CFactoryMethod Create() => new();
    }
}
