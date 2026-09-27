namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The generated code calls the methods it finds by name without type arguments, so a generic method whose type
// arguments the call cannot infer used to be taken and failed there (CS0411). A generic method is not taken: not
// as the method of [MapFrom], nor as a converter, a condition, a [MapUsing] method, a callback or the mapper of
// [MapCollection] / [MapNested] in the mapper class. It is reported as the method that does not match, as one
// inferring its type arguments from the arguments already was.
public class GenericMethodTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static string Source(string attributes, string methods) =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class Src
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Item Nested { get; set; } = new();
            public List<Item> Items { get; set; } = [];
            public int Get<T>() => 0;
        }
        public class Dst { public int X { get; set; } public int Y { get; set; } public Item? Nested { get; set; } public List<Item>? Items { get; set; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            public static partial Dst Map(Src src);

            {{methods}}
        }
        """;

    [Theory]
    [InlineData("[MapFrom(nameof(Dst.X), \"Get\")]", "", "SMP0204")]
    [InlineData("[MapProperty(nameof(Dst.X), nameof(Src.Y), Converter = nameof(Conv))]", "private static int Conv<T>(int value) => value;", "SMP0104")]
    [InlineData("[MapProperty(nameof(Dst.X), nameof(Src.Y))] [MapCondition(nameof(Dst.X), nameof(Cond))]", "private static bool Cond<T>(int value) => true;", "SMP0106")]
    [InlineData("[MapUsing(nameof(Dst.X), nameof(Calc))]", "private static int Calc<T>(Src src) => 1;", "SMP0201")]
    [InlineData("[BeforeMap(nameof(Before))]", "private static void Before<T>(Src src, Dst dst) { }", "SMP0102")]
    [InlineData("[AfterMap(nameof(After))]", "private static void After<T>(Src src, Dst dst) { }", "SMP0103")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]", "private static Item MapItem<T>(Item source) => source;", "SMP0210")]
    [InlineData("[MapNested(nameof(Dst.Nested), Mapper = nameof(MapItem))]", "private static Item MapItem<T>(Item source) => source;", "SMP0211")]
    public void GenericMethodEmitsDiagnostic(string attributes, string methods, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(attributes, methods));

        Assert.Equal(id, Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // The overload without type parameters is taken next to a generic one
    [Fact]
    public void NonGenericOverloadIsTaken()
    {
        var source = Source(
            "[MapProperty(nameof(Dst.X), nameof(Src.Y), Converter = nameof(Conv))]",
            "private static int Conv<T>(int value) => value; private static int Conv(int value) => value + 1;");

        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .ToList();
        Assert.Empty(problems);
        Assert.Contains("__d.X = Conv(src.Y);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
