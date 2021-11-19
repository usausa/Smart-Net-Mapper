namespace Smart.Mapper.Struct;

using Xunit;

public class StructMapperTest
{
    //--------------------------------------------------------------------------------
    // Mapper
    //--------------------------------------------------------------------------------

    private static ObjectMapper CreateMapper()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>();
        return config.ToMapper();
    }

    [Fact]
    public void MapByActionNotWork()
    {
        using var mapper = CreateMapper();

        var destination = default(Destination);
        mapper.Map(new Source { Value = -1 }, destination);

        // Copy not work
        Assert.Equal(0, destination.Value);
    }

    [Fact]
    public void MapByFunc()
    {
        using var mapper = CreateMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapByParameterActionNotWork()
    {
        using var mapper = CreateMapper();

        var destination = default(Destination);
        mapper.Map(new Source { Value = -1 }, destination, 0);

        // Copy not work
        Assert.Equal(0, destination.Value);
    }

    [Fact]
    public void MapByParameterFunc()
    {
        using var mapper = CreateMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, 0);

        Assert.Equal(-1, destination.Value);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public struct Source
    {
        public int Value { get; set; }
    }

    public struct Destination
    {
        public int Value { get; set; }
    }
}
