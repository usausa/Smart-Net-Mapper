namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MapperAttribute : Attribute
{
    public bool AutoMap { get; set; } = true;

    // Enables strict mode. When true, destination properties that are not mapped (automatically,
    // explicitly, or via MapIgnore) cause a compile-time warning (SMP0501).
    public bool Strict { get; set; }

    // Name comparison used to match property names, both in automatic mapping and for the names
    // written in mapping attributes, where an exact match always wins. Defaults to Ordinal.
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    // Culture name (e.g. "ja-JP") used for string conversions. With it, the specialized methods of
    // the value converter are called through their overload taking the culture and the format,
    // (value, IFormatProvider, string?), which the converter has to provide (SMP0104 otherwise).
    public string? Culture { get; set; }

    // Format string applied when converting DateTime / DateOnly / TimeOnly / DateTimeOffset / TimeSpan
    // to or from string. Requires a culture, from Culture or the profile (SMP0401 otherwise).
    public string? DateTimeFormat { get; set; }

    // Format string applied when converting numeric types to or from string.
    // Requires a culture, from Culture or the profile (SMP0401 otherwise).
    public string? NumberFormat { get; set; }
}
