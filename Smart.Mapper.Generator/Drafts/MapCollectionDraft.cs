namespace Smart.Mapper.Generator.Drafts;

using System.Linq;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Mutable carrier used while MapCollectionModel is being assembled.
// MapperModelBuilder rewrites these fields across many passes; ToModel() freezes the result.
internal sealed class MapCollectionDraft
{
    public string SourceName { get; set; } = default!;

    public string SourceType { get; set; } = default!;

    public string SourceElementType { get; set; } = default!;

    public string TargetName { get; set; } = default!;

    public string TargetType { get; set; } = default!;

    public string TargetElementType { get; set; } = default!;

    public string? Mapper { get; set; }

    public int Order { get; set; }

    public int DefinitionOrder { get; set; }

    public CollectionSourceShape SourceShape { get; set; } = CollectionSourceShape.Enumerable;

    public CollectionTargetShape TargetShape { get; set; } = CollectionTargetShape.List;

    public string TargetCollectionMethod { get; set; } = "ToList";

    public bool MapperReturnsValue { get; set; }

    public bool IsSourceNullable { get; set; }

    public bool TargetIsArray { get; set; }

    public bool UseHelperPath { get; set; }

    public string? Converter { get; set; }

    public bool InPlace { get; set; }

    public string? InPlaceFallbackTypeName { get; set; }

    public MapCollectionModel ToModel() => new(
        SourceName,
        SourceType,
        SourceElementType,
        TargetName,
        TargetType,
        TargetElementType,
        Mapper,
        Order,
        DefinitionOrder,
        SourceShape,
        TargetShape,
        TargetCollectionMethod,
        MapperReturnsValue,
        IsSourceNullable,
        TargetIsArray,
        UseHelperPath,
        Converter,
        InPlace,
        InPlaceFallbackTypeName);

    // Mirrors MapCollectionModelExtensions.HasCustomConverter for the draft stage.
    public bool HasCustomConverter() => !String.IsNullOrEmpty(Converter);
}
