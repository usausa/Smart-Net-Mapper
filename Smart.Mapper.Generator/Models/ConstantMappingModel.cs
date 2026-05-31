namespace Smart.Mapper.Generator.Models;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel
{
    public string TargetName { get; set; } = default!;
    public string? Value { get; set; }
    public string TargetType { get; set; } = default!;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
