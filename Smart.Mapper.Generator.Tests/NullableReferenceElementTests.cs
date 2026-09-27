namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A nullable reference element of [MapCollection] goes to an element mapper whose parameter does not take null only
// when it has a value, as the value of a nullable struct element does, and a null one gives default, as a null source
// of [MapNested] does; the loop used to pass the null on, which warned in the generated code (CS8604) and threw in the
// mapper. A mapper whose parameter takes null gets every element as before, and so does one a collection converter
// takes as a delegate.
public class NullableReferenceElementTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Lines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Source(string attributes, string mapper = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class ItemDto { public int V { get; set; } }
        public class Src { public List<Item?> Items { get; set; } = []; public IEnumerable<Item?> Sequence { get; set; } = []; }
        public class Dst { public List<ItemDto?> Items { get; set; } = []; public ItemDto?[] Sequence { get; set; } = []; }
        public static partial class M
        {
            [Mapper]
            public static partial ItemDto MapItem(Item source);

            [Mapper]
            public static partial ItemDto? MapMaybe(Item? source);

            [Mapper]
            public static partial void FillItem(Item source, ItemDto destination);

            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}
        }
        """;

    [Fact]
    public void NullElementGivesDefault()
    {
        var (generated, problems) = Build(Source(
            "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))] [MapCollection(nameof(Dst.Sequence), Mapper = nameof(MapItem))]"));

        Assert.Empty(problems);
        Assert.Contains("__dst[__i] = __src[__i] is { } __value ? MapItem(__value) : default!;", generated, StringComparison.Ordinal);
        Assert.Contains("__list.Add(__item is { } __value ? MapItem(__value) : default!);", generated, StringComparison.Ordinal);
    }

    // A void mapper fills an instance for an element with a value only
    [Fact]
    public void VoidMapperFillsElementWithValue()
    {
        var (generated, problems) = Build(Source("[MapCollection(nameof(Dst.Items), Mapper = nameof(FillItem), Strategy = CollectionStrategy.InPlace)]"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                            if (__srcSpan[__i] is { } __value)
                            {
                                var __dest = new global::Test.ItemDto();
                                FillItem(__value, __dest);
                                __dstColl.Add(__dest);
                            }
                            else
                            {
                                __dstColl.Add(default!);
                            }
            """),
            generated,
            StringComparison.Ordinal);
    }

    // A mapper whose parameter takes null gets every element as before
    [Fact]
    public void MapperTakingNullGetsEveryElement()
    {
        var (generated, problems) = Build(Source("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapMaybe))]"));

        Assert.Empty(problems);
        Assert.Contains("__dst[__i] = MapMaybe(__src[__i]);", generated, StringComparison.Ordinal);
    }
}
