namespace Smart.Mapper.Generator.Models;

/// <summary>
/// Represents an expression mapping configuration.
/// </summary>
internal sealed record ExpressionMappingModel
{
    public string TargetName { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
