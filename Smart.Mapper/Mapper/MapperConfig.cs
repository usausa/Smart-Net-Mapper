namespace Smart.Mapper;

using Smart.ComponentModel;
using Smart.Mapper.Components;
using Smart.Mapper.Expressions;
using Smart.Mapper.Mappers;
using Smart.Mapper.Options;
using Smart.Reflection;

public sealed class MapperConfig
{
    private readonly ComponentConfig config = new();

    private readonly DefaultOption defaultOption = new();

    private readonly List<MapperEntry> entries = new();

    public MapperConfig()
    {
        if (ReflectionHelper.IsCodegenAllowed)
        {
            config.Add<IMapperFactory, EmitMapperFactory>();
        }
        else
        {
            config.Add<IMapperFactory, ReflectionMapperFactory>();
        }

        config.Add<IServiceProvider, StandardServiceProvider>();
    }

    public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var option = new MappingOption(typeof(TSource), typeof(TDestination));
        entries.Add(new MapperEntry(null, option));
        return new MappingExpression<TSource, TDestination>(option);
    }

    public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>(string profile)
    {
        var option = new MappingOption(typeof(TSource), typeof(TDestination));
        entries.Add(new MapperEntry(profile, option));
        return new MappingExpression<TSource, TDestination>(option);
    }

    public MapperConfig Default(Action<IDefaultExpression> option)
    {
        option(new DefaultExpression(defaultOption));
        return this;
    }

    public MapperConfig Configure(Action<ComponentConfig> action)
    {
        action(config);
        return this;
    }

    internal ComponentContainer GetComponentContainer() => config.ToContainer();

    internal DefaultOption GetDefaultOption() => defaultOption;

    internal IEnumerable<MapperEntry> GetEntries() => entries;
}
