namespace Smart.Mapper.Generator.Models;

// Represents a MapUsing mapping (target property computed from source via a method in containing class).
internal sealed record MapUsingModel
{
    public string TargetName { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string Method { get; set; } = default!;
    public string MethodReturnType { get; set; } = default!;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
    public bool AcceptsCustomParameters { get; set; }
    public bool IsTargetInitOnly { get; set; }
    public bool IsTargetRequired { get; set; }
}
