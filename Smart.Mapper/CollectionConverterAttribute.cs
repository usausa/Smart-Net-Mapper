namespace Smart.Mapper;

using System.Diagnostics.CodeAnalysis;

// Names the class whose methods build the target collections of [MapCollection]. A method of it obsolete as an
// error is not called, and is reported as not matching (SMP0104). One obsolete as a warning is called.
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
