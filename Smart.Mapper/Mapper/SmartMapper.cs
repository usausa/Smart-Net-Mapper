namespace Smart.Mapper;

using System.Runtime.CompilerServices;

using Smart.ComponentModel;

using Smart.Mapper.Handlers;
using Smart.Mapper.Helpers;
using Smart.Mapper.Mappers;
using Smart.Mapper.Options;
using Smart.Mapper.Rules;

public sealed class SmartMapper : DisposableObject, INestedMapper
{
    // ReSharper disable NotAccessedPositionalProperty.Local
    private readonly record struct RecordKey(string? Profile, Type SourceType, Type DestinationType);
    // ReSharper restore NotAccessedPositionalProperty.Local

    private readonly object sync = new();

    private readonly ComponentContainer components;

    private readonly IMissingHandler[] handlers;

    private readonly IMappingRule[] rules;

    private readonly IMapperFactory factory;

    private readonly DefaultOption defaultOption;

    private readonly Dictionary<RecordKey, MappingOption> mapperOptions;

    private readonly MapperHashArray mapperCache;

    private readonly ProfileMapperHashArray profileMapperCache;

    internal SmartMapper(MapperConfig config)
    {
        mapperCache = new MapperHashArray(sync, 128);
        profileMapperCache = new ProfileMapperHashArray(sync, 32);

        components = config.GetComponentContainer();

        handlers = [.. components.GetAll<IMissingHandler>().OrderByDescending(static x => x.Priority)];
        rules = [.. components.GetAll<IMappingRule>()];
        factory = components.Get<IMapperFactory>();

        defaultOption = config.GetDefaultOption();
        mapperOptions = config.GetEntries().ToDictionary(
            static x => new RecordKey(x.Profile, x.Option.SourceType, x.Option.DestinationType),
            static x => x.Option);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private object CreateTypeInfo(string? profile, Type sourceType, Type destinationType)
    {
        var mapperOption = FindMappingOption(profile, sourceType, destinationType);
        if (mapperOption is null)
        {
            throw new InvalidOperationException(String.IsNullOrEmpty(profile)
                ? $"Mapper not found. sourceType=[{sourceType}], destinationType=[{destinationType}]"
                : $"Mapper not found. profile=[{profile}], sourceType=[{sourceType}], destinationType=[{destinationType}]");
        }

        foreach (var rule in rules)
        {
            rule.EditMapping(mapperOption);
            foreach (var memberOption in mapperOption.MemberOptions)
            {
                rule.EditMember(memberOption);
            }
        }

        return factory.Create(new MapperCreateContext(sourceType, destinationType, defaultOption, mapperOption, this));
    }

    private MappingOption? FindMappingOption(string? profile, Type sourceType, Type destinationType)
    {
        if (mapperOptions.TryGetValue(new RecordKey(profile, sourceType, destinationType), out var mapperOption))
        {
            return mapperOption;
        }

        var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType);
        var destinationUnderlyingType = Nullable.GetUnderlyingType(destinationType);

        if ((destinationUnderlyingType is not null) &&
            mapperOptions.TryGetValue(new RecordKey(profile, sourceType, destinationUnderlyingType), out mapperOption))
        {
            return mapperOption;
        }

        if ((sourceUnderlyingType is not null) &&
            mapperOptions.TryGetValue(new RecordKey(profile, sourceUnderlyingType, destinationType), out mapperOption))
        {
            return mapperOption;
        }

        if ((sourceUnderlyingType is not null) &&
            (destinationUnderlyingType is not null) &&
            mapperOptions.TryGetValue(new RecordKey(profile, sourceUnderlyingType, destinationUnderlyingType), out mapperOption))
        {
            return mapperOption;
        }

        return handlers
            .Select(x => x.Handle(sourceType, destinationType))
            .FirstOrDefault(static x => x is not null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MapperInfo<TSource, TDestination> FindTypeInfo<TSource, TDestination>()
    {
        if (!mapperCache.TryGetValue(typeof(TSource), typeof(TDestination), out var info))
        {
            info = mapperCache.AddIfNotExist(typeof(TSource), typeof(TDestination), (ts, td) => CreateTypeInfo(null, ts, td));
        }

        return (MapperInfo<TSource, TDestination>)info;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MapperInfo<TSource, TDestination> FindTypeInfo<TSource, TDestination>(string profile)
    {
        if (!profileMapperCache.TryGetValue(profile, typeof(TSource), typeof(TDestination), out var info))
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            info = profileMapperCache.AddIfNotExist(profile, typeof(TSource), typeof(TDestination), CreateTypeInfo);
        }

        return (MapperInfo<TSource, TDestination>)info;
    }

    //--------------------------------------------------------------------------------
    // Get
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Action<TSource, TDestination> GetMapperAction<TSource, TDestination>() =>
        FindTypeInfo<TSource, TDestination>().MapAction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Action<TSource, TDestination, object?> GetParameterMapperAction<TSource, TDestination>() =>
        FindTypeInfo<TSource, TDestination>().ParameterMapAction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>() =>
        FindTypeInfo<TSource, TDestination>().MapFunc;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Func<TSource, object?, TDestination> GetParameterMapperFunc<TSource, TDestination>() =>
        FindTypeInfo<TSource, TDestination>().ParameterMapFunc;

    // With profile

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Action<TSource, TDestination> GetMapperAction<TSource, TDestination>(string profile) =>
        FindTypeInfo<TSource, TDestination>(profile).MapAction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Action<TSource, TDestination, object?> GetParameterMapperAction<TSource, TDestination>(string profile) =>
        FindTypeInfo<TSource, TDestination>(profile).ParameterMapAction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>(string profile) =>
        FindTypeInfo<TSource, TDestination>(profile).MapFunc;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Func<TSource, object?, TDestination> GetParameterMapperFunc<TSource, TDestination>(string profile) =>
        FindTypeInfo<TSource, TDestination>(profile).ParameterMapFunc;

    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source) =>
        FindTypeInfo<TSource, TDestination>().MapFunc(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Map<TSource, TDestination>(TSource source, TDestination destination) =>
        FindTypeInfo<TSource, TDestination>().MapAction(source, destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination MapAlso<TSource, TDestination>(TSource source, TDestination destination)
    {
        FindTypeInfo<TSource, TDestination>().MapAction(source, destination);
        return destination;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source, object? parameter) =>
        FindTypeInfo<TSource, TDestination>().ParameterMapFunc(source, parameter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Map<TSource, TDestination>(TSource source, TDestination destination, object? parameter) =>
        FindTypeInfo<TSource, TDestination>().ParameterMapAction(source, destination, parameter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination MapAlso<TSource, TDestination>(TSource source, TDestination destination, object? parameter)
    {
        FindTypeInfo<TSource, TDestination>().ParameterMapAction(source, destination, parameter);
        return destination;
    }

    // With profile

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(string profile, TSource source) =>
        FindTypeInfo<TSource, TDestination>(profile).MapFunc(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Map<TSource, TDestination>(string profile, TSource source, TDestination destination) =>
        FindTypeInfo<TSource, TDestination>(profile).MapAction(source, destination);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination MapAlso<TSource, TDestination>(string profile, TSource source, TDestination destination)
    {
        FindTypeInfo<TSource, TDestination>(profile).MapAction(source, destination);
        return destination;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(string profile, TSource source, object? parameter) =>
        FindTypeInfo<TSource, TDestination>(profile).ParameterMapFunc(source, parameter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Map<TSource, TDestination>(string profile, TSource source, TDestination destination, object? parameter) =>
        FindTypeInfo<TSource, TDestination>(profile).ParameterMapAction(source, destination, parameter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination MapAlso<TSource, TDestination>(string profile, TSource source, TDestination destination, object? parameter)
    {
        FindTypeInfo<TSource, TDestination>(profile).ParameterMapAction(source, destination, parameter);
        return destination;
    }
}
