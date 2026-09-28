namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The instances the generated code creates keep the nullable annotations of their type arguments (Box<string?>), so that
// they are of the type the declaration says: the destination of a return-type mapper, created with new T() or through
// its constructor, an intermediate member of a dotted target created with ??= or in the object initializer, a required
// member created in the object initializer, and the instance a void mapper of [MapNested] / [MapCollection] fills. They
// used to leave them out (new Box<string>()), which warned (CS8601, CS8604, CS8619, CS8620). A type's own annotation is
// left out, as new cannot take it (CS8628), a type parameter's (T?) included.
public class NullableTypeArgumentCreationTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string attributes, string mapper, string mapperAttribute) =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Box<T> { public T Value { get; set; } = default!; }
        public class InitBox<T> { public T Value { get; init; } = default!; }
        public record RBox<T>(T Value);
        public class Item { public string? Name { get; set; } }
        public class Src
        {
            public string? Value { get; set; }
            public string? Name { get; set; }
            public Item Nested { get; set; } = new();
            public List<Item> Items { get; set; } = [];
        }
        public class Dst
        {
            public Box<string?>? Holder { get; set; }
            public Box<string?> Nested { get; set; } = new();
            public List<Box<string?>> Items { get; set; } = [];
        }
        public class InitDst
        {
            public required Box<string?> Holder { get; set; }
            public InitBox<string?> Init { get; init; } = new();
        }
        public static partial class M
        {
            [Mapper]
            public static partial void FillBox(Item source, Box<string?> destination);

            {{mapperAttribute}}
            {{attributes}}
            {{mapper}}
        }
        """;

    [Theory]
    [InlineData("[Mapper]", "", "public static partial Box<string?> Map(Src source);", "var __d = new global::Test.Box<string?>();")]
    [InlineData("[Mapper]", "", "public static partial RBox<string?> Map(Src source);", "var __d = new global::Test.RBox<string?>(source.Value);")]
    [InlineData("[Mapper(AutoMap = false)]", "[MapProperty(\"Holder.Value\", nameof(Src.Name))]", "public static partial void Map(Src source, Dst destination);", "destination.Holder ??= new global::Test.Box<string?>();")]
    [InlineData("[Mapper(AutoMap = false)]", "[MapNested(nameof(Dst.Nested), Mapper = nameof(FillBox))]", "public static partial void Map(Src source, Dst destination);", "var __nested_Nested = new global::Test.Box<string?>();")]
    [InlineData("[Mapper(AutoMap = false)]", "[MapCollection(nameof(Dst.Items), Mapper = nameof(FillBox))]", "public static partial void Map(Src source, Dst destination);", "var __dest = new global::Test.Box<string?>();")]
    [InlineData("[Mapper(AutoMap = false)]", "[MapProperty(\"Holder.Value\", nameof(Src.Name))]", "public static partial InitDst Map(Src source);", "Holder = new global::Test.Box<string?>(),")]
    [InlineData("[Mapper(AutoMap = false)]", "[MapProperty(\"Holder.Value\", nameof(Src.Name))] [MapProperty(\"Init.Value\", nameof(Src.Value))]", "public static partial InitDst Map(Src source);", "Init = new global::Test.InitBox<string?>()")]
    public void CreatedInstanceKeepsTypeArgumentAnnotations(string mapperAttribute, string attributes, string mapper, string expected)
    {
        var (generated, problems) = Build(Source(attributes, mapper, mapperAttribute));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The annotation of the type itself is left out, a type parameter's as well
    [Theory]
    [InlineData("public static partial T? Map<T>(Src source) where T : new();")]
    [InlineData("public static partial T? Map<T>(Src source) where T : class, new();")]
    public void TypeParameterIsCreatedWithoutAnnotation(string mapper)
    {
        var (generated, problems) = Build(Source(string.Empty, mapper, "[Mapper]"));

        Assert.Empty(problems);
        Assert.Contains("var __d = new T();", generated, StringComparison.Ordinal);
    }
}
