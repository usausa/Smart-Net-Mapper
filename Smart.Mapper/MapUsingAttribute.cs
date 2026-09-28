namespace Smart.Mapper;

// Assigns the value the named method computes from the source to the target
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
