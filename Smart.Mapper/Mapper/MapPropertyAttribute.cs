namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a property mapping between source and destination with different names.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets or sets the source property name.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the converter method name for type conversion.
    /// </summary>
    public string? Converter { get; set; }

    /// <summary>
    /// Gets or sets the behavior when the source value is null.
    /// </summary>
    public NullBehavior NullBehavior { get; set; } = NullBehavior.SetDefault;

    /// <summary>
    /// Gets or sets the order of this mapping. Lower values are processed first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapPropertyAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    public MapPropertyAttribute(string target)
    {
        Target = target;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapPropertyAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="source">The source property name.</param>
    public MapPropertyAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
