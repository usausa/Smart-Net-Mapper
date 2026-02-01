namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies that a collection property should be mapped using a specified mapper method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapCollectionAttribute : Attribute
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
    /// Gets or sets the mapper method name to use for mapping each element.
    /// </summary>
    public string MapperMethod { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapCollectionAttribute"/> class.
    /// </summary>
    /// <param name="source">The source property name.</param>
    /// <param name="target">The target property name.</param>
    public MapCollectionAttribute(string source, string target)
    {
        Source = source;
        Target = target;
    }
}
