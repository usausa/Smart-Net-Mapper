namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapNestedAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    public string Mapper { get; set; } = default!;

    public int Order { get; set; }

    public MapNestedAttribute(string target)
    {
        Target = target;
    }

    public MapNestedAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
