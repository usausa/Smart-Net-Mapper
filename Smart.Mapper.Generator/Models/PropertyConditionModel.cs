namespace Smart.Mapper.Generator.Models;

// Represents a condition mapping for a target property.
internal sealed record PropertyConditionModel(
    string TargetName = default!,
    string? ConditionMethod = default,
    // The attribute this was declared by, as its index in MapperMethodModel.AttributeLocations, which the
    // diagnostics about it point to; -1 for one the automatic mapping made
    int AttributeIndex = -1);
