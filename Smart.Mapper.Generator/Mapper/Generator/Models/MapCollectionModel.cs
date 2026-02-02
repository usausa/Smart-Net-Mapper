namespace Smart.Mapper.Generator.Models;

using System;

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
               DefinitionOrder == other.DefinitionOrder;
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
            return hash;
        }
    }
}
