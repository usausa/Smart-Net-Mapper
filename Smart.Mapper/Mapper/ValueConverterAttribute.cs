namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class ValueConverterAttribute : Attribute
{
    public Type ConverterType { get; }

    public string Method { get; set; } = "Convert";

    public ValueConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
