namespace Smart.Mapper.Generator.Models;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel(
    // Target member
    string TargetName = default!,
    string TargetType = default!,
    // Literal written into the generated code as-is
    string? Value = default,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Target member traits. Decide object-initializer entry vs plain assignment
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default);
