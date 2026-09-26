namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// A mapper declared with nullable annotations builds under WarningsAsErrors=nullable, and a null source
// or destination maps nothing: default is returned, or a void mapper leaves the destination as it is
public class NullableSignatureMappingTests
{
    [Fact]
    public void MapNullableSignatureMapsSource()
    {
        var source = new BasicSource { Id = 7, Name = "Name", Description = "Description" };

        var withContext = TestMappers.MapNullableSignature(source, new HookContext { Prefix = "+" });
        var withoutContext = TestMappers.MapNullableSignature(source, null);

        Assert.NotNull(withContext);
        Assert.Equal(7, withContext.Id);
        Assert.Equal("Name", withContext.Name);
        Assert.Equal("+Description", withContext.Description);
        Assert.NotNull(withoutContext);
        Assert.Equal("-Description", withoutContext.Description);
    }

    [Fact]
    public void MapNullableSignatureReturnsDefaultForNullSource()
    {
        Assert.Null(TestMappers.MapNullableSignature(null, new HookContext { Prefix = "+" }));
    }

    [Fact]
    public void MapNullableSourceToDestinationReturnsDefaultForNullSource()
    {
        var mapped = TestMappers.MapNullableSourceToDestination(new BasicSource { Id = 8, Name = "Name", Description = "Description" });
        var empty = TestMappers.MapNullableSourceToDestination(null);

        Assert.Equal(8, mapped.Id);
        Assert.Equal("Name", mapped.Name);
        Assert.Null(empty);
    }

    [Fact]
    public void MapNullableSourceToStructReturnsDefaultForNullSource()
    {
        var mapped = TestMappers.MapNullableSourceToStruct(new BasicSource { Id = 9, Name = "Name", Description = "Description" });
        var empty = TestMappers.MapNullableSourceToStruct(null);

        Assert.Equal(9, mapped.Id);
        Assert.Equal("Name", mapped.Name);
        Assert.Equal(0, empty.Id);
        Assert.Null(empty.Name);
        Assert.Equal(0, empty.Total);
    }

    [Fact]
    public void MapNullableSourceIntoLeavesDestinationForNullSource()
    {
        var mapped = new BasicDestination();
        var kept = new BasicDestination { Id = 1, Name = "Keep", Description = "Keep" };

        TestMappers.MapNullableSourceInto(new BasicSource { Id = 10, Name = "Name", Description = "Description" }, mapped);
        TestMappers.MapNullableSourceInto(null, kept);

        Assert.Equal(10, mapped.Id);
        Assert.Equal("Name", mapped.Name);
        Assert.Equal(1, kept.Id);
        Assert.Equal("Keep", kept.Name);
        Assert.Equal("Keep", kept.Description);
    }

    [Fact]
    public void MapIntoNullableDestinationDoesNothingForNullDestination()
    {
        var source = new BasicSource { Id = 11, Name = "Name", Description = "Description" };
        var destination = new BasicDestination();

        TestMappers.MapIntoNullableDestination(source, null);
        TestMappers.MapIntoNullableDestination(source, destination);

        Assert.Equal(11, destination.Id);
        Assert.Equal("Name", destination.Name);
        Assert.Equal("Description", destination.Description);
    }
}
