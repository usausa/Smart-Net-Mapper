namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while PropertyMappingModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class PropertyMappingDraft
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

    // Set when this mapping supplies a constructor argument instead of an assignment. The mapping
    // stays in PropertyMappings so it still goes through every analysis pass, but the emitters skip
    // it when writing assignments and object-initializer entries.
    public bool IsConstructorParameter { get; set; }

    public int Order { get; set; }

    public int DefinitionOrder { get; set; }

    // Optional per-mapping settings
    public string? ConverterMethod { get; set; }

    public bool ConverterAcceptsCustomParameters { get; set; }

    public string? ConditionMethod { get; set; }

    public bool ConditionAcceptsCustomParameters { get; set; }

    public NullBehaviorType NullBehavior { get; set; } = NullBehaviorType.Default;

    public string? NullValue { get; set; }

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

    public PropertyMappingModel ToModel() => new(
        SourcePath,
        TargetPath,
        SourceType,
        TargetType,
        SourceUnderlyingType,
        TargetUnderlyingType,
        SourcePathSegments,
        TargetPathSegments,
        RequiresConversion,
        IsSourceNullable,
        IsTargetNullable,
        IsTargetInitOnly,
        IsTargetRequired,
        HasExplicitMapping,
        IsConstructorParameter,
        Order,
        DefinitionOrder,
        ConverterMethod,
        ConverterAcceptsCustomParameters,
        ConditionMethod,
        ConditionAcceptsCustomParameters,
        NullBehavior,
        NullValue,
        EffectiveCulture,
        EffectiveDateTimeFormat,
        EffectiveNumberFormat,
        SpecializedConverterMethod,
        ParseMethod,
        UserDefinedConversion,
        RequiresExplicitNumericCast,
        UseFormattable,
        EnumMappingKind,
        SourceEnumMembers,
        DestEnumMembers);

    // The analysis passes need these while the mapping is still a draft. Each one mirrors the
    // identically named member of PropertyMappingModelExtensions, which serves the emitters.
    public bool IsEnumMapping() => EnumMappingKind != EnumMappingKind.None;

    public bool HasConverter() => !String.IsNullOrEmpty(ConverterMethod);

    public bool HasSpecializedConverter() => !String.IsNullOrEmpty(SpecializedConverterMethod);

    public bool HasParsableMethod() => ParseMethod != ParseMethodKind.None;

    public bool HasUserDefinedExplicit() => UserDefinedConversion == UserDefinedConversionKind.Explicit;

    public bool HasCulture() => !String.IsNullOrEmpty(EffectiveCulture);
}
