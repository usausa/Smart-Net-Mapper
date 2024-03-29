namespace Smart.Mapper;

using Smart.Mapper.Functions;

public sealed class MapFromTest
{
    //--------------------------------------------------------------------------------
    // MapFrom
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapFromByExpressionProperty()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom(s => s.Value));
        using var mapper = config.ToMapper();

        var destination = new Destination();
        mapper.Map(new Source { Value = -1 }, destination);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapFromByExpressionFunc()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom(s => s.Value - 1));
        using var mapper = config.ToMapper();

        var destination = new Destination();
        mapper.Map(new Source { Value = -1 }, destination);

        Assert.Equal(-2, destination.Value);
    }

    [Fact]
    public void MapFromByFunc()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom((s, d) => d.Value + s.Value));
        using var mapper = config.ToMapper();

        var destination = new Destination { Value = -1 };
        mapper.Map(new Source { Value = -1 }, destination);

        Assert.Equal(-2, destination.Value);
    }

    [Fact]
    public void MapFromByFuncContext()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom((s, d, c) => d.Value + s.Value + (int)c.Parameter!));
        using var mapper = config.ToMapper();

        var destination = new Destination { Value = -1 };
        mapper.Map(new Source { Value = -1 }, destination, -1);

        Assert.Equal(-3, destination.Value);
    }

    [Fact]
    public void MapFromByInterface()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom<CustomValueProvider>());
        using var mapper = config.ToMapper();

        var destination = new Destination { Value = -1 };
        mapper.Map(new Source { Value = -1 }, destination, -1);

        Assert.Equal(-3, destination.Value);
    }

    [Fact]
    public void MapFromByInterface2()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom(new CustomValueProvider()));
        using var mapper = config.ToMapper();

        var destination = new Destination { Value = -1 };
        mapper.Map(new Source { Value = -1 }, destination, -1);

        Assert.Equal(-3, destination.Value);
    }

    [Fact]
    public void MapFromByPath()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom($"String{opt.DestinationMember.Name}.Length"));
        using var mapper = config.ToMapper();

        var destination = new Destination();
        mapper.Map(new Source { StringValue = "abc" }, destination);

        Assert.Equal(3, destination.Value);
    }

    [Fact]
    public void MapFromByPath2()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom(nameof(Source.FieldValue)));
        using var mapper = config.ToMapper();

        var destination = new Destination();
        mapper.Map(new Source { FieldValue = -1 }, destination);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapFromByPathInvalid()
    {
        var config = new MapperConfig();
        Assert.Throws<ArgumentException>(() =>
            config.CreateMap<Source, Destination>().ForMember(d => d.Value, opt => opt.MapFrom(string.Empty)));
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

#pragma warning disable CA1051
#pragma warning disable SA1401
    public sealed class Source
    {
        public int Value { get; set; }

        public string? StringValue { get; set; }

        public int FieldValue;
    }
#pragma warning restore SA1401
#pragma warning restore CA1051

    public sealed class Destination
    {
        public int Value { get; set; }
    }

    public sealed class CustomValueProvider : IValueProvider<Source, Destination, int>
    {
        public int Provide(Source source, Destination destination, ResolutionContext context)
        {
            return destination.Value + source.Value + (int)context.Parameter!;
        }
    }
}
