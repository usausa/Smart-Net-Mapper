namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class CollectionConverterAttribute : Attribute
{
    public Type ConverterType { get; }

    public CollectionConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
