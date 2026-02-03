namespace Smart.Mapper;

/// <summary>
/// Specifies that a collection property should be mapped.
/// When Mapper is specified, each element is mapped using the specified mapper method.
/// When Mapper is not specified, elements are directly assigned or converted using the value converter.
/// The destination collection is always a new instance.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapCollectionAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets or sets the source property name. If not specified, uses the same name as Target.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the mapper method name for mapping each element.
    /// The method should have signature: TDest Method(TSource source [, custom parameters]).
    /// If not specified, elements are directly assigned or converted.
    /// </summary>
    public string? Mapper { get; set; }

    /// <summary>
    /// Gets or sets the converter method name to use for converting the collection.
    /// If not specified, ToArray is used for array targets, ToList for others.
    /// </summary>
    public string? Converter { get; set; }

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
