namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A return mapper creates its destination, so a required member it ignores would be left unset: reported
// (SMP0216) instead of failing in the generated code (CS9035). A void mapper fills an instance that exists, so it
// may ignore one. A constructor with [SetsRequiredMembers] sets the required members itself: the one a return
// mapper calls neither needs them mapped (SMP0303) nor refuses ignoring them (SMP0216), and a required target of
// [MapNested] / [MapCollection] can then be assigned after construction; without it, the value is made before
// construction and set in the object initializer, as that of a constructor argument is. A type along a dotted path
// with such a constructor is created, as before.
public class RequiredConstructionTests
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

    private static void AssertDiagnostic(string source, string id, string target)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains(target, diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private static string Lines(string source) =>
        String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string destination, string attributes, bool returns = true) =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class Src { public int X { get; set; } public int Y { get; set; } public Item Nested { get; set; } = new(); public List<Item> Items { get; set; } = []; }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            public static partial Item MapItem(Item source);

            [Mapper]
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
        }
        """;

    private const string RequiredProperty = "public class Dst { public required int X { get; set; } public int Y { get; set; } }";

    private const string RequiredField = "public class Dst { public required int X; public int Y { get; set; } }";

    private const string SetsRequired = "public class Dst { [SetsRequiredMembers] public Dst() { } public required int X { get; set; } public required int Z { get; set; } public int Y { get; set; } }";

    [Theory]
    [InlineData(RequiredProperty)]
    [InlineData(RequiredField)]
    public void IgnoredRequiredMemberInReturnMapperEmitsDiagnostic(string destination)
    {
        AssertDiagnostic(Source(destination, "[MapIgnore(nameof(Dst.X))]"), "SMP0216", "target=[X]");
    }

    [Fact]
    public void IgnoredRequiredMemberInVoidMapperCompiles()
    {
        var source = Source(RequiredProperty, "[MapIgnore(nameof(Dst.X))]", returns: false);

        AssertCompiles(source);
        Assert.Contains("dst.Y = src.Y;", Lines(source), StringComparison.Ordinal);
    }

    // The constructor sets them: an unmapped one (Z) is not reported, the mapped one (X) stays in the initializer
    [Fact]
    public void SetsRequiredMembersDoesNotRequireMapping()
    {
        var source = Source(SetsRequired, string.Empty);

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst()\n{\nX = src.X,\n};\n__d.Y = src.Y;", Lines(source), StringComparison.Ordinal);
    }

    [Fact]
    public void SetsRequiredMembersAllowsIgnoringThem()
    {
        var source = Source(SetsRequired, "[MapIgnore(nameof(Dst.X))]");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst();\n__d.Y = src.Y;", Lines(source), StringComparison.Ordinal);
    }

    // The parameterized constructor a return mapper calls counts as well
    [Fact]
    public void SetsRequiredMembersOnParameterizedConstructorDoesNotRequireMapping()
    {
        var source = Source(
            "public class Dst { [SetsRequiredMembers] public Dst(int y) { Y = y; } public required int Z { get; set; } public int Y { get; } }",
            string.Empty);

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst(src.Y);", Lines(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public class Dst { public required Item Nested { get; set; } }", "[MapNested(nameof(Dst.Nested), Mapper = nameof(MapItem))]", "__init0 = MapItem(src.Nested);\nvar __d = new global::Test.Dst()\n{\nNested = __init0,\n};")]
    [InlineData("public class Dst { public required List<Item> Items { get; set; } }", "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]", "__init0 = __list;\n}\nvar __d = new global::Test.Dst()\n{\nItems = __init0,\n};")]
    public void RequiredNestedOrCollectionTargetIsSetInInitializer(string destination, string attribute, string expected)
    {
        var source = Source(destination, attribute);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public class Dst { [SetsRequiredMembers] public Dst() { Nested = new(); } public required Item Nested { get; set; } }", "[MapNested(nameof(Dst.Nested), Mapper = nameof(MapItem))]", "__d.Nested = MapItem(src.Nested);")]
    [InlineData("public class Dst { [SetsRequiredMembers] public Dst() { Items = []; } public required List<Item> Items { get; set; } }", "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]", "__d.Items = __list;")]
    public void RequiredNestedOrCollectionTargetWithSetsRequiredMembersCompiles(string destination, string attribute, string expected)
    {
        var source = Source(destination, attribute);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // A type along a dotted path is created when its constructor sets its required members, and written into
    // when it holds an instance otherwise
    [Fact]
    public void DottedPathTypeWithSetsRequiredMembersIsCreated()
    {
        var source = Source(
            """
            public class Sets { [SetsRequiredMembers] public Sets() { } public required int K { get; set; } public int V { get; set; } }
            public class Plain { public required int K { get; set; } public int V { get; set; } }
            public class Dst { public Sets? A { get; set; } public Plain? B { get; set; } }
            """,
            "[MapProperty(\"A.V\", nameof(Src.X))] [MapProperty(\"B.V\", nameof(Src.Y))]",
            returns: false).Replace("[Mapper]\n    [MapProperty", "[Mapper(AutoMap = false)]\n    [MapProperty", StringComparison.Ordinal);

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("dst.A ??= new global::Test.Sets();\ndst.A.V = src.X;", lines, StringComparison.Ordinal);
        Assert.Contains("if (dst.B is not null)\n{\ndst.B.V = src.Y;\n}", lines, StringComparison.Ordinal);
    }
}
