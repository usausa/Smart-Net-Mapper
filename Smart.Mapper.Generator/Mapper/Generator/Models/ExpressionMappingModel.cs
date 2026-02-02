namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents an expression mapping configuration.
/// </summary>
internal sealed class ExpressionMappingModel : IEquatable<ExpressionMappingModel>
{
    /// <summary>
    /// Gets or sets the target property name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expression to evaluate.
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order of this mapping.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the definition order (for stable sorting within same Order).
    /// </summary>
    public int DefinitionOrder { get; set; }

    public bool Equals(ExpressionMappingModel? other)
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
               Expression == other.Expression &&
               Order == other.Order &&
               DefinitionOrder == other.DefinitionOrder;
    }

    public override bool Equals(object? obj) => Equals(obj as ExpressionMappingModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Expression?.GetHashCode() ?? 0);
            hash = (hash * 31) + Order.GetHashCode();
            hash = (hash * 31) + DefinitionOrder.GetHashCode();
            return hash;
        }
    }
}
