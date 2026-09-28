namespace Smart.Mapper;

// Leaves a member of the destination, as a whole, out of the automatic mapping
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapIgnoreAttribute : Attribute
{
    public string Target { get; }

    public MapIgnoreAttribute(string target)
    {
        Target = target;
    }
}
