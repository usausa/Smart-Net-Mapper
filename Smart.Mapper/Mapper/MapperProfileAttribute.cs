namespace Smart.Mapper;

// Specifies class-level default settings for all mapper methods in the containing class.
// Method-level MapperAttribute settings take precedence over these defaults.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class MapperProfileAttribute : Attribute
{
    // Enables strict mode for all mapper methods in the class. When true, destination properties
    // that are not mapped cause a compile-time warning (ML0017).
    public bool Strict { get; set; }

    // Name comparison strategy used for automatic property matching. Defaults to Ordinal.
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    // Default culture name (e.g. "ja-JP") for all mapper methods in the class.
    // Method-level or property-level settings take precedence.
    public string? Culture { get; set; }

    // Default DateTime format string for all mapper methods in the class.
    // Requires Culture to be set.
    public string? DateTimeFormat { get; set; }

    // Default numeric format string for all mapper methods in the class.
    // Requires Culture to be set.
    public string? NumberFormat { get; set; }
}
