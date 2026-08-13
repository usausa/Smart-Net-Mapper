namespace Smart.Mapper.Generator.Models;

// Represents a segment in a nested property path.
// Path is the dotted path up to and including this segment, TypeName the type it evaluates to.
internal sealed record NestedPathSegment(
    string Path,
    string TypeName,
    bool IsNullable);
