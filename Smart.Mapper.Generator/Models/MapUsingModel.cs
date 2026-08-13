namespace Smart.Mapper.Generator.Models;

// Represents a MapUsing mapping (target property computed from source via a method in containing class).
internal sealed record MapUsingModel(
    // Target member
    string TargetName = default!,
    string TargetType = default!,
    // Method in the containing class that computes the value, and what it returns
    string Method = default!,
    string MethodReturnType = default!,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Method takes the mapper's custom parameters after the source argument
    bool AcceptsCustomParameters = default,
    // Target member traits. Decide object-initializer entry vs plain assignment
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default);
