namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class MapConverterAttribute : Attribute
{
    public Type ConverterType { get; }

    public string Method { get; set; } = "Convert";

    public MapConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
