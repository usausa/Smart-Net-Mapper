namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A reference declared without nullable annotations (nullable disabled) says nothing about null, so it may hold null:
// the source, its members and the elements of its collections of such a type are taken as nullable, and the null
// handling of a nullable one applies to them. They used to be read as they are, and a null one got to a converter, a
// condition or a mapper that does not take null, or was read through.
public class ObliviousNullHandlingTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string attributes, string mapper = "public static partial void Map(Src src, Dst dst);", string members = "") =>
        $$"""
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class ItemDst { public int V { get; set; } }
        #nullable disable
        public class Src
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public Src Parent { get; set; }
            public Item Child { get; set; }
            public List<Item> Items { get; set; }
        }
        public class Dst
        {
            public string Name { get; set; }
            public int Code { get; set; }
            public string ParentName { get; set; }
            public ItemDst Child { get; set; }
            public List<ItemDst> Items { get; set; }
        }
        public record DstRecord(string Name, int Code);
        #nullable enable
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            [Mapper]
            public static partial ItemDst MapItem(Item source);
        {{members}}
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name), NullValue = \"Unknown\")]", "dst.Name = src.Name ?? \"Unknown\";")]
    [InlineData("[MapProperty(nameof(Dst.Code), NullValue = 0)]", "dst.Code = src.Code is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(src.Code) : 0;")]
    [InlineData("[MapProperty(nameof(Dst.Name), NullBehavior = NullBehavior.Skip)]", "if (src.Name is not null)")]
    [InlineData("[MapProperty(nameof(Dst.Code), NullBehavior = NullBehavior.Skip)]", "if (src.Code is not null)")]
    public void NullHandlingAppliesToObliviousSource(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullValueAppliesToObliviousConstructorArgument()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(DstRecord.Name), NullValue = \"Unknown\")] [MapProperty(nameof(DstRecord.Code), NullValue = 0)]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.DstRecord(src.Name ?? \"Unknown\", src.Code is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(src.Code) : 0)", generated, StringComparison.Ordinal);
    }

    // Without an attribute asking for null handling, the value goes as a nullable one does: as it is to a reference
    // target, and to a conversion only when it has a value
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name))]", "dst.Name = src.Name!;")]
    [InlineData("[MapProperty(nameof(Dst.Code))]", "dst.Code = src.Code is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(src.Code) : default!;")]
    public void ObliviousSourceIsTakenAsNullable(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The source parameter declared with nullable annotations disabled is checked before anything is mapped, as a
    // nullable one is, and so is the destination parameter of a void mapper; the custom parameters are passed on as
    // they come
    [Theory]
    [InlineData("public static partial void Map(Src src, Dst dst);", "if (src is null || dst is null)", "return;")]
    [InlineData("public static partial void Map(Src src, Dst dst, Item context);", "if (src is null || dst is null)", "return;")]
    [InlineData("public static partial Dst Map(Src src);", "if (src is null)", "return default!;")]
    public void ObliviousParametersAreChecked(string mapper, string check, string expected)
    {
        var source = Source("[MapProperty(nameof(Dst.Name))]", mapper)
            .Replace("[Mapper(AutoMap = false)]", "#nullable disable\n    [Mapper(AutoMap = false)]", StringComparison.Ordinal)
            .Replace(mapper, mapper + "\n#nullable enable", StringComparison.Ordinal);
        var (generated, problems) = Build(source);

        Assert.Empty(problems);
        Assert.Contains(check + "\n", generated.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // With nullable annotations enabled, a destination parameter declared not null is not checked
    [Fact]
    public void AnnotatedDestinationParameterIsNotChecked()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Name))]", "public static partial void Map(Src src, Dst dst);"));

        Assert.Empty(problems);
        Assert.DoesNotContain("dst is null", generated, StringComparison.Ordinal);
    }

    // A converter or a condition whose parameter does not take null is called for a value only
    [Fact]
    public void ObliviousSourceGoesToConverterForValueOnly()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Name), Converter = nameof(Upper))]",
            members: "    private static string Upper(string value) => value.ToUpperInvariant();"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Name is not null)", generated, StringComparison.Ordinal);
        Assert.Contains("dst.Name = Upper(src.Name);", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ObliviousSourceGoesToConditionForValueOnly()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Name))] [MapCondition(nameof(Dst.Name), nameof(IsSet))]",
            members: "    private static bool IsSet(string value) => value.Length > 0;"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Name is not null && IsSet(src.Name))", generated, StringComparison.Ordinal);
    }

    // A converter declared with nullable annotations disabled takes null, and gets the value as it is
    [Fact]
    public void ObliviousConverterTakesObliviousSource()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Name), Converter = nameof(Upper))]",
            members: "#nullable disable\n    private static string Upper(string value) => value?.ToUpperInvariant();\n#nullable enable"));

        Assert.Empty(problems);
        Assert.Contains("dst.Name = Upper(src.Name);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("if (src.Name is not null)", generated, StringComparison.Ordinal);
    }

    // The mapper of [MapNested] and the element mapper of [MapCollection] not taking null are called for a value only,
    // and the collection is checked
    [Fact]
    public void ObliviousNestedSourceGoesToMapperForValueOnly()
    {
        var (generated, problems) = Build(Source("[MapNested(nameof(Dst.Child), Mapper = nameof(MapItem))]"));

        Assert.Empty(problems);
        Assert.Contains("dst.Child = src.Child is not null ? MapItem(src.Child!) : default!;", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ObliviousCollectionAndElementsAreChecked()
    {
        var (generated, problems) = Build(Source("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Items is null)", generated, StringComparison.Ordinal);
        Assert.Contains("__src[__i] is { } __value ? MapItem(__value) : default!", generated, StringComparison.Ordinal);
    }

    // A path through a member declared so is read under its null check, as for [MapFrom]
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.ParentName), \"Parent.Name\")]")]
    [InlineData("[MapFrom(nameof(Dst.ParentName), \"Parent.Name\")]")]
    public void ObliviousIntermediateIsChecked(string attributes)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains("if (src.Parent is not null)", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.Parent!.Name", generated, StringComparison.Ordinal);
    }
}
