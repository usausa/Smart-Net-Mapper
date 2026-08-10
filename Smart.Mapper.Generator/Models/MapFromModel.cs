namespace Smart.Mapper.Generator.Models;

// Represents a MapFrom mapping (target property set from source expression - method call or property path).
internal sealed record MapFromModel(
    string TargetName,
    string TargetType,
    string Member,
    string ReturnType,
    int Order,
    int DefinitionOrder,
    bool IsMethodCall,
    bool IsTargetInitOnly,
    bool IsTargetRequired);
