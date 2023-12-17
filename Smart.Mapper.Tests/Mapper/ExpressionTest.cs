namespace Smart.Mapper;

public sealed class ExpressionTest
{
    [Fact]
    public void InvalidPropertyExpression()
    {
        var config = new MapperConfig();
        Assert.Throws<ArgumentException>(() =>
            config.CreateMap<Source, Destination>()
                .ForMember(x => 1, opt => opt.Const(0)));
    }

    [Fact]
    public void InvalidTargetProperty()
    {
        var config = new MapperConfig();
        Assert.Throws<ArgumentException>(() =>
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.ReadOnlyValue, opt => opt.Const(0)));
    }

    [Fact]
    public void InvalidPath()
    {
        var config = new MapperConfig();
        Assert.Throws<ArgumentException>(() =>
            config.CreateMap<Source, Destination>()
            .ForMember(x => x.Value, opt => opt.MapFrom("Invalid")));
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class Source
    {
    }

    public sealed class Destination
    {
        public int Value { get; set; }

        public int ReadOnlyValue => Value;
    }
}
