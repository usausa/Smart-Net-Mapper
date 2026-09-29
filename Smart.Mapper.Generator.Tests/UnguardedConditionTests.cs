namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A [MapCondition] guards the property mapping of its target: the automatic one, or a [MapProperty], also along a
// dotted path. On a target no property mapping assigns it used to do nothing without a word: one nothing maps, one
// ignored, and one another attribute assigns ([MapConstant], [MapExpression], [MapUsing], [MapFrom], [MapNested],
// [MapCollection]), which the condition does not guard. It is reported (SMP0109).
public class UnguardedConditionTests
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
            public int Z { get; set; }
            public int Field;
            public Item Nested { get; set; } = new();
            public List<Item>? Items { get; set; }
        }
        public static partial class M
        {
            [Mapper]
            public static partial Item MapItem(Item source);

            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);

            private static bool IsPositive(int value) => value > 0;
            private static bool HasItem(Item value) => true;
            private static bool HasItems(List<Item> value) => true;
            private static int Calc(Src src) => src.X;
        }
        """;

    [Theory]
    // Nothing maps it
    [InlineData("[MapCondition(nameof(Dst.Z), nameof(IsPositive))]", "[Mapper]", "Z")]
    [InlineData("[MapCondition(nameof(Dst.X), nameof(IsPositive))]", "[Mapper(AutoMap = false)]", "X")]
    [InlineData("[MapCondition(nameof(Dst.Field), nameof(IsPositive))]", "[Mapper]", "Field")]
    // Ignored
    [InlineData("[MapIgnore(nameof(Dst.X))] [MapCondition(nameof(Dst.X), nameof(IsPositive))]", "[Mapper]", "X")]
    // Another attribute assigns it
    [InlineData("[MapConstant(nameof(Dst.Z), 1)] [MapCondition(nameof(Dst.Z), nameof(IsPositive))]", "[Mapper(AutoMap = false)]", "Z")]
    [InlineData("[MapExpression(nameof(Dst.Z), \"1\")] [MapCondition(nameof(Dst.Z), nameof(IsPositive))]", "[Mapper(AutoMap = false)]", "Z")]
    [InlineData("[MapUsing(nameof(Dst.Z), nameof(Calc))] [MapCondition(nameof(Dst.Z), nameof(IsPositive))]", "[Mapper(AutoMap = false)]", "Z")]
    [InlineData("[MapFrom(nameof(Dst.Z), nameof(Src.GetCount))] [MapCondition(nameof(Dst.Z), nameof(IsPositive))]", "[Mapper(AutoMap = false)]", "Z")]
    [InlineData("[MapNested(nameof(Dst.Nested), Mapper = nameof(MapItem))] [MapCondition(nameof(Dst.Nested), nameof(HasItem))]", "[Mapper(AutoMap = false)]", "Nested")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))] [MapCondition(nameof(Dst.Items), nameof(HasItems))]", "[Mapper(AutoMap = false)]", "Items")]
    [InlineData("[MapConstant(nameof(Dst.Field), 1)] [MapCondition(nameof(Dst.Field), nameof(IsPositive))]", "[Mapper(AutoMap = false)]", "Field")]
    public void ConditionWithoutPropertyMappingEmitsDiagnostic(string attributes, string mapper, string target)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(attributes, mapper));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0109", diagnostic.Id);
        Assert.Contains($"target=[{target}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // The property mappings it guards, the automatic one, [MapProperty] and a dotted [MapProperty], whose intermediate
    // member is created under the condition, when it assigns
    [Theory]
    [InlineData("[MapCondition(nameof(Dst.X), nameof(IsPositive))]", "[Mapper]", "if (IsPositive(src.X))\n{\n__d.X = src.X;\n}")]
    [InlineData("[MapProperty(nameof(Dst.Z), nameof(Src.Y))] [MapCondition(nameof(Dst.Z), nameof(IsPositive))]", "[Mapper(AutoMap = false)]", "if (IsPositive(src.Y))\n{\n__d.Z = src.Y;\n}")]
    [InlineData(
        "[MapProperty(\"Nested.V\", nameof(Src.Y))] [MapCondition(\"Nested.V\", nameof(IsPositive))]",
        "[Mapper(AutoMap = false)]",
        "if (IsPositive(src.Y))\n{\n__d.Nested ??= new global::Test.Item();\n__d.Nested.V = src.Y;\n}")]
    public void ConditionOnPropertyMappingGuardsIt(string attributes, string mapper, string expected)
    {
        var source = Source(attributes, mapper);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var body = String.Join("\n", generated.Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));
        Assert.Contains(expected, body, StringComparison.Ordinal);
    }
}
