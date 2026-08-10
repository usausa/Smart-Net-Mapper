namespace Smart.Mapper.Generator.Models;

// Represents a MapNested mapping (nested object property mapped using a mapper method).
internal sealed record MapNestedModel
{
    public string SourceName { get; init; } = default!;
    public string SourceType { get; init; } = default!;
    public string TargetName { get; init; } = default!;
    public string TargetType { get; init; } = default!;
    public string Mapper { get; init; } = default!;
    public int Order { get; init; }
    public int DefinitionOrder { get; init; }
    public bool MapperReturnsValue { get; init; }
    public bool IsSourceNullable { get; init; }
}
