namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies that a nested object property should be mapped using a specified mapper method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapNestedAttribute : Attribute
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
    /// Gets or sets the mapper method name to use for mapping the nested object.
    /// </summary>
    public string MapperMethod { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapNestedAttribute"/> class.
    /// </summary>
    /// <param name="source">The source property name.</param>
    /// <param name="target">The target property name.</param>
    public MapNestedAttribute(string source, string target)
    {
        Source = source;
        Target = target;
    }
}
