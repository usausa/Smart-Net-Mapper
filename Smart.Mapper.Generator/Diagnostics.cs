namespace Smart.Mapper.Generator;

using Microsoft.CodeAnalysis;

// Core Mapper generator diagnostics. The IDs are in a band for each phase of the pipeline (MapperModelBuilder), each
// numbered in the order the pipeline makes its checks, the warnings of a band after its errors:
//   SMP00xx  method definition   (the declaration: partial, ref return, parameter count, reserved names, modifiers, nullable struct source, collection mapped as a whole, CultureInfo parameters)
//   SMP01xx  mapping attributes  (duplicate targets, the targets and sources of the attributes, the methods they name: hidden by a parameter, instance ones of a static mapper, callbacks, converters, conditions)
//   SMP02xx  member mapping      (MapUsing / MapFrom / MapCollection / MapNested / MapConstant)
//   SMP03xx  construction        (constructor-assigned members, init-only members, the constructor and its parameters, required members)
//   SMP04xx  conversion / AOT    (culture names, TypeConverter fallback, reflection usage, the culture of [Mapper] a CultureInfo parameter takes over)
//   SMP05xx  strict mode         (advisory warnings: unmapped properties, nullable values, unmatched enum members)
internal static class Diagnostics
{
    // An error leaves the mapper to an implementation throwing (MapperSourceBuilder), so it cannot be suppressed, which
    // would build the throwing one into the program; the warnings can. NotConfigurable keeps #pragma, NoWarn and the
    // severity settings from it, and Compiler [SuppressMessage], which does not apply to the diagnostics of the compiler.
    private static readonly string[] ErrorTags = [WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.Compiler];

    // ==================================================================
    // SMP00xx — method definition
    // ==================================================================

