namespace Smart.Mapper.Generator.Models;

// Represents a MapUsing mapping (target property computed from source via a method in containing class).
internal sealed record MapUsingModel(
    string TargetName,
    string TargetType,
    string Method,
    string MethodReturnType,
    int Order,
    int DefinitionOrder,
    bool AcceptsCustomParameters,
    bool IsTargetInitOnly,
    bool IsTargetRequired);
