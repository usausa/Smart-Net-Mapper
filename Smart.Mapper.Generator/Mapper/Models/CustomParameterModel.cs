namespace Smart.Mapper.Generator.Models;

using System;

/// <summary>
/// Represents a custom parameter for a mapper method.
/// </summary>
internal sealed class CustomParameterModel : IEquatable<CustomParameterModel>
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter type as a fully qualified name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    public bool Equals(CustomParameterModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name && TypeName == other.TypeName;
    }

    public override bool Equals(object? obj) => Equals(obj as CustomParameterModel);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Name?.GetHashCode() ?? 0) * 31) + (TypeName?.GetHashCode() ?? 0);
        }
    }
}
