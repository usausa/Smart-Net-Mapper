namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapIgnoreAttribute : Attribute
{
    public string Target { get; }

    public MapIgnoreAttribute(string target)
    {
        Target = target;
    }
}
