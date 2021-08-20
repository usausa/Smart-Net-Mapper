namespace Smart.Mapper
{
    using System;

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
    }
}
