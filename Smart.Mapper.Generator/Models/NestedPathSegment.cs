namespace Smart.Mapper.Generator.Models;

// Represents a segment in a nested property path.
internal sealed record NestedPathSegment
{
    public string Path { get; set; } = default!;
    public string TypeName { get; set; } = default!;
    public bool IsNullable { get; set; }
}
