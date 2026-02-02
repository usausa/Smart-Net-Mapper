namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a MapFrom mapping (target property set from source expression - method call or property path).
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
    /// Gets or sets the source expression (method name or property path).
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the source expression is a method call.
    /// </summary>
    public bool IsMethodCall { get; set; }

    /// <summary>
    /// Gets or sets the return type of the source expression.
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order of this mapping.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the definition order (for stable sorting within same Order).
    /// </summary>
    public int DefinitionOrder { get; set; }

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
               From == other.From &&
               IsMethodCall == other.IsMethodCall &&
               ReturnType == other.ReturnType &&
               Order == other.Order &&
               DefinitionOrder == other.DefinitionOrder;
    }

    public override bool Equals(object? obj) => Equals(obj as MapFromModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (From?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsMethodCall.GetHashCode();
            hash = (hash * 31) + (ReturnType?.GetHashCode() ?? 0);
            hash = (hash * 31) + Order.GetHashCode();
            hash = (hash * 31) + DefinitionOrder.GetHashCode();
            return hash;
        }
    }
}
