namespace Smart.Mapper.Generator.Models;

// Represents a MapFrom mapping (target property set from source expression - method call or property path).
internal sealed record MapFromModel
{
    public string TargetName { get; init; } = default!;
    public string TargetType { get; init; } = default!;
    public string Member { get; init; } = default!;
    public string ReturnType { get; init; } = default!;
    public int Order { get; init; }
    public int DefinitionOrder { get; init; }
    public bool IsMethodCall { get; init; }
    public bool IsTargetInitOnly { get; init; }
    public bool IsTargetRequired { get; init; }
}
