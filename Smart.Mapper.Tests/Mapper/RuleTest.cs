namespace Smart.Mapper;

public sealed class RuleTest
{
    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    [Fact]
    public void IgnoreRule1()
    {
        var config = new MapperConfig()
            .AddIgnoreRule(nameof(Source.DateTime));
        config.CreateMap<Source, Destination>();
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = 1, DateTime = new DateTime(2000, 12, 31, 23, 59, 59) });

        Assert.Equal(1, destination.Value);
        Assert.Equal(default, destination.DateTime);
    }

    [Fact]
    public void IgnoreByForAllMember()
    {
        var config = new MapperConfig()
            .AddIgnoreRule(typeof(DateTime));
        config.CreateMap<Source, Destination>();
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = 1, DateTime = new DateTime(2000, 12, 31, 23, 59, 59) });

        Assert.Equal(1, destination.Value);
        Assert.Equal(default, destination.DateTime);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class Source
    {
        public int Value { get; set; }

        public DateTime DateTime { get; set; }
    }

    public sealed class Destination
    {
        public int Value { get; set; }

        public DateTime DateTime { get; set; }
    }
}
