namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Represents a mapper method model.
internal sealed record MapperMethodModel(
    string Namespace = default!,
    string ClassName = default!,
    bool IsValueType = default,
    Accessibility MethodAccessibility = default,
    string MethodName = default!,
    string SourceTypeName = default!,
    string SourceParameterName = default!,
    // Modifiers the defining declaration puts on the parameter besides this (in, ref readonly, ref,
    // scoped, params), which the implementation repeats, and its RefKind, which decides how the
    // parameter is passed on to the local function of a [MapExpression].
    string SourceParameterModifiers = "",
    RefKind SourceRefKind = default,
    // The source type as declared, nullable annotations included, which the implementation repeats
    // (CS8611 otherwise). SourceTypeName leaves them out and is what types are compared by.
    string SourceDeclaredTypeName = default!,
    // Declared as a nullable reference type. The generated code reads the source, so when it is null
    // nothing is mapped; past that check the local functions of [MapExpression] take it as
    // SourceNonNullableTypeName.
    bool IsSourceParameterNullable = default,
    string SourceNonNullableTypeName = default!,
    string DestinationTypeName = default!,
    string? DestinationParameterName = default,
    // The same for the destination parameter of a void mapper.
    string DestinationParameterModifiers = "",
    RefKind DestinationRefKind = default,
    // The return type or the destination parameter type as declared. DestinationTypeName leaves the
    // annotations out, as the instance is created under it.
    string DestinationDeclaredTypeName = default!,
    // The same null check for the destination parameter of a void mapper, which the generated code writes.
    bool IsDestinationParameterNullable = default,
    string DestinationNonNullableTypeName = default!,
    // What a return-type mapper returns for a null source: default, or default! for a reference type
    // that is not nullable, so that the generated code does not warn.
    string DefaultReturnValue = "default",
    bool ReturnsDestination = default,
    bool AutoMap = true,
    bool Strict = default,
    bool StrictExplicitlySet = default,
    int NameComparison = default,
    bool NameComparisonExplicitlySet = default,
    string? Culture = default,
    bool CultureExplicitlySet = default,
    string? DateTimeFormat = default,
    string? NumberFormat = default,
    // The defining declaration has the this modifier on the source parameter. The implementing
    // declaration must repeat it (CS0755), so the emitter carries it over.
    bool IsExtensionMethod = default,
    string? MapConverterTypeName = default,
    string MapConverterMethodName = "Convert",
    string? CollectionConverterTypeName = default,
    EquatableArray<CustomParameterModel> CustomParameters = default,
    EquatableArray<PropertyMappingModel> PropertyMappings = default,
    // Snapshot of the parsed [MapProperty] mappings, taken by ValidateExplicitPropertyMappings.
    // BuildPropertyMappings rebuilds PropertyMappings from the destination members and drops anything
    // with no matching property, so constructor resolution reads the renames and their options
    // (Converter, NullValue, Culture, Order) from here instead.
    EquatableArray<PropertyMappingModel> ExplicitPropertyMappings = default,
    EquatableArray<string> IgnoredProperties = default,
    EquatableArray<PropertyConditionModel> PropertyConditions = default,
    EquatableArray<ConstantMappingModel> ConstantMappings = default,
    EquatableArray<ExpressionMappingModel> ExpressionMappings = default,
    EquatableArray<MapUsingModel> MapUsingMappings = default,
    EquatableArray<MapFromModel> MapFromMappings = default,
    EquatableArray<MapCollectionModel> MapCollectionMappings = default,
    EquatableArray<MapNestedModel> MapNestedMappings = default,
    string? BeforeMapMethod = default,
    bool BeforeMapAcceptsCustomParameters = default,
    // RefKinds of the matched callback's parameters, which decide how each argument is passed.
    EquatableArray<RefKind> BeforeMapParameterRefKinds = default,
    string? AfterMapMethod = default,
    bool AfterMapAcceptsCustomParameters = default,
    EquatableArray<RefKind> AfterMapParameterRefKinds = default,
    bool UseConstructorMapping = default,
    // TargetPath names the PropertyMappings entry that supplies the argument, carrying its
    // conversion metadata. BuildConstructorParameterMappings guarantees the entry exists: it either
    // flags an existing mapping or synthesizes one under the parameter's own name.
    EquatableArray<(string ParamName, string TargetPath)> ConstructorParameters = default,
    EquatableArray<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)> Warnings = default);
