namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a MapFromMethod mapping (target property set from calling a method on source).
/// </summary>
internal sealed class MapFromMethodModel : IEquatable<MapFromMethodModel>
{
    /// <summary>
    /// Gets or sets the target property name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property type.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method name to call on the source object.
    /// </summary>
    public string SourceMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type of the source method.
    /// </summary>
    public string MethodReturnType { get; set; } = string.Empty;

    public bool Equals(MapFromMethodModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TargetName == other.TargetName &&
               TargetType == other.TargetType &&
               SourceMethod == other.SourceMethod &&
               MethodReturnType == other.MethodReturnType;
    }

    public override bool Equals(object? obj) => Equals(obj as MapFromMethodModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (SourceMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + (MethodReturnType?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
