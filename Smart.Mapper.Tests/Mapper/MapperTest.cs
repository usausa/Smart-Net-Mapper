namespace Smart.Mapper;

using Xunit;

public class MapperTest
{
    //--------------------------------------------------------------------------------
    // Mapper
    //--------------------------------------------------------------------------------

    private static SmartMapper CreateMapper()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>();
        return config.ToMapper();
    }

    [Fact]
    public void MapByAction()
    {
        using var mapper = CreateMapper();

        var destination = new Destination();
        mapper.Map(new Source { Value = -1 }, destination);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapAlsoByAction()
    {
        using var mapper = CreateMapper();

        var destination = mapper.MapAlso(new Source { Value = -1 }, new Destination());

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapByFunc()
    {
        using var mapper = CreateMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapByParameterAction()
    {
        using var mapper = CreateMapper();

        var destination = new Destination();
        mapper.Map(new Source { Value = -1 }, destination, 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapAlsoByParameterAction()
    {
        using var mapper = CreateMapper();

        var destination = mapper.MapAlso(new Source { Value = -1 }, new Destination(), 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void MapByParameterFunc()
    {
        using var mapper = CreateMapper();

        var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheMapByAction()
    {
        using var mapper = CreateMapper();
        var action = mapper.GetMapperAction<Source, Destination>();

        var destination = new Destination();
        action(new Source { Value = -1 }, destination);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheMapByFunc()
    {
        using var mapper = CreateMapper();
        var func = mapper.GetMapperFunc<Source, Destination>();

        var destination = func(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheMapByParameterAction()
    {
        using var mapper = CreateMapper();
        var action = mapper.GetParameterMapperAction<Source, Destination>();

        var destination = new Destination();
        action(new Source { Value = -1 }, destination, 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheMapByParameterFunc()
    {
        using var mapper = CreateMapper();
        var func = mapper.GetParameterMapperFunc<Source, Destination>();

        var destination = func(new Source { Value = -1 }, 0);

        Assert.Equal(-1, destination.Value);
    }

    //--------------------------------------------------------------------------------
    // Profile
    //--------------------------------------------------------------------------------

    private const string Profile = "sub";

    private static SmartMapper CreateProfileMapper()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>(Profile);
        return config.ToMapper();
    }

    [Fact]
    public void ProfileMapByAction()
    {
        using var mapper = CreateProfileMapper();

        var destination = new Destination();
        mapper.Map(Profile, new Source { Value = -1 }, destination);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void ProfileMapAlsoByAction()
    {
        using var mapper = CreateProfileMapper();

        var destination = mapper.MapAlso(Profile, new Source { Value = -1 }, new Destination());

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void ProfileMapByFunc()
    {
        using var mapper = CreateProfileMapper();

        var destination = mapper.Map<Source, Destination>(Profile, new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void ProfileMapByParameterAction()
    {
        using var mapper = CreateProfileMapper();

        var destination = new Destination();
        mapper.Map(Profile, new Source { Value = -1 }, destination, 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void ProfileMapAlsoByParameterAction()
    {
        using var mapper = CreateProfileMapper();

        var destination = mapper.MapAlso(Profile, new Source { Value = -1 }, new Destination(), 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void ProfileMapByParameterFunc()
    {
        using var mapper = CreateProfileMapper();

        var destination = mapper.Map<Source, Destination>(Profile, new Source { Value = -1 }, 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheProfileMapByAction()
    {
        using var mapper = CreateProfileMapper();
        var action = mapper.GetMapperAction<Source, Destination>(Profile);

        var destination = new Destination();
        action(new Source { Value = -1 }, destination);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheProfileMapByFunc()
    {
        using var mapper = CreateProfileMapper();
        var func = mapper.GetMapperFunc<Source, Destination>(Profile);

        var destination = func(new Source { Value = -1 });

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheProfileMapByParameterAction()
    {
        using var mapper = CreateProfileMapper();
        var action = mapper.GetParameterMapperAction<Source, Destination>(Profile);

        var destination = new Destination();
        action(new Source { Value = -1 }, destination, 0);

        Assert.Equal(-1, destination.Value);
    }

    [Fact]
    public void CacheProfileMapByParameterFunc()
    {
        using var mapper = CreateProfileMapper();
        var func = mapper.GetParameterMapperFunc<Source, Destination>(Profile);

        var destination = func(new Source { Value = -1 }, 0);

        Assert.Equal(-1, destination.Value);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public class Source
    {
        public int Value { get; set; }
    }

    public class Destination
    {
        public int Value { get; set; }
    }
}
