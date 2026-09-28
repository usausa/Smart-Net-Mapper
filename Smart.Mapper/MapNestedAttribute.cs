namespace Smart.Mapper;

// Maps the target with the mapper method, a static method that is not generic, found as a converter is (of the mapper
// class, a base class of it, a class containing it, or a type a global using static directive imports). For a member
// the constructor of a return mapper assigns, or a parameter the target names when no member has its name, the value of
// the parameter's type is made before construction and passed as the argument, and for an init-only member, or a
// required one the constructor does not set, it is made before construction and set in the object initializer (a void
// mapper cannot assign an init-only member, SMP0212). The source is a property of the source type, not a dotted path
// (SMP0206), and the mapper has to be given (SMP0211 without it). The result of a mapper returning a nullable reference
// goes with ! to a target not annotated as nullable, unless the mapper gets a value and [return: NotNullIfNotNull] of
// its first parameter says the result is not null for it, as a generated mapper declares, and a void mapper fills an
// instance created with the nullable annotations of the target's type arguments. A source that may be null (nullable,
// or declared with nullable annotations disabled) goes to a mapper taking null as it is, and a mapper not taking null
// is called for a value only, the target getting default for null. The mapper may take a type the source converts to by
// an implicit reference conversion, and return one converting the same way to the target; a nullable struct source goes
// to a mapper taking the struct as its value, after a null check. Of overloads, the one the call binds to is used, and
// a call binding to a method that does not match is reported (SMP0211).
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapNestedAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    public string? Mapper { get; set; }

    public int Order { get; set; }

    public MapNestedAttribute(string target)
    {
        Target = target;
    }

    public MapNestedAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
