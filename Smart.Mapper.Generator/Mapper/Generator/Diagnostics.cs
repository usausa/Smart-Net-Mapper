namespace Smart.Mapper.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidMethodDefinition { get; } = new(
        id: "ML0001",
        title: "Invalid method definition",
        messageFormat: "Mapper method must be static partial. method=[{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMethodParameter { get; } = new(
        id: "ML0002",
        title: "Invalid method parameter",
        messageFormat: "Mapper method must have at least 1 parameter (for return type) or 2 parameters (for void). method=[{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DuplicateCustomParameterType { get; } = new(
        id: "ML0003",
        title: "Duplicate custom parameter type",
        messageFormat: "Custom parameters must have unique types. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidBeforeMapSignature { get; } = new(
        id: "ML0004",
        title: "Invalid BeforeMap method signature",
        messageFormat: "BeforeMap method signature does not match. Expected (Source, Destination) or (Source, Destination, customParams...). [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidAfterMapSignature { get; } = new(
        id: "ML0005",
        title: "Invalid AfterMap method signature",
        messageFormat: "AfterMap method signature does not match. Expected (Source, Destination) or (Source, Destination, customParams...). [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidConverterSignature { get; } = new(
        id: "ML0006",
        title: "Invalid Converter method signature",
        messageFormat: "Converter method signature does not match. Expected (SourceType) or (SourceType, customParams...) returning TargetType. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidConditionSignature { get; } = new(
        id: "ML0007",
        title: "Invalid Condition method signature",
        messageFormat: "Condition method signature does not match. Expected (Source, Destination) or (Source, Destination, customParams...) returning bool. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidPropertyConditionSignature { get; } = new(
        id: "ML0008",
        title: "Invalid Property Condition method signature",
        messageFormat: "Property condition method signature does not match. Expected (SourceType) or (SourceType, customParams...) returning bool. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapFromSignature { get; } = new(
        id: "ML0009",
        title: "Invalid MapFrom method signature",
        messageFormat: "MapFrom method signature does not match. Expected (Source) or (Source, customParams...) returning target property type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapFromMethodSignature { get; } = new(
        id: "ML0010",
        title: "Invalid MapFromMethod method signature",
        messageFormat: "MapFromMethod method must be a parameterless method on the source type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapFromReturnTypeMismatch { get; } = new(
        id: "ML0011",
        title: "MapFrom return type mismatch",
        messageFormat: "MapFrom method return type does not match target property type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapFromMethodReturnTypeMismatch { get; } = new(
        id: "ML0012",
        title: "MapFromMethod return type mismatch",
        messageFormat: "MapFromMethod method return type does not match target property type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapCollectionMapperMethod { get; } = new(
        id: "ML0013",
        title: "Invalid MapCollection mapper method",
        messageFormat: "MapCollection mapper method not found or signature does not match. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapNestedMapperMethod { get; } = new(
        id: "ML0014",
        title: "Invalid MapNested mapper method",
        messageFormat: "MapNested mapper method not found or signature does not match. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DuplicateTargetMapping { get; } = new(
        id: "ML0015",
        title: "Duplicate target mapping",
        messageFormat: "Multiple mapping attributes specify the same target property. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RedundantMappingWithIgnore { get; } = new(
        id: "ML0016",
        title: "Redundant mapping with MapIgnore",
        messageFormat: "A mapping attribute is specified for a property that is also marked with MapIgnore. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnmappedDestinationProperty { get; } = new(
        id: "ML0017",
        title: "Unmapped destination property",
        messageFormat: "Destination property '{0}' is not mapped (strict mode).",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnmappedRequiredProperty { get; } = new(
        id: "ML0018",
        title: "Unmapped required property",
        messageFormat: "Required destination property '{0}' has no mapping. Add [MapProperty], [MapConstant], [MapExpression], [MapUsing], [MapFrom], [MapCollection], [MapNested], or [MapIgnore].",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InitOnlyDestinationRequiresReturnMapper { get; } = new(
        id: "ML0019",
        title: "Init-only destination requires return mapper",
        messageFormat: "Destination type '{0}' has init-only or constructor-only properties and cannot be used with a void mapper. Use a return-type mapper instead.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor FormatWithoutCulture { get; } = new(
        id: "ML0020",
        title: "Format specified without Culture",
        messageFormat: "DateTimeFormat or NumberFormat is specified but Culture is not set. Format requires a Culture to be meaningful. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CultureAwareConverterNotFound { get; } = new(
        id: "ML0021",
        title: "Culture-aware converter overload not found",
        messageFormat: "Culture is specified but no culture-aware converter overload (IFormatProvider, string?) was found for property '{0}'. Add an overload or remove the Culture setting.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
