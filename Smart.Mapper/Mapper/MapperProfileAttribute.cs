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
}
