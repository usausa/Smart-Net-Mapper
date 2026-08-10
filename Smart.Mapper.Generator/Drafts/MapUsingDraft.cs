namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while MapUsingModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class MapUsingDraft
{
    public string TargetName { get; set; } = default!;

    public string TargetType { get; set; } = default!;

    public string Method { get; set; } = default!;

    public string MethodReturnType { get; set; } = default!;

    public int Order { get; set; }

    public int DefinitionOrder { get; set; }

    public bool AcceptsCustomParameters { get; set; }

    public bool IsTargetInitOnly { get; set; }

    public bool IsTargetRequired { get; set; }

    public MapUsingModel ToModel() => new(
        TargetName,
        TargetType,
        Method,
        MethodReturnType,
        Order,
        DefinitionOrder,
        AcceptsCustomParameters,
        IsTargetInitOnly,
        IsTargetRequired);
}
