namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    public string? Converter { get; set; }

    public NullBehavior NullBehavior { get; set; } = NullBehavior.Default;

    public int Order { get; set; }

    public MapPropertyAttribute(string target)
    {
        Target = target;
    }

    public MapPropertyAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