    public static DiagnosticDescriptor InvalidMethodDefinition { get; } = new(
        id: "SMP0001",
        title: "Invalid mapper method definition",
        messageFormat: "[Mapper] method must be partial, in types that are all partial and not file-local. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor RefReturnMapper { get; } = new(
        id: "SMP0002",
        title: "Mapper returning by reference",
        messageFormat: "[Mapper] method cannot return by reference, it returns the destination it creates by value. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidMethodParameter { get; } = new(
        id: "SMP0003",
        title: "Invalid mapper method parameters",
        messageFormat: "[Mapper] method parameter count is invalid. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor ReservedParameterName { get; } = new(
        id: "SMP0004",
        title: "Reserved parameter name",
        messageFormat: "[Mapper] parameter names starting with __ are reserved for the generated code, rename the parameter. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnsupportedParameterModifier { get; } = new(
        id: "SMP0005",
        title: "Unsupported parameter modifier",
        messageFormat: "[Mapper] parameter modifier is not supported, the generated code reads every parameter and assigns the destination members, which a struct destination takes by ref. method=[{0}], parameter=[{1}], modifier=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor NullableValueTypeSource { get; } = new(
        id: "SMP0006",
        title: "Nullable value type source",
        messageFormat: "[Mapper] source parameter cannot be a nullable value type, which has none of the members of the struct it holds. method=[{0}], parameter=[{1}], type=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor CollectionMapper { get; } = new(
        id: "SMP0007",
        title: "Collection mapped as a whole",
        messageFormat: "[Mapper] cannot map a collection, an array or a tuple as a whole, map the elements with a mapper of the element type (source.Select(ToDto).ToList()) or use [MapCollection] on a type holding the collection. method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor AmbiguousCultureParameter { get; } = new(
        id: "SMP0008",
        title: "Ambiguous CultureInfo parameter",
        messageFormat: "[Mapper] takes several CultureInfo parameters, of which the one named {1} gives the culture of the conversions, and none is named so. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // ==================================================================
    // SMP01xx — mapping attributes (targets, sources and the methods the attributes name)
    // ==================================================================

    public static DiagnosticDescriptor DuplicateTargetMapping { get; } = new(
        id: "SMP0101",
        title: "Duplicate target mapping",
        messageFormat: "Multiple attributes map or ignore the same target, or map the target and a member of it. method=[{0}], target=[{1}], attributes=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnresolvedMapPropertyTargetProperty { get; } = new(
        id: "SMP0102",
        title: "Unassignable mapping target",
        messageFormat: "Mapping target is not found or cannot be assigned. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // The same diagnostic for a dotted target going through a nullable struct, which the message tells
    public static DiagnosticDescriptor NullableStructTargetPath { get; } = new(
        id: "SMP0102",
        title: "Unassignable mapping target",
        messageFormat: "Mapping target goes through a nullable struct, which a dotted target cannot write into, map the struct as a whole. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor DottedIgnoreTarget { get; } = new(
        id: "SMP0103",
        title: "Dotted ignore target",
        messageFormat: "[MapIgnore] target is a member of a member, which the automatic mapping never assigns on its own. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // The generated code calls the method by its name, which the parameter would hide
    public static DiagnosticDescriptor ParameterHidesMethod { get; } = new(
        id: "SMP0104",
        title: "Parameter hides the method",
        messageFormat: "A parameter of the mapper has the name of the method, which the generated code calls by it, rename the parameter or the method. method=[{0}], callee=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InstanceMethodOfStaticMapper { get; } = new(
        id: "SMP0105",
        title: "Instance method named by a static mapper",
        messageFormat: "A static mapper cannot call an instance method, make the mapper an instance method or the method static. method=[{0}], callee=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidBeforeMapSignature { get; } = new(
        id: "SMP0106",
        title: "Invalid BeforeMap method signature",
        messageFormat: "[BeforeMap] signature does not match. method=[{0}], callback=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidAfterMapSignature { get; } = new(
        id: "SMP0107",
        title: "Invalid AfterMap method signature",
        messageFormat: "[AfterMap] signature does not match. method=[{0}], callback=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnresolvedMapPropertySourceProperty { get; } = new(
        id: "SMP0108",
        title: "Unresolved MapProperty source",
        messageFormat: "[MapProperty] source is not found. method=[{0}], target=[{1}], source=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnguardedConditionTarget { get; } = new(
        id: "SMP0109",
        title: "Condition without a property mapping",
        messageFormat: "[MapCondition] target has no property mapping for the condition to guard. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidConverterSignature { get; } = new(
        id: "SMP0110",
        title: "Invalid converter method signature",
        messageFormat: "Converter signature does not match. method=[{0}], converter=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidConverterReturnType { get; } = new(
        id: "SMP0111",
        title: "Converter return type mismatch",
        messageFormat: "Converter return type does not match. method=[{0}], converter=[{1}], expected=[{2}], actual=[{3}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidPropertyConditionSignature { get; } = new(
        id: "SMP0112",
        title: "Invalid property condition signature",
        messageFormat: "Condition signature does not match. method=[{0}], condition=[{1}], target=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // ==================================================================
    // SMP02xx — member mapping (MapUsing / MapFrom / MapCollection / MapNested / MapConstant)
    // ==================================================================

    public static DiagnosticDescriptor InvalidMapUsingSignature { get; } = new(
        id: "SMP0201",
        title: "Invalid MapUsing method signature",
        messageFormat: "[MapUsing] signature does not match. method=[{0}], using=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor MapUsingReturnTypeMismatch { get; } = new(
        id: "SMP0202",
        title: "MapUsing return type mismatch",
        messageFormat: "[MapUsing] return type does not match. method=[{0}], using=[{1}], expected=[{2}], actual=[{3}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnresolvedMapFromTargetProperty { get; } = new(
        id: "SMP0203",
        title: "Unresolved MapFrom target property",
        messageFormat: "[MapFrom] target property is not found. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidMapFromMember { get; } = new(
        id: "SMP0204",
        title: "Invalid MapFrom member",
        messageFormat: "[MapFrom] member is not supported. method=[{0}], member=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor MapFromReturnTypeMismatch { get; } = new(
        id: "SMP0205",
        title: "MapFrom member type mismatch",
        messageFormat: "[MapFrom] member type does not match. method=[{0}], member=[{1}], expected=[{2}], actual=[{3}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnresolvedMapCollectionSourceProperty { get; } = new(
        id: "SMP0206",
        title: "Unresolved MapCollection source",
        messageFormat: "[MapCollection]/[MapNested] source property is not found, the source is a property of the source type and cannot be a dotted path. method=[{0}], source=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnresolvedMapCollectionTargetProperty { get; } = new(
        id: "SMP0207",
        title: "Unresolved MapCollection target",
        messageFormat: "[MapCollection]/[MapNested] target property is not found. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnsupportedInPlaceCollectionTarget { get; } = new(
        id: "SMP0208",
        title: "Unsupported InPlace target",
        messageFormat: "[MapCollection] InPlace target cannot be cleared and refilled. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnsupportedInitOnlyCollectionTarget { get; } = new(
        id: "SMP0209",
        title: "Unassignable target",
        messageFormat: "[MapCollection]/[MapNested] target has no setter or init accessor the mapper can call, or is init-only in a void mapper. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor MapCollectionSourceNotCollection { get; } = new(
        id: "SMP0210",
        title: "Source property is not a collection",
        messageFormat: "[MapCollection] source is not a collection. method=[{0}], source=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor MapCollectionTargetNotCollection { get; } = new(
        id: "SMP0211",
        title: "Target property is not a collection",
        messageFormat: "[MapCollection] target is not a collection. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnsupportedCollectionTarget { get; } = new(
        id: "SMP0212",
        title: "Unsupported collection target",
        messageFormat: "[MapCollection] target cannot take the collection the generated code creates for it. method=[{0}], target=[{1}], collection=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidMapCollectionMapperMethod { get; } = new(
        id: "SMP0213",
        title: "Invalid MapCollection mapper method",
        messageFormat: "[MapCollection] element mapper method does not match. method=[{0}], mapper=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // The same diagnostic for an attribute without Mapper, which the message tells
    public static DiagnosticDescriptor MapCollectionMapperNotSpecified { get; } = new(
        id: "SMP0213",
        title: "Invalid MapCollection mapper method",
        messageFormat: "[MapCollection] Mapper is not specified, the elements are mapped with an element mapper. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InvalidMapNestedMapperMethod { get; } = new(
        id: "SMP0214",
        title: "Invalid MapNested mapper method",
        messageFormat: "[MapNested] mapper method does not match. method=[{0}], mapper=[{1}], target=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // The same diagnostic for an attribute without Mapper, which the message tells
    public static DiagnosticDescriptor MapNestedMapperNotSpecified { get; } = new(
        id: "SMP0214",
        title: "Invalid MapNested mapper method",
        messageFormat: "[MapNested] Mapper is not specified, the member is mapped with a mapper. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnsupportedConstantValue { get; } = new(
        id: "SMP0215",
        title: "Unsupported constant value",
        messageFormat: "Constant value cannot be written in the generated code. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnassignableConstantValue { get; } = new(
        id: "SMP0216",
        title: "Unassignable constant value",
        messageFormat: "Constant value cannot be assigned to the target. method=[{0}], target=[{1}], value=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // ==================================================================
    // SMP03xx — construction (constructor-assigned members, init-only members, the constructor and its parameters, required members)
    // ==================================================================

    public static DiagnosticDescriptor ConstructorAssignedTargetPath { get; } = new(
        id: "SMP0301",
        title: "Dotted target in a constructor-assigned member",
        messageFormat: "Dotted target goes into a member the constructor assigns from an argument. method=[{0}], target=[{1}], parameter=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor InitOnlyDestinationRequiresReturnMapper { get; } = new(
        id: "SMP0302",
        title: "Return-type mapper is required",
        messageFormat: "Void mapper cannot assign init-only or constructor-only members. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UncreatableDestination { get; } = new(
        id: "SMP0303",
        title: "Uncreatable destination",
        messageFormat: "Return-type mapper cannot create the destination: it is abstract or an interface, or has no constructor the mapper can call. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor IgnoredConstructorParameter { get; } = new(
        id: "SMP0304",
        title: "Ignored member required by construction",
        messageFormat: "[MapIgnore] member has to be assigned when the destination is constructed. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnresolvedConstructorParameter { get; } = new(
        id: "SMP0305",
        title: "Unresolved constructor parameter",
        messageFormat: "Constructor parameter has no source. method=[{0}], parameter=[{1}], type=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnsupportedConstructorAssignedOption { get; } = new(
        id: "SMP0306",
        title: "Unsupported constructor-assigned option",
        messageFormat: "[MapCondition] / NullBehavior.Skip requires a property assignment, not a constructor argument or an object initializer entry. method=[{0}], target=[{1}], option=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor RequiredMemberConstructorArgument { get; } = new(
        id: "SMP0307",
        title: "Required member assigned by a constructor argument",
        messageFormat: "Constructor argument assigns a required member, which the object initializer would have to set again. method=[{0}], member=[{1}], parameter=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor UnmappedRequiredProperty { get; } = new(
        id: "SMP0308",
        title: "Unmapped required member",
        messageFormat: "Required member has no mapping. method=[{0}], member=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // ==================================================================
    // SMP04xx — conversion / AOT (culture names, TypeConverter fallback, reflection usage, the culture of [Mapper])
    // ==================================================================

    public static DiagnosticDescriptor InvalidCultureName { get; } = new(
        id: "SMP0401",
        title: "Invalid culture name",
        messageFormat: "Culture is not a culture name. method=[{0}], culture=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // The same diagnostic for the profile of the assembly, reported once for all the mappers
    public static DiagnosticDescriptor InvalidAssemblyCultureName { get; } = new(
        id: "SMP0401",
        title: "Invalid culture name",
        messageFormat: "Culture of the assembly profile is not a culture name. culture=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor TypeConverterFallbackNotAllowed { get; } = new(
        id: "SMP0402",
        title: "TypeConverter fallback is not AOT-safe",
        messageFormat: "Conversion falls back to a non-AOT-safe path. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    // The same diagnostic for a class, a struct or a collection no conversion takes, telling the attribute that maps it
    public static DiagnosticDescriptor UnmappedCompositeConversion { get; } = new(
        id: "SMP0402",
        title: "TypeConverter fallback is not AOT-safe",
        messageFormat: "Conversion falls back to a non-AOT-safe path; map a nested member with [MapNested], a collection with [MapCollection], and other types with a converter. method=[{0}], target=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: ErrorTags);

    public static DiagnosticDescriptor MapExpressionReflectionNotAllowed { get; } = new(
        id: "SMP0403",
        title: "MapExpression uses reflection",
        messageFormat: "[MapExpression] may use reflection. method=[{0}], target=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CultureOverriddenByParameter { get; } = new(
        id: "SMP0404",
        title: "Culture of [Mapper] is not used",
        messageFormat: "Culture of [Mapper] is not used, the CultureInfo parameter gives the culture of the conversions. method=[{0}], culture=[{1}], parameter=[{2}]",
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
