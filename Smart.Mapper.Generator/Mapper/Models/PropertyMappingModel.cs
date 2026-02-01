namespace Smart.Mapper.Generator.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a property mapping configuration.
/// </summary>
internal sealed class PropertyMappingModel : IEquatable<PropertyMappingModel>
{
    /// <summary>
    /// Gets or sets the source property path (supports dot notation).
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property path (supports dot notation).
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source property type as a fully qualified name.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property type as a fully qualified name.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether type conversion is required.
    /// </summary>
    public bool RequiresConversion { get; set; }

    /// <summary>
    /// Gets or sets the nested target path segments with their types for auto-instantiation.
    /// </summary>
    public List<NestedPathSegment> TargetPathSegments { get; set; } = [];

    /// <summary>
    /// Gets or sets the nested source path segments for null checking.
    /// </summary>
    public List<NestedPathSegment> SourcePathSegments { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether source type is nullable.
    /// </summary>
    public bool IsSourceNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether target type is nullable.
    /// </summary>
    public bool IsTargetNullable { get; set; }

    /// <summary>
    /// Gets a value indicating whether the source path is nested.
    /// </summary>
    public bool IsSourceNested => SourcePath.Contains('.');

    /// <summary>
    /// Gets a value indicating whether the target path is nested.
    /// </summary>
    public bool IsTargetNested => TargetPath.Contains('.');

    /// <summary>
    /// Gets a value indicating whether null check is required before mapping.
    /// This is true only when source has nullable nested path segments (intermediate elements).
    /// </summary>
    public bool RequiresNullCheck => SourcePathSegments.Any(s => s.IsNullable);

    /// <summary>
    /// Gets a value indicating whether null coalescing is required for the assignment.
    /// This is true when source is nullable but target is not (terminal element).
    /// </summary>
    public bool RequiresNullCoalescing => IsSourceNullable && !IsTargetNullable;

    /// <summary>
    /// Gets or sets the converter method name for custom type conversion.
    /// </summary>
    public string? ConverterMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the converter method accepts custom parameters.
    /// </summary>
    public bool ConverterAcceptsCustomParameters { get; set; }

    /// <summary>
    /// Gets a value indicating whether a custom converter is specified.
    /// </summary>
    public bool HasConverter => !string.IsNullOrEmpty(ConverterMethod);

    /// <summary>
    /// Gets or sets the condition method name for conditional mapping.
    /// </summary>
    public string? ConditionMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the condition method accepts custom parameters.
    /// </summary>
    public bool ConditionAcceptsCustomParameters { get; set; }

    /// <summary>
    /// Gets a value indicating whether a condition is specified.
    /// </summary>
    public bool HasCondition => !string.IsNullOrEmpty(ConditionMethod);

    // Legacy property names for compatibility
    public string SourceName
    {
        get => SourcePath;
        set => SourcePath = value;
    }




    public string TargetName
    {
        get => TargetPath;
        set => TargetPath = value;
    }

    public bool Equals(PropertyMappingModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SourcePath == other.SourcePath &&
               TargetPath == other.TargetPath &&
               SourceType == other.SourceType &&
               TargetType == other.TargetType &&
               RequiresConversion == other.RequiresConversion &&
               IsSourceNullable == other.IsSourceNullable &&
               IsTargetNullable == other.IsTargetNullable &&
               ConverterMethod == other.ConverterMethod &&
               ConverterAcceptsCustomParameters == other.ConverterAcceptsCustomParameters &&
               ConditionMethod == other.ConditionMethod &&
               ConditionAcceptsCustomParameters == other.ConditionAcceptsCustomParameters &&
               TargetPathSegments.SequenceEqual(other.TargetPathSegments) &&
               SourcePathSegments.SequenceEqual(other.SourcePathSegments);
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyMappingModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (SourcePath?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetPath?.GetHashCode() ?? 0);
            hash = (hash * 31) + (SourceType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            hash = (hash * 31) + RequiresConversion.GetHashCode();
            hash = (hash * 31) + IsSourceNullable.GetHashCode();
            hash = (hash * 31) + IsTargetNullable.GetHashCode();
            hash = (hash * 31) + (ConverterMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + ConverterAcceptsCustomParameters.GetHashCode();
            hash = (hash * 31) + (ConditionMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + ConditionAcceptsCustomParameters.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Represents a segment in a nested property path.
/// </summary>
internal sealed class NestedPathSegment : IEquatable<NestedPathSegment>
{
    /// <summary>
    /// Gets or sets the path up to this segment (e.g., "Child1" for "Child1.Value").
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of this segment.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this segment is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    public bool Equals(NestedPathSegment? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Path == other.Path && TypeName == other.TypeName && IsNullable == other.IsNullable;
    }

    public override bool Equals(object? obj) => Equals(obj as NestedPathSegment);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (Path?.GetHashCode() ?? 0) * 31 + (TypeName?.GetHashCode() ?? 0);
            hash = hash * 31 + IsNullable.GetHashCode();
            return hash;
        }
    }
}
