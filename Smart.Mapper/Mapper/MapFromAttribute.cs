namespace Smart.Mapper;

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
