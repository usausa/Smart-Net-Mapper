namespace Smart.Mapper.Generator.Models;

// Represents a MapUsing mapping (target property computed from source via a method in containing class).
internal sealed record MapUsingModel
{
    public string TargetName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string MethodReturnType { get; set; } = string.Empty;
    public bool AcceptsCustomParameters { get; set; }
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
