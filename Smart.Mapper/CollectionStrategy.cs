namespace Smart.Mapper;

// Specifies the strategy used when mapping a collection property.
public enum CollectionStrategy
{
    // Replace the destination collection with a new instance (default behavior).
    Replace = 0,

    // Clear the existing destination collection instance and re-add mapped elements.
    // Useful when the destination collection reference must be preserved (e.g., data-binding scenarios).
    // The declared type has to implement ICollection<T> without being read-only by design (SMP0219). If
    // the destination collection is null, a property the mapper can assign gets a new instance of its
    // type (a List<T>, HashSet<T> or Dictionary<TKey, TValue> for an interface, SMP0217 when none fits), and
    // one it cannot assign is left null. An instance that is read-only at run time throws NotSupportedException
    // from Clear. A required member a return mapper sets before construction has no instance to refill (SMP0219),
    // unless the constructor called has [SetsRequiredMembers], and a null source leaves the target as it is.
    InPlace = 1
}
