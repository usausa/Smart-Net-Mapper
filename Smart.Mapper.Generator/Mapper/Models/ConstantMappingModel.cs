namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a constant value mapping configuration.
/// </summary>
internal sealed class ConstantMappingModel : IEquatable<ConstantMappingModel>
{
    /// <summary>
    /// Gets or sets the target property name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the constant value as a string representation.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the expression to evaluate.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Gets or sets the target property type.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this mapping uses an expression.
    /// </summary>
    public bool IsExpression => !string.IsNullOrEmpty(Expression);

    public bool Equals(ConstantMappingModel? other)
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
               Value == other.Value &&
               Expression == other.Expression &&
               TargetType == other.TargetType;
    }

    public override bool Equals(object? obj) => Equals(obj as ConstantMappingModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Value?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Expression?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
