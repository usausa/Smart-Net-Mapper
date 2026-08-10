namespace Smart.Mapper.Generator.Models;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel(
    string TargetName,
    string TargetType,
    string? Value,
    int Order,
    int DefinitionOrder,
    bool IsTargetInitOnly,
    bool IsTargetRequired);
