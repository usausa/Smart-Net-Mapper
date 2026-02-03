namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConditionAttribute : Attribute
{
    public string? Target { get; }

    public string Condition { get; }

    public MapConditionAttribute(string condition)
    {
        Condition = condition;
    }

    public MapConditionAttribute(string target, string condition)
    {
        Target = target;
        Condition = condition;
    }
}
