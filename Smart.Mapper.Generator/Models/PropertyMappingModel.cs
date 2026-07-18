namespace Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Represents the kind of enum conversion to perform.
internal enum EnumMappingKind
{
    None = 0,
    EnumToEnum = 1,
    EnumToNumeric = 2,
    NumericToEnum = 3,
    EnumToString = 4,
    StringToEnum = 5
}

// Represents the kind of user-defined conversion operator to apply.
internal enum UserDefinedConversionKind
{
    None = 0,
    Implicit = 1,
    Explicit = 2
}

// Represents the kind of IParsable / ISpanParsable parse method to use.
internal enum ParseMethodKind
{
    None = 0,
    SpanParsable = 1,
    Parsable = 2
}

// Represents the null behavior for property mapping.
internal enum NullBehaviorType
{
    Default = 0,
    Skip = 1
}

// Represents a property mapping configuration.
internal sealed record PropertyMappingModel
{
    // Identity
    public string SourcePath { get; set; } = default!;
    public string TargetPath { get; set; } = default!;
    public string SourceType { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string SourceUnderlyingType { get; set; } = default!;
    public string TargetUnderlyingType { get; set; } = default!;
    public EquatableArray<NestedPathSegment> SourcePathSegments { get; set; } = new([]);
    public EquatableArray<NestedPathSegment> TargetPathSegments { get; set; } = new([]);

    // Base analysis flags / ordering
    public bool RequiresConversion { get; set; }
    public bool IsSourceNullable { get; set; }
    public bool IsTargetNullable { get; set; }
    public bool IsTargetInitOnly { get; set; }
    public bool IsTargetRequired { get; set; }
    public bool HasExplicitMapping { get; set; }
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }

    // Optional per-mapping settings
    public string? ConverterMethod { get; set; }
    public bool ConverterAcceptsCustomParameters { get; set; }
    public string? ConditionMethod { get; set; }
    public bool ConditionAcceptsCustomParameters { get; set; }
    public NullBehaviorType NullBehavior { get; set; } = NullBehaviorType.Default;
    public string? NullSubstitute { get; set; }
    public string? EffectiveCulture { get; set; }
    public string? EffectiveDateTimeFormat { get; set; }
    public string? EffectiveNumberFormat { get; set; }

    // Conversion-detection results
    public string? SpecializedConverterMethod { get; set; }
    public ParseMethodKind ParseMethod { get; set; } = ParseMethodKind.None;
    public UserDefinedConversionKind UserDefinedConversion { get; set; } = UserDefinedConversionKind.None;
    public bool RequiresExplicitNumericCast { get; set; }
    public bool UseFormattable { get; set; }
    public EnumMappingKind EnumMappingKind { get; set; } = EnumMappingKind.None;
    public EquatableArray<string> SourceEnumMembers { get; set; } = new([]);
    public EquatableArray<string> DestEnumMembers { get; set; } = new([]);
}

// Represents a segment in a nested property path.
internal sealed record NestedPathSegment
{
    public string Path { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public bool IsNullable { get; set; }
}

internal static class PropertyMappingModelExtensions
{
    public static bool IsEnumMapping(this PropertyMappingModel m) => m.EnumMappingKind != EnumMappingKind.None;

    public static bool HasConverter(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.ConverterMethod);

    public static bool HasSpecializedConverter(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.SpecializedConverterMethod);

    public static bool HasParsableMethod(this PropertyMappingModel m) => m.ParseMethod != ParseMethodKind.None;

    public static bool HasUserDefinedExplicit(this PropertyMappingModel m) => m.UserDefinedConversion == UserDefinedConversionKind.Explicit;

    public static bool HasCondition(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.ConditionMethod);

    public static bool HasNullSubstitute(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.NullSubstitute);

    public static bool HasCulture(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.EffectiveCulture);

    public static bool RequiresNullCheck(this PropertyMappingModel m) =>
        m.SourcePathSegments.Any(s => s.IsNullable);

    public static bool RequiresNullCoalescing(this PropertyMappingModel m) =>
        m.IsSourceNullable && !m.IsTargetNullable && m.NullBehavior == NullBehaviorType.Default && !m.HasNullSubstitute();
}
