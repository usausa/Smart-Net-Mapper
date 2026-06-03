namespace Smart.Mapper.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    // SMP0001-SMP0002: Method definition validation

    public static DiagnosticDescriptor InvalidMethodDefinition { get; } = new(
        id: "SMP0001",
        title: "Invalid method definition",
        messageFormat: "Mapper method must be static partial. method=[{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMethodParameter { get; } = new(
        id: "SMP0002",
        title: "Invalid method parameter",
        messageFormat: "Mapper method must have at least 1 parameter (for return type) or 2 parameters (for void). method=[{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0003: Custom parameter validation

    public static DiagnosticDescriptor DuplicateCustomParameterType { get; } = new(
        id: "SMP0003",
        title: "Duplicate custom parameter type",
        messageFormat: "Custom parameters must have unique types. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0004-SMP0005: Callback method validation

    public static DiagnosticDescriptor InvalidBeforeMapSignature { get; } = new(
        id: "SMP0004",
        title: "Invalid BeforeMap method signature",
        messageFormat: "BeforeMap method signature does not match. Expected (Source, Destination) or (Source, Destination, customParams...). [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidAfterMapSignature { get; } = new(
        id: "SMP0005",
        title: "Invalid AfterMap method signature",
        messageFormat: "AfterMap method signature does not match. Expected (Source, Destination) or (Source, Destination, customParams...). [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0006-SMP0007: Converter validation

    public static DiagnosticDescriptor InvalidConverterSignature { get; } = new(
        id: "SMP0006",
        title: "Invalid Converter method signature",
        messageFormat: "Converter method signature does not match. Expected (SourceType) or (SourceType, customParams...) returning TargetType. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidConverterReturnType { get; } = new(
        id: "SMP0007",
        title: "Invalid Converter return type",
        messageFormat: "Converter method parameter types match but return type does not match target property type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0008: Property condition validation

    public static DiagnosticDescriptor InvalidPropertyConditionSignature { get; } = new(
        id: "SMP0008",
        title: "Invalid Property Condition method signature",
        messageFormat: "Property condition method signature does not match. Expected (SourceType) or (SourceType, customParams...) returning bool. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0009-SMP0012: MapFrom / MapFromMethod validation

    public static DiagnosticDescriptor InvalidMapFromSignature { get; } = new(
        id: "SMP0009",
        title: "Invalid MapFrom method signature",
        messageFormat: "MapFrom method signature does not match. Expected (Source) or (Source, customParams...) returning target property type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapFromReturnTypeMismatch { get; } = new(
        id: "SMP0010",
        title: "MapFrom return type mismatch",
        messageFormat: "MapFrom method return type does not match target property type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapFromMethodSignature { get; } = new(
        id: "SMP0011",
        title: "Invalid MapFromMethod method signature",
        messageFormat: "MapFromMethod method must be a parameterless method on the source type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MapFromMethodReturnTypeMismatch { get; } = new(
        id: "SMP0012",
        title: "MapFromMethod return type mismatch",
        messageFormat: "MapFromMethod method return type does not match target property type. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0013-SMP0014: MapCollection / MapNested validation

    public static DiagnosticDescriptor InvalidMapCollectionMapperMethod { get; } = new(
        id: "SMP0013",
        title: "Invalid MapCollection mapper method",
        messageFormat: "MapCollection mapper method not found or signature does not match. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMapNestedMapperMethod { get; } = new(
        id: "SMP0014",
        title: "Invalid MapNested mapper method",
        messageFormat: "MapNested mapper method not found or signature does not match. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0015: Mapping conflict validation

    public static DiagnosticDescriptor DuplicateTargetMapping { get; } = new(
        id: "SMP0015",
        title: "Duplicate target mapping",
        messageFormat: "Multiple mapping attributes specify the same target property. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0016-SMP0018: Strict mode / required member validation

    public static DiagnosticDescriptor UnmappedDestinationProperty { get; } = new(
        id: "SMP0016",
        title: "Unmapped destination property",
        messageFormat: "Destination property '{0}' is not mapped (strict mode).",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnmappedRequiredProperty { get; } = new(
        id: "SMP0017",
        title: "Unmapped required property",
        messageFormat: "Required destination property '{0}' has no mapping. Add [MapProperty], [MapConstant], [MapExpression], [MapUsing], [MapFrom], [MapCollection], [MapNested], or [MapIgnore].",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InitOnlyDestinationRequiresReturnMapper { get; } = new(
        id: "SMP0018",
        title: "Init-only destination requires return mapper",
        messageFormat: "Destination type '{0}' has init-only or constructor-only properties and cannot be used with a void mapper. Use a return-type mapper instead.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0019: Culture / format validation

    public static DiagnosticDescriptor FormatWithoutCulture { get; } = new(
        id: "SMP0019",
        title: "Format specified without Culture",
        messageFormat: "DateTimeFormat or NumberFormat is specified but Culture is not set. Format requires a Culture to be meaningful. [{0}]",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0020: Type conversion validation

    public static DiagnosticDescriptor TypeConverterFallbackNotAllowed { get; } = new(
        id: "SMP0020",
        title: "TypeConverter fallback is not AOT-safe",
        messageFormat: "Property '{0}' has no specialized converter and would fall back to Convert<TSource,TDest> which is not AOT-safe. Provide a specialized conversion (ConvertTo{TargetType} method, IParsable, IFormattable, explicit cast, or op_Implicit/op_Explicit), or apply [ValueConverter(typeof(...))] at method level to opt into the generic converter path.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0021: Reflection usage in MapExpression

    public static DiagnosticDescriptor MapExpressionReflectionNotAllowed { get; } = new(
        id: "SMP0021",
        title: "MapExpression contains potentially AOT-incompatible reflection",
        messageFormat: "MapExpression for '{0}' contains potentially AOT-incompatible reflection. Avoid Activator, Type.GetType, MethodInfo, PropertyInfo, FieldInfo, Assembly.Load, and RuntimeHelpers.GetUninitializedObject. Consider using [MapFrom(Method = nameof(...))] or [MapUsing(Method = nameof(...))] instead.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // SMP0022: Constructor parameter cannot be resolved from source

    public static DiagnosticDescriptor UnresolvedConstructorParameter { get; } = new(
        id: "SMP0022",
        title: "Unresolved constructor parameter",
        messageFormat: "Constructor parameter '{0}' of '{1}' has no matching source property. Add a [MapProperty] attribute to specify the source, or ensure a source property with a matching name exists.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // SMP0023-SMP0025: Explicit attribute member resolution failures

    public static DiagnosticDescriptor UnresolvedMapCollectionSourceProperty { get; } = new(
        id: "SMP0023",
        title: "Unresolved MapCollection source property",
        messageFormat: "MapCollection source property '{0}' was not found on the source type. Check the property name for typos.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnresolvedMapCollectionTargetProperty { get; } = new(
        id: "SMP0024",
        title: "Unresolved MapCollection/MapNested target property",
        messageFormat: "MapCollection/MapNested target property '{0}' was not found on the destination type. Check the property name for typos.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnresolvedMapFromTargetProperty { get; } = new(
        id: "SMP0025",
        title: "Unresolved MapFrom target property",
        messageFormat: "MapFrom target property '{0}' was not found on the destination type. Check the property name for typos.",
        category: "Smart.Mapper",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
