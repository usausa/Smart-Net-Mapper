namespace Smart.Mapper;

/// <summary>
/// Specifies class-level default settings for all mapper methods in the containing class.
/// Method-level <see cref="MapperAttribute"/> settings take precedence over these defaults.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class MapperProfileAttribute : Attribute
{
    /// <summary>
    /// Enables strict mode for all mapper methods in the class. When true, destination properties
    /// that are not mapped cause a compile-time warning (ML0017).
    /// </summary>
    public bool Strict { get; set; }

    /// <summary>
    /// Name comparison strategy used for automatic property matching. Defaults to Ordinal.
    /// </summary>
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    /// <summary>
    /// Default culture name (e.g. "ja-JP") for all mapper methods in the class.
    /// Method-level or property-level settings take precedence.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// Default DateTime format string for all mapper methods in the class.
    /// Requires <see cref="Culture"/> to be set.
    /// </summary>
    public string? DateTimeFormat { get; set; }

    /// <summary>
    /// Default numeric format string for all mapper methods in the class.
    /// Requires <see cref="Culture"/> to be set.
    /// </summary>
    public string? NumberFormat { get; set; }
}
