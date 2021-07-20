namespace Smart.Mapper.Components
{
    using System;

    public interface IFactoryResolver
    {
        Func<T> Resolve<T>();
    }
}
