namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// InPlace into a dictionary interface: a null target gets a Dictionary<TKey, TValue>, and one holding an instance is
// cleared and refilled
public class InPlaceDictionaryMappingTests
{
    [Fact]
    public void NullTargetGetsDictionary()
    {
        var source = new InPlaceDictionarySource { Items = { ["a"] = new ReferenceElementItem { Value = 1 } } };
        var destination = new InPlaceDictionaryDestination();

        TestMappers.MapInPlaceDictionary(source, destination);

        Assert.IsType<Dictionary<string, ReferenceElementItemDto>>(destination.Items);
        Assert.Equal(source.Items["a"].Value, destination.Items["a"].Value);
    }

    [Fact]
    public void InstanceIsRefilled()
    {
        var items = new SortedDictionary<string, ReferenceElementItemDto> { ["old"] = new() };
        var destination = new InPlaceDictionaryDestination { Items = items };

        TestMappers.MapInPlaceDictionary(new InPlaceDictionarySource { Items = { ["b"] = new ReferenceElementItem { Value = 2 } } }, destination);

        Assert.Same(items, destination.Items);
        Assert.Equal(2, Assert.Single(items).Value.Value);
    }
}
