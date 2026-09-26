namespace Smart.Mapper;

// Specifies class-level default settings for all mapper methods in the containing class.
// Method-level MapperAttribute settings take precedence over these defaults.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class MapperProfileAttribute : Attribute
{
    // Enables strict mode for all mapper methods in the class. When true, destination properties
    // that are not mapped cause a compile-time warning (SMP0501).
    public bool Strict { get; set; }

    // Name comparison used to match property names, both in automatic mapping and for the names
    // written in mapping attributes, where an exact match always wins. Defaults to Ordinal.
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    // Default culture name (e.g. "ja-JP") for all mapper methods in the class.
    // Method-level or property-level settings take precedence.
    public string? Culture { get; set; }

    // Default DateTime format string for the mapper methods in the class that do not set Culture
    // themselves. Requires Culture to be set here (SMP0401 otherwise).
    public string? DateTimeFormat { get; set; }

    // Default numeric format string for the mapper methods in the class that do not set Culture
    // themselves. Requires Culture to be set here (SMP0401 otherwise).
    public string? NumberFormat { get; set; }
}
