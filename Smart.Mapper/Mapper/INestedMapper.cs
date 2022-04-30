namespace Smart.Mapper;

public interface INestedMapper
{
    Action<TSource, TDestination> GetMapperAction<TSource, TDestination>();

    Action<TSource, TDestination, object?> GetParameterMapperAction<TSource, TDestination>();

    Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>();

    Func<TSource, object?, TDestination> GetParameterMapperFunc<TSource, TDestination>();

    TDestination Map<TSource, TDestination>(TSource source);

    void Map<TSource, TDestination>(TSource source, TDestination destination);

    TDestination Map<TSource, TDestination>(TSource source, object? parameter);

    void Map<TSource, TDestination>(TSource source, TDestination destination, object? parameter);

    // With profile

    Action<TSource, TDestination> GetMapperAction<TSource, TDestination>(string profile);

    Action<TSource, TDestination, object?> GetParameterMapperAction<TSource, TDestination>(string profile);

    Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>(string profile);

    Func<TSource, object?, TDestination> GetParameterMapperFunc<TSource, TDestination>(string profile);

    TDestination Map<TSource, TDestination>(string profile, TSource source);

    void Map<TSource, TDestination>(string profile, TSource source, TDestination destination);

    TDestination Map<TSource, TDestination>(string profile, TSource source, object? parameter);

    void Map<TSource, TDestination>(string profile, TSource source, TDestination destination, object? parameter);
}
