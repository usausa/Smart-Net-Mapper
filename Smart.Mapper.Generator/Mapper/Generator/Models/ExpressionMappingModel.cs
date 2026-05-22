namespace Smart.Mapper.Generator.Models;

// Represents an expression mapping configuration.
internal sealed record ExpressionMappingModel
{
    public string TargetName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
