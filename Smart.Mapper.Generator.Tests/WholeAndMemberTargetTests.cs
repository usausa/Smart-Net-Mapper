namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A member mapped as a whole by an attribute and a member of it mapped by a dotted path of an attribute (Item and
// Item.B) cannot both apply: the whole used to be dropped ([MapProperty]), to replace what the path wrote (the other
// attributes), or to be written twice in the object initializer (CS1912), and writing through the path after the
// whole would change the object the whole came from. They are reported as mapping the same target (SMP0101). The
// automatic mapping of the whole gives way to the dotted path, and [MapIgnore] of the whole leaves the path to be
// applied.
public class WholeAndMemberTargetTests
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
        public class Child { public int A { get; set; } public int B { get; set; } }
        public class Holder { public Child? Inner { get; set; } }
        public class Src
        {
            public Child? Item { get; set; }
            public Child? Other { get; set; }
            public int Y { get; set; }
            public List<Child> Items { get; set; } = [];
            public Child GetChild() => new();
        }
        public class Dst
        {
            public Child? Item { get; set; }
            public List<Child>? Items { get; set; }
            public Holder? Holder { get; set; }
        }
        public class InitDst { public Child? Item { get; init; } }
        public static partial class M
        {
            [Mapper]
            public static partial Child MapChild(Child source);

            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);

            private static Child Make(Src src) => new();
            private static int Number(Src src) => src.Y;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Item), nameof(Src.Other))] [MapProperty(\"Item.B\", nameof(Src.Y))]", "Item", "MapProperty, MapProperty (Item.B)")]
    [InlineData("[MapExpression(nameof(Dst.Item), \"new Child { A = 1 }\")] [MapProperty(\"Item.B\", nameof(Src.Y))]", "Item", "MapExpression, MapProperty (Item.B)")]
    [InlineData("[MapConstant(nameof(Dst.Item), null)] [MapConstant(\"Item.B\", 1)]", "Item", "MapConstant, MapConstant (Item.B)")]
    [InlineData("[MapUsing(nameof(Dst.Item), nameof(Make))] [MapExpression(\"Item.A\", \"1\")]", "Item", "MapUsing, MapExpression (Item.A)")]
    [InlineData("[MapFrom(nameof(Dst.Item), nameof(Src.GetChild))] [MapUsing(\"Item.B\", nameof(Number))]", "Item", "MapFrom, MapUsing (Item.B)")]
    [InlineData("[MapNested(nameof(Dst.Item), nameof(Src.Other), Mapper = nameof(MapChild))] [MapProperty(\"Item.B\", nameof(Src.Y))]", "Item", "MapNested, MapProperty (Item.B)")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapChild))] [MapConstant(\"Items.Capacity\", 1)]", "Items", "MapCollection, MapConstant (Items.Capacity)")]
    [InlineData("[MapProperty(\"Holder.Inner\", nameof(Src.Other))] [MapProperty(\"Holder.Inner.B\", nameof(Src.Y))]", "Holder.Inner", "MapProperty, MapProperty (Holder.Inner.B)")]
    public void WholeAndMemberOfItEmitDiagnostic(string attributes, string target, string kinds)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(attributes));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0101", diagnostic.Id);
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains($"target=[{target}]", message, StringComparison.Ordinal);
        Assert.Contains($"attributes=[{kinds}]", message, StringComparison.Ordinal);
    }

    // An init-only whole used to be written twice in the object initializer (CS1912)
    [Fact]
    public void InitOnlyWholeAndMemberOfItEmitDiagnostic()
    {
        var source = Source("[MapUsing(nameof(InitDst.Item), nameof(Make))] [MapProperty(\"Item.B\", nameof(Src.Y))]")
            .Replace("public static partial Dst Map(Src src);", "public static partial InitDst Map(Src src);", StringComparison.Ordinal);

        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        Assert.Equal("SMP0101", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // Different members of one member, and a member next to one of another name, do not conflict
    [Theory]
    [InlineData("[MapProperty(\"Item.A\", nameof(Src.Y))] [MapConstant(\"Item.B\", 1)]")]
    [InlineData("[MapProperty(nameof(Dst.Item), nameof(Src.Other))] [MapConstant(\"Holder.Inner.B\", 1)]")]
    public void SeparateMembersCompile(string attributes)
    {
        AssertCompiles(Source(attributes));
    }

    // The automatic mapping of the whole gives way to a dotted [MapProperty] into it
    [Fact]
    public void DottedPathTakesPrecedenceOverAutomaticMappingOfWhole()
    {
        var source = Source("[MapProperty(\"Item.B\", nameof(Src.Y))]", "[Mapper]");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Item.B = src.Y;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Item = src.Item;", generated, StringComparison.Ordinal);
    }

    // [MapIgnore] of the whole leaves the dotted path to be applied
    [Fact]
    public void IgnoredWholeLeavesDottedPathApplied()
    {
        var source = Source("[MapIgnore(nameof(Dst.Item))] [MapProperty(\"Item.B\", nameof(Src.Y))]", "[Mapper]");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Item ??= new global::Test.Child();", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Item.B = src.Y;", generated, StringComparison.Ordinal);
    }
}
