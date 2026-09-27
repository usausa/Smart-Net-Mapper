namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A member the dotted path of [MapConstant], [MapExpression] or [MapUsing] goes into is not mapped as a whole by
// the automatic mapping, as with a dotted [MapProperty]. The whole used to be copied from the source, and the path
// then wrote into the source's own object (throwing when it was null), or, in an object initializer, the whole
// replaced the object the path created or was assigned twice (CS1912).
public class DottedPathAutoMapTests
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

    private static string Lines(string source) =>
        String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string types, string attributes, string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Child { public int B { get; set; } public int C { get; set; } }
        public class Holder { public Child? Inner { get; set; } }
        {{types}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{signature}}

            private static int Calc(Src src) => src.Y;
        }
        """;

    private const string Plain = """
        public class Src { public Child? Item { get; set; } public Holder? Holder { get; set; } public int Y { get; set; } }
        public class Dst { public Child? Item { get; set; } public Holder? Holder { get; set; } public int Y { get; set; } }
        """;

    [Theory]
    [InlineData("[MapConstant(\"Item.B\", 3)]", "__d.Item.B = 3;")]
    [InlineData("[MapExpression(\"Item.B\", \"src.Y + 1\")]", "__d.Item.B = __expression0(src);")]
    [InlineData("[MapUsing(\"Item.B\", nameof(Calc))]", "__d.Item.B = Calc(src);")]
    public void DottedPathTakesPrecedenceOverAutomaticMappingOfWhole(string attribute, string expected)
    {
        var source = Source(Plain, attribute);

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("__d.Item ??= new global::Test.Child();", lines, StringComparison.Ordinal);
        Assert.Contains(expected, lines, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Item = src.Item;", lines, StringComparison.Ordinal);
        Assert.Contains("__d.Holder = src.Holder;", lines, StringComparison.Ordinal);
        Assert.Contains("__d.Y = src.Y;", lines, StringComparison.Ordinal);
    }

    // The member at the start of a longer path, and the destination a void mapper fills
    [Fact]
    public void LongerPathTakesPrecedenceOverAutomaticMappingOfWhole()
    {
        var source = Source(Plain, "[MapConstant(\"Holder.Inner.B\", 3)]");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("__d.Holder ??= new global::Test.Holder();\n__d.Holder.Inner ??= new global::Test.Child();", lines, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Holder = src.Holder;", lines, StringComparison.Ordinal);
        Assert.Contains("__d.Item = src.Item;", lines, StringComparison.Ordinal);
    }

    [Fact]
    public void DottedPathTakesPrecedenceInVoidMapper()
    {
        var source = Source(Plain, "[MapExpression(\"Item.B\", \"src.Y\")]", "public static partial void Map(Src src, Dst dst);");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("dst.Item ??= new global::Test.Child();", lines, StringComparison.Ordinal);
        Assert.DoesNotContain("dst.Item = src.Item;", lines, StringComparison.Ordinal);
    }

    // A path to an init-only member creates the member in the object initializer, which the automatic mapping
    // of the whole used to replace after construction, or assign a second time in the initializer (CS1912)
    [Theory]
    [InlineData("public InitChild? Item { get; set; }")]
    [InlineData("public InitChild? Item { get; init; }")]
    public void PathCreatedInInitializerTakesPrecedence(string item)
    {
        var source = Source(
            $$"""
            public class InitChild { public int B { get; init; } }
            public class Src { public InitChild? Item { get; set; } public int Y { get; set; } }
            public class Dst { {{item}} public int Y { get; set; } }
            """,
            "[MapConstant(\"Item.B\", 3)]");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("var __d = new global::Test.Dst()\n{\nItem = new global::Test.InitChild()\n{\nB = 3,\n},\n};\n__d.Y = src.Y;", lines, StringComparison.Ordinal);
        Assert.DoesNotContain("Item = src.Item", lines, StringComparison.Ordinal);
    }

    // A struct member is written through a copy as before, without its value copied from the source first
    [Fact]
    public void StructMemberIsNotCopiedAsWhole()
    {
        var source = Source(
            """
            public struct Point { public int B { get; set; } public int C { get; set; } }
            public class Src { public Point Item { get; set; } public int Y { get; set; } }
            public class Dst { public Point Item { get; set; } public int Y { get; set; } }
            """,
            "[MapConstant(\"Item.B\", 3)]");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("{\nvar __copy0 = __d.Item;\n__copy0.B = 3;\n__d.Item = __copy0;\n}", lines, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Item = src.Item;", lines, StringComparison.Ordinal);
    }

    // A required member a path writes into after construction is created in the object initializer, as with a
    // dotted [MapProperty], instead of being copied from the source
    [Theory]
    [InlineData("[MapConstant(\"Item.B\", 3)]", "__d.Item.B = 3;")]
    [InlineData("[MapProperty(\"Item.B\", nameof(Src.Y))]", "__d.Item.B = src.Y;")]
    public void RequiredMemberWrittenAfterConstructionIsCreatedInInitializer(string attribute, string expected)
    {
        var source = Source(
            """
            public class Src { public Child Item { get; set; } = new(); public int Y { get; set; } }
            public class Dst { public required Child Item { get; set; } public int Y { get; set; } }
            """,
            attribute);

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("var __d = new global::Test.Dst()\n{\nItem = new global::Test.Child(),\n};", lines, StringComparison.Ordinal);
        Assert.Contains(expected, lines, StringComparison.Ordinal);
        Assert.DoesNotContain("src.Item", lines, StringComparison.Ordinal);
    }
}
