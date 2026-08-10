namespace Smart.Mapper.Generator.Models;

// Represents a MapNested mapping (nested object property mapped using a mapper method).
internal sealed record MapNestedModel(
    string SourceName = default!,
    string SourceType = default!,
    string TargetName = default!,
    string TargetType = default!,
    string Mapper = default!,
    int Order = default,
    int DefinitionOrder = default,
    bool MapperReturnsValue = default,
    bool IsSourceNullable = default);
