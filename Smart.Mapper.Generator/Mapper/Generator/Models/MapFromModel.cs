namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a MapFrom mapping (target property computed from source via a method).
/// </summary>
internal sealed class MapFromModel : IEquatable<MapFromModel>
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
    /// Gets or sets the method name that computes the value.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type of the method.
    /// </summary>
    public string MethodReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the method accepts custom parameters.
    /// </summary>
    public bool AcceptsCustomParameters { get; set; }

    public bool Equals(MapFromModel? other)
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
               MethodName == other.MethodName &&
               MethodReturnType == other.MethodReturnType &&
               AcceptsCustomParameters == other.AcceptsCustomParameters;
    }

    public override bool Equals(object? obj) => Equals(obj as MapFromModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (MethodName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (MethodReturnType?.GetHashCode() ?? 0);
            hash = (hash * 31) + AcceptsCustomParameters.GetHashCode();
            return hash;
        }
    }
}
