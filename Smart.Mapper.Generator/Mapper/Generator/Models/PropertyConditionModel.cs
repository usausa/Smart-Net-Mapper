namespace Smart.Mapper.Generator.Models;

/// <summary>
/// Represents a condition mapping for a target property.
/// </summary>
internal sealed record PropertyConditionModel
{
    public string TargetName { get; set; } = string.Empty;
    public string? ConditionMethod { get; set; }
}
