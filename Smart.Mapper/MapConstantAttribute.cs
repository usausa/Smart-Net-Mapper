namespace Smart.Mapper;

// Assigns the value to the target, or passes it to the constructor of a return mapper for a member the
// constructor assigns, or a parameter the target names when no member has its name; the value is checked for the
// type of the parameter. A dotted target (Child.Value) writes into that member, which the automatic mapping then
// leaves out, and cannot go into a member the constructor assigns (SMP0222).
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConstantAttribute : Attribute
{
    public string Target { get; }

    public object? Value { get; }

    public int Order { get; set; }

    public MapConstantAttribute(string target, object? value)
    {
        Target = target;
        Value = value;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConstantAttribute<T> : Attribute
{
    public string Target { get; }

    public T Value { get; }

    public int Order { get; set; }

    public MapConstantAttribute(string target, T value)
    {
        Target = target;
        Value = value;
    }
}
