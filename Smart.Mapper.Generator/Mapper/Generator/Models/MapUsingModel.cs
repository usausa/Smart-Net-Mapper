namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a MapUsing mapping (target property computed from source via a method in containing class).
/// Supports custom parameters similar to AfterMap/BeforeMap.
/// </summary>
internal sealed class MapUsingModel : IEquatable<MapUsingModel>
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
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type of the method.
    /// </summary>
    public string MethodReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the method accepts custom parameters.
    /// </summary>
    public bool AcceptsCustomParameters { get; set; }

    /// <summary>
    /// Gets or sets the order of this mapping.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the definition order (for stable sorting within same Order).
    /// </summary>
    public int DefinitionOrder { get; set; }

    public bool Equals(MapUsingModel? other)
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
               Method == other.Method &&
               MethodReturnType == other.MethodReturnType &&
               AcceptsCustomParameters == other.AcceptsCustomParameters &&
               Order == other.Order &&
               DefinitionOrder == other.DefinitionOrder;
    }

    public override bool Equals(object? obj) => Equals(obj as MapUsingModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Method?.GetHashCode() ?? 0);
            hash = (hash * 31) + (MethodReturnType?.GetHashCode() ?? 0);
            hash = (hash * 31) + AcceptsCustomParameters.GetHashCode();
            hash = (hash * 31) + Order.GetHashCode();
            hash = (hash * 31) + DefinitionOrder.GetHashCode();
            return hash;
        }
    }
}
