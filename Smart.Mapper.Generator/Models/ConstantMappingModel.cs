namespace Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel(
    // Target member, and the intermediate members of a dotted path to it
    string TargetName = default!,
    EquatableArray<NestedPathSegment> TargetPathSegments = default,
    // Expression written into the generated code as-is, null when the constant cannot be written (SMP0215).
    // HasNullElement tells an array holding null, whose elements the target has to take
    string? Value = default,
    bool HasNullElement = default,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Target member traits. Decide object-initializer entry vs plain assignment
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default,
    // The value goes to the argument of the constructor a return mapper calls, as the parameter assigns the
    // member
    bool IsConstructorArgument = default,
    // The attribute this was declared by, as its index in MapperMethodModel.AttributeLocations, which the
    // diagnostics about it point to; -1 for one the automatic mapping made
    int AttributeIndex = -1);
