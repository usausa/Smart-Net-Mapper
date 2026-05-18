namespace Smart.Mapper.Generator.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents the kind of enum conversion to perform.
/// </summary>
internal enum EnumMappingKind
{
    /// <summary>No enum conversion.</summary>
    None = 0,

    /// <summary>enum → enum by name (switch expression).</summary>
    EnumToEnum = 1,

    /// <summary>enum → int/long/short/byte (numeric cast).</summary>
    EnumToNumeric = 2,

    /// <summary>int/long/short/byte → enum (numeric cast).</summary>
    NumericToEnum = 3,

    /// <summary>enum → string (.ToString()).</summary>
    EnumToString = 4,

    /// <summary>string → enum (Enum.Parse).</summary>
    StringToEnum = 5
}

/// <summary>
/// Represents the kind of user-defined conversion operator to apply.
/// </summary>
internal enum UserDefinedConversionKind
{
    /// <summary>No user-defined conversion.</summary>
    None = 0,

    /// <summary>Implicit conversion operator (op_Implicit) — generates plain assignment.</summary>
    Implicit = 1,

    /// <summary>Explicit conversion operator (op_Explicit) — generates cast expression.</summary>
    Explicit = 2,
}

/// <summary>
/// Represents the kind of IParsable / ISpanParsable parse method to use.
/// </summary>
internal enum ParseMethodKind
{
    /// <summary>No parsable conversion.</summary>
    None = 0,

    /// <summary>T.Parse(ReadOnlySpan&lt;char&gt;, IFormatProvider?) via ISpanParsable&lt;T&gt;.</summary>
    ISpanParsable = 1,

    /// <summary>T.Parse(string, IFormatProvider?) via IParsable&lt;T&gt;.</summary>
    IParsable = 2,
}

