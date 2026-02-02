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
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type of the source method.
    /// </summary>
    public string MethodReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order of this mapping.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the definition order (for stable sorting within same Order).
    /// </summary>
    public int DefinitionOrder { get; set; }

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
               Method == other.Method &&
               MethodReturnType == other.MethodReturnType &&
               Order == other.Order &&
               DefinitionOrder == other.DefinitionOrder;
    }

    public override bool Equals(object? obj) => Equals(obj as MapFromMethodModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Method?.GetHashCode() ?? 0);
            hash = (hash * 31) + (MethodReturnType?.GetHashCode() ?? 0);
            hash = (hash * 31) + Order.GetHashCode();
            hash = (hash * 31) + DefinitionOrder.GetHashCode();
            return hash;
        }
    }
}
