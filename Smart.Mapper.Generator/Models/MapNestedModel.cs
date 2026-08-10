namespace Smart.Mapper.Generator.Models;

// Represents a MapNested mapping (nested object property mapped using a mapper method).
internal sealed record MapNestedModel(
    string SourceName,
    string SourceType,
    string TargetName,
    string TargetType,
    string Mapper,
    int Order,
    int DefinitionOrder,
    bool MapperReturnsValue,
    bool IsSourceNullable);
