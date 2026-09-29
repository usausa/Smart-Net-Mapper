namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A property overriding one accessor only inherits the other one from the property it overrides. The accessors
// used to be looked up on the overriding property alone, so one overriding the getter only was taken as get-only:
// the target of an attribute was reported (SMP0102), where the automatic mapping reached the base property. The
// setter found up the overridden properties decides whether the property can be assigned, where (init-only), and
// by whom (its accessibility), for the automatic mapping and the attributes alike, and so does the getter for a
// member a dotted path goes through.
public class OverriddenAccessorTests
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

    private static string Source(string baseProperty, string attributes, string mapper = "[Mapper(AutoMap = false)]", string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class Src { public int X { get; set; } public int Y { get; set; } public Item Nested { get; set; } = new(); public List<Item> Items { get; set; } = []; public int Count() => 3; }
        public class Base { {{baseProperty}} }
        public class Dst : Base
        {
            public override int X => base.X;
            public override Item? Nested => base.Nested;
            public override List<Item>? Items => base.Items;
        }
        public static partial class M
        {
            [Mapper]
            public static partial Item MapItem(Item source);

            {{mapper}}
            {{attributes}}
            {{signature}}

            private static int Calc(Src src) => src.Y;
        }
        """;

    private const string Settable = "public virtual int X { get; set; } public virtual Item? Nested { get; set; } public virtual List<Item>? Items { get; set; }";

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.X), nameof(Src.Y))]", "__d.X = src.Y;")]
    [InlineData("[MapConstant(nameof(Dst.X), 1)]", "__d.X = 1;")]
    [InlineData("[MapExpression(nameof(Dst.X), \"src.Y + 1\")]", "__d.X = __expression0(src);")]
    [InlineData("[MapUsing(nameof(Dst.X), nameof(Calc))]", "__d.X = Calc(src);")]
    [InlineData("[MapFrom(nameof(Dst.X), nameof(Src.Count))]", "__d.X = src.Count();")]
    [InlineData("[MapNested(nameof(Dst.Nested), Mapper = nameof(MapItem))]", "__d.Nested = MapItem(src.Nested);")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]", "__d.Items = __list;")]
    public void TargetInheritsSetter(string attribute, string expected)
    {
        var source = Source(Settable, attribute);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    [Fact]
    public void AutomaticMappingAssignsOnce()
    {
        var source = Source(Settable, string.Empty, "[Mapper]");

        AssertCompiles(source);
        var lines = Lines(source).Split('\n');
        Assert.Single(lines, static l => l == "__d.X = src.X;");
        Assert.Single(lines, static l => l == "__d.Nested = src.Nested;");
    }

    // An init accessor inherited is set in the object initializer, which a void mapper cannot do
    [Fact]
    public void InheritedInitAccessorIsSetInInitializer()
    {
        var source = Source("public virtual int X { get; init; } public virtual Item? Nested { get; set; } public virtual List<Item>? Items { get; set; }", "[MapProperty(nameof(Dst.X), nameof(Src.Y))]");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst()\n{\nX = src.Y,\n};", Lines(source), StringComparison.Ordinal);
    }

    [Fact]
    public void InheritedInitAccessorInVoidMapperEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            "public virtual int X { get; init; } public virtual Item? Nested { get; set; } public virtual List<Item>? Items { get; set; }",
            "[MapProperty(nameof(Dst.X), nameof(Src.Y))]",
            signature: "public static partial void Map(Src src, Dst dst);"));

        Assert.Equal("SMP0302", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
    }

    // A setter the mapper class cannot call is not one it assigns through
    [Fact]
    public void InheritedProtectedSetterEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            "public virtual int X { get; protected set; } public virtual Item? Nested { get; set; } public virtual List<Item>? Items { get; set; }",
            "[MapProperty(nameof(Dst.X), nameof(Src.Y))]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0102", diagnostic.Id);
        Assert.Contains("target=[X]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // A member overriding the setter only is read through the getter it inherits, on a dotted path
    [Fact]
    public void DottedPathReadsInheritedGetter()
    {
        const string source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Item { public int V { get; set; } }
            public class Src { public int Y { get; set; } }
            public class Base { public virtual Item? Child { get; set; } }
            public class Dst : Base { public override Item? Child { set => base.Child = value; } }
            public static partial class M
            {
                [Mapper(AutoMap = false)]
                [MapProperty("Child.V", nameof(Src.Y))]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        Assert.Contains("__d.Child ??= new global::Test.Item();\n__d.Child.V = src.Y;", Lines(source), StringComparison.Ordinal);
    }
}
