namespace Smart.Mapper.Generator.Models;

// Represents a MapNested mapping (nested object property mapped using a mapper method).
internal sealed record MapNestedModel
{
    public string SourceName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string Mapper { get; set; } = string.Empty;
    public bool MapperReturnsValue { get; set; }
    public bool IsSourceNullable { get; set; }
    public int Order { get; set; }
    public int DefinitionOrder { get; set; }
}
