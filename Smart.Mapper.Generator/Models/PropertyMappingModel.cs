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
    public string SourcePath { get; init; } = default!;
    public string TargetPath { get; init; } = default!;
    public string SourceType { get; init; } = default!;
    public string TargetType { get; init; } = default!;
    public string SourceUnderlyingType { get; init; } = default!;
    public string TargetUnderlyingType { get; init; } = default!;
    public EquatableArray<NestedPathSegment> SourcePathSegments { get; init; } = new([]);
    public EquatableArray<NestedPathSegment> TargetPathSegments { get; init; } = new([]);

    // Base analysis flags / ordering
    public bool RequiresConversion { get; init; }
    public bool IsSourceNullable { get; init; }
    public bool IsTargetNullable { get; init; }
    public bool IsTargetInitOnly { get; init; }
    public bool IsTargetRequired { get; init; }
    public bool HasExplicitMapping { get; init; }

    // Set when this mapping supplies a constructor argument instead of an assignment. The mapping
    // stays in PropertyMappings so it still goes through every analysis pass, but the emitters skip
    // it when writing assignments and object-initializer entries.
    public bool IsConstructorParameter { get; init; }
    public int Order { get; init; }
    public int DefinitionOrder { get; init; }

    // Optional per-mapping settings
    public string? ConverterMethod { get; init; }
    public bool ConverterAcceptsCustomParameters { get; init; }
    public string? ConditionMethod { get; init; }
    public bool ConditionAcceptsCustomParameters { get; init; }
    public NullBehaviorType NullBehavior { get; init; } = NullBehaviorType.Default;
    public string? NullValue { get; init; }
    public string? EffectiveCulture { get; init; }
    public string? EffectiveDateTimeFormat { get; init; }
    public string? EffectiveNumberFormat { get; init; }

    // Conversion-detection results
    public string? SpecializedConverterMethod { get; init; }
    public ParseMethodKind ParseMethod { get; init; } = ParseMethodKind.None;
    public UserDefinedConversionKind UserDefinedConversion { get; init; } = UserDefinedConversionKind.None;
    public bool RequiresExplicitNumericCast { get; init; }
    public bool UseFormattable { get; init; }
    public EnumMappingKind EnumMappingKind { get; init; } = EnumMappingKind.None;
    public EquatableArray<string> SourceEnumMembers { get; init; } = new([]);
    public EquatableArray<string> DestEnumMembers { get; init; } = new([]);
}

internal static class PropertyMappingModelExtensions
{
    public static bool IsEnumMapping(this PropertyMappingModel m) => m.EnumMappingKind != EnumMappingKind.None;

    public static bool HasConverter(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.ConverterMethod);

    public static bool HasSpecializedConverter(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.SpecializedConverterMethod);

    public static bool HasParsableMethod(this PropertyMappingModel m) => m.ParseMethod != ParseMethodKind.None;

    public static bool HasUserDefinedExplicit(this PropertyMappingModel m) => m.UserDefinedConversion == UserDefinedConversionKind.Explicit;

    public static bool HasCondition(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.ConditionMethod);

    public static bool HasNullValue(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.NullValue);

    public static bool HasCulture(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.EffectiveCulture);

    public static bool RequiresNullCheck(this PropertyMappingModel m) =>
        m.SourcePathSegments.Any(s => s.IsNullable);

    public static bool RequiresNullCoalescing(this PropertyMappingModel m) =>
        m.IsSourceNullable && !m.IsTargetNullable && m.NullBehavior == NullBehaviorType.Default && !m.HasNullValue();
}
