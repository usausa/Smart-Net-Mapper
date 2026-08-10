namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while MapFromModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class MapFromDraft
{
    public string TargetName { get; set; } = default!;

    public string TargetType { get; set; } = default!;

    public string Member { get; set; } = default!;

    public string ReturnType { get; set; } = default!;

    public int Order { get; set; }

    public int DefinitionOrder { get; set; }

    public bool IsMethodCall { get; set; }

    public bool IsTargetInitOnly { get; set; }

    public bool IsTargetRequired { get; set; }

    public MapFromModel ToModel() => new(
        TargetName,
        TargetType,
        Member,
        ReturnType,
        Order,
        DefinitionOrder,
        IsMethodCall,
        IsTargetInitOnly,
        IsTargetRequired);
}
