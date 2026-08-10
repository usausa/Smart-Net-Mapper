namespace Smart.Mapper.Generator.Models;

// Represents a MapFrom mapping (target property set from source expression - method call or property path).
internal sealed record MapFromModel(
    string TargetName = default!,
    string TargetType = default!,
    string Member = default!,
    string ReturnType = default!,
    int Order = default,
    int DefinitionOrder = default,
    bool IsMethodCall = default,
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default);
