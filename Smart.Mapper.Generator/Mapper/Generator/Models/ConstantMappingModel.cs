namespace Smart.Mapper.Generator.Models;

// Represents a constant value mapping configuration.
internal sealed record ConstantMappingModel
{
    public string TargetName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
