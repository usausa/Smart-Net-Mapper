namespace Smart.Mapper.Generator.Models;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel
{
    public string TargetName { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string? Value { get; set; }
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
    public bool IsTargetInitOnly { get; set; }
    public bool IsTargetRequired { get; set; }
}