/// <summary>
/// Represents the null behavior for property mapping.
/// </summary>
internal enum NullBehaviorType
{
    /// <summary>
    /// Sets default! to the target property when the source is null.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Skips setting the target property when the source is null.
    /// </summary>
    Skip = 1
}

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
    /// Gets or sets the underlying source type for nullable types.
    /// If source is int?, this would be int. If source is not nullable, this is same as SourceType.
    /// </summary>
    public string SourceUnderlyingType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the underlying target type for nullable types.
    /// If target is int?, this would be int. If target is not nullable, this is same as TargetType.
    /// </summary>
    public string TargetUnderlyingType { get; set; } = string.Empty;

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
    public bool RequiresNullCoalescing => IsSourceNullable && !IsTargetNullable && NullBehavior == NullBehaviorType.Default && !HasNullSubstitute;

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

    /// <summary>
    /// Gets or sets a value indicating whether this mapping was explicitly specified via MapProperty attribute.
    /// Used to distinguish from auto-mapped properties.
    /// </summary>
    public bool HasExplicitMapping { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the destination property is init-only.
    /// Init-only properties must be set via object initializer syntax, not regular assignment.
    /// </summary>
    public bool IsTargetInitOnly { get; set; }

    /// <summary>
    /// Gets or sets the order of this mapping.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the definition order (for stable sorting within same Order).
    /// </summary>
    public int DefinitionOrder { get; set; }

    /// <summary>
    /// Gets or sets the null behavior for this mapping.
    /// </summary>
    public NullBehaviorType NullBehavior { get; set; } = NullBehaviorType.Default;

    /// <summary>
    /// Gets or sets the literal code expression to substitute when the source value is null.
    /// When set, generated code emits <c>source ?? &lt;NullSubstitute&gt;</c> for the assignment.
    /// </summary>
    public string? NullSubstitute { get; set; }

    /// <summary>
    /// Gets a value indicating whether a null-substitute value is configured.
    /// </summary>
    public bool HasNullSubstitute => !string.IsNullOrEmpty(NullSubstitute);

    /// <summary>
    /// Gets or sets the specialized converter method name (e.g., "ConvertToInt32" for string -> int).
    /// If set, this method will be used instead of the generic Convert method.
    /// </summary>
    public string? SpecializedConverterMethod { get; set; }

    /// <summary>
    /// Gets a value indicating whether a specialized converter method is available.
    /// </summary>
    public bool HasSpecializedConverter => !string.IsNullOrEmpty(SpecializedConverterMethod);

    /// <summary>
    /// Gets or sets the kind of parsable parse method to use (B3: IParsable / ISpanParsable).
    /// </summary>
    public ParseMethodKind ParseMethod { get; set; } = ParseMethodKind.None;

    /// <summary>
    /// Gets a value indicating whether a parsable parse method should be used.
    /// </summary>
    public bool HasParsableMethod => ParseMethod != ParseMethodKind.None;

    /// <summary>
    /// Gets or sets the effective culture name resolved from MapProperty > Mapper > MapperProfile precedence.
    /// Null means InvariantCulture (no override).
    /// </summary>
    public string? EffectiveCulture { get; set; }

    /// <summary>
    /// Gets or sets the effective DateTime format string (only meaningful when EffectiveCulture is set).
    /// </summary>
    public string? EffectiveDateTimeFormat { get; set; }

    /// <summary>
    /// Gets or sets the effective numeric format string (only meaningful when EffectiveCulture is set).
    /// </summary>
    public string? EffectiveNumberFormat { get; set; }

    /// <summary>
    /// Gets a value indicating whether this mapping uses culture-aware conversion.
    /// </summary>
    public bool HasCulture => !string.IsNullOrEmpty(EffectiveCulture);

    /// <summary>
    /// Gets or sets the kind of user-defined conversion operator detected for this mapping.
    /// Implicit: op_Implicit found — RequiresConversion is set to false (plain assignment).
    /// Explicit: op_Explicit found — generates (TargetType)source.X cast.
    /// </summary>
    public UserDefinedConversionKind UserDefinedConversion { get; set; } = UserDefinedConversionKind.None;

    /// <summary>
    /// Gets a value indicating whether a user-defined explicit conversion operator applies.
    /// </summary>
    public bool HasUserDefinedExplicit => UserDefinedConversion == UserDefinedConversionKind.Explicit;

    /// <summary>
    /// Gets or sets a value indicating whether an explicit numeric narrowing/sign-changing cast is required.
    /// E.g. int -> short, long -> int, int -> uint, double -> int.
    /// </summary>
    public bool RequiresExplicitNumericCast { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether IFormattable.ToString(format, culture) should be used
    /// for T -> string conversion when a Culture or Format is specified.
    /// </summary>
    public bool UseFormattable { get; set; }

    /// <summary>
    /// Gets or sets the kind of enum conversion to apply.
    /// </summary>
    public EnumMappingKind EnumMappingKind { get; set; } = EnumMappingKind.None;

    /// <summary>
    /// Gets or sets the source enum member names (used for enum-to-enum switch generation).
    /// </summary>
    public List<string> SourceEnumMembers { get; set; } = [];

    /// <summary>
    /// Gets or sets the destination enum member names (used for enum-to-enum switch generation).
    /// </summary>
    public List<string> DestEnumMembers { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether this mapping uses enum conversion.
    /// </summary>
    public bool IsEnumMapping => EnumMappingKind != EnumMappingKind.None;

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
               SourceUnderlyingType == other.SourceUnderlyingType &&
               TargetUnderlyingType == other.TargetUnderlyingType &&
               RequiresConversion == other.RequiresConversion &&
               IsSourceNullable == other.IsSourceNullable &&
               IsTargetNullable == other.IsTargetNullable &&
               HasExplicitMapping == other.HasExplicitMapping &&
               ConverterMethod == other.ConverterMethod &&
               ConverterAcceptsCustomParameters == other.ConverterAcceptsCustomParameters &&
               ConditionMethod == other.ConditionMethod &&
               ConditionAcceptsCustomParameters == other.ConditionAcceptsCustomParameters &&
               SpecializedConverterMethod == other.SpecializedConverterMethod &&
               NullSubstitute == other.NullSubstitute &&
               EnumMappingKind == other.EnumMappingKind &&
               UserDefinedConversion == other.UserDefinedConversion &&
               RequiresExplicitNumericCast == other.RequiresExplicitNumericCast &&
               UseFormattable == other.UseFormattable &&
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
            hash = (hash * 31) + (SourceUnderlyingType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetUnderlyingType?.GetHashCode() ?? 0);
            hash = (hash * 31) + RequiresConversion.GetHashCode();
            hash = (hash * 31) + IsSourceNullable.GetHashCode();
            hash = (hash * 31) + IsTargetNullable.GetHashCode();
            hash = (hash * 31) + HasExplicitMapping.GetHashCode();
            hash = (hash * 31) + (ConverterMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + ConverterAcceptsCustomParameters.GetHashCode();
            hash = (hash * 31) + (ConditionMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + ConditionAcceptsCustomParameters.GetHashCode();
            hash = (hash * 31) + (SpecializedConverterMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + (NullSubstitute?.GetHashCode() ?? 0);
            hash = (hash * 31) + EnumMappingKind.GetHashCode();
            hash = (hash * 31) + UserDefinedConversion.GetHashCode();
            hash = (hash * 31) + RequiresExplicitNumericCast.GetHashCode();
            hash = (hash * 31) + UseFormattable.GetHashCode();
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
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
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
