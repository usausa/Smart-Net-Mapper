namespace Smart.Mapper;

/// <summary>
/// Specifies the strategy used when mapping a collection property.
/// </summary>
public enum CollectionStrategy
{
    /// <summary>
    /// Replace the destination collection with a new instance (default behavior).
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Clear the existing destination collection instance and re-add mapped elements.
    /// Useful when the destination collection reference must be preserved (e.g., data-binding scenarios).
    /// If the destination collection is null, a new instance is created.
    /// </summary>
    InPlace = 1
}
