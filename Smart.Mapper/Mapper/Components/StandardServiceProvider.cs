namespace Smart.Mapper.Components
{
    using System;

    public sealed class StandardServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => Activator.CreateInstance(serviceType);
    }
}
