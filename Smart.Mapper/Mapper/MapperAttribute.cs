namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MapperAttribute : Attribute
{
    public bool AutoMap { get; set; } = true;
}
