namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A return-type mapper sets an init-only member, or a required one the constructor called does not set, that
// [MapNested] / [MapCollection] maps in its object initializer: the value is made before construction into a local,
// as the one of a constructor argument is, with the null handling, the element annotations and the mappers of the
// attributes as before. They used to be reported (SMP0212). A void mapper cannot assign an init-only member, which is
// still reported (SMP0212), and InPlace cannot refill a required member made before construction (SMP0219), while it
// still refills the instance an init-only member holds.
public class InitializerCollectionTargetTests
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

    private static string Source(string destination, string attributes, string mapper = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int V { get; set; } }
        public class ItemDto { public int V { get; set; } }
        public class Src { public int Id { get; set; } public Item? Head { get; set; } public List<Item>? Items { get; set; } public List<Item> All { get; set; } = []; }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            public static partial ItemDto MapItem(Item source);

            [Mapper]
            public static partial void FillItem(Item source, ItemDto destination);

            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}
        }
        """;

    [Fact]
    public void InitOnlyTargetsAreSetInInitializer()
    {
        var (generated, problems) = Build(Source(
            "public class Dst { public ItemDto? Head { get; init; } public IReadOnlyList<ItemDto> Items { get; init; } = []; }",
            "[MapNested(nameof(Dst.Head), Mapper = nameof(MapItem))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))]"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    global::Test.ItemDto __init0;
                    __init0 = src.Head is not null ? MapItem(src.Head!) : default!;
                    global::System.Collections.Generic.IReadOnlyList<global::Test.ItemDto> __init1;
                    if (src.Items is null)
                    {
                        __init1 = default!;
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            Lines("""
                    var __d = new global::Test.Dst()
                    {
                        Head = __init0,
                        Items = __init1,
                    };
            """),
            generated,
            StringComparison.Ordinal);
    }

    // A required member, of a record constructed with arguments as well, and a void element mapper
    [Fact]
    public void RequiredTargetsAreSetInInitializer()
    {
        var (generated, problems) = Build(Source(
            "public record Dst(int Id) { public required List<ItemDto> All { get; init; } public required ItemDto? Head { get; set; } }",
            "[MapCollection(nameof(Dst.All), Mapper = nameof(FillItem))] [MapNested(nameof(Dst.Head), Mapper = nameof(FillItem))] [MapProperty(nameof(Dst.Id))]"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    global::Test.ItemDto __init0;
                    if (src.Head is not null)
                    {
                        var __nested_Head = new global::Test.ItemDto();
                        FillItem(src.Head!, __nested_Head);
                        __init0 = __nested_Head;
                    }
                    else
                    {
                        __init0 = default!;
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            Lines("""
                    var __d = new global::Test.Dst(src.Id)
                    {
                        Head = __init0,
                        All = __init1,
                    };
            """),
            generated,
            StringComparison.Ordinal);
    }

    // A void mapper cannot assign an init-only member, and InPlace cannot refill a required member made before
    // construction
    [Theory]
    [InlineData("public class Dst { public List<ItemDto> All { get; init; } = []; }", "[MapCollection(nameof(Dst.All), Mapper = nameof(MapItem))]", "public static partial void Map(Src src, Dst dst);", "SMP0212")]
    [InlineData("public class Dst { public ItemDto? Head { get; init; } }", "[MapNested(nameof(Dst.Head), Mapper = nameof(MapItem))]", "public static partial void Map(Src src, Dst dst);", "SMP0212")]
    [InlineData("public class Dst { public required List<ItemDto> All { get; set; } }", "[MapCollection(nameof(Dst.All), Mapper = nameof(MapItem), Strategy = CollectionStrategy.InPlace)]", "public static partial Dst Map(Src src);", "SMP0219")]
    public void UnassignableTargetIsReported(string destination, string attributes, string mapper, string id)
    {
        var (_, problems) = Build(Source(destination, attributes, mapper));

        Assert.Equal(id, Assert.Single(problems));
    }

    // InPlace still refills the instance an init-only member holds
    [Fact]
    public void InPlaceRefillsInitOnlyMember()
    {
        var (generated, problems) = Build(Source(
            "public class Dst { public List<ItemDto> All { get; init; } = []; }",
            "[MapCollection(nameof(Dst.All), Mapper = nameof(MapItem), Strategy = CollectionStrategy.InPlace)]"));

        Assert.Empty(problems);
        Assert.Contains("var __d = new global::Test.Dst();", generated, StringComparison.Ordinal);
        Assert.Contains("if (__d.All is not null)", generated, StringComparison.Ordinal);
    }
}
