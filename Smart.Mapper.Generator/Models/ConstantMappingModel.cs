namespace Smart.Mapper.Generator.Models;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel
{
    public string TargetName { get; init; } = default!;
    public string TargetType { get; init; } = default!;
    public string? Value { get; init; }
    public int Order { get; init; }
    public int DefinitionOrder { get; init; }
    public bool IsTargetInitOnly { get; init; }
    public bool IsTargetRequired { get; init; }
}
