namespace Smart.Mapper;

using System.Collections.ObjectModel;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Collection targets InPlace can now fill, collection classes of their own, setters the mapper cannot call,
// constants and the culture / format resolution
public class CollectionTargetMappingTests
{
    private static List<MatrixSrcItem> MakeItems(params int[] values) =>
        [.. values.Select(static v => new MatrixSrcItem { Value = v })];

    // The null elements a mapper declared to take null returns are kept, and the member it maps into a target not
    // annotated as nullable gets its result
    [Fact]
    public void MapNullableElementsKeepsNullElements()
    {
        var destination = TestMappers.MapNullableElements(new NullableElementSource
        {
            Items = [new MatrixSrcItem { Value = 1 }, null],
            Head = new MatrixSrcItem { Value = 3 }
        });

        Assert.Collection(destination.Items, static x => Assert.Equal(1, x!.Value), Assert.Null);
        Assert.Collection(destination.Array, static x => Assert.Equal(1, x!.Value), Assert.Null);
        Assert.Equal(3, destination.Head.Value);
    }

    // A dictionary interface target gets a Dictionary<TKey, TValue> filled with the mapped pairs
    [Fact]
    public void MapDictionaryFillsDictionary()
    {
        var destination = TestMappers.MapDictionary(new DictionarySource
        {
            Items = new Dictionary<string, MatrixSrcItem> { ["a"] = new() { Value = 1 }, ["b"] = new() { Value = 2 } }
        });

        Assert.IsType<Dictionary<string, MatrixDstItem>>(destination.Items);
        Assert.Equal(2, destination.Items["b"].Value);
        Assert.Equal(1, destination.Editable["a"].Value);
        Assert.Equal(2, destination.Editable.Count);
    }

    [Fact]
    public void MapInPlaceGetOnlyRefillsHeldInstance()
    {
        var destination = new InPlaceGetOnlyDestination();
        var items = destination.Items;

        TestMappers.MapInPlaceGetOnly(new InPlaceTargetSource { Items = MakeItems(1, 2) }, destination);

        Assert.Same(items, destination.Items);
        Assert.Collection(items, static x => Assert.Equal(1, x.Value), static x => Assert.Equal(2, x.Value));
    }

    [Fact]
    public void MapInPlaceGetOnlyLeavesNullTarget()
    {
        var destination = new InPlaceNullGetOnlyDestination();

        TestMappers.MapInPlaceNullGetOnly(new InPlaceTargetSource { Items = MakeItems(1) }, destination);

        Assert.Null(destination.Items);
    }

    [Fact]
    public void MapInPlaceObservableCreatesAndRefills()
    {
        var destination = new InPlaceObservableDestination();

        TestMappers.MapInPlaceObservable(new InPlaceTargetSource { Items = MakeItems(1, 2) }, destination);

        var created = destination.Items;
        Assert.NotNull(created);
        Assert.Collection(created, static x => Assert.Equal(1, x.Value), static x => Assert.Equal(2, x.Value));

        var changes = 0;
        created.CollectionChanged += (_, _) => changes++;
        TestMappers.MapInPlaceObservable(new InPlaceTargetSource { Items = MakeItems(3) }, destination);

        Assert.Same(created, destination.Items);
        Assert.Collection(created, static x => Assert.Equal(3, x.Value));
        Assert.Equal(2, changes);
    }

    [Fact]
    public void MapCollectionClassBuildsClassesOfTheirOwn()
    {
        var source = new CollectionClassSource();
        source.Items.AddRange(MakeItems(1, 2, 3));

        var destination = TestMappers.MapCollectionClass(source);

        Assert.IsType<MatrixDstItemList>(destination.Items);
        Assert.Collection(destination.Items, static x => Assert.Equal(1, x.Value), static x => Assert.Equal(2, x.Value), static x => Assert.Equal(3, x.Value));
        Assert.IsType<ObservableCollection<MatrixDstItem>>(destination.Observed);
        Assert.Equal(3, destination.Observed.Count);
    }

    [Fact]
    public void MapPrivateSetterLeavesPropertyAsItIs()
    {
        var destination = TestMappers.MapPrivateSetter(new PrivateSetterSource { Id = 5, Name = "changed" });

        Assert.Equal(5, destination.Id);
        Assert.Equal("keep", destination.Name);
    }

    [Fact]
    public void MapConstantConversionAssignsConvertedConstants()
    {
        var destination = TestMappers.MapConstantConversion(new PrivateSetterSource());

        Assert.Equal(1L, destination.Big);
        Assert.Equal((byte)2, destination.Small);
        Assert.Equal(3, destination.Count);
    }

    [Theory]
    [InlineData("profile", "1,235")]
    [InlineData("method format", "1,234.50")]
    [InlineData("method culture", "1.235")]
    public void CultureAndFormatComeFromMethodThenProfile(string mapper, string expected)
    {
        var source = new CultureProfileSource { Amount = 1234.5m };

        var destination = mapper switch
        {
            "profile" => CultureProfileMappers.MapProfileFormat(source),
            "method format" => CultureProfileMappers.MapMethodFormat(source),
            _ => CultureProfileMappers.MapMethodCulture(source)
        };

        Assert.Equal(expected, destination.Amount);
    }
}
