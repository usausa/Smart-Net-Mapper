namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies that a collection property should be mapped using a specified mapper method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapCollectionAttribute : Attribute
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
    /// Gets or sets the mapper method name to use for mapping each element.
    /// </summary>
    public string Mapper { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order of this mapping. Lower values are processed first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapCollectionAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    public MapCollectionAttribute(string target)
    {
        Target = target;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapCollectionAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="source">The source property name.</param>
    public MapCollectionAttribute(string target, string source)
    {
        Target = target;
        Source = source;
    }
}
