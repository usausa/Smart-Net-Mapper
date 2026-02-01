namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a custom type converter for mapping between types.
/// Can be applied at class level or assembly level.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MapperConverterAttribute : Attribute
{
    /// <summary>
    /// Gets the source type.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// Gets the target type.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// Gets the converter class type that contains the conversion method.
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Gets the converter method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapperConverterAttribute"/> class.
    /// </summary>
    /// <param name="sourceType">The source type.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="converterType">The converter class type.</param>
    /// <param name="methodName">The converter method name.</param>
    public MapperConverterAttribute(Type sourceType, Type targetType, Type converterType, string methodName)
    {
        SourceType = sourceType;
        TargetType = targetType;
        ConverterType = converterType;
        MethodName = methodName;
    }
}
