namespace Smart.Mapper.Generator.Models;

// Represents a MapFrom mapping (target property set from source expression - method call or property path).
internal sealed record MapFromModel
{
    public string TargetName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string Member { get; set; } = string.Empty;
    public bool IsMethodCall { get; set; }
    public string ReturnType { get; set; } = string.Empty;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
