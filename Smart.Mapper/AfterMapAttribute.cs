namespace Smart.Mapper;

// Calls the method after the mapping, as BeforeMap calls its method (SMP0103 for one that does not match).
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterMapAttribute : Attribute
{
    public string Method { get; }

    public AfterMapAttribute(string method)
    {
        Method = method;
    }
}
