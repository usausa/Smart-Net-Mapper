namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a property mapping between source and destination with different names.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets the source property name.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets or sets the mapper type for nested object mapping.
    /// </summary>
    public Type? MapperType { get; set; }

    /// <summary>
    /// Gets or sets the mapper method name. Default is "Map".
    /// </summary>
    public string? MapperMethod { get; set; }

    /// <summary>
    /// Gets or sets the converter method name for type conversion.
    /// </summary>
    public string? Converter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapPropertyAttribute"/> class.
    /// </summary>
    /// <param name="source">The source property name.</param>
    /// <param name="target">The target property name.</param>
    public MapPropertyAttribute(string source, string target)
    {
        Source = source;
        Target = target;
    }
}
