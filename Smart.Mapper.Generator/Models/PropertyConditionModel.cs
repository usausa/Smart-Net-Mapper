namespace Smart.Mapper.Generator.Models;

// Represents a condition mapping for a target property.
internal sealed record PropertyConditionModel
{
    public string TargetName { get; set; } = default!;
    public string? ConditionMethod { get; set; }
}
