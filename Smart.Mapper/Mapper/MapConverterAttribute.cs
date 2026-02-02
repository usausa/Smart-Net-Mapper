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
    /// or a non-generic method matching the specific types.
    /// </summary>
    public Type Converter { get; }

    /// <summary>
    /// Gets or sets the method name prefix for conversion.
    /// Default is "Convert".
    /// The generator will also search for "{Method}To{DestinationType}" methods.
    /// </summary>
    public string Method { get; set; } = "Convert";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConverterAttribute"/> class.
    /// </summary>
    /// <param name="converter">The converter type.</param>
    public MapConverterAttribute(Type converter)
    {
        Converter = converter;
    }
}
