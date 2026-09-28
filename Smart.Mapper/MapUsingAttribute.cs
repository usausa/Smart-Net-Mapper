namespace Smart.Mapper;

// Assigns the value the method returns to the target, or passes it to the constructor of a return mapper for a member
// the constructor assigns, or a parameter the target names when no member has its name; the method returns the type of
// the parameter then. The method is a static method found as a converter is (of the mapper class, a base class of it, a
// class containing it, or a type a global using static directive imports), not a generic one, taking the source as its
// type or, by value, as a type it converts to implicitly (of overloads, the one the call binds to, an ambiguous call
// being reported, SMP0201), and returning the type of the target or one converting to it implicitly, as the assignment
// does (int to long or int?), SMP0202 otherwise; a nullable reference it returns into a target not annotated as
// nullable is taken with !, unless [return: NotNullIfNotNull] of its first parameter says the result is not null for
// the source, which it gets past the null check of the mapper. A dotted target (Child.Value) writes into that member,
// which the automatic mapping then leaves out, cannot go into a member the constructor assigns (SMP0222), nor through a
// nullable struct, whose Value is a copy no setter takes back (SMP0214).
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapUsingAttribute : Attribute
{
    public string Target { get; }

    public string Method { get; }

    public int Order { get; set; }

    public MapUsingAttribute(string target, string method)
    {
        Target = target;
        Method = method;
    }
}
