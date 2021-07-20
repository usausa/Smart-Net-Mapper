namespace Smart.Mapper.Components
{
    using System;

    public interface IConverterResolver
    {
        Func<TSource, TDestination> Resolve<TSource, TDestination>();
    }
}
