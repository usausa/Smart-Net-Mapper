namespace Smart.Mapper.Generator.Models;

/// <summary>
/// Classifies the source collection type for optimized emit strategy selection.
/// </summary>
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

/// <summary>
/// Classifies the target collection type for optimized emit strategy selection.
/// </summary>
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

/// <summary>
/// Represents a MapCollection mapping (collection property mapped using a mapper method).
/// </summary>
internal sealed record MapCollectionModel
{
    public string SourceName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceElementType { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetElementType { get; set; } = string.Empty;
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
