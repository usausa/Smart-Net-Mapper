namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The generated code reads a source property through its getter, so a property without a getter the mapper class
// can call (a private or protected get, or set-only) used to fail there (CS0271 / CS0154). Such a property is not a
// source: the automatic mapping leaves it out, and one named in an attribute, also as a segment of a path, is
// reported as a source that is not found.
public class UnreadableSourceTests
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

    private static string Source(string attributes, string destination = "public class Dst { public int X { get; set; } public int Y { get; set; } public Item? Nested { get; set; } public List<Item>? Items { get; set; } }", string mapper = "[Mapper(AutoMap = false)]") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class Inner { public int V { get; set; } }
        public class Src
        {
            private int hidden;
            public int X { private get; set; }
            public int Y { get; set; }
            public int SetOnly { set => hidden = value; }
            public Inner Child { protected get; set; } = new();
            public Item Nested { private get; set; } = new();
            public List<Item> Items { private get; set; } = [];
            public int Hidden() => hidden;
        }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            public static partial Item MapItem(Item source);

            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    [Fact]
    public void AutomaticMappingLeavesOutUnreadableProperties()
    {
        var source = Source(string.Empty, "public class Dst { public int X { get; set; } public int Y { get; set; } public int SetOnly { get; set; } }", "[Mapper]");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Y = src.Y;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.X", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.SetOnly", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Y), \"X\")]", "SMP0108")]
    [InlineData("[MapProperty(nameof(Dst.Y), \"SetOnly\")]", "SMP0108")]
    [InlineData("[MapProperty(nameof(Dst.Y), \"Child.V\")]", "SMP0108")]
    [InlineData("[MapFrom(nameof(Dst.Y), \"Child.V\")]", "SMP0204")]
    [InlineData("[MapNested(nameof(Dst.Nested), Mapper = nameof(MapItem))]", "SMP0206")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]", "SMP0206")]
    public void UnreadableSourceEmitsDiagnostic(string attribute, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(attribute));

        Assert.Equal(id, Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // Nor is it the source of a constructor argument
    [Fact]
    public void UnreadableConstructorSourceEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(string.Empty, "public record Dst(int X);", "[Mapper]"));

        Assert.Equal("SMP0305", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }
}
