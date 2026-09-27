namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// [MapIgnore] and an attribute mapping the same target contradict each other. The mapping used to be dropped
// without a word for [MapProperty], and applied for the other attributes. They are reported as naming the same
// target (SMP0101), whichever the attribute is. The [MapIgnore] of a member with dotted paths into it is not one
// of them: it keeps the member from being mapped as a whole, and the paths are applied.
public class IgnoreAndMappingTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static string Source(string attributes, string mapper = "[Mapper(AutoMap = false)]") =>
        $$"""
        #nullable enable
        using System;
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
            public int GetCount() => 1;
        }
        public class Dst
        {
            public int X { get; set; }
            public Item? Nested { get; set; }
            public List<Item>? Items { get; set; }
        }
        public static partial class M
        {
            [Mapper]
            public static partial Item MapItem(Item source);

            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);

            private static int Calc(Src src) => src.Y;
        }
        """;

    [Theory]
    [InlineData("[MapIgnore(nameof(Dst.X))] [MapProperty(nameof(Dst.X), nameof(Src.Y))]", "X", "MapIgnore, MapProperty")]
    [InlineData("[MapIgnore(nameof(Dst.X))] [MapConstant(nameof(Dst.X), 1)]", "X", "MapIgnore, MapConstant")]
    [InlineData("[MapExpression(nameof(Dst.X), \"1\")] [MapIgnore(nameof(Dst.X))]", "X", "MapIgnore, MapExpression")]
    [InlineData("[MapIgnore(nameof(Dst.X))] [MapUsing(nameof(Dst.X), nameof(Calc))]", "X", "MapIgnore, MapUsing")]
    [InlineData("[MapIgnore(nameof(Dst.X))] [MapFrom(nameof(Dst.X), nameof(Src.GetCount))]", "X", "MapIgnore, MapFrom")]
    [InlineData("[MapIgnore(nameof(Dst.Nested))] [MapNested(nameof(Dst.Nested), Mapper = nameof(MapItem))]", "Nested", "MapIgnore, MapNested")]
    [InlineData("[MapIgnore(nameof(Dst.Items))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]", "Items", "MapIgnore, MapCollection")]
    [InlineData("[MapIgnore(\"Nested.V\")] [MapProperty(\"Nested.V\", nameof(Src.Y))]", "Nested.V", "MapIgnore, MapProperty")]
    public void IgnoreAndMappingOfSameTargetEmitDiagnostic(string attributes, string target, string kinds)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(attributes));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0101", diagnostic.Id);
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains($"target=[{target}]", message, StringComparison.Ordinal);
        Assert.Contains($"attributes=[{kinds}]", message, StringComparison.Ordinal);
    }

    // The names are matched as declared, under the mapper's name comparison
    [Fact]
    public void IgnoreAndMappingNamedIgnoringCaseEmitDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            "[MapIgnore(\"x\")] [MapProperty(\"X\", nameof(Src.Y))]",
            "[Mapper(AutoMap = false, NameComparison = StringComparison.OrdinalIgnoreCase)]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0101", diagnostic.Id);
        Assert.Contains("target=[X]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // [MapIgnore] of the whole with dotted paths into it keeps the whole from the automatic mapping, and the
    // paths are applied
    [Theory]
    [InlineData("[MapProperty(\"Nested.V\", nameof(Src.Y))]", "__d.Nested.V = src.Y;")]
    [InlineData("[MapConstant(\"Nested.V\", 2)]", "__d.Nested.V = 2;")]
    [InlineData("[MapExpression(\"Nested.V\", \"src.X\")]", "__d.Nested.V = __expression0(src);")]
    [InlineData("[MapUsing(\"Nested.V\", nameof(Calc))]", "__d.Nested.V = Calc(src);")]
    public void IgnoredWholeLeavesDottedPathApplied(string attribute, string expected)
    {
        var source = Source("[MapIgnore(nameof(Dst.Nested))] " + attribute, "[Mapper]");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Nested ??= new global::Test.Item();", generated, StringComparison.Ordinal);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Nested = ", generated, StringComparison.Ordinal);
    }

    // [MapIgnore] naming a target twice does not contradict itself
    [Fact]
    public void RepeatedIgnoreCompiles()
    {
        var source = Source("[MapIgnore(nameof(Dst.X))] [MapIgnore(nameof(Dst.X))]", "[Mapper]");

        AssertCompiles(source);
        Assert.DoesNotContain("__d.X", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
