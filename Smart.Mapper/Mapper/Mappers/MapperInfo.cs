namespace Smart.Mapper.Mappers;

#pragma warning disable SA1401
#pragma warning disable CA1812
internal sealed class MapperInfo<TSource, TDestination>
{
    public Action<TSource, TDestination> MapAction = default!;

    public Func<TSource, TDestination> MapFunc = default!;

    public Action<TSource, TDestination, object?> ParameterMapAction = default!;

    public Func<TSource, object?, TDestination> ParameterMapFunc = default!;
}
#pragma warning restore CA1812
#pragma warning restore SA1401
