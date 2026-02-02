namespace Smart.Mapper.Generator.Models;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

/// <summary>
/// Represents a mapper method model.
/// </summary>
internal sealed class MapperMethodModel : IEquatable<MapperMethodModel>
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the class name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the containing type is a value type.
    /// </summary>
    public bool IsValueType { get; set; }

    /// <summary>
    /// Gets or sets the method accessibility.
    /// </summary>
    public Accessibility MethodAccessibility { get; set; }

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source type name.
    /// </summary>
    public string SourceTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source parameter name.
    /// </summary>
    public string SourceParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination type name.
    /// </summary>
    public string DestinationTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination parameter name (for void methods).
    /// </summary>
    public string? DestinationParameterName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the method returns the destination.
    /// </summary>
    public bool ReturnsDestination { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically map properties with matching names.
    /// </summary>
    public bool AutoMap { get; set; } = true;

    /// <summary>
    /// Gets or sets the custom parameters (additional parameters beyond source and destination).
    /// </summary>
    public List<CustomParameterModel> CustomParameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the property mappings.
    /// </summary>
    public List<PropertyMappingModel> PropertyMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the ignored property names.
    /// </summary>
    public HashSet<string> IgnoredProperties { get; set; } = [];

    /// <summary>
    /// Gets or sets the property condition mappings (target property -> condition method name).
    /// </summary>
    public Dictionary<string, string?> PropertyConditions { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the constant mappings.
    /// </summary>
    public List<ConstantMappingModel> ConstantMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the expression mappings (dynamic values like DateTime.Now).
    /// </summary>
    public List<ExpressionMappingModel> ExpressionMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the MapUsing mappings (computed from source via a method in containing class).
    /// </summary>
    public List<MapUsingModel> MapUsingMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the MapFrom mappings (source expression - method call or property path).
    /// </summary>
    public List<MapFromModel> MapFromMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the MapCollection mappings (collection properties using a mapper method).
    /// </summary>
    public List<MapCollectionModel> MapCollectionMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the MapNested mappings (nested object properties using a mapper method).
    /// </summary>
    public List<MapNestedModel> MapNestedMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the method name to call before mapping.
    /// </summary>
    public string? BeforeMapMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether BeforeMap method accepts custom parameters.
    /// </summary>
    public bool BeforeMapAcceptsCustomParameters { get; set; }

    /// <summary>
    /// Gets or sets the method name to call after mapping.
    /// </summary>
    public string? AfterMapMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether AfterMap method accepts custom parameters.
    /// </summary>
    public bool AfterMapAcceptsCustomParameters { get; set; }

    /// <summary>
    /// Gets or sets the global condition method name for the entire mapping.
    /// </summary>
    public string? ConditionMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the global condition method accepts custom parameters.
    /// </summary>
    public bool ConditionAcceptsCustomParameters { get; set; }

    /// <summary>
    /// Gets or sets the custom type converter type name.
    /// If null, DefaultValueConverter is used.
    /// </summary>
    public string? MapConverterTypeName { get; set; }

    /// <summary>
    /// Gets or sets the method name prefix for the custom converter.
    /// Default is "Convert".
    /// </summary>
    public string MapConverterMethodName { get; set; } = "Convert";

    /// <summary>
    /// Gets or sets the custom collection converter type name.
    /// If null, DefaultCollectionConverter is used.
    /// </summary>
    public string? CollectionConverterTypeName { get; set; }

    public bool Equals(MapperMethodModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Namespace == other.Namespace &&
               ClassName == other.ClassName &&
               IsValueType == other.IsValueType &&
               MethodAccessibility == other.MethodAccessibility &&
               MethodName == other.MethodName &&
               SourceTypeName == other.SourceTypeName &&
               SourceParameterName == other.SourceParameterName &&
               DestinationTypeName == other.DestinationTypeName &&
               DestinationParameterName == other.DestinationParameterName &&
               ReturnsDestination == other.ReturnsDestination &&
               CustomParameters.SequenceEqual(other.CustomParameters) &&
               PropertyMappings.SequenceEqual(other.PropertyMappings) &&
               IgnoredProperties.SetEquals(other.IgnoredProperties) &&
               ConstantMappings.SequenceEqual(other.ConstantMappings) &&
               BeforeMapMethod == other.BeforeMapMethod &&
               BeforeMapAcceptsCustomParameters == other.BeforeMapAcceptsCustomParameters &&
               AfterMapMethod == other.AfterMapMethod &&
               AfterMapAcceptsCustomParameters == other.AfterMapAcceptsCustomParameters &&
               ConditionMethod == other.ConditionMethod &&
               ConditionAcceptsCustomParameters == other.ConditionAcceptsCustomParameters;
    }

    public override bool Equals(object? obj) => Equals(obj as MapperMethodModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (Namespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ClassName?.GetHashCode() ?? 0);
            hash = (hash * 31) + IsValueType.GetHashCode();
            hash = (hash * 31) + MethodAccessibility.GetHashCode();
            hash = (hash * 31) + (MethodName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (SourceTypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (SourceParameterName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DestinationTypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (DestinationParameterName?.GetHashCode() ?? 0);
            hash = (hash * 31) + ReturnsDestination.GetHashCode();
            hash = (hash * 31) + (BeforeMapMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + BeforeMapAcceptsCustomParameters.GetHashCode();
            hash = (hash * 31) + (AfterMapMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + AfterMapAcceptsCustomParameters.GetHashCode();
            hash = (hash * 31) + (ConditionMethod?.GetHashCode() ?? 0);
            hash = (hash * 31) + ConditionAcceptsCustomParameters.GetHashCode();
            return hash;
        }
    }
}
