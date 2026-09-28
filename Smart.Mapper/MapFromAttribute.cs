namespace Smart.Mapper;

// Assigns the target from a parameterless method or a property path of the source
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapFromAttribute : Attribute
{
    public string Target { get; }

    public string Member { get; }

    public int Order { get; set; }

    public MapFromAttribute(string target, string member)
    {
        Target = target;
        Member = member;
    }
}
