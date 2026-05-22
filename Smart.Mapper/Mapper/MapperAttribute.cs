namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MapperAttribute : Attribute
{
    public bool AutoMap { get; set; } = true;

    // Enables strict mode. When true, destination properties that are not mapped (automatically,
    // explicitly, or via MapIgnore) cause a compile-time warning (ML0017).
    public bool Strict { get; set; }

    // Name comparison strategy used for automatic property matching. Defaults to Ordinal.
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    // Culture name (e.g. "ja-JP") used for string conversions. When specified, the converter
    // must provide an overload accepting System.IFormatProvider.
    public string? Culture { get; set; }

    // Format string applied when converting DateTime / DateOnly / TimeOnly / DateTimeOffset / TimeSpan
    // to or from string. Requires Culture to be set.
    public string? DateTimeFormat { get; set; }

    // Format string applied when converting numeric types to or from string.
    // Requires Culture to be set.
    public string? NumberFormat { get; set; }
}
