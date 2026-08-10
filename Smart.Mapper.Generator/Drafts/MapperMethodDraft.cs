namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Microsoft.CodeAnalysis;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while MapperMethodModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class MapperMethodDraft
{
    public string Namespace { get; set; } = default!;

    public string ClassName { get; set; } = default!;

    public bool IsValueType { get; set; }

    public Accessibility MethodAccessibility { get; set; }

    public string MethodName { get; set; } = default!;

    public string SourceTypeName { get; set; } = default!;

    public string SourceParameterName { get; set; } = default!;

    public string DestinationTypeName { get; set; } = default!;

    public string? DestinationParameterName { get; set; }

    public bool ReturnsDestination { get; set; }

    public bool AutoMap { get; set; } = true;

    public bool Strict { get; set; }

    public bool StrictExplicitlySet { get; set; }

    public int NameComparison { get; set; }

    public bool NameComparisonExplicitlySet { get; set; }

    public string? Culture { get; set; }

    public bool CultureExplicitlySet { get; set; }

    public string? DateTimeFormat { get; set; }

    public string? NumberFormat { get; set; }

    public bool IsSourceReadOnlyStruct { get; set; }

    public string? MapConverterTypeName { get; set; }

    public string MapConverterMethodName { get; set; } = "Convert";

    public string? CollectionConverterTypeName { get; set; }

    public EquatableArray<CustomParameterModel> CustomParameters { get; set; } = new([]);

    public EquatableArray<PropertyMappingDraft> PropertyMappings { get; set; } = new([]);

    // Snapshot of the parsed [MapProperty] mappings, taken by ValidateExplicitPropertyMappings.
    // BuildPropertyMappings rebuilds PropertyMappings from the destination members and drops anything
    // with no matching property, so constructor resolution reads the renames and their options
    // (Converter, NullValue, Culture, Order) from here instead.
    public EquatableArray<PropertyMappingDraft> ExplicitPropertyMappings { get; set; } = new([]);

    public EquatableArray<string> IgnoredProperties { get; set; } = new([]);

    public EquatableArray<PropertyConditionDraft> PropertyConditions { get; set; } = new([]);

    public EquatableArray<ConstantMappingDraft> ConstantMappings { get; set; } = new([]);

    public EquatableArray<ExpressionMappingDraft> ExpressionMappings { get; set; } = new([]);

    public EquatableArray<MapUsingDraft> MapUsingMappings { get; set; } = new([]);

    public EquatableArray<MapFromDraft> MapFromMappings { get; set; } = new([]);

    public EquatableArray<MapCollectionDraft> MapCollectionMappings { get; set; } = new([]);

    public EquatableArray<MapNestedDraft> MapNestedMappings { get; set; } = new([]);

    public string? BeforeMapMethod { get; set; }

    public bool BeforeMapAcceptsCustomParameters { get; set; }

    public string? AfterMapMethod { get; set; }

    public bool AfterMapAcceptsCustomParameters { get; set; }

    public bool UseConstructorMapping { get; set; }

    // TargetPath names the PropertyMappings entry that supplies the argument, carrying its
    // conversion metadata. BuildConstructorParameterMappings guarantees the entry exists: it either
    // flags an existing mapping or synthesizes one under the parameter's own name.
    public EquatableArray<(string ParamName, string TargetPath)> ConstructorParameters { get; set; } = new([]);

    public EquatableArray<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)> Warnings { get; set; } = new([]);

    public MapperMethodModel ToModel() => new(
        Namespace,
        ClassName,
        IsValueType,
        MethodAccessibility,
        MethodName,
        SourceTypeName,
        SourceParameterName,
        DestinationTypeName,
        DestinationParameterName,
        ReturnsDestination,
        AutoMap,
        Strict,
        StrictExplicitlySet,
        NameComparison,
        NameComparisonExplicitlySet,
        Culture,
        CultureExplicitlySet,
        DateTimeFormat,
        NumberFormat,
        IsSourceReadOnlyStruct,
        MapConverterTypeName,
        MapConverterMethodName,
        CollectionConverterTypeName,
        CustomParameters,
        new EquatableArray<PropertyMappingModel>([.. PropertyMappings.Select(static x => x.ToModel())]),
        new EquatableArray<PropertyMappingModel>([.. ExplicitPropertyMappings.Select(static x => x.ToModel())]),
        IgnoredProperties,
        new EquatableArray<PropertyConditionModel>([.. PropertyConditions.Select(static x => x.ToModel())]),
        new EquatableArray<ConstantMappingModel>([.. ConstantMappings.Select(static x => x.ToModel())]),
        new EquatableArray<ExpressionMappingModel>([.. ExpressionMappings.Select(static x => x.ToModel())]),
        new EquatableArray<MapUsingModel>([.. MapUsingMappings.Select(static x => x.ToModel())]),
        new EquatableArray<MapFromModel>([.. MapFromMappings.Select(static x => x.ToModel())]),
        new EquatableArray<MapCollectionModel>([.. MapCollectionMappings.Select(static x => x.ToModel())]),
        new EquatableArray<MapNestedModel>([.. MapNestedMappings.Select(static x => x.ToModel())]),
        BeforeMapMethod,
        BeforeMapAcceptsCustomParameters,
        AfterMapMethod,
        AfterMapAcceptsCustomParameters,
        UseConstructorMapping,
        ConstructorParameters,
        Warnings);
}
