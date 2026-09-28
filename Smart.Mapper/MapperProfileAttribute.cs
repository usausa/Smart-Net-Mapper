namespace Smart.Mapper;

// Default settings of the mapper methods in the class, or in the assembly: each applies unless the method, or for
// the assembly the class, sets it
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class MapperProfileAttribute : Attribute
{
    public bool Strict { get; set; }

    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    // The culture used when no culture name applies
    public MapperCulture DefaultCulture { get; set; }

    // Culture name (e.g. "ja-JP")
    public string? Culture { get; set; }

    public string? DateTimeFormat { get; set; }

    public string? NumberFormat { get; set; }
}
