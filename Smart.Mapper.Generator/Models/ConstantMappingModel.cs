namespace Smart.Mapper.Generator.Models;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel(
    string TargetName = default!,
    string TargetType = default!,
    string? Value = default,
    int Order = default,
    int DefinitionOrder = default,
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default);
