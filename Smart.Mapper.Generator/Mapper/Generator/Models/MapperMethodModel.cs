namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

using System.Collections.Immutable;

/// <summary>
/// Represents a mapper method model.
/// </summary>
internal sealed record MapperMethodModel
{
    public string Namespace { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public bool IsValueType { get; set; }
    public Accessibility MethodAccessibility { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string SourceTypeName { get; set; } = string.Empty;
    public string SourceParameterName { get; set; } = string.Empty;
    public string DestinationTypeName { get; set; } = string.Empty;
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
    public EquatableArray<PropertyMappingModel> PropertyMappings { get; set; } = new([]);
    public EquatableArray<string> IgnoredProperties { get; set; } = new([]);
    public EquatableArray<PropertyConditionModel> PropertyConditions { get; set; } = new([]);
    public EquatableArray<ConstantMappingModel> ConstantMappings { get; set; } = new([]);
    public EquatableArray<ExpressionMappingModel> ExpressionMappings { get; set; } = new([]);
    public EquatableArray<MapUsingModel> MapUsingMappings { get; set; } = new([]);
    public EquatableArray<MapFromModel> MapFromMappings { get; set; } = new([]);
    public EquatableArray<MapCollectionModel> MapCollectionMappings { get; set; } = new([]);
    public EquatableArray<MapNestedModel> MapNestedMappings { get; set; } = new([]);
    public string? BeforeMapMethod { get; set; }
    public bool BeforeMapAcceptsCustomParameters { get; set; }
    public string? AfterMapMethod { get; set; }
    public bool AfterMapAcceptsCustomParameters { get; set; }
    public bool UseConstructorMapping { get; set; }
    public EquatableArray<(string ParamName, string SourceExpression)> ConstructorParameters { get; set; } = new([]);
    public EquatableArray<(DiagnosticDescriptor Descriptor, string Arg)> Warnings { get; set; } = new([]);
}
