namespace Smart.Mapper.Mappers;

#pragma warning disable CA1812
internal sealed class ReflectionMapperFactory : IMapperFactory
{
    // IConverterResolver, IFactoryResolver, IFunctionActivatorはDI

    public object Create(MapperCreateContext context)
    {
        throw new NotImplementedException();
    }
}
#pragma warning restore CA1812
