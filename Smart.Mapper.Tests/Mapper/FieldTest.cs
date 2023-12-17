namespace Smart.Mapper;

public sealed class FieldTest
{
    //--------------------------------------------------------------------------------
    // Mapper
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapPropertyToField()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, FieldDestination>();
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<Source, FieldDestination>(new Source { Value = -1 }).Value);
    }

    [Fact]
    public void MapFieldToProperty()
    {
        var config = new MapperConfig();
        config.CreateMap<FieldSource, Destination>();
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<FieldSource, Destination>(new FieldSource { Value = -1 }).Value);
    }

    [Fact]
    public void MapFieldToField()
    {
        var config = new MapperConfig();
        config.CreateMap<FieldSource, FieldDestination>();
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<FieldSource, FieldDestination>(new FieldSource { Value = -1 }).Value);
    }

    [Fact]
    public void MapFieldToFieldManual()
    {
        var config = new MapperConfig();
        config.CreateMap<FieldSource, FieldDestination>()
            .ForMember(x => x.Value, opt => opt.MapFrom(x => x.Value));
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<FieldSource, FieldDestination>(new FieldSource { Value = -1 }).Value);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class Source
    {
        public int Value { get; set; }
    }

#pragma warning disable CA1051
#pragma warning disable SA1401
    public sealed class FieldSource
    {
        public int Value;
    }
#pragma warning restore SA1401
#pragma warning restore CA1051

    public sealed class Destination
    {
        public int Value { get; set; }
    }

#pragma warning disable CA1051
#pragma warning disable SA1401
    public sealed class FieldDestination
    {
        public int Value;
    }
#pragma warning restore SA1401
#pragma warning restore CA1051
}
