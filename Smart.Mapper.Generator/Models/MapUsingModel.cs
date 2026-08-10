namespace Smart.Mapper.Generator.Models;

// Represents a MapUsing mapping (target property computed from source via a method in containing class).
internal sealed record MapUsingModel
{
    public string TargetName { get; init; } = default!;
    public string TargetType { get; init; } = default!;
    public string Method { get; init; } = default!;
    public string MethodReturnType { get; init; } = default!;
    public int Order { get; init; }
    public int DefinitionOrder { get; init; }
    public bool AcceptsCustomParameters { get; init; }
    public bool IsTargetInitOnly { get; init; }
    public bool IsTargetRequired { get; init; }
}
