namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Represents a mapper method model.
internal sealed record MapperMethodModel
{
    public string Namespace { get; init; } = default!;
    public string ClassName { get; init; } = default!;
    public bool IsValueType { get; init; }
    public Accessibility MethodAccessibility { get; init; }
    public string MethodName { get; init; } = default!;
    public string SourceTypeName { get; init; } = default!;
    public string SourceParameterName { get; init; } = default!;
    public string DestinationTypeName { get; init; } = default!;
    public string? DestinationParameterName { get; init; }
    public bool ReturnsDestination { get; init; }
    public bool AutoMap { get; init; } = true;
    public bool Strict { get; init; }
    public bool StrictExplicitlySet { get; init; }
    public int NameComparison { get; init; }
    public bool NameComparisonExplicitlySet { get; init; }
    public string? Culture { get; init; }
    public bool CultureExplicitlySet { get; init; }
    public string? DateTimeFormat { get; init; }
    public string? NumberFormat { get; init; }
    public bool IsSourceReadOnlyStruct { get; init; }
    public string? MapConverterTypeName { get; init; }
    public string MapConverterMethodName { get; init; } = "Convert";
    public string? CollectionConverterTypeName { get; init; }
    public EquatableArray<CustomParameterModel> CustomParameters { get; init; } = new([]);
    public EquatableArray<PropertyMappingModel> PropertyMappings { get; init; } = new([]);

    // Snapshot of the parsed [MapProperty] mappings, taken by ValidateExplicitPropertyMappings.
    // BuildPropertyMappings rebuilds PropertyMappings from the destination members and drops anything
    // with no matching property, so constructor resolution reads the renames and their options
    // (Converter, NullValue, Culture, Order) from here instead.
    public EquatableArray<PropertyMappingModel> ExplicitPropertyMappings { get; init; } = new([]);

    public EquatableArray<string> IgnoredProperties { get; init; } = new([]);
    public EquatableArray<PropertyConditionModel> PropertyConditions { get; init; } = new([]);
    public EquatableArray<ConstantMappingModel> ConstantMappings { get; init; } = new([]);
    public EquatableArray<ExpressionMappingModel> ExpressionMappings { get; init; } = new([]);
    public EquatableArray<MapUsingModel> MapUsingMappings { get; init; } = new([]);
    public EquatableArray<MapFromModel> MapFromMappings { get; init; } = new([]);
    public EquatableArray<MapCollectionModel> MapCollectionMappings { get; init; } = new([]);
    public EquatableArray<MapNestedModel> MapNestedMappings { get; init; } = new([]);
    public string? BeforeMapMethod { get; init; }
    public bool BeforeMapAcceptsCustomParameters { get; init; }
    public string? AfterMapMethod { get; init; }
    public bool AfterMapAcceptsCustomParameters { get; init; }
    public bool UseConstructorMapping { get; init; }
    // TargetPath names the PropertyMappings entry that supplies the argument, carrying its
    // conversion metadata. BuildConstructorParameterMappings guarantees the entry exists: it either
    // flags an existing mapping or synthesizes one under the parameter's own name.
    public EquatableArray<(string ParamName, string TargetPath)> ConstructorParameters { get; init; } = new([]);
    public EquatableArray<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)> Warnings { get; init; } = new([]);
}
