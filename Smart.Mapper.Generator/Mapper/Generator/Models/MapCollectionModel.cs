namespace Smart.Mapper.Generator.Models;

using System;

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
internal sealed class MapCollectionModel : IEquatable<MapCollectionModel>
{
    /// <summary>
    /// Gets or sets the source property name.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source property type.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source element type.
    /// </summary>
    public string SourceElementType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property type.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target element type.
    /// </summary>
    public string TargetElementType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mapper method name.
    /// </summary>
    public string Mapper { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the mapper method returns a value (vs void).
    /// </summary>
    public bool MapperReturnsValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the target is an array.
    /// </summary>
    public bool TargetIsArray { get; set; }

    /// <summary>
    /// Gets or sets the collection converter method name used when no custom converter is specified.
    /// Defaults are determined by the target type (ToArray / ToList / ToImmutableArray / ToImmutableList /
    /// ToImmutableHashSet / ToHashSet / ToFrozenSet).
    /// </summary>
    public string TargetCollectionMethod { get; set; } = "ToList";

    /// <summary>
    /// Gets or sets a value indicating whether the source is nullable.
    /// </summary>
    public bool IsSourceNullable { get; set; }

    /// <summary>
    /// Gets or sets the order of this mapping.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the definition order (for stable sorting within same Order).
    /// </summary>
    public int DefinitionOrder { get; set; }

    /// <summary>
    /// Gets or sets the custom converter method name.
    /// If null, ToArray is used for arrays, ToList for others.
    /// </summary>
    public string? Converter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether in-place update strategy is used.
    /// When true, the existing destination collection is cleared and re-populated instead of replaced.
    /// </summary>
    public bool InPlace { get; set; }

    /// <summary>
    /// Gets or sets the concrete collection type name used when the destination is null in InPlace mode
    /// (e.g., "List&lt;ElementType&gt;" to create a new instance).
    /// </summary>
    public string? InPlaceFallbackTypeName { get; set; }

    /// <summary>
    /// Gets or sets the source collection shape used to select the optimal emit strategy.
    /// </summary>
    public CollectionSourceShape SourceShape { get; set; } = CollectionSourceShape.Enumerable;

    /// <summary>
    /// Gets or sets the target collection shape used to select the optimal emit strategy.
    /// </summary>
    public CollectionTargetShape TargetShape { get; set; } = CollectionTargetShape.List;

    /// <summary>
    /// Gets or sets a value indicating whether the helper path (DefaultCollectionConverter) should be used
    /// instead of inline code generation. True when a custom converter type or custom converter method is specified.
    /// </summary>
    public bool UseHelperPath { get; set; }

    public bool Equals(MapCollectionModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SourceName == other.SourceName &&
               SourceType == other.SourceType &&
               SourceElementType == other.SourceElementType &&
               TargetName == other.TargetName &&
               TargetType == other.TargetType &&
               TargetElementType == other.TargetElementType &&
               Mapper == other.Mapper &&
               MapperReturnsValue == other.MapperReturnsValue &&
               TargetIsArray == other.TargetIsArray &&
               IsSourceNullable == other.IsSourceNullable &&
               Order == other.Order &&
               DefinitionOrder == other.DefinitionOrder &&
               Converter == other.Converter &&
               InPlace == other.InPlace;
    }

    public override bool Equals(object? obj) => Equals(obj as MapCollectionModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (SourceName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Mapper?.GetHashCode() ?? 0);
            hash = (hash * 31) + Order.GetHashCode();
            hash = (hash * 31) + DefinitionOrder.GetHashCode();
            hash = (hash * 31) + (Converter?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
