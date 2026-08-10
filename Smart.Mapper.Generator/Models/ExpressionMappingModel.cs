namespace Smart.Mapper.Generator.Models;

// Represents an expression mapping configuration.
internal sealed record ExpressionMappingModel(
    string TargetName,
    string Expression,
    int Order,
    int DefinitionOrder,
    bool IsTargetInitOnly,
    bool IsTargetRequired);
