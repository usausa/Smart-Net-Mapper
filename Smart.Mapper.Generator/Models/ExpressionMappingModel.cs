namespace Smart.Mapper.Generator.Models;

// Represents an expression mapping configuration.
internal sealed record ExpressionMappingModel
{
    public string TargetName { get; init; } = default!;
    public string Expression { get; init; } = default!;
    public int Order { get; init; }
    public int DefinitionOrder { get; init; }
    public bool IsTargetInitOnly { get; init; }
    public bool IsTargetRequired { get; init; }
}
