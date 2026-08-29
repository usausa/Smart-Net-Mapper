namespace Smart.Mapper.Generator;

using Microsoft.CodeAnalysis;

// Core Mapper generator diagnostics. IDs follow a phase-based banding aligned with the pipeline:
//   SMP00xx  method definition   (BuildModel entry: static partial / parameter shape / custom parameters)
//   SMP01xx  attribute validation(duplicate targets, callbacks, converters, conditions)
//   SMP02xx  explicit features   (MapUsing / MapFrom / MapCollection / MapNested resolution)
//   SMP03xx  construction        (constructor parameters, init-only / required members)
//   SMP04xx  conversion / AOT    (culture-format pairing, TypeConverter fallback, reflection usage)
//   SMP05xx  strict mode         (advisory unmapped-property warnings)
internal static class Diagnostics
{
    // ==================================================================
    // SMP00xx — method definition
    // ==================================================================

    public static DiagnosticDescriptor InvalidMethodDefinition { get; } = new(
        id: "SMP0001",
        title: "Invalid mapper method definition",
        messageFormat: "[Mapper] method must be static partial. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMethodParameter { get; } = new(
        id: "SMP0002",
        title: "Invalid mapper method parameters",
        messageFormat: "[Mapper] method parameter count is invalid. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DuplicateCustomParameterType { get; } = new(
        id: "SMP0003",
        title: "Duplicate custom parameter type",
        messageFormat: "[Mapper] custom parameters must have unique types. method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP01xx — attribute validation
    // ==================================================================

    public static DiagnosticDescriptor DuplicateTargetMapping { get; } = new(
        id: "SMP0101",
        title: "Duplicate target mapping",
        messageFormat: "Multiple attributes specify the same target. method=[{0}], target=[{1}], attributes=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidBeforeMapSignature { get; } = new(
        id: "SMP0102",
        title: "Invalid BeforeMap method signature",
        messageFormat: "[BeforeMap] signature does not match. method=[{0}], callback=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidAfterMapSignature { get; } = new(
        id: "SMP0103",
        title: "Invalid AfterMap method signature",
        messageFormat: "[AfterMap] signature does not match. method=[{0}], callback=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidConverterSignature { get; } = new(
        id: "SMP0104",
        title: "Invalid converter method signature",
        messageFormat: "Converter signature does not match. method=[{0}], converter=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidConverterReturnType { get; } = new(
        id: "SMP0105",
        title: "Converter return type mismatch",
        messageFormat: "Converter return type does not match. method=[{0}], converter=[{1}], expected=[{2}], actual=[{3}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidPropertyConditionSignature { get; } = new(
        id: "SMP0106",
        title: "Invalid property condition signature",
        messageFormat: "Condition signature does not match. method=[{0}], condition=[{1}], target=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP02xx — explicit features (MapUsing / MapFrom / MapCollection / MapNested)
    // ==================================================================

    public static DiagnosticDescriptor InvalidMapUsingSignature { get; } = new(
        id: "SMP0201",
        title: "Invalid MapUsing method signature",
        messageFormat: "[MapUsing] signature does not match. method=[{0}], using=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapUsingReturnTypeMismatch { get; } = new(
        id: "SMP0202",
        title: "MapUsing return type mismatch",
        messageFormat: "[MapUsing] return type does not match. method=[{0}], using=[{1}], expected=[{2}], actual=[{3}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnresolvedMapFromTargetProperty { get; } = new(
        id: "SMP0203",
        title: "Unresolved MapFrom target property",
        messageFormat: "[MapFrom] target property is not found. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapFromMember { get; } = new(
        id: "SMP0204",
        title: "Invalid MapFrom member",
        messageFormat: "[MapFrom] member is not supported. method=[{0}], member=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapFromReturnTypeMismatch { get; } = new(
        id: "SMP0205",
        title: "MapFrom member type mismatch",
        messageFormat: "[MapFrom] member type does not match. method=[{0}], member=[{1}], expected=[{2}], actual=[{3}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnresolvedMapCollectionSourceProperty { get; } = new(
        id: "SMP0206",
        title: "Unresolved MapCollection source",
        messageFormat: "[MapCollection]/[MapNested] source property is not found. method=[{0}], source=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnresolvedMapCollectionTargetProperty { get; } = new(
        id: "SMP0207",
        title: "Unresolved MapCollection target",
        messageFormat: "[MapCollection]/[MapNested] target property is not found. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapCollectionSourceNotCollection { get; } = new(
        id: "SMP0208",
        title: "Source property is not a collection",
        messageFormat: "[MapCollection] source is not a collection. method=[{0}], source=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapCollectionTargetNotCollection { get; } = new(
        id: "SMP0209",
        title: "Target property is not a collection",
        messageFormat: "[MapCollection] target is not a collection. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapCollectionMapperMethod { get; } = new(
        id: "SMP0210",
        title: "Invalid MapCollection mapper method",
        messageFormat: "[MapCollection] element mapper method does not match. method=[{0}], mapper=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapNestedMapperMethod { get; } = new(
        id: "SMP0211",
        title: "Invalid MapNested mapper method",
        messageFormat: "[MapNested] mapper method does not match. method=[{0}], mapper=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedInitOnlyCollectionTarget { get; } = new(
        id: "SMP0212",
        title: "Unsupported init-only target",
        messageFormat: "[MapCollection]/[MapNested] target is init-only or required. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnresolvedMapPropertySourceProperty { get; } = new(
        id: "SMP0213",
        title: "Unresolved MapProperty source",
        messageFormat: "[MapProperty] source is not found. method=[{0}], target=[{1}], source=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnresolvedMapPropertyTargetProperty { get; } = new(
        id: "SMP0214",
        title: "Unresolved MapProperty target",
        messageFormat: "[MapProperty] target is not assignable. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedConstructorAssignedOption { get; } = new(
        id: "SMP0215",
        title: "Unsupported constructor-assigned option",
        messageFormat: "[MapCondition] requires a property assignment. method=[{0}], target=[{1}], option=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor IgnoredConstructorParameter { get; } = new(
        id: "SMP0216",
        title: "Ignored constructor parameter",
        messageFormat: "[MapIgnore] member is assigned by a constructor. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP03xx — construction (constructor parameters, init-only / required members)
    // ==================================================================

    public static DiagnosticDescriptor UnresolvedConstructorParameter { get; } = new(
        id: "SMP0301",
        title: "Unresolved constructor parameter",
        messageFormat: "Constructor parameter has no source. method=[{0}], parameter=[{1}], type=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InitOnlyDestinationRequiresReturnMapper { get; } = new(
        id: "SMP0302",
        title: "Return-type mapper is required",
        messageFormat: "Void mapper cannot assign init-only members. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnmappedRequiredProperty { get; } = new(
        id: "SMP0303",
        title: "Unmapped required property",
        messageFormat: "Required property has no mapping. method=[{0}], property=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP04xx — conversion / AOT
    // ==================================================================

    public static DiagnosticDescriptor FormatWithoutCulture { get; } = new(
        id: "SMP0401",
        title: "Format specified without Culture",
        messageFormat: "Format is specified without Culture. method=[{0}], target=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypeConverterFallbackNotAllowed { get; } = new(
        id: "SMP0402",
        title: "TypeConverter fallback is not AOT-safe",
        messageFormat: "Conversion falls back to a non-AOT-safe path. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapExpressionReflectionNotAllowed { get; } = new(
        id: "SMP0403",
        title: "MapExpression uses reflection",
        messageFormat: "[MapExpression] may use reflection. method=[{0}], target=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP05xx — strict mode
    // ==================================================================

    public static DiagnosticDescriptor UnmappedDestinationProperty { get; } = new(
        id: "SMP0501",
        title: "Unmapped destination property",
        messageFormat: "Destination property is not mapped. method=[{0}], property=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
