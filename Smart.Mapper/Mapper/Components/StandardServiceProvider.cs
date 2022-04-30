namespace Smart.Mapper.Components;

public sealed class StandardServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => Activator.CreateInstance(serviceType);
}
