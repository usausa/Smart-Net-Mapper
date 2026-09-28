namespace Smart.Mapper;

// Assigns the target only when the condition method returns true for the source value; it guards a property mapping
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConditionAttribute : Attribute
{
    public string Target { get; }

    public string Condition { get; }

    public MapConditionAttribute(string target, string condition)
    {
        Target = target;
        Condition = condition;
    }
}
