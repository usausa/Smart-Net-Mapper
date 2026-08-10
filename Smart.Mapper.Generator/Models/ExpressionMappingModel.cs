namespace Smart.Mapper.Generator.Models;

// Represents an expression mapping configuration.
internal sealed record ExpressionMappingModel(
    string TargetName = default!,
    string Expression = default!,
    int Order = default,
    int DefinitionOrder = default,
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default);
