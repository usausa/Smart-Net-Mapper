namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Represents a MapUsing mapping (target property computed from source via a method in containing class).
internal sealed record MapUsingModel(
    // Target member, and the intermediate members of a dotted path to it
    string TargetName = default!,
    EquatableArray<NestedPathSegment> TargetPathSegments = default,
    // Method in the containing class that computes the value
    string Method = default!,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // The custom parameters of the mapper the method takes after the source, as their indexes, in the order of its
    // parameters
    EquatableArray<int> CustomArguments = default,
    // RefKinds of the method's parameters, which decide how each argument is passed
    EquatableArray<RefKind> ParameterRefKinds = default,
    // The method returns a nullable reference into a target not annotated as nullable, so its result is taken with !
    bool ForgivesNull = default,
    // Target member traits. Decide object-initializer entry vs plain assignment
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default,
    // The value goes to the argument of the constructor a return mapper calls, as the parameter assigns the
    // member
    bool IsConstructorArgument = default,
    // The attribute this was declared by, as its index in MapperMethodModel.AttributeLocations, which the
    // diagnostics about it point to; -1 for one the automatic mapping made
    int AttributeIndex = -1);
