namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MapperAttribute : Attribute
{
    public bool AutoMap { get; set; } = true;

    /// <summary>
    /// Enables strict mode. When true, destination properties that are not mapped (automatically,
    /// explicitly, or via MapIgnore) cause a compile-time warning (ML0017).
    /// </summary>
    public bool Strict { get; set; }

    /// <summary>
    /// Name comparison strategy used for automatic property matching. Defaults to Ordinal.
    /// </summary>
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    /// <summary>
    /// Culture name (e.g. "ja-JP") used for string conversions. When specified, the converter
    /// must provide an overload accepting <see cref="System.IFormatProvider"/>.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Format string applied when converting DateTime / DateOnly / TimeOnly / DateTimeOffset / TimeSpan
    /// to or from string. Requires <see cref="Culture"/> to be set.
    /// </summary>
    public string? DateTimeFormat { get; set; }

    /// <summary>
    /// Format string applied when converting numeric types to or from string.
    /// Requires <see cref="Culture"/> to be set.
    /// </summary>
    public string? NumberFormat { get; set; }
}
