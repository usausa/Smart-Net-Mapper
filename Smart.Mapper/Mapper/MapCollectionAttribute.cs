namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapCollectionAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    public string? Mapper { get; set; }

    public string? Converter { get; set; }

    public int Order { get; set; }

    public MapCollectionAttribute(string target)
    {
        Target = target;
    }

    public MapCollectionAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
