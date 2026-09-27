namespace Smart.Mapper;

// Maps the target from the source member. A dotted target (Child.Value) writes into that member, which the
// automatic mapping then leaves out, and cannot go into a member the constructor of a return mapper assigns
// (SMP0222).
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    // A static method converting the source value, found as the generated code calls it by name: of the mapper class or
    // a base class of it, or else of a class containing it or a base class of that, or else of a type a global using
    // static directive imports. It takes the value as its type, by value as a type the value converts to implicitly (a
    // base class or an interface, boxing, a wider number, a nullable struct, a user-defined conversion), or, from a
    // nullable struct, as the struct it holds or a type that struct converts to implicitly (its Value), and returns the
    // target type or one converting to it implicitly; a nullable reference it returns into a target not annotated as
    // nullable is taken with !. One whose parameter does not take null, or takes the value a nullable struct holds, is
    // called for a value only: a null source takes NullValue, or without one leaves the target as it is (a constructor
    // argument or an object initializer entry gets null or default). Of overloads, the one the call binds to is used;
    // an ambiguous call is reported (SMP0104).
    public string? Converter { get; set; }

    // Skip leaves the target as it is for a null source, the Converter, which takes the source as it is, not being
    // called either. It and NullValue apply to a reference declared with nullable annotations disabled as well.
    public NullBehavior NullBehavior { get; set; } = NullBehavior.Default;

    public int Order { get; set; }

    // The value a null source gives. With the Converter, a null source takes it and the converter is called for a
    // value only, and a dotted source takes it when an intermediate member is null as well.
    public object? NullValue { get; set; }

    // Culture name override for this property's string conversion (e.g. "en-US").
    public string? Culture { get; set; }

    // DateTime format string override for this property. Requires a culture, from Culture here, the
    // mapper method or the profile (SMP0401 otherwise).
    public string? DateTimeFormat { get; set; }

    // Numeric format string override for this property. Requires a culture, from Culture here, the
    // mapper method or the profile (SMP0401 otherwise).
    public string? NumberFormat { get; set; }

    public MapPropertyAttribute(string target)
    {
        Target = target;
    }

    public MapPropertyAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute<T> : Attribute
{
    public string Target { get; }

    public string? Source { get; }

    // A static method converting the source value, found as the generated code calls it by name: of the mapper class or
    // a base class of it, or else of a class containing it or a base class of that, or else of a type a global using
    // static directive imports. It takes the value as its type, by value as a type the value converts to implicitly (a
    // base class or an interface, boxing, a wider number, a nullable struct, a user-defined conversion), or, from a
    // nullable struct, as the struct it holds or a type that struct converts to implicitly (its Value), and returns the
    // target type or one converting to it implicitly; a nullable reference it returns into a target not annotated as
    // nullable is taken with !. One whose parameter does not take null, or takes the value a nullable struct holds, is
    // called for a value only: a null source takes NullValue, or without one leaves the target as it is (a constructor
    // argument or an object initializer entry gets null or default). Of overloads, the one the call binds to is used;
    // an ambiguous call is reported (SMP0104).
    public string? Converter { get; set; }

    // Skip leaves the target as it is for a null source, the Converter, which takes the source as it is, not being
    // called either. It and NullValue apply to a reference declared with nullable annotations disabled as well.
    public NullBehavior NullBehavior { get; set; } = NullBehavior.Default;

    public int Order { get; set; }

    // The value a null source gives. With the Converter, a null source takes it and the converter is called for a
    // value only, and a dotted source takes it when an intermediate member is null as well.
    public T NullValue { get; set; } = default!;

    // Culture name override for this property's string conversion (e.g. "en-US").
    public string? Culture { get; set; }

    // DateTime format string override for this property. Requires a culture, from Culture here, the
    // mapper method or the profile (SMP0401 otherwise).
    public string? DateTimeFormat { get; set; }

    // Numeric format string override for this property. Requires a culture, from Culture here, the
    // mapper method or the profile (SMP0401 otherwise).
    public string? NumberFormat { get; set; }

    public MapPropertyAttribute(string target)
    {
        Target = target;
    }

    public MapPropertyAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
