namespace Smart.Mapper;

// Calls the named method after the mapping, with the source and the destination
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterMapAttribute : Attribute
{
    public string Method { get; }

    public AfterMapAttribute(string method)
    {
        Method = method;
    }
}
