namespace Smart.Mapper;

using System.Diagnostics.CodeAnalysis;

// Names the class whose methods build the target collections of MapCollection, for a method or all of a class
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class CollectionConverterAttribute : Attribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
    public Type ConverterType { get; }

    public CollectionConverterAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        Type converterType)
    {
        ConverterType = converterType;
    }
}
