namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while ConstantMappingModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class ConstantMappingDraft
{
    public string TargetName { get; set; } = default!;

    public string TargetType { get; set; } = default!;

    public string? Value { get; set; }

    public int Order { get; set; }

    public int DefinitionOrder { get; set; }

    public bool IsTargetInitOnly { get; set; }

    public bool IsTargetRequired { get; set; }

    public ConstantMappingModel ToModel() => new(
        TargetName,
        TargetType,
        Value,
        Order,
        DefinitionOrder,
        IsTargetInitOnly,
        IsTargetRequired);
}
