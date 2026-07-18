namespace Smart.Mapper.Generator.Models;

// Represents a MapNested mapping (nested object property mapped using a mapper method).
internal sealed record MapNestedModel
{
    public string SourceName { get; set; } = default!;
    public string SourceType { get; set; } = default!;
    public string TargetName { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string Mapper { get; set; } = default!;
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
    public bool MapperReturnsValue { get; set; }
    public bool IsSourceNullable { get; set; }
}
