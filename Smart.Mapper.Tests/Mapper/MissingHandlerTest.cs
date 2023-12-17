namespace Smart.Mapper;

using Smart.Mapper.Handlers;

public sealed class MissingHandlerTest
{
    //--------------------------------------------------------------------------------
    // Mapper
    //--------------------------------------------------------------------------------

    [Fact]
    public void UseDefaultMapper()
    {
        using var mapper = new MapperConfig()
            .AddDefaultMapper()
            .ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MissingHandlerPriority()
    {
        using var mapper = new MapperConfig()
            .AddMissingHandler(new DefaultMapperHandler { Priority = 0 })
            .ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapperNotFound()
    {
        using var mapper = new MapperConfig().ToMapper();

        Assert.Throws<InvalidOperationException>(() => mapper.Map<Source, Destination>(new Source { Value = -1 }));
        Assert.Throws<InvalidOperationException>(() => mapper.Map<Source, Destination>("dummy", new Source { Value = -1 }));
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class Source
    {
        public int Value { get; set; }
    }

    public sealed class Destination
    {
        public int Value { get; set; }
    }
}
