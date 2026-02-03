namespace Smart.Mapper;

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
