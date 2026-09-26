namespace Smart.Mapper;

// Specifies the strategy used when mapping a collection property.
public enum CollectionStrategy
{
    // Replace the destination collection with a new instance (default behavior).
    Replace = 0,

    // Clear the existing destination collection instance and re-add mapped elements.
    // Useful when the destination collection reference must be preserved (e.g., data-binding scenarios).
    // If the destination collection is null, a new List<T> (HashSet<T> for a set) is created, so the
    // destination property has to be settable and able to take it (SMP0212 / SMP0217 otherwise).
    InPlace = 1
}
