namespace Smart.Mapper.Generator.Models;

// Represents a MapFrom mapping (target property set from source expression - method call or property path).
internal sealed record MapFromModel
{
    public string TargetName { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string Member { get; set; } = default!;
    public bool IsMethodCall { get; set; }
    public string ReturnType { get; set; } = default!;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
