namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    public string? Converter { get; set; }

    public NullBehavior NullBehavior { get; set; } = NullBehavior.Default;

    public int Order { get; set; }

    public object? NullSubstitute { get; set; }

    /// <summary>Culture name override for this property's string conversion (e.g. "en-US").</summary>
    public string? Culture { get; set; }

    /// <summary>DateTime format string override for this property. Requires <see cref="Culture"/>.</summary>
    public string? DateTimeFormat { get; set; }

    /// <summary>Numeric format string override for this property. Requires <see cref="Culture"/>.</summary>
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

    public string? Converter { get; set; }

    public NullBehavior NullBehavior { get; set; } = NullBehavior.Default;

    public int Order { get; set; }

    public T NullSubstitute { get; set; } = default!;

    /// <summary>Culture name override for this property's string conversion (e.g. "en-US").</summary>
    public string? Culture { get; set; }

    /// <summary>DateTime format string override for this property. Requires <see cref="Culture"/>.</summary>
    public string? DateTimeFormat { get; set; }

    /// <summary>Numeric format string override for this property. Requires <see cref="Culture"/>.</summary>
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
