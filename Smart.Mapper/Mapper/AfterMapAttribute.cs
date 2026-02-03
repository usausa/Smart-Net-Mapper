namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterMapAttribute : Attribute
{
    public string Method { get; }

    public AfterMapAttribute(string method)
    {
        Method = method;
    }
}
