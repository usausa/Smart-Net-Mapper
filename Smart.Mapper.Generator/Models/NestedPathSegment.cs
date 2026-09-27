namespace Smart.Mapper.Generator.Models;

// How the generated code reaches through an intermediate member of a target path.
internal enum TargetSegmentAccess
{
    // A class the mapper can assign and create: created with ??= when null
    Create,
    // A member the mapper cannot assign (get-only, init-only, or with a setter it cannot call), or of a type it
    // cannot create (abstract, an interface, without a constructor it can call without arguments, or with
    // required members): the instance it holds is written into, and nothing when it is null
    Existing,
    // A struct property, which is a value: copied into a local, written into, and assigned back
    Copy,
    // A struct field, a variable written into as it is
    Direct
}

// Represents a segment in a nested property path.
// Path is the dotted path up to and including this segment, TypeName the type it evaluates to.
// IsNullable applies to a source path, Access to a target path.
internal sealed record NestedPathSegment(
    string Path,
    string TypeName,
    bool IsNullable,
    TargetSegmentAccess Access = TargetSegmentAccess.Create);
