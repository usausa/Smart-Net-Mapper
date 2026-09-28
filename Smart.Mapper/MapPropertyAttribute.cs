namespace Smart.Mapper;

// Maps the target from a source member, of the target's name by default; either can be a dotted path (Child.Value)
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    // Method converting the source value of this mapping
    public string? Converter { get; set; }

    // Skip leaves the target as it is for a null source
    public NullBehavior NullBehavior { get; set; } = NullBehavior.Default;

    public int Order { get; set; }

    // Value the target gets for a null source
    public object? NullValue { get; set; }

    // Culture name of this mapping (e.g. "en-US"), over the culture of the method
    public string? Culture { get; set; }

    // Format of the date and time conversions of this mapping
    public string? DateTimeFormat { get; set; }

    // Format of the number conversions of this mapping
    public string? NumberFormat { get; set; }

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

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute<T> : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    // Method converting the source value of this mapping
    public string? Converter { get; set; }

    // Skip leaves the target as it is for a null source
    public NullBehavior NullBehavior { get; set; } = NullBehavior.Default;

    public int Order { get; set; }

    // Value the target gets for a null source
    public T NullValue { get; set; } = default!;

    // Culture name of this mapping (e.g. "en-US"), over the culture of the method
    public string? Culture { get; set; }

    // Format of the date and time conversions of this mapping
    public string? DateTimeFormat { get; set; }

    // Format of the number conversions of this mapping
    public string? NumberFormat { get; set; }

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
