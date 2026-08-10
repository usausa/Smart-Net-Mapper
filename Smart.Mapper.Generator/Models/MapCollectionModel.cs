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
    string SourceName,
    string SourceType,
    string SourceElementType,
    string TargetName,
    string TargetType,
    string TargetElementType,
    string? Mapper,
    int Order,
    int DefinitionOrder,
    CollectionSourceShape SourceShape,
    CollectionTargetShape TargetShape,
    string TargetCollectionMethod,
    bool MapperReturnsValue,
    bool IsSourceNullable,
    bool TargetIsArray,
    bool UseHelperPath,
    string? Converter,
    bool InPlace,
    string? InPlaceFallbackTypeName);

internal static class MapCollectionModelExtensions
{
    public static bool HasCustomConverter(this MapCollectionModel m) => !String.IsNullOrEmpty(m.Converter);
}
