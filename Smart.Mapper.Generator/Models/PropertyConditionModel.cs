namespace Smart.Mapper.Generator.Models;

// Represents a condition mapping for a target property.
internal sealed record PropertyConditionModel(
    string TargetName,
    string? ConditionMethod);
