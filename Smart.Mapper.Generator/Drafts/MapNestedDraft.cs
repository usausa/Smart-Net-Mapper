namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while MapNestedModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class MapNestedDraft
{
    public string SourceName { get; set; } = default!;

    public string SourceType { get; set; } = default!;

    public string TargetName { get; set; } = default!;

    public string TargetType { get; set; } = default!;

    public string Mapper { get; set; } = default!;

    public int Order { get; set; }

    public int DefinitionOrder { get; set; }

    public bool MapperReturnsValue { get; set; }

    public bool IsSourceNullable { get; set; }

    public MapNestedModel ToModel() => new(
        SourceName,
        SourceType,
        TargetName,
        TargetType,
        Mapper,
        Order,
        DefinitionOrder,
        MapperReturnsValue,
        IsSourceNullable);
}
