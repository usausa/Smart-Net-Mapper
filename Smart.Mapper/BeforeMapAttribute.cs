namespace Smart.Mapper;

// Calls the method before the mapping: a static method found as a converter is (of the mapper class, a base class of
// it, a class containing it, or a type a global using static directive imports), not a generic one, taking the source
// and the destination, then the custom parameters when it declares them. The source and the destination go to
// parameters of their types, or, by value, of a base class or an interface they convert to (a struct to its own type
// only, as a boxed copy would take the writes); SMP0102 otherwise.
[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeMapAttribute : Attribute
{
    public string Method { get; }

    public BeforeMapAttribute(string method)
    {
        Method = method;
    }
}
