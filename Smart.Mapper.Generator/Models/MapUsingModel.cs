namespace Smart.Mapper.Generator.Models;

// Represents a MapUsing mapping (target property computed from source via a method in containing class).
internal sealed record MapUsingModel(
    string TargetName = default!,
    string TargetType = default!,
    string Method = default!,
    string MethodReturnType = default!,
    int Order = default,
    int DefinitionOrder = default,
    bool AcceptsCustomParameters = default,
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default);
