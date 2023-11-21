namespace Smart.Mapper.Mappers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Ignore")]
internal sealed class ReflectionMapperFactory : IMapperFactory
{
    // IConverterResolver, IFactoryResolver, IFunctionActivatorはDI

    public object Create(MapperCreateContext context)
    {
        throw new NotImplementedException();
    }
}
