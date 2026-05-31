namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeMapAttribute : Attribute
{
    public string Method { get; }

    public BeforeMapAttribute(string method)
    {
        Method = method;
    }
}
