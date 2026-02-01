namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a custom type converter class for the mapper.
/// Can be applied at class level or method level.
/// Method level takes precedence over class level.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class MapConverterAttribute : Attribute
{
    /// <summary>
    /// Gets the converter type.
    /// The type must have a static method with signature:
    /// <code>TDestination Convert&lt;TSource, TDestination&gt;(TSource source)</code>
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConverterAttribute"/> class.
    /// </summary>
    /// <param name="converterType">The converter type.</param>
    public MapConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
