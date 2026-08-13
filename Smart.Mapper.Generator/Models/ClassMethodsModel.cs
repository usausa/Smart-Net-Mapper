namespace Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Represents one partial class and every mapper method declared on it.
internal sealed record ClassMethodsModel(
    string Namespace,
    string ClassName,
    EquatableArray<MapperMethodModel> Methods);
