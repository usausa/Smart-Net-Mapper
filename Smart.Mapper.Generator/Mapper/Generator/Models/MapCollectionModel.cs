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
    Memory
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
    public string SourceName { get; set; } = default!;
    public string SourceType { get; set; } = default!;
    public string SourceElementType { get; set; } = default!;
    public string TargetName { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string TargetElementType { get; set; } = default!;
    public string? Mapper { get; set; }
    public bool MapperReturnsValue { get; set; }
    public bool TargetIsArray { get; set; }
    public string TargetCollectionMethod { get; set; } = "ToList";
    public bool IsSourceNullable { get; set; }
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
    public string? Converter { get; set; }
    public bool InPlace { get; set; }
    public string? InPlaceFallbackTypeName { get; set; }
    public CollectionSourceShape SourceShape { get; set; } = CollectionSourceShape.Enumerable;
    public CollectionTargetShape TargetShape { get; set; } = CollectionTargetShape.List;
    public bool UseHelperPath { get; set; }
}

internal static class MapCollectionModelExtensions
{
    public static bool HasCustomConverter(this MapCollectionModel m) => !string.IsNullOrEmpty(m.Converter);
}
