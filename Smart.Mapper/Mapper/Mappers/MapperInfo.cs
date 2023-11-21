namespace Smart.Mapper.Mappers;

#pragma warning disable 0649
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Ignore")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
internal sealed class MapperInfo<TSource, TDestination>
{
    public Action<TSource, TDestination> MapAction = default!;

    public Func<TSource, TDestination> MapFunc = default!;

    public Action<TSource, TDestination, object?> ParameterMapAction = default!;

    public Func<TSource, object?, TDestination> ParameterMapFunc = default!;
}
#pragma warning restore 0649
