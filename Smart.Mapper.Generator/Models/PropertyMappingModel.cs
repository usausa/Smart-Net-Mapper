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
internal sealed record PropertyMappingModel(
    // Identity
    string SourcePath = default!,
    string TargetPath = default!,
    string SourceType = default!,
    string TargetType = default!,
    string SourceUnderlyingType = default!,
    string TargetUnderlyingType = default!,
    EquatableArray<NestedPathSegment> SourcePathSegments = default,
    EquatableArray<NestedPathSegment> TargetPathSegments = default,
    // Base analysis flags / ordering
    bool RequiresConversion = default,
    bool IsSourceNullable = default,
    bool IsTargetNullable = default,
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default,
    bool HasExplicitMapping = default,
    // Set when this mapping supplies a constructor argument instead of an assignment. The mapping
    // stays in PropertyMappings so it still goes through every analysis pass, but the emitters skip
    // it when writing assignments and object-initializer entries.
    bool IsConstructorParameter = default,
    int Order = default,
    int DefinitionOrder = default,
    // Optional per-mapping settings
    string? ConverterMethod = default,
    bool ConverterAcceptsCustomParameters = default,
    string? ConditionMethod = default,
    bool ConditionAcceptsCustomParameters = default,
    NullBehaviorType NullBehavior = NullBehaviorType.Default,
    string? NullValue = default,
    string? EffectiveCulture = default,
    string? EffectiveDateTimeFormat = default,
    string? EffectiveNumberFormat = default,
    // Conversion-detection results
    string? SpecializedConverterMethod = default,
    ParseMethodKind ParseMethod = ParseMethodKind.None,
    UserDefinedConversionKind UserDefinedConversion = UserDefinedConversionKind.None,
    bool RequiresExplicitNumericCast = default,
    bool UseFormattable = default,
    EnumMappingKind EnumMappingKind = EnumMappingKind.None,
    EquatableArray<string> SourceEnumMembers = default,
    EquatableArray<string> DestEnumMembers = default);

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
        m.IsSourceNullable && !m.IsTargetNullable && (m.NullBehavior == NullBehaviorType.Default) && !m.HasNullValue();
}
