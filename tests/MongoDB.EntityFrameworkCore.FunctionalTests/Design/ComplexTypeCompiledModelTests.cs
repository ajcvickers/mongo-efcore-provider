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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Design;
using MongoDB.EntityFrameworkCore.Extensions;
using MongoDB.EntityFrameworkCore.Metadata;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.Design;

#nullable enable

/// <summary>
/// Design-time smoke test for complex types (Task 15): the compiled-model generator runs over a model with nested,
/// struct and renamed complex properties (and, on EF10, a complex collection), the generated code carries each
/// element-name annotation, and the generated model compiled in memory reads and writes the same stored shape as the
/// runtime-built model.
/// </summary>
[XUnitCollection("DesignTests")]
public class ComplexTypeCompiledModelTests(TemporaryDatabaseFixture database) : IClassFixture<TemporaryDatabaseFixture>
{
    public struct Point
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Place
    {
        public string City { get; set; } = null!;
        public Point Where { get; set; }
    }

    public class Site
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; } = null!;
        public Place Home { get; set; } = null!;
#if !EF8 && !EF9
        public List<Place> Others { get; set; } = [];
#endif
    }

    public class ComplexContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Site> Sites { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
            => mb.Entity<Site>(e =>
            {
                e.ToCollection("sites");
                e.ComplexProperty(s => s.Home, h =>
                {
                    h.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "h");
                    h.Property(x => x.City).Metadata.SetElementName("c");
                    h.ComplexProperty(x => x.Where).HasPropertyAnnotation(MongoAnnotationNames.ElementName, "w");
                });
#if !EF8 && !EF9
                e.ComplexCollection(s => s.Others, o =>
                {
                    o.HasPropertyAnnotation(MongoAnnotationNames.ElementName, "o");
                    o.ComplexProperty(x => x.Where);
                });
#endif
            });
    }

    private (ComplexContext, IServiceScope) Create(Action<DbContextOptionsBuilder>? configure = null, string? databaseName = null)
    {
        var services = new ServiceCollection()
            .AddEntityFrameworkMongoDB()
            .AddEntityFrameworkDesignTimeServices()
            .AddDbContext<ComplexContext>((p, b) =>
            {
                b.UseMongoDB(database.Client, databaseName ?? database.MongoDatabase.DatabaseNamespace.DatabaseName).UseInternalServiceProvider(p);
                configure?.Invoke(b);
            });
        new MongoDesignTimeServices().ConfigureDesignTimeServices(services);
        var scope = services.BuildServiceProvider(validateScopes: true).CreateScope();
        return (scope.ServiceProvider.GetRequiredService<ComplexContext>(), scope);
    }

    private IReadOnlyCollection<ScaffoldedFile> Generate()
    {
        var (db, scope) = Create();
        using (scope)
        {
            return db.GetService<ICompiledModelCodeGenerator>().GenerateModel(
                db.GetService<IDesignTimeModel>().Model,
                new CompiledModelCodeGenerationOptions { Language = "C#", ModelNamespace = "ComplexCompiled", ContextType = typeof(ComplexContext) });
        }
    }

    [Fact]
    public void Compiled_model_generation_carries_complex_element_names()
    {
        var files = Generate();
        var code = string.Join("\n", files.Select(f => f.Code));

        Assert.Contains(files, f => f.Path == nameof(Site) + "EntityType.cs");
        Assert.Contains("AddComplexProperty", code);
        Assert.Contains("\"Mongo:ElementName\", \"h\"", code);
        Assert.Contains("\"Mongo:ElementName\", \"c\"", code);
        Assert.Contains("\"Mongo:ElementName\", \"w\"", code);
#if !EF8 && !EF9
        Assert.Contains("\"Mongo:ElementName\", \"o\"", code);
#endif
    }

    [Fact]
    public void Compiled_model_compiled_in_memory_round_trips_the_stored_shape()
    {
        var model = CompileModel(Generate());

        var runtimeSite = FindSite(model);
        Assert.Equal("h", runtimeSite.FindComplexProperty(nameof(Site.Home))!.GetElementName());
        Assert.Equal("c", runtimeSite.FindComplexProperty(nameof(Site.Home))!.ComplexType.FindProperty(nameof(Place.City))!.GetElementName());

        var (writer, writeScope) = Create(b => b.UseModel(model));
        using (writeScope)
        {
            writer.Sites.Add(new Site
            {
                Name = "s1", Home = new Place { City = "Oslo", Where = new Point { X = 1, Y = 2 } },
#if !EF8 && !EF9
                Others = [new Place { City = "Rome", Where = new Point { X = 3, Y = 4 } }]
#endif
            });
            writer.SaveChanges();
        }

        var raw = database.MongoDatabase.GetCollection<BsonDocument>("sites").Find(Builders<BsonDocument>.Filter.Eq("Name", "s1")).Single();
        Assert.Equal("Oslo", raw["h"]["c"].AsString);
        Assert.Equal(2.0, raw["h"]["w"]["Y"].AsDouble);
#if !EF8 && !EF9
        Assert.Equal("Rome", raw["o"][0]["City"].AsString);
#endif

        // The runtime-built model reads what the compiled model wrote, and vice versa.
        var (reader, readScope) = Create();
        using (readScope)
        {
            var site = reader.Sites.Single(s => s.Name == "s1" && s.Home.City == "Oslo");
            Assert.Equal((1.0, 2.0), (site.Home.Where.X, site.Home.Where.Y));
        }

        var (compiledReader, compiledScope) = Create(b => b.UseModel(model));
        using (compiledScope)
        {
            Assert.Equal(["Oslo"], compiledReader.Sites.Where(s => s.Home.Where.X < 2).Select(s => s.Home.City).ToList());
        }
    }

    private static IEntityType FindSite(IModel model) => model.FindEntityType(typeof(Site))!;

    // Compiles the generated files in memory against the loaded assemblies and returns the model's Instance.
    private static IModel CompileModel(IReadOnlyCollection<ScaffoldedFile> files)
    {
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(f.Code, new CSharpParseOptions(LanguageVersion.Preview), f.Path)).ToList();
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));
        var compilation = CSharpCompilation.Create(
            "ComplexCompiledModel" + Guid.NewGuid().ToString("N"), trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Take(20)));

        var assembly = Assembly.Load(stream.ToArray());
        var modelType = assembly.GetTypes().Single(t => t.Name == nameof(ComplexContext) + "Model");
        return (IModel)modelType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
    }
}
