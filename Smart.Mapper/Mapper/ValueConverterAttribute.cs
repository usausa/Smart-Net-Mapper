namespace Smart.Mapper;

/// <summary>
/// Specifies a custom value converter to use for type conversions.
/// Can be applied at class level (all mappers) or method level (specific mapper).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class ValueConverterAttribute : Attribute
{
    /// <summary>
    /// Gets the converter type containing conversion methods.
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Gets or sets the method name prefix for conversion methods.
    /// Default is "Convert". Specialized methods follow the pattern "{Method}To{TargetType}".
    /// </summary>
    public string Method { get; set; } = "Convert";

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueConverterAttribute"/> class.
    /// </summary>
    /// <param name="converterType">The converter type.</param>
    public ValueConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
