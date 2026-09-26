namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Converter classes nested in another class, and a dotted path to a parsable type
public class ConverterTypeResolutionMappingTests
{
    [Fact]
    public void MapWithNestedValueConverterUsesSpecializedMethod()
    {
        var destination = TestMappers.MapWithNestedValueConverter(new NestedConverterSource { Value = 123 });

        Assert.Equal("N:123", destination.Value);
    }

    [Fact]
    public void MapWithNestedCollectionConverterBuildsCollection()
    {
        var destination = TestMappers.MapWithNestedCollectionConverter(new NestedConverterSource { Value = 5, Items = [1, 2, 3] });

        Assert.Equal("5", destination.Value);
        Assert.Collection(destination.Items, static x => Assert.Equal("3", x), static x => Assert.Equal("2", x), static x => Assert.Equal("1", x));
    }

    [Fact]
    public void MapParsePathParsesDottedSource()
    {
        var destination = TestMappers.MapParsePath(new ParsePathSource { Child = new ParsePathChild { Text = "42" } });

        Assert.Equal(42, destination.Id.Value);
    }
}
