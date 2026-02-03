namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapUsingAttribute : Attribute
{
    public string Target { get; }

    public string Method { get; }

    public int Order { get; set; }

    public MapUsingAttribute(string target, string method)
    {
        Target = target;
        Method = method;
    }
}
