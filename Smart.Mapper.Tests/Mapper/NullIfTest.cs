namespace Smart.Mapper;

public sealed class NullIfTest
{
    //--------------------------------------------------------------------------------
    // MapFrom
    //--------------------------------------------------------------------------------

    [Fact]
    public void NullIfForClass()
    {
        var config = new MapperConfig();
        config.CreateMap<ClassSource, ClassDestination>()
            .ForMember(d => d.Value, opt => opt.NullIf("-"));
        using var mapper = config.ToMapper();

        var destination1 = mapper.Map<ClassSource, ClassDestination>(new ClassSource { Value = "x" });
        Assert.Equal("x", destination1.Value);

        var destination2 = mapper.Map<ClassSource, ClassDestination>(new ClassSource());
        Assert.Equal("-", destination2.Value);
    }

    [Fact]
    public void NullIfForNullable()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableSource, NullableDestination>()
            .ForMember(d => d.Value, opt => opt.NullIf(-1));
        using var mapper = config.ToMapper();

        var destination1 = mapper.Map<NullableSource, NullableDestination>(new NullableSource { Value = -1 });
        Assert.Equal(-1, destination1.Value);

        var destination2 = mapper.Map<NullableSource, NullableDestination>(new NullableSource());
        Assert.Equal(-1, destination2.Value);
    }

    [Fact]
    public void NullIfByDefault()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.NullIf("-"));
        config.CreateMap<ClassSource, ClassDestination>();
        using var mapper = config.ToMapper();

        var destination = mapper.Map<ClassSource, ClassDestination>(new ClassSource());
        Assert.Equal("-", destination.Value);
    }

    [Fact]
    public void NullIfByMapping()
    {
        var config = new MapperConfig();
        config.CreateMap<ClassSource, ClassDestination>()
            .Default(opt => opt.NullIf("-"));
        using var mapper = config.ToMapper();

        var destination = mapper.Map<ClassSource, ClassDestination>(new ClassSource());
        Assert.Equal("-", destination.Value);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class ClassSource
    {
        public string? Value { get; set; }
    }

    public sealed class ClassDestination
    {
        public string? Value { get; set; }
    }

    public sealed class NullableSource
    {
        public int? Value { get; set; }

        public string? StringValue { get; set; }
    }

    public sealed class NullableDestination
    {
        public int? Value { get; set; }
    }
}
