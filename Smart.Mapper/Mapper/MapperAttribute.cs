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
}
