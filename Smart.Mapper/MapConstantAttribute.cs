namespace Smart.Mapper;

// Assigns a constant value to the target, written in the generated code as an expression of its own type
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
