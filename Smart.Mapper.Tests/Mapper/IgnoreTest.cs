namespace Smart.Mapper;

public sealed class IgnoreTest
{
    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    [Fact]
    public void IgnoreByForMember()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(x => x.Value1, opt => opt.Ignore());
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value1 = 1, Value2 = -2 });

        Assert.Equal(0, destination.Value1);
        Assert.Equal(-2, destination.Value2);
    }

    [Fact]
    public void IgnoreByForAllMember()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForAllMember(opt => opt.Ignore());
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value1 = 1, Value2 = -2 });

        Assert.Equal(0, destination.Value1);
        Assert.Equal(0, destination.Value2);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class Source
    {
        public int Value1 { get; set; }

        public int Value2 { get; set; }
    }

    public sealed class Destination
    {
        public int Value1 { get; set; }

        public int Value2 { get; set; }
    }
}
