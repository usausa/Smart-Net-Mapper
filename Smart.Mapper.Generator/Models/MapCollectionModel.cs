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
internal sealed record MapCollectionModel
{
    public string SourceName { get; init; } = default!;
    public string SourceType { get; init; } = default!;
    public string SourceElementType { get; init; } = default!;
    public string TargetName { get; init; } = default!;
    public string TargetType { get; init; } = default!;
    public string TargetElementType { get; init; } = default!;
    public string? Mapper { get; init; }
    public int Order { get; init; }
    public int DefinitionOrder { get; init; }
    public CollectionSourceShape SourceShape { get; init; } = CollectionSourceShape.Enumerable;
    public CollectionTargetShape TargetShape { get; init; } = CollectionTargetShape.List;
    public string TargetCollectionMethod { get; init; } = "ToList";
    public bool MapperReturnsValue { get; init; }
    public bool IsSourceNullable { get; init; }
    public bool TargetIsArray { get; init; }
    public bool UseHelperPath { get; init; }
    public string? Converter { get; init; }
    public bool InPlace { get; init; }
    public string? InPlaceFallbackTypeName { get; init; }
}

internal static class MapCollectionModelExtensions
{
    public static bool HasCustomConverter(this MapCollectionModel m) => !String.IsNullOrEmpty(m.Converter);
}
