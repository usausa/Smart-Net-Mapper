namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a condition mapping for a target property.
/// </summary>
internal sealed class PropertyConditionModel : IEquatable<PropertyConditionModel>
{
    /// <summary>
    /// Gets or sets the target property name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the condition method name, or null when no condition is set.
    /// </summary>
    public string? ConditionMethod { get; set; }

    public bool Equals(PropertyConditionModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TargetName == other.TargetName && ConditionMethod == other.ConditionMethod;
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyConditionModel);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((TargetName?.GetHashCode() ?? 0) * 31) + (ConditionMethod?.GetHashCode() ?? 0);
        }
    }
}
