namespace Smart.Mapper;

// How MapCollection sets the target collection
public enum CollectionStrategy
{
    // Assigns a new collection (the default)
    Replace = 0,

    // Clears the existing collection and adds the mapped elements, keeping an instance referenced elsewhere (data
    // binding, for example); its declared type has to implement ICollection<T>
    InPlace = 1
}
