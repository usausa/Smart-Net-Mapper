namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Strict mode reports a mapping giving a value that may be null to a target that does not take null, which gets null
// or default for it without the mapping saying what it is to get (SMP0502): a nullable source member, a nullable
// result the generated code takes with !, and a source or an element of [MapNested] / [MapCollection] a mapper not
// taking null is not called for. NullValue, NullBehavior.Skip and [MapCondition] say what the target gets, and a
// reference declared with nullable annotations disabled is not taken as nullable here. It is reported at the attribute
// of the mapping, or at the method for the automatic mapping.
public class StrictNullableValueTests
{
    private static List<Diagnostic> Warnings(string source) =>
        GeneratorTestHelper.GetDiagnostics(source).Where(static d => d.Id == "SMP0502").ToList();

    // The line the marker comment is on
    private static int MarkedLine(string source) =>
        Array.FindIndex(source.Split('\n'), static line => line.Contains("/*here*/", StringComparison.Ordinal));

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);", string members = "", bool strict = true) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class ItemDst { public int V { get; set; } }
        #nullable disable
        public class Oblivious { public string Name { get; set; } public Item Child { get; set; } }
        #nullable enable
        public class Src
        {
            public string? Name { get; set; }
            public int? Count { get; set; }
            public string Plain { get; set; } = "";
            public Src? Parent { get; set; }
            public Item? Child { get; set; }
            public List<Item>? Items { get; set; }
            public List<Item?> Elements { get; set; } = new();
        }
        public class Dst
        {
            public string Name { get; set; } = "";
            public int Count { get; set; }
            public string Plain { get; set; } = "";
            public ItemDst Child { get; set; } = new();
            public List<ItemDst> Items { get; set; } = new();
            public List<ItemDst> Elements { get; set; } = new();
        }
        public class NullableDst { public string? Name { get; set; } public int? Count { get; set; } }
        public struct DstStruct { public int Count { get; set; } }
        public record Rec(string Name, string ParentPlain);
        public static partial class M
        {
            [Mapper(Strict = {{(strict ? "true" : "false")}}, AutoMap = false)]
            {{attributes}}
            {{mapper}}

            [Mapper]
            public static partial ItemDst MapItem(Item source);

            [Mapper]
            public static partial ItemDst? MapItemOrNull(Item? source);

            private static ItemDst MapAny(Item? source) => new() { V = source?.V ?? 0 };
        {{members}}
        }
        """;

    // A nullable source member to a target not taking null, at the attribute declaring the mapping
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name))] /*here*/", "Name")]
    [InlineData("[MapProperty(nameof(Dst.Count))] /*here*/", "Count")]
    [InlineData("[MapUsing(nameof(Dst.Plain), nameof(Describe))] /*here*/", "Plain")]
    [InlineData("[MapProperty(nameof(Dst.Plain), nameof(Src.Name), Converter = nameof(Trim))] /*here*/", "Plain")]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = nameof(MapItem))] /*here*/", "Child")]
    [InlineData("[MapNested(nameof(Dst.Child), nameof(Src.Child), Mapper = nameof(MapItemOrNull))] /*here*/", "Child")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))] /*here*/", "Items")]
    [InlineData("[MapCollection(nameof(Dst.Elements), Mapper = nameof(MapItem))] /*here*/", "Elements")]
    public void NullableValueIsReportedAtAttribute(string attributes, string target)
    {
        var source = Source(
            attributes,
            members: "    private static string? Describe(Src src) => src.Name;\n    private static string? Trim(string? value) => value?.Trim();");

        var warning = Assert.Single(Warnings(source));
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains($"target=[{target}]", warning.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Equal(MarkedLine(source), warning.Location.GetLineSpan().StartLinePosition.Line);
    }

    // The automatic mapping, at the method
    [Fact]
    public void AutomaticMappingIsReportedAtMethod()
    {
        const string source =
            """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public string? Name { get; set; } public int? Count { get; set; } public string Plain { get; set; } = ""; }
            public class Dst { public string Name { get; set; } = ""; public int Count { get; set; } public string Plain { get; set; } = ""; }
            public static partial class M
            {
                [Mapper(Strict = true)] /*here*/
                public static partial Dst Map(Src src);
            }
            """;

        var warnings = Warnings(source);
        Assert.Equal(["target=[Name]", "target=[Count]"], warnings.Select(static w => w.GetMessage(CultureInfo.InvariantCulture)).Select(static m => m[m.IndexOf("target=[", StringComparison.Ordinal)..]));
        Assert.All(warnings, w => Assert.Equal(MarkedLine(source), w.Location.GetLineSpan().StartLinePosition.Line));
    }

    // A value read through a nullable member is null for the target only as an expression (a constructor argument),
    // a statement leaving the target as it is
    [Fact]
    public void NullableIntermediateOfConstructorArgumentIsReported()
    {
        var source = Source(
            "[MapProperty(nameof(Rec.Name), nameof(Src.Plain))] [MapProperty(nameof(Rec.ParentPlain), \"Parent.Plain\")] /*here*/",
            "public static partial Rec Map(Src src);");

        var warning = Assert.Single(Warnings(source));
        Assert.Contains("target=[ParentPlain]", warning.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Equal(MarkedLine(source), warning.Location.GetLineSpan().StartLinePosition.Line);
    }

    [Fact]
    public void NullableIntermediateOfStatementIsNotReported()
    {
        Assert.Empty(Warnings(Source("[MapProperty(nameof(Dst.Plain), \"Parent.Plain\")]")));
    }

    // What the mapping says the target gets, a target taking null, a value that is not null, a mapper or a converter
    // taking null, and a reference declared with nullable annotations disabled
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name), NullValue = \"\")]")]
    [InlineData("[MapProperty(nameof(Dst.Name), NullBehavior = NullBehavior.Skip)]")]
    [InlineData("[MapProperty(nameof(Dst.Name))] [MapCondition(nameof(Dst.Name), nameof(IsSet))]")]
    [InlineData("[MapProperty(nameof(Dst.Plain))]")]
    [InlineData("[MapProperty(nameof(Dst.Plain), nameof(Src.Name), Converter = nameof(OrEmpty))]")]
    [InlineData("[MapProperty(nameof(Dst.Plain), nameof(Src.Name), Converter = nameof(Upper))]")]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = nameof(MapAny))]")]
    [InlineData("[MapCollection(nameof(Dst.Elements), Mapper = nameof(MapAny))]")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem), Strategy = CollectionStrategy.InPlace)]")]
    public void HandledValueIsNotReported(string attributes)
    {
        var source = Source(
            attributes,
            members: "    private static bool IsSet(string? value) => value is not null;\n" +
                     "    private static string OrEmpty(string? value) => value ?? \"\";\n" +
                     "    private static string Upper(string value) => value.ToUpperInvariant();");

        Assert.Empty(Warnings(source));
    }

    [Fact]
    public void NullableTargetIsNotReported()
    {
        Assert.Empty(Warnings(Source("[MapProperty(nameof(NullableDst.Name))] [MapProperty(nameof(NullableDst.Count))]", "public static partial NullableDst Map(Src src);")));
    }

    [Fact]
    public void ObliviousSourceIsNotReported()
    {
        Assert.Empty(Warnings(Source(
            "[MapProperty(nameof(Dst.Name))] [MapNested(nameof(Dst.Child), Mapper = nameof(MapItem))]",
            "public static partial Dst Map(Oblivious src);")));
    }

    // A return-type mapper returns default for a source declared nullable when it is null, which a return type not taking
    // null gets, reported at the method as the target (return)
    [Theory]
    [InlineData("public static partial Dst Map(Src? src);")]
    [InlineData("public static partial DstStruct Map(Src? src);")]
    public void NullableSourceToReturnNotTakingNullIsReportedAtMethod(string mapper)
    {
        var source = Source(string.Empty, mapper);

        var warning = Assert.Single(Warnings(source));
        Assert.Contains("target=[(return)]", warning.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Equal(
            Array.FindIndex(source.Split('\n'), static line => line.Contains("[Mapper(Strict = true", StringComparison.Ordinal)),
            warning.Location.GetLineSpan().StartLinePosition.Line);
    }

    // A return type taking null, a source that is not nullable, [return: MaybeNull], and a void mapper
    [Theory]
    [InlineData("public static partial Dst? Map(Src? src);")]
    [InlineData("public static partial Dst Map(Src src);")]
    [InlineData("[return: System.Diagnostics.CodeAnalysis.MaybeNull] public static partial Dst Map(Src? src);")]
    [InlineData("public static partial void Map(Src? src, Dst dst);")]
    public void ReturnTakingNullOrSourceNotNullableIsNotReported(string mapper)
    {
        Assert.Empty(Warnings(Source(string.Empty, mapper)));
    }

    [Fact]
    public void NotReportedWithoutStrict()
    {
        Assert.Empty(Warnings(Source("[MapProperty(nameof(Dst.Name))] [MapProperty(nameof(Dst.Count))]", strict: false)));
    }
}
