namespace Smart.Mapper.Struct;

public sealed class NullableTest
{
    //--------------------------------------------------------------------------------
    // Nested
    //--------------------------------------------------------------------------------

    [Fact]
    public void StructToNullableByStructToStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<StructSource, StructDestination>();
        using var mapper = config.ToMapper();

        var destination = mapper.Map<StructSource, StructDestination?>(new StructSource { IntValue = -1 });

        Assert.Equal(-1, destination!.Value.IntValue);
    }

    [Fact]
    public void NullableToStructByStructToStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<StructSource, StructDestination>();
        using var mapper = config.ToMapper();

        var destination = mapper.Map<StructSource?, StructDestination>(new StructSource { IntValue = -1 });

        Assert.Equal(-1, destination.IntValue);

        var destination2 = mapper.Map<StructSource?, StructDestination>(null);

        Assert.Equal(0, destination2.IntValue);
    }

    [Fact]
    public void NullableToNullableByStructToStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<StructSource, StructDestination>();
        using var mapper = config.ToMapper();

        var destination = mapper.Map<StructSource?, StructDestination?>(new StructSource { IntValue = -1 });

        Assert.Equal(-1, destination!.Value.IntValue);

        var destination2 = mapper.Map<StructSource?, StructDestination?>(null);

        Assert.Null(destination2);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public struct StructSource
    {
        public int IntValue { get; set; }
    }

    public struct StructDestination
    {
        public int IntValue { get; set; }
    }
}
