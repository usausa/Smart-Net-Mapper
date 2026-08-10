namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while PropertyConditionModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class PropertyConditionDraft
{
    public string TargetName { get; set; } = default!;

    public string? ConditionMethod { get; set; }

    public PropertyConditionModel ToModel() => new(
        TargetName,
        ConditionMethod);
}
