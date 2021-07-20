namespace Smart.Mapper.Components
{
    using System;

    using Smart.Reflection;

    public sealed class StandardFactoryResolver : IFactoryResolver
    {
        private readonly IDelegateFactory delegateFactory;

        public StandardFactoryResolver(IDelegateFactory delegateFactory)
        {
            this.delegateFactory = delegateFactory;
        }

        public Func<T> Resolve<T>() => delegateFactory.CreateFactory<T>();
    }
}
