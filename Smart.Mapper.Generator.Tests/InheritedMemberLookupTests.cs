namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Members are looked up the way member access reaches them: up the base types, and through the interfaces an
// interface extends. The method of [MapFrom] used to be looked up among the members the source type declares
// itself (SMP0204 for an inherited one), and the properties an interface inherits were left out of the automatic
// mapping, of the targets of [MapProperty], [MapFrom], [MapNested] and [MapCollection], of the sources of
// constructor arguments, and of the Strict check.
public class InheritedMemberLookupTests
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

    private static string Source(string types, string attributes, string signature, string mapper = "[Mapper(AutoMap = false)]") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        {{types}}
        public static partial class M
        {
            [Mapper]
            public static partial Item MapItem(Item source);

            {{mapper}}
            {{attributes}}
            {{signature}}
        }
        """;

    // [MapFrom] method: of a base class, of an interface the source interface extends, and of object
    [Theory]
    [InlineData("public class Base { public int Count() => 1; } public class Src : Base { }", "Count", "public int X { get; set; }", "__d.X = src.Count();")]
    [InlineData("public interface IBase { int Count(); } public interface Src : IBase { }", "Count", "public int X { get; set; }", "__d.X = src.Count();")]
    [InlineData("public class Src { }", "ToString", "public string? X { get; set; }", "__d.X = src.ToString();")]
    public void MapFromFindsInheritedMethod(string types, string member, string property, string expected)
    {
        var source = Source(
            types + " public class Dst { " + property + " }",
            $"[MapFrom(nameof(Dst.X), \"{member}\")]",
            "public static partial Dst Map(Src src);");

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // A method hiding one of a base type (new) wins, as the call binds to it: its long, not the int of the base
    [Fact]
    public void MapFromPrefersHidingMethod()
    {
        var source = Source(
            "public class Base { public int Count() => 1; } public class Src : Base { public new long Count() => 2; } public class Dst { public long X { get; set; } }",
            "[MapFrom(nameof(Dst.X), \"Count\")]",
            "public static partial Dst Map(Src src);");

        AssertCompiles(source);
        Assert.Contains("__d.X = src.Count();", Lines(source), StringComparison.Ordinal);
    }

    // The name comparison applies to an inherited method as well, an exact match first
    [Fact]
    public void MapFromFindsInheritedMethodIgnoringCase()
    {
        var source = Source(
            "public class Base { public int Count() => 1; } public class Src : Base { } public class Dst { public int X { get; set; } }",
            "[MapFrom(nameof(Dst.X), \"count\")]",
            "public static partial Dst Map(Src src);",
            "[Mapper(AutoMap = false, NameComparison = StringComparison.OrdinalIgnoreCase)]");

        AssertCompiles(source);
        Assert.Contains("__d.X = src.Count();", Lines(source), StringComparison.Ordinal);
    }

    // A method the mapper class cannot call is not found: a protected one of a base class, and a private one
    [Theory]
    [InlineData("public class Base { protected int Count() => 1; } public class Src : Base { }")]
    [InlineData("public class Src { private int Count() => 1; public int Other() => Count(); }")]
    public void MapFromInaccessibleMethodEmitsDiagnostic(string types)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(
            types + " public class Dst { public int X { get; set; } }",
            "[MapFrom(nameof(Dst.X), \"Count\")]",
            "public static partial Dst Map(Src src);"));

        Assert.Equal("SMP0204", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private const string InterfaceSource = "public interface ISrcBase { int X { get; } Item Nested { get; } List<Item> Items { get; } } public interface ISrc : ISrcBase { int Y { get; } }";

    private const string InterfaceDestination = "public interface IDstBase { int X { get; set; } Item? Nested { get; set; } List<Item>? Items { get; set; } } public interface IDst : IDstBase { int Y { get; set; } }";

    // The automatic mapping from and to an interface, with the properties it inherits
    [Fact]
    public void AutoMapFromInterfaceIncludesInheritedProperties()
    {
        var source = Source(
            InterfaceSource + " public class Dst { public int X { get; set; } public int Y { get; set; } }",
            string.Empty,
            "public static partial Dst Map(ISrc src);",
            "[Mapper]");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("__d.X = src.X;", lines, StringComparison.Ordinal);
        Assert.Contains("__d.Y = src.Y;", lines, StringComparison.Ordinal);
    }

    [Fact]
    public void AutoMapToInterfaceIncludesInheritedProperties()
    {
        var source = Source(
            InterfaceDestination + " public class Src { public int X { get; set; } public int Y { get; set; } }",
            string.Empty,
            "public static partial void Map(Src src, IDst dst);",
            "[Mapper]");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("dst.X = src.X;", lines, StringComparison.Ordinal);
        Assert.Contains("dst.Y = src.Y;", lines, StringComparison.Ordinal);
    }

    // The targets of the attributes on an inherited property of an interface
    [Theory]
    [InlineData("[MapProperty(\"X\", nameof(Src.Z))]", "dst.X = src.Z;")]
    [InlineData("[MapFrom(\"X\", nameof(Src.Count))]", "dst.X = src.Count();")]
    [InlineData("[MapNested(\"Nested\", Mapper = nameof(MapItem))]", "dst.Nested = MapItem(src.Nested);")]
    [InlineData("[MapCollection(\"Items\", Mapper = nameof(MapItem))]", "dst.Items = __list;")]
    public void AttributeTargetsInheritedInterfaceProperty(string attribute, string expected)
    {
        var source = Source(
            InterfaceDestination + " public class Src { public int Z { get; set; } public Item Nested { get; set; } = new(); public List<Item> Items { get; set; } = []; public int Count() => 1; }",
            attribute,
            "public static partial void Map(Src src, IDst dst);");

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // The source of a constructor argument without a destination member of its own
    [Fact]
    public void ConstructorArgumentFromInheritedInterfaceProperty()
    {
        var source = Source(
            InterfaceSource + " public record Dst(int X, int Y);",
            string.Empty,
            "public static partial Dst Map(ISrc src);",
            "[Mapper]");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst(src.X, src.Y);", Lines(source), StringComparison.Ordinal);
    }

    // Strict reports an inherited property of an interface that is not mapped
    [Fact]
    public void StrictReportsInheritedInterfaceProperty()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            InterfaceDestination + " public class Src { public int Y { get; set; } }",
            "[MapIgnore(\"Nested\")] [MapIgnore(\"Items\")]",
            "public static partial void Map(Src src, IDst dst);",
            "[Mapper(Strict = true)]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0501", diagnostic.Id);
        Assert.Contains("property=[X]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}
