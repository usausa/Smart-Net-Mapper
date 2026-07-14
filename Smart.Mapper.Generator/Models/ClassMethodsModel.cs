namespace Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Represents a constant value mapping configuration.
internal sealed record ClassMethodsModel(
    string Namespace,
    string ClassName,
    EquatableArray<MapperMethodModel> Methods);
