namespace Smart.Mapper.Generator.Models;

// Classifies the source collection type for optimized emit strategy selection.
internal enum CollectionSourceShape
{
    Enumerable = 0,
    ReadOnlyCollection,
    Array,
    List,
    ImmutableArray,
    ReadOnlyMemory,
    Memory,
    IndexedList
}

// Classifies the target collection type for optimized emit strategy selection.
internal enum CollectionTargetShape
{
    List = 0,
    Array,
    ImmutableArray,
    ImmutableList,
    HashSet,
    ImmutableHashSet,
    FrozenSet
}

// Represents a MapCollection mapping (collection property mapped using a mapper method).
internal sealed record MapCollectionModel(
    // Identity
    string SourceName = default!,
    string SourceType = default!,
    string SourceElementType = default!,
    string TargetName = default!,
    string TargetType = default!,
    string TargetElementType = default!,
    // Mapper method applied to each element
    string? Mapper = default,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Emit strategy. Shapes pick the optimized loop, UseHelperPath routes through a converter instead
    CollectionSourceShape SourceShape = CollectionSourceShape.Enumerable,
    CollectionTargetShape TargetShape = CollectionTargetShape.List,
    string TargetCollectionMethod = "ToList",
    bool MapperReturnsValue = default,
    bool IsSourceNullable = default,
    bool TargetIsArray = default,
    bool UseHelperPath = default,
    // Optional per-mapping settings
    string? Converter = default,
    // Reuse of an existing target collection instead of building a new one
    bool InPlace = default,
    string? InPlaceFallbackTypeName = default);

internal static class MapCollectionModelExtensions
{
    public static bool HasCustomConverter(this MapCollectionModel m) => !String.IsNullOrEmpty(m.Converter);
}
