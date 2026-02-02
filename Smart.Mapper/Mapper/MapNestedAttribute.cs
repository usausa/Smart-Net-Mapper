namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies that a nested object property should be mapped using a specified mapper method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapNestedAttribute : Attribute
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
    /// Gets or sets the mapper method name to use for mapping the nested object.
    /// </summary>
    public string Mapper { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order of this mapping. Lower values are processed first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapNestedAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    public MapNestedAttribute(string target)
    {
        Target = target;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapNestedAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="source">The source property name.</param>
    public MapNestedAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
