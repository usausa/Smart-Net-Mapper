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
    string DestinationTypeName = default!,
    string? DestinationParameterName = default,
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
    bool IsSourceReadOnlyStruct = default,
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
    string? AfterMapMethod = default,
    bool AfterMapAcceptsCustomParameters = default,
    bool UseConstructorMapping = default,
    // TargetPath names the PropertyMappings entry that supplies the argument, carrying its
    // conversion metadata. BuildConstructorParameterMappings guarantees the entry exists: it either
    // flags an existing mapping or synthesizes one under the parameter's own name.
    EquatableArray<(string ParamName, string TargetPath)> ConstructorParameters = default,
    EquatableArray<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)> Warnings = default);
