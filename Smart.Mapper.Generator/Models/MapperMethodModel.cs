namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Represents a mapper method model.
internal sealed record MapperMethodModel(
    string Namespace,
    string ClassName,
    bool IsValueType,
    Accessibility MethodAccessibility,
    string MethodName,
    string SourceTypeName,
    string SourceParameterName,
    string DestinationTypeName,
    string? DestinationParameterName,
    bool ReturnsDestination,
    bool AutoMap,
    bool Strict,
    bool StrictExplicitlySet,
    int NameComparison,
    bool NameComparisonExplicitlySet,
    string? Culture,
    bool CultureExplicitlySet,
    string? DateTimeFormat,
    string? NumberFormat,
    bool IsSourceReadOnlyStruct,
    string? MapConverterTypeName,
    string MapConverterMethodName,
    string? CollectionConverterTypeName,
    EquatableArray<CustomParameterModel> CustomParameters,
    EquatableArray<PropertyMappingModel> PropertyMappings,
    // Snapshot of the parsed [MapProperty] mappings, taken by ValidateExplicitPropertyMappings.
    // BuildPropertyMappings rebuilds PropertyMappings from the destination members and drops anything
    // with no matching property, so constructor resolution reads the renames and their options
    // (Converter, NullValue, Culture, Order) from here instead.
    EquatableArray<PropertyMappingModel> ExplicitPropertyMappings,
    EquatableArray<string> IgnoredProperties,
    EquatableArray<PropertyConditionModel> PropertyConditions,
    EquatableArray<ConstantMappingModel> ConstantMappings,
    EquatableArray<ExpressionMappingModel> ExpressionMappings,
    EquatableArray<MapUsingModel> MapUsingMappings,
    EquatableArray<MapFromModel> MapFromMappings,
    EquatableArray<MapCollectionModel> MapCollectionMappings,
    EquatableArray<MapNestedModel> MapNestedMappings,
    string? BeforeMapMethod,
    bool BeforeMapAcceptsCustomParameters,
    string? AfterMapMethod,
    bool AfterMapAcceptsCustomParameters,
    bool UseConstructorMapping,
    // TargetPath names the PropertyMappings entry that supplies the argument, carrying its
    // conversion metadata. BuildConstructorParameterMappings guarantees the entry exists: it either
    // flags an existing mapping or synthesizes one under the parameter's own name.
    EquatableArray<(string ParamName, string TargetPath)> ConstructorParameters,
    EquatableArray<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)> Warnings);
