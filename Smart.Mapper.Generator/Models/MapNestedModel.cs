namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Represents a MapNested mapping (nested object property mapped using a mapper method).
internal sealed record MapNestedModel(
    // Source and target members
    string SourceName = default!,
    string SourceType = default!,
    string TargetName = default!,
    string TargetType = default!,
    // Mapper method that maps the nested object. The RefKinds of its parameters decide how the instance a
    // void mapper fills is passed
    string Mapper = default!,
    EquatableArray<RefKind> MapperParameterRefKinds = default,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Emit shape. A value-returning mapper is assigned, a void one fills the existing instance
    bool MapperReturnsValue = default,
    bool IsSourceNullable = default);
