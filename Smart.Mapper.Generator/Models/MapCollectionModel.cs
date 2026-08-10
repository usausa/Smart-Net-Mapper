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
    string SourceName = default!,
    string SourceType = default!,
    string SourceElementType = default!,
    string TargetName = default!,
    string TargetType = default!,
    string TargetElementType = default!,
    string? Mapper = default,
    int Order = default,
    int DefinitionOrder = default,
    CollectionSourceShape SourceShape = CollectionSourceShape.Enumerable,
    CollectionTargetShape TargetShape = CollectionTargetShape.List,
    string TargetCollectionMethod = "ToList",
    bool MapperReturnsValue = default,
    bool IsSourceNullable = default,
    bool TargetIsArray = default,
    bool UseHelperPath = default,
    string? Converter = default,
    bool InPlace = default,
    string? InPlaceFallbackTypeName = default);

internal static class MapCollectionModelExtensions
{
    public static bool HasCustomConverter(this MapCollectionModel m) => !String.IsNullOrEmpty(m.Converter);
}
