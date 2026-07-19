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

    public static readonly DiagnosticDescriptor InvalidMethodDefinition = new(
        id: "SMP0001",
        title: "Invalid mapper method definition",
        messageFormat: "Mapper method must be static partial. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMethodParameter = new(
        id: "SMP0002",
        title: "Invalid mapper method parameters",
        messageFormat: "Mapper method must have at least 1 parameter (return pattern) or 2 parameters (void pattern). method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateCustomParameterType = new(
        id: "SMP0003",
        title: "Duplicate custom parameter type",
        messageFormat: "Custom parameters must have unique types. method=[{0}], type=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP01xx — attribute validation
    // ==================================================================

    public static readonly DiagnosticDescriptor DuplicateTargetMapping = new(
        id: "SMP0101",
        title: "Duplicate target mapping",
        messageFormat: "Multiple mapping attributes specify the same target property. method=[{0}], target=[{1}], attributes=[{2}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidBeforeMapSignature = new(
        id: "SMP0102",
        title: "Invalid BeforeMap method signature",
        messageFormat: "BeforeMap method signature does not match; expected (Source, Destination) or (Source, Destination, customParams...). method=[{0}], callback=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidAfterMapSignature = new(
        id: "SMP0103",
        title: "Invalid AfterMap method signature",
        messageFormat: "AfterMap method signature does not match; expected (Source, Destination) or (Source, Destination, customParams...). method=[{0}], callback=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidConverterSignature = new(
        id: "SMP0104",
        title: "Invalid converter method signature",
        messageFormat: "Converter method signature does not match; expected (SourceType) or (SourceType, customParams...) returning the target property type. method=[{0}], converter=[{1}], target=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidConverterReturnType = new(
        id: "SMP0105",
        title: "Converter return type mismatch",
        messageFormat: "Converter method parameter types match but the return type does not match the target property type. method=[{0}], converter=[{1}], expected=[{2}], actual=[{3}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidPropertyConditionSignature = new(
        id: "SMP0106",
        title: "Invalid property condition method signature",
        messageFormat: "Property condition method signature does not match; expected (SourceType) or (SourceType, customParams...) returning bool. method=[{0}], condition=[{1}], target=[{2}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP02xx — explicit features (MapUsing / MapFrom / MapCollection / MapNested)
    // ==================================================================

    public static readonly DiagnosticDescriptor InvalidMapUsingSignature = new(
        id: "SMP0201",
        title: "Invalid MapUsing method signature",
        messageFormat: "MapUsing method signature does not match; expected (Source) or (Source, customParams...) returning the target property type. method=[{0}], using=[{1}], target=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MapUsingReturnTypeMismatch = new(
        id: "SMP0202",
        title: "MapUsing return type mismatch",
        messageFormat: "MapUsing method return type does not match the target property type. method=[{0}], using=[{1}], expected=[{2}], actual=[{3}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnresolvedMapFromTargetProperty = new(
        id: "SMP0203",
        title: "Unresolved MapFrom target property",
        messageFormat: "MapFrom target property was not found on the destination type. method=[{0}], target=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMapFromMember = new(
        id: "SMP0204",
        title: "Invalid MapFrom member",
        messageFormat: "MapFrom member must be a parameterless method or a property path on the source type. method=[{0}], member=[{1}], target=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MapFromReturnTypeMismatch = new(
        id: "SMP0205",
        title: "MapFrom member type mismatch",
        messageFormat: "MapFrom member type does not match the target property type. method=[{0}], member=[{1}], expected=[{2}], actual=[{3}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnresolvedMapCollectionSourceProperty = new(
        id: "SMP0206",
        title: "Unresolved MapCollection/MapNested source property",
        messageFormat: "Source property specified in [MapCollection]/[MapNested] was not found on the source type. method=[{0}], source=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnresolvedMapCollectionTargetProperty = new(
        id: "SMP0207",
        title: "Unresolved MapCollection/MapNested target property",
        messageFormat: "Target property specified in [MapCollection]/[MapNested] was not found on the destination type. method=[{0}], target=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MapCollectionSourceNotCollection = new(
        id: "SMP0208",
        title: "MapCollection source property is not a collection",
        messageFormat: "[MapCollection] source property is not a collection type; IEnumerable<T> implementations, Memory<T>, and ReadOnlyMemory<T> are supported. method=[{0}], source=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MapCollectionTargetNotCollection = new(
        id: "SMP0209",
        title: "MapCollection target property is not a collection",
        messageFormat: "[MapCollection] target property is not a collection type; only IEnumerable<T> implementations are supported. method=[{0}], target=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMapCollectionMapperMethod = new(
        id: "SMP0210",
        title: "Invalid MapCollection element mapper method",
        messageFormat: "MapCollection element mapper method was not found or its signature does not match. method=[{0}], mapper=[{1}], target=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMapNestedMapperMethod = new(
        id: "SMP0211",
        title: "Invalid MapNested mapper method",
        messageFormat: "MapNested mapper method was not found or its signature does not match. method=[{0}], mapper=[{1}], target=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedInitOnlyCollectionTarget = new(
        id: "SMP0212",
        title: "Unsupported init-only or required target for MapCollection/MapNested",
        messageFormat: "[MapCollection]/[MapNested] cannot target an init-only property (or a required property with a return-type mapper) because the generated loop cannot run inside an object initializer; use a settable property. method=[{0}], target=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnresolvedMapPropertySourceProperty = new(
        id: "SMP0213",
        title: "Unresolved MapProperty source property",
        messageFormat: "Source property specified in [MapProperty] was not found on the source type. method=[{0}], target=[{1}], source=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnresolvedMapPropertyTargetProperty = new(
        id: "SMP0214",
        title: "Unresolved MapProperty target property",
        messageFormat: "Target property specified in [MapProperty] was not found on the destination type, or has no setter and is not assigned by a constructor. method=[{0}], target=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedConstructorAssignedOption = new(
        id: "SMP0215",
        title: "Unsupported option for a constructor-assigned target",
        messageFormat: "[MapCondition] and NullBehavior.Skip require a property assignment and cannot apply to a member assigned through a constructor or object initializer. method=[{0}], target=[{1}], option=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IgnoredConstructorParameter = new(
        id: "SMP0216",
        title: "Ignored member is assigned through a constructor",
        messageFormat: "An ignored member is assigned through a constructor, which requires a value for it; remove [MapIgnore] or provide the value explicitly. method=[{0}], target=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP03xx — construction (constructor parameters, init-only / required members)
    // ==================================================================

    public static readonly DiagnosticDescriptor UnresolvedConstructorParameter = new(
        id: "SMP0301",
        title: "Unresolved constructor parameter",
        messageFormat: "Constructor parameter has no matching source property; add a [MapProperty] for it or provide a source property with a matching name. method=[{0}], parameter=[{1}], type=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InitOnlyDestinationRequiresReturnMapper = new(
        id: "SMP0302",
        title: "Init-only destination requires a return-type mapper",
        messageFormat: "Destination type has init-only or constructor-only members that a void mapper can never assign; use a return-type mapper. method=[{0}], type=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnmappedRequiredProperty = new(
        id: "SMP0303",
        title: "Unmapped required property",
        messageFormat: "Required destination property has no mapping; add [MapProperty], [MapConstant], [MapExpression], [MapUsing], [MapFrom], [MapCollection], [MapNested], or [MapIgnore]. method=[{0}], property=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP04xx — conversion / AOT
    // ==================================================================

    public static readonly DiagnosticDescriptor FormatWithoutCulture = new(
        id: "SMP0401",
        title: "Format specified without Culture",
        messageFormat: "DateTimeFormat or NumberFormat is specified but Culture is not set; a format requires a culture to be meaningful. method=[{0}], target=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeConverterFallbackNotAllowed = new(
        id: "SMP0402",
        title: "TypeConverter fallback is not AOT-safe",
        messageFormat: "Property has no specialized conversion and would fall back to Convert<TSource, TDestination>, which is not AOT-safe; provide a specialized conversion (ConvertToXxx method, IParsable, IFormattable, explicit cast, or op_Implicit/op_Explicit), or apply [ValueConverter(typeof(...))] to opt into the generic converter path. method=[{0}], target=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MapExpressionReflectionNotAllowed = new(
        id: "SMP0403",
        title: "MapExpression contains potentially AOT-incompatible reflection",
        messageFormat: "MapExpression contains potentially AOT-incompatible reflection; avoid Activator, Type.GetType, MethodInfo, PropertyInfo, FieldInfo, Assembly.Load, and RuntimeHelpers.GetUninitializedObject, or use [MapFrom] / [MapUsing] instead. method=[{0}], target=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP05xx — strict mode
    // ==================================================================

    public static readonly DiagnosticDescriptor UnmappedDestinationProperty = new(
        id: "SMP0501",
        title: "Unmapped destination property",
        messageFormat: "Destination property is not mapped in strict mode. method=[{0}], property=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
