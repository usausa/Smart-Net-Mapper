namespace Smart.Mapper.Generator.Helpers;

// How C# reports a use of a member marked [Obsolete]: not at all, as a warning (CS0618), or as an error (CS0619).
internal enum ObsoleteKind
{
    None,
    Warning,
    Error
}
