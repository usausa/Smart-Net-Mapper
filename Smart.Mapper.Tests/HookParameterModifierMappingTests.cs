namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Hooks get each argument the way their parameter takes it
public class HookParameterModifierMappingTests
{
    [Fact]
    public void MapStructWithAfterMapReturnsChangeMadeByRef()
    {
        var source = new MutableStructSource { Id = 3, Name = "Return" };

        var destination = TestMappers.MapStructWithAfterMap(source);

        Assert.Equal(3, destination.Id);
        Assert.Equal("Return", destination.Name);
        Assert.Equal(30, destination.Total);
    }

    [Fact]
    public void MapIntoStructWithAfterMapKeepsChangeMadeByRef()
    {
        var source = new MutableStructSource { Id = 4, Name = "Void" };
        var destination = new MutableStructDestination { Total = 1 };

        TestMappers.MapIntoStructWithAfterMap(source, ref destination);

        Assert.Equal(4, destination.Id);
        Assert.Equal("Void", destination.Name);
        Assert.Equal(41, destination.Total);
    }

    [Fact]
    public void MapWithInHookPassesSourceByIn()
    {
        var source = new ReadOnlyStructSource { Id = 5, Name = "In" };

        var destination = TestMappers.MapWithInHook(source);

        Assert.Equal(5, destination.Id);
        Assert.Equal("In#5", destination.Name);
    }

    [Fact]
    public void MapWithHookContextPassesContextToConverterAndCondition()
    {
        var source = new BasicSource { Id = 6, Name = "Name", Description = "Description" };
        var copy = new HookContext { Prefix = "> ", CopyDescription = true };
        var skip = new HookContext { Prefix = "- ", CopyDescription = false };

        var copied = TestMappers.MapWithHookContext(source, copy);
        var skipped = TestMappers.MapWithHookContext(source, skip);

        Assert.Equal(6, copied.Id);
        Assert.Equal("> Name", copied.Name);
        Assert.Equal("Description", copied.Description);
        Assert.Equal("- Name", skipped.Name);
        Assert.Null(skipped.Description);
    }
}
