namespace Smart.Mapper;

// Maps the target collection element by element with the mapper method, a static method that is not generic, found as a
// converter is (of the mapper class, a base class of it, a class containing it, or a type a global using static
// directive imports). For a member the constructor of a return mapper assigns, or a parameter the target names when no
// member has its name, the collection of the parameter's type is made before construction and passed as the argument,
// which leaves nothing for InPlace to refill (SMP0219), and for an init-only member, or a required one the constructor
// does not set, it is made before construction and set in the object initializer (a void mapper cannot assign an
// init-only member, SMP0212; InPlace refills the instance an init-only one holds, and cannot refill a required one,
// SMP0219). A null nullable reference element does not go to a mapper whose parameter does not take null, and gives
// default. The collection created keeps the nullable annotations of the target's elements, and the result of a mapper
// returning a nullable reference goes with ! to elements not annotated as nullable. A dictionary interface target gets
// a Dictionary<TKey, TValue>, filled with the pairs the mapper returns. The mapper matches the element types as the one
// of MapNested does.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapCollectionAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    public string? Mapper { get; set; }

    public string? Converter { get; set; }

    public CollectionStrategy Strategy { get; set; }

    public int Order { get; set; }

    public MapCollectionAttribute(string target)
    {
        Target = target;
    }

    public MapCollectionAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
