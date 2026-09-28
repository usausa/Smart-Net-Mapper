namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A mapper whose source or destination is a collection, an array or a tuple is reported (SMP0007) and gets an
// implementation throwing, as it would map the members of the collection (Count, Capacity) and none of its elements: it
// used to create an empty collection, or fail on new T[]() and new (int, string)(). The collections are those of the
// framework (System.Collections and the namespaces under it: lists, sets, dictionaries and their interfaces, the immutable,
// frozen, concurrent and object model ones), and classes deriving from one (class ItemList : List<Item>). A type of its
// own that only implements IEnumerable<T>, such as a page of items with its count, is mapped by its members.
public class CollectionMapperTests
{
    private static string Source(string declarations) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Collections.ObjectModel;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int Id { get; set; } }
        public class ItemDto { public int Id { get; set; } }
        public class ItemList : List<Item> { public int Extra { get; set; } }
        public class Page<T> : IEnumerable<T>
        {
            public List<T> Items { get; set; } = [];
            public int Total { get; set; }
            public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public class PageDto { public int Total { get; set; } }
        public static partial class M
        {
            {{declarations}}
        }
        """;

    [Theory]
    [InlineData("[Mapper] public static partial List<ItemDto> Map(List<Item> source);", "List<Item>", "List<Item> source")]
    [InlineData("[Mapper] public static partial ItemDto[] Map(Item[] source);", "Item[]", "Item[] source")]
    [InlineData("[Mapper] public static partial IEnumerable<ItemDto> Map(IEnumerable<Item> source);", "IEnumerable<Item>", "IEnumerable<Item> source")]
    [InlineData("[Mapper] public static partial void Map(List<Item> source, List<ItemDto> destination);", "List<Item>", "List<Item> source")]
    [InlineData("[Mapper] public static partial Dictionary<int, ItemDto> Map(Dictionary<int, Item> source);", "Dictionary<int, Item>", "Dictionary<int, Item> source")]
    [InlineData("[Mapper] public static partial ItemDto Map(ImmutableArray<Item> source);", "ImmutableArray<Item>", "ImmutableArray<Item> source")]
    [InlineData("[Mapper] public static partial ItemDto Map(ArrayList source);", "ArrayList", "ArrayList source")]
    [InlineData("[Mapper] public static partial ItemDto Map(ItemList source);", "ItemList", "ItemList source")]
    [InlineData("[Mapper] public static partial IReadOnlyList<ItemDto> Map(Item source);", "IReadOnlyList<ItemDto>", "IReadOnlyList<ItemDto>")]
    [InlineData("[Mapper] public static partial ObservableCollection<ItemDto> Map(Item source);", "ObservableCollection<ItemDto>", "ObservableCollection<ItemDto>")]
    [InlineData("[Mapper] public static partial void Map(Item source, HashSet<ItemDto> destination);", "HashSet<ItemDto>", "HashSet<ItemDto> destination")]
    [InlineData("[Mapper] public static partial (int Id, string Name) Map(Item source);", "(int Id, string Name)", "(int Id, string Name)")]
    [InlineData("[Mapper] public static partial Tuple<int, string> Map(Item source);", "Tuple<int, string>", "Tuple<int, string>")]
    public void CollectionIsReported(string declaration, string type, string location)
    {
        var source = Source(declaration);

        var errors = GeneratorTestHelper.GetDiagnosticsAll(source).Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        var error = Assert.Single(errors);
        Assert.Equal("SMP0007", error.Id);
        var message = error.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains($"type=[{type}]", message, StringComparison.Ordinal);
        Assert.Contains("Select(ToDto).ToList()", message, StringComparison.Ordinal);
        Assert.Contains("[MapCollection]", message, StringComparison.Ordinal);
        Assert.Equal(location, error.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(error.Location.SourceSpan));
        Assert.Contains("throw new global::System.NotImplementedException(", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A type of its own implementing IEnumerable<T> is mapped by its members, and so is a pair
    [Theory]
    [InlineData("[Mapper] public static partial PageDto Map(Page<Item> source);", "__d.Total = source.Total;")]
    [InlineData("[Mapper] public static partial ItemDto Map(KeyValuePair<int, Item> source);", "var __d = new global::Test.ItemDto();")]
    public void OtherTypeIsMappedByMembers(string declaration, string expected)
    {
        var source = Source(declaration);

        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnosticsAll(source), static d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
