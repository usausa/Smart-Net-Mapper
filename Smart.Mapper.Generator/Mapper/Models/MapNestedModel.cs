namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a MapNested mapping (nested object property mapped using a mapper method).
/// </summary>
internal sealed class MapNestedModel : IEquatable<MapNestedModel>
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
    /// Gets or sets the target property name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property type.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mapper method name.
    /// </summary>
    public string MapperMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the mapper method returns a value (vs void).
    /// </summary>
    public bool MapperReturnsValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source is nullable.
    /// </summary>
    public bool IsSourceNullable { get; set; }

    public bool Equals(MapNestedModel? other)
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
               TargetName == other.TargetName &&
               TargetType == other.TargetType &&
               MapperMethod == other.MapperMethod &&
               MapperReturnsValue == other.MapperReturnsValue &&
               IsSourceNullable == other.IsSourceNullable;
    }

    public override bool Equals(object? obj) => Equals(obj as MapNestedModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (SourceName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (MapperMethod?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
