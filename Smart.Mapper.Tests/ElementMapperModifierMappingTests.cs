namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Element mappers, collection converters and the culture overload of a value converter get each argument
// the way their parameter takes it
public class ElementMapperModifierMappingTests
{
    private static List<MatrixSrcItem> MakeItems(int first, int count) =>
        [.. Enumerable.Range(first, count).Select(static i => new MatrixSrcItem { Value = i })];

    [Fact]
    public void MapMultiCollectionInPassesElementsByIn()
    {
        var source = new MultiCollectionSource
        {
            Lines = MakeItems(1, 2),
            Items = MakeItems(10, 2),
            Values = [.. MakeItems(20, 1)],
            Sequence = MakeItems(30, 2),
            Optional = MakeItems(40, 1),
            Child = new NestedObjectSourceChild { Value = 7, Text = "Child" }
        };

        var destination = TestMappers.MapMultiCollectionIn(source);

        Assert.Collection(destination.Lines, static x => Assert.Equal(101, x.Value), static x => Assert.Equal(102, x.Value));
        Assert.Collection(destination.Items, static x => Assert.Equal(110, x.Value), static x => Assert.Equal(111, x.Value));
        Assert.Collection(destination.Values, static x => Assert.Equal(120, x.Value));
        Assert.Collection(destination.Sequence.Select(static x => x.Value).Order(), static x => Assert.Equal(130, x), static x => Assert.Equal(131, x));
        Assert.NotNull(destination.Optional);
        Assert.Collection(destination.Optional, static x => Assert.Equal(140, x.Value));
        Assert.NotNull(destination.Child);
        Assert.Equal(107, destination.Child.Value);
        Assert.Equal("Child", destination.Child.Text);
    }

    [Fact]
    public void MapPathFillsStructInstancesByRef()
    {
        var source = new PathSource
        {
            Points = [new PointSource { X = 1, Y = 2 }, new PointSource { X = 3, Y = 4 }],
            Route = [new PointSource { X = 5, Y = 6 }],
            Origin = new PointSource { X = 7, Y = 8 }
        };

        var destination = TestMappers.MapPath(source);

        Assert.Collection(
            destination.Points,
            static p => Assert.Equal((1, 2), (p.X, p.Y)),
            static p => Assert.Equal((3, 4), (p.X, p.Y)));
        Assert.Collection(destination.Route, static p => Assert.Equal((5, 6), (p.X, p.Y)));
        Assert.Equal(7, destination.Origin.X);
        Assert.Equal(8, destination.Origin.Y);
    }

    [Fact]
    public void MapListWithInConverterPassesArgumentsByIn()
    {
        var source = new MatrixListSource { Items = MakeItems(1, 2) };

        var destination = TestMappers.MapListWithInConverter(source);

        Assert.NotNull(destination.Items);
        Assert.Collection(destination.Items, static x => Assert.Equal(1, x.Value), static x => Assert.Equal(2, x.Value));
    }

    [Fact]
    public void MapWithCultureReferencePassesCultureByIn()
    {
        var source = new CultureReferenceSource { Amount = 1234.5m };

        var destination = TestMappers.MapWithCultureReference(source);

        Assert.Equal("1234,5 (de-DE)", destination.Amount);
    }
}
