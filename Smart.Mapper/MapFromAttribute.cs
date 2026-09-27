namespace Smart.Mapper;

// Assigns the target from a member of the source: a parameterless instance method the mapper class can call,
// not a generic one, also one of a base type or of an interface the source interface extends (the most derived
// one first), or a property path through properties with a getter the mapper class can call. A member obsolete as
// a warning is used, and one obsolete as an error is reported (SMP0204). A member the constructor of a return
// mapper assigns, or a parameter the target names when no member has its name, takes the value as the argument,
// of the parameter's type. The member gives the type of the target or one converting to it implicitly (SMP0205
// otherwise). A property path through members that may be null is read under their null check, as the source path
// of MapProperty: the target is left as it is when one is null, and a constructor argument or an object initializer
// entry gets null for a target taking it, or default. A nullable reference going to a target not annotated as
// nullable is taken with !.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapFromAttribute : Attribute
{
    public string Target { get; }

    public string Member { get; }

    public int Order { get; set; }

    public MapFromAttribute(string target, string member)
    {
        Target = target;
        Member = member;
    }
}
