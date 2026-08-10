namespace Smart.Mapper.Generator.Models;

// Represents a segment in a nested property path.
internal sealed record NestedPathSegment(
    string Path,
    string TypeName,
    bool IsNullable);
