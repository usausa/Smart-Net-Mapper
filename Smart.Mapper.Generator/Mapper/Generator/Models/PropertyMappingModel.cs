namespace Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

/// <summary>
/// Represents the kind of enum conversion to perform.
/// </summary>
internal enum EnumMappingKind
{
    None = 0,
    EnumToEnum = 1,
    EnumToNumeric = 2,
    NumericToEnum = 3,
    EnumToString = 4,
    StringToEnum = 5
}

/// <summary>
/// Represents the kind of user-defined conversion operator to apply.
/// </summary>
internal enum UserDefinedConversionKind
{
    None = 0,
    Implicit = 1,
    Explicit = 2,
}

/// <summary>
/// Represents the kind of IParsable / ISpanParsable parse method to use.
/// </summary>
internal enum ParseMethodKind
{
    None = 0,
    ISpanParsable = 1,
    IParsable = 2,
}

/// <summary>
/// Represents the null behavior for property mapping.
/// </summary>
internal enum NullBehaviorType
{
    Default = 0,
    Skip = 1
}

/// <summary>
/// Represents a property mapping configuration.
/// </summary>
internal sealed record PropertyMappingModel
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string SourceUnderlyingType { get; set; } = string.Empty;
    public string TargetUnderlyingType { get; set; } = string.Empty;
    public bool RequiresConversion { get; set; }
    public EquatableArray<NestedPathSegment> TargetPathSegments { get; set; } = new([]);
    public EquatableArray<NestedPathSegment> SourcePathSegments { get; set; } = new([]);
    public bool IsSourceNullable { get; set; }
    public bool IsTargetNullable { get; set; }
    public string? ConverterMethod { get; set; }
    public bool ConverterAcceptsCustomParameters { get; set; }
    public string? ConditionMethod { get; set; }
    public bool ConditionAcceptsCustomParameters { get; set; }
    public bool HasExplicitMapping { get; set; }
    public bool IsTargetInitOnly { get; set; }
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
    public NullBehaviorType NullBehavior { get; set; } = NullBehaviorType.Default;
    public string? NullSubstitute { get; set; }
    public string? SpecializedConverterMethod { get; set; }
    public ParseMethodKind ParseMethod { get; set; } = ParseMethodKind.None;
    public string? EffectiveCulture { get; set; }
    public string? EffectiveDateTimeFormat { get; set; }
    public string? EffectiveNumberFormat { get; set; }
    public UserDefinedConversionKind UserDefinedConversion { get; set; } = UserDefinedConversionKind.None;
    public bool RequiresExplicitNumericCast { get; set; }
    public bool UseFormattable { get; set; }
    public EnumMappingKind EnumMappingKind { get; set; } = EnumMappingKind.None;
    public EquatableArray<string> SourceEnumMembers { get; set; } = new([]);
    public EquatableArray<string> DestEnumMembers { get; set; } = new([]);
}

/// <summary>
/// Represents a segment in a nested property path.
/// </summary>
internal sealed record NestedPathSegment
{
    public string Path { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
}
