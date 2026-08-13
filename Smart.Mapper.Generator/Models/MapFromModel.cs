namespace Smart.Mapper.Generator.Models;

// Represents a MapFrom mapping (target property set from source expression - method call or property path).
internal sealed record MapFromModel(
    // Target member
    string TargetName = default!,
    string TargetType = default!,
    // Source. A method name or a property path on the source object, and what it yields
    string Member = default!,
    string ReturnType = default!,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Member is a method call rather than a property path
    bool IsMethodCall = default,
    // Target member traits. Decide object-initializer entry vs plain assignment
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default);
