namespace Smart.Mapper.Mappers;

using System;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Ignore")]
internal sealed class ReflectionMapperFactory : IMapperFactory
{
    // IConverterResolver, IFactoryResolver, IFunctionActivatorはDI

    public object Create(MapperCreateContext context)
    {
        throw new NotImplementedException();
    }
}
