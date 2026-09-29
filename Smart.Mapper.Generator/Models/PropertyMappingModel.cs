namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

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
    // Optional per-mapping settings. The custom parameters of the mapper the matched method takes after the value, as
    // their indexes in the order of its parameters, and the RefKinds of its parameters, which decide how each argument
    // is passed
    string? ConverterMethod = default,
    EquatableArray<int> ConverterCustomArguments = default,
    EquatableArray<RefKind> ConverterParameterRefKinds = default,
    string? ConditionMethod = default,
    EquatableArray<int> ConditionCustomArguments = default,
    EquatableArray<RefKind> ConditionParameterRefKinds = default,
    // The parameter of the converter / the condition taking the source value does not take null (a reference
    // annotated as not null, [DisallowNull], or the struct a nullable struct source holds, which goes to it as its
    // Value), so a nullable source goes to it only when it has a value. A converter returning a nullable reference
    // into a target not annotated as nullable is taken with !
    bool ConverterUnwrapsSource = default,
    bool ConverterRejectsNull = default,
    bool ConverterForgivesNull = default,
    bool ConditionUnwrapsSource = default,
    bool ConditionRejectsNull = default,
    NullBehaviorType NullBehavior = NullBehaviorType.Default,
    string? NullValue = default,
    // Set for a NullValue that cannot be written (NullValue is then null, SMP0215), and for one that is
    // an array holding null
    bool IsNullValueUnsupported = default,
    bool NullValueHasNullElement = default,
    // The culture name whose field the conversion uses, directly or for a null CultureInfo parameter
    string? EffectiveCulture = default,
    string? EffectiveDateTimeFormat = default,
    string? EffectiveNumberFormat = default,
    // The culture the conversion goes with, as the generated code writes it: the field of a culture name, the current
    // or the invariant culture, or the CultureInfo parameter; null for the conversions without a culture
    string? CultureArgument = default,
    // Conversion-detection results. With a culture, the overload of the specialized method taking the
    // culture and the format is called, and the culture goes to it with CultureArgumentModifier
    string? SpecializedConverterMethod = default,
    string CultureArgumentModifier = "",
    ParseMethodKind ParseMethod = ParseMethodKind.None,
    UserDefinedConversionKind UserDefinedConversion = UserDefinedConversionKind.None,
    bool RequiresExplicitNumericCast = default,
    bool UseFormattable = default,
    EnumMappingKind EnumMappingKind = EnumMappingKind.None,
    EquatableArray<string> SourceEnumMembers = default,
    EquatableArray<string> DestEnumMembers = default,
    // Parallel to the member names: the number a member marked [Obsolete] is written as, cast to the enum
    // type, as naming it warns (CS0618) or fails (CS0619); empty for a member written by its name
    EquatableArray<string> SourceEnumCastValues = default,
    EquatableArray<string> DestEnumCastValues = default,
    // The attribute this was declared by, as its index in MapperMethodModel.AttributeLocations, which the
    // diagnostics about it point to; -1 for one the automatic mapping made
    int AttributeIndex = -1);

internal static class PropertyMappingModelExtensions
{
    public static bool IsEnumMapping(this PropertyMappingModel m) => m.EnumMappingKind != EnumMappingKind.None;

    public static bool HasConverter(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.ConverterMethod);

    public static bool HasSpecializedConverter(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.SpecializedConverterMethod);

    public static bool HasParsableMethod(this PropertyMappingModel m) => m.ParseMethod != ParseMethodKind.None;

    public static bool HasUserDefinedExplicit(this PropertyMappingModel m) => m.UserDefinedConversion == UserDefinedConversionKind.Explicit;

    public static bool HasCondition(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.ConditionMethod);

    public static bool HasNullValue(this PropertyMappingModel m) => !String.IsNullOrEmpty(m.NullValue);

    public static bool HasCulture(this PropertyMappingModel m) => m.CultureArgument is not null;

    public static bool RequiresNullCheck(this PropertyMappingModel m) =>
        m.SourcePathSegments.Any(s => s.IsNullable);

    // Whether the assignment of the mapping is written under a check of its own, which may leave the target as it is:
    // a condition, NullBehavior.Skip for a nullable source, or, for a nullable source, a converter whose parameter
    // does not take null and no NullValue to give instead. Such an assignment creates the intermediate members of its
    // target path only when it assigns.
    public static bool IsAssignmentGuarded(this PropertyMappingModel m) =>
        m.HasCondition() ||
        (m.IsSourceNullable && ((m.NullBehavior == NullBehaviorType.Skip) || (m.HasConverter() && m.ConverterRejectsNull && !m.HasNullValue())));

    public static bool RequiresNullCoalescing(this PropertyMappingModel m) =>
        m.IsSourceNullable && !m.IsTargetNullable && (m.NullBehavior == NullBehaviorType.Default) && !m.HasNullValue();
}
