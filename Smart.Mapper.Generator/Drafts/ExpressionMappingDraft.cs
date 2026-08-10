namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while ExpressionMappingModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class ExpressionMappingDraft
{
    public string TargetName { get; set; } = default!;

    public string Expression { get; set; } = default!;

    public int Order { get; set; }

    public int DefinitionOrder { get; set; }

    public bool IsTargetInitOnly { get; set; }

    public bool IsTargetRequired { get; set; }

    public ExpressionMappingModel ToModel() => new(
        TargetName,
        Expression,
        Order,
        DefinitionOrder,
        IsTargetInitOnly,
        IsTargetRequired);
}
