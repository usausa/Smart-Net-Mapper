namespace Smart.Mapper.Generator;

using Microsoft.CodeAnalysis;

// Core Mapper generator diagnostics. IDs follow a phase-based banding aligned with the pipeline:
//   SMP00xx  method definition   (BuildModel entry: static partial / parameter shape / reserved parameter names / parameter modifiers / custom parameters / nullable struct source / collection mapped as a whole / ref return)
//   SMP01xx  attribute validation(duplicate targets, callbacks, converters, conditions)
//   SMP02xx  explicit features   (MapUsing / MapFrom / MapCollection / MapNested resolution)
//   SMP03xx  construction        (constructor parameters, init-only / required members)
//   SMP04xx  conversion / AOT    (culture-format pairing, TypeConverter fallback, reflection usage)
//   SMP05xx  strict mode         (advisory warnings: unmapped properties, nullable values, unmatched enum members)
internal static class Diagnostics
{
    // ==================================================================
    // SMP00xx — method definition
    // ==================================================================

    public static DiagnosticDescriptor InvalidMethodDefinition { get; } = new(
        id: "SMP0001",
        title: "Invalid mapper method definition",
        messageFormat: "[Mapper] method must be static partial, in types that are all partial and not file-local. method=[{0}]",
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

    public static DiagnosticDescriptor ReservedParameterName { get; } = new(
        id: "SMP0004",
        title: "Reserved parameter name",
        messageFormat: "[Mapper] parameter names starting with __ are reserved for the generated code, rename the parameter. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedParameterModifier { get; } = new(
        id: "SMP0005",
        title: "Unsupported parameter modifier",
        messageFormat: "[Mapper] parameter modifier is not supported, the generated code reads every parameter and assigns the destination members, which a struct destination takes by ref. method=[{0}], parameter=[{1}], modifier=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NullableValueTypeSource { get; } = new(
        id: "SMP0006",
        title: "Nullable value type source",
        messageFormat: "[Mapper] source parameter cannot be a nullable value type, which has none of the members of the struct it holds. method=[{0}], parameter=[{1}], type=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CollectionMapper { get; } = new(
        id: "SMP0007",
        title: "Collection mapped as a whole",
        messageFormat: "[Mapper] cannot map a collection, an array or a tuple as a whole, map the elements with a mapper of the element type (source.Select(ToDto).ToList()) or use [MapCollection] on a type holding the collection. method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RefReturnMapper { get; } = new(
        id: "SMP0008",
        title: "Mapper returning by reference",
        messageFormat: "[Mapper] method cannot return by reference, it returns the destination it creates by value. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SMP01xx — attribute validation
    // ==================================================================

    public static DiagnosticDescriptor DuplicateTargetMapping { get; } = new(
        id: "SMP0101",
        title: "Duplicate target mapping",
        messageFormat: "Multiple attributes map or ignore the same target, or map the target and a member of it. method=[{0}], target=[{1}], attributes=[{2}]",
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
        messageFormat: "[MapCollection]/[MapNested] source property is not found, the source is a property of the source type and cannot be a dotted path. method=[{0}], source=[{1}]",
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

    // The same diagnostic for an attribute without Mapper, which the message tells
    public static DiagnosticDescriptor MapCollectionMapperNotSpecified { get; } = new(
        id: "SMP0210",
        title: "Invalid MapCollection mapper method",
        messageFormat: "[MapCollection] Mapper is not specified, the elements are mapped with an element mapper. method=[{0}], target=[{1}]",
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

    // The same diagnostic for an attribute without Mapper, which the message tells
    public static DiagnosticDescriptor MapNestedMapperNotSpecified { get; } = new(
        id: "SMP0211",
        title: "Invalid MapNested mapper method",
        messageFormat: "[MapNested] Mapper is not specified, the member is mapped with a mapper. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedInitOnlyCollectionTarget { get; } = new(
        id: "SMP0212",
        title: "Unassignable target",
        messageFormat: "[MapCollection]/[MapNested] target has no setter or init accessor the mapper can call, or is init-only in a void mapper. method=[{0}], target=[{1}]",
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
        title: "Unassignable mapping target",
        messageFormat: "Mapping target is not found or cannot be assigned. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // The same diagnostic for a dotted target going through a nullable struct, which the message tells
    public static DiagnosticDescriptor NullableStructTargetPath { get; } = new(
        id: "SMP0214",
        title: "Unassignable mapping target",
        messageFormat: "Mapping target goes through a nullable struct, which a dotted target cannot write into, map the struct as a whole. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedConstructorAssignedOption { get; } = new(
        id: "SMP0215",
        title: "Unsupported constructor-assigned option",
        messageFormat: "[MapCondition] / NullBehavior.Skip requires a property assignment, not a constructor argument or an object initializer entry. method=[{0}], target=[{1}], option=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor IgnoredConstructorParameter { get; } = new(
        id: "SMP0216",
        title: "Ignored member required by construction",
        messageFormat: "[MapIgnore] member has to be assigned when the destination is constructed. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedCollectionTarget { get; } = new(
        id: "SMP0217",
        title: "Unsupported collection target",
        messageFormat: "[MapCollection] target cannot take the collection the generated code creates for it. method=[{0}], target=[{1}], collection=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnassignableConstantValue { get; } = new(
        id: "SMP0218",
        title: "Unassignable constant value",
        messageFormat: "Constant value cannot be assigned to the target. method=[{0}], target=[{1}], value=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedInPlaceCollectionTarget { get; } = new(
        id: "SMP0219",
        title: "Unsupported InPlace target",
        messageFormat: "[MapCollection] InPlace target cannot be cleared and refilled. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedConstantValue { get; } = new(
        id: "SMP0220",
        title: "Unsupported constant value",
        messageFormat: "Constant value cannot be written in the generated code. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnguardedConditionTarget { get; } = new(
        id: "SMP0221",
        title: "Condition without a property mapping",
        messageFormat: "[MapCondition] target has no property mapping for the condition to guard. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ConstructorAssignedTargetPath { get; } = new(
        id: "SMP0222",
        title: "Dotted target in a constructor-assigned member",
        messageFormat: "Dotted target goes into a member the constructor assigns from an argument. method=[{0}], target=[{1}], parameter=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DottedIgnoreTarget { get; } = new(
        id: "SMP0223",
        title: "Dotted ignore target",
        messageFormat: "[MapIgnore] target is a member of a member, which the automatic mapping never assigns on its own. method=[{0}], target=[{1}]",
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
        messageFormat: "Void mapper cannot assign init-only or constructor-only members. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnmappedRequiredProperty { get; } = new(
        id: "SMP0303",
        title: "Unmapped required member",
        messageFormat: "Required member has no mapping. method=[{0}], member=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RequiredMemberConstructorArgument { get; } = new(
        id: "SMP0304",
        title: "Required member assigned by a constructor argument",
        messageFormat: "Constructor argument assigns a required member, which the object initializer would have to set again. method=[{0}], member=[{1}], parameter=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UncreatableDestination { get; } = new(
        id: "SMP0305",
        title: "Uncreatable destination",
        messageFormat: "Return-type mapper cannot create the destination: it is abstract or an interface, or has no constructor the mapper can call. method=[{0}], type=[{1}]",
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

    // The same diagnostic for a class, a struct or a collection no conversion takes, telling the attribute that maps it
    public static DiagnosticDescriptor UnmappedCompositeConversion { get; } = new(
        id: "SMP0402",
        title: "TypeConverter fallback is not AOT-safe",
        messageFormat: "Conversion falls back to a non-AOT-safe path; map a nested member with [MapNested], a collection with [MapCollection], and other types with a converter. method=[{0}], target=[{1}]",
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

    public static DiagnosticDescriptor InvalidCultureName { get; } = new(
        id: "SMP0404",
        title: "Invalid culture name",
        messageFormat: "Culture is not a culture name. method=[{0}], culture=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
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

    public static DiagnosticDescriptor NullableValueToNonNullableTarget { get; } = new(
        id: "SMP0502",
        title: "Nullable value mapped to a target that does not take null",
        messageFormat: "A value that may be null goes to a target that does not take null, which gets null or default for it. method=[{0}], target=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnmatchedEnumMember { get; } = new(
        id: "SMP0503",
        title: "Enum member without a member of the same name in the target enum",
        messageFormat: "Members of the source enum have no member of the same name in the target enum, which gets default or null for them. method=[{0}], target=[{1}], members=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
