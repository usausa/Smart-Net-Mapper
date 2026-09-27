namespace Smart.Mapper;

using System.Diagnostics.CodeAnalysis;

// Names the class the value conversions of the mapper call. A method of it obsolete as an error is not called:
// another conversion takes over when there is one (the generic method for a specialized one), and otherwise it is
// reported as not matching (SMP0104). One obsolete as a warning is called.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class ValueConverterAttribute : Attribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
    public Type ConverterType { get; }

    public string Method { get; set; } = "Convert";

    public ValueConverterAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        Type converterType)
    {
        ConverterType = converterType;
    }
}
