namespace Smart.Mapper.Struct;

using Smart.Mapper.Functions;

using Xunit;

public class StructMapFromTest
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

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapFromByExpressionFunc()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom(s => s.Value - 1));
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

        Assert.Equal(-2, destination.Value);
    }

    [Fact]
    public void MapFromByFunc()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom((s, _) => s.Value));
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapFromByFuncContext()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom((s, _, c) => s.Value + (int)c.Parameter!));
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, -1);

        Assert.Equal(-2, destination.Value);
    }

    [Fact]
    public void MapFromByInterface()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom<CustomValueProvider>());
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, -1);

        Assert.Equal(-2, destination.Value);
    }

    [Fact]
    public void MapFromByInterface2()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom(new CustomValueProvider()));
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, -1);

        Assert.Equal(-2, destination.Value);
    }

    [Fact]
    public void MapFromByPath()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(d => d.Value, opt => opt.MapFrom("StringValue.Length"));
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(new Source { StringValue = "abc" });

        Assert.Equal(3, destination.Value);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public struct Source
    {
        public int Value { get; set; }

        public string? StringValue { get; set; }
    }

    public struct Destination
    {
        public int Value { get; set; }
    }

    public sealed class CustomValueProvider : IValueProvider<Source, Destination, int>
    {
        public int Provide(Source source, Destination destination, ResolutionContext context)
        {
            return source.Value + (int)context.Parameter!;
        }
    }
}
