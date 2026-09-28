namespace Smart.Mapper;

// Calls the named method before the mapping, with the source and the destination
[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeMapAttribute : Attribute
{
    public string Method { get; }

    public BeforeMapAttribute(string method)
    {
        Method = method;
    }
}
