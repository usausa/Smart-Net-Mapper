namespace Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Represents an expression mapping configuration.
internal sealed record ExpressionMappingModel(
    // Target member, and the intermediate members of a dotted path to it
    string TargetName = default!,
    EquatableArray<NestedPathSegment> TargetPathSegments = default,
    // Return type of the local function that computes the value: the target member's type with its
    // nullable annotations. Null while the target is unresolved
    string? TargetType = default,
    // The target was declared with nullable annotations disabled, so its type is emitted the same way
    bool IsTargetTypeOblivious = default,
    // Expression text written into the generated code as-is
    string Expression = default!,
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
