namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A generic mapper method is implemented with its type parameters and their constraints, which the implementing
// declaration of a partial method repeats. It used to be implemented without them, which did not compile (CS0759).
// A type parameter as the source or the destination has the properties of its constraint types, as the members
// of a generic mapper class already did not, and a destination one is created with new T(), which its constraints
// have to allow (SMP0303 otherwise, instead of CS0304 in the generated code).
public class GenericMapperMethodTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string mapperClass, string members) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Box<T> { public T Value { get; set; } = default!; public List<T> Items { get; set; } = []; public int Count { get; set; } }
        public class Wrap<T> { public T Value { get; set; } = default!; public List<T> Items { get; set; } = []; public long Count { get; set; } }
        public interface IHasId { int Id { get; } }
        public class Entity { public int Id { get; set; } public string Name { get; set; } = ""; }
        public class Src { public int Id { get; set; } public string Name { get; set; } = ""; }
        public static partial class {{mapperClass}}
        {
            {{members}}
        }
        """;

    [Theory]
    [InlineData("[Mapper] public static partial Wrap<T> Map<T>(Box<T> src);", "public static partial global::Test.Wrap<T> Map<T>(global::Test.Box<T> src)\n")]
    [InlineData("[Mapper] public static partial Wrap<T> Map<T>(Box<T> src) where T : class;", "Map<T>(global::Test.Box<T> src) where T : class\n")]
    [InlineData("[Mapper] public static partial Wrap<T> Map<T>(Box<T> src) where T : class?;", "Map<T>(global::Test.Box<T> src) where T : class?\n")]
    [InlineData("[Mapper] public static partial Wrap<T> Map<T>(Box<T> src) where T : struct;", "Map<T>(global::Test.Box<T> src) where T : struct\n")]
    [InlineData("[Mapper] public static partial Wrap<T> Map<T>(Box<T> src) where T : unmanaged, IComparable<T>;", "Map<T>(global::Test.Box<T> src) where T : unmanaged, global::System.IComparable<T>\n")]
    [InlineData("[Mapper] public static partial Wrap<T> Map<T>(Box<T> src) where T : notnull;", "Map<T>(global::Test.Box<T> src) where T : notnull\n")]
    [InlineData("[Mapper] public static partial Wrap<T> Map<T>(Box<T> src) where T : IHasId?, new();", "Map<T>(global::Test.Box<T> src) where T : global::Test.IHasId?, new()\n")]
    [InlineData("[Mapper] public static partial void Map<TKey, TValue>(Box<TValue> src, Wrap<TValue> dst, TKey key) where TKey : struct where TValue : class;", "Map<TKey, TValue>(global::Test.Box<TValue> src, global::Test.Wrap<TValue> dst, TKey key) where TKey : struct where TValue : class\n")]
    [InlineData("[Mapper] [MapExpression(\"Count\", \"src.Count + 1L\")] public static partial Wrap<T> Map<T>(Box<T> src);", "static long __expression0(global::Test.Box<T> src)")]
    public void TypeParametersAndConstraintsAreRepeated(string members, string expected)
    {
        var (generated, problems) = Build(Source("M", members));

        Assert.Empty(problems);
        Assert.Contains(expected, generated.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains(".Value = src.Value;", generated, StringComparison.Ordinal);
    }

    // A type parameter has the properties of its constraint types, as a source and as a destination, in a generic
    // method and in a generic mapper class alike
    [Theory]
    [InlineData("M", "[Mapper] public static partial T Create<T>(Src src) where T : Entity, new();", "__d.Id = src.Id;", "__d.Name = src.Name;")]
    [InlineData("M", "[Mapper] public static partial void Fill<T>(Src src, T dst) where T : Entity;", "dst.Id = src.Id;", "dst.Name = src.Name;")]
    [InlineData("M", "[Mapper] public static partial Entity From<T>(T src) where T : IHasId;", "__d.Id = src.Id;", "")]
    [InlineData("M<T> where T : Entity, new()", "[Mapper] public static partial T Create(Src src);", "__d.Id = src.Id;", "__d.Name = src.Name;")]
    [InlineData("M<T> where T : IHasId", "[Mapper] [MapProperty(\"Name\", \"Id\")] public static partial Entity From(T src);", "__d.Name = global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.Id);", "")]
    public void TypeParameterHasMembersOfConstraints(string mapperClass, string members, string expected, string expected2)
    {
        var (generated, problems) = Build(Source(mapperClass, members));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.Contains(expected2, generated, StringComparison.Ordinal);
    }

    // A destination type parameter that new T() cannot create is reported
    [Theory]
    [InlineData("M", "[Mapper] public static partial T Create<T>(Src src) where T : Entity;")]
    [InlineData("M", "[Mapper] public static partial T Create<T>(Src src);")]
    [InlineData("M<T> where T : Entity", "[Mapper] public static partial T Create(Src src);")]
    public void UncreatableTypeParameterEmitsDiagnostic(string mapperClass, string members)
    {
        var (_, problems) = Build(Source(mapperClass, members));

        Assert.Equal("SMP0303", Assert.Single(problems));
    }

    [Theory]
    [InlineData("[Mapper] public static partial T Create<T>(Src src) where T : Entity, new();")]
    [InlineData("[Mapper] public static partial T Create<T>(Src src) where T : struct;")]
    public void CreatableTypeParameterIsCreated(string members)
    {
        var (generated, problems) = Build(Source("M", members));

        Assert.Empty(problems);
        Assert.Contains("var __d = new T();", generated, StringComparison.Ordinal);
    }
}
