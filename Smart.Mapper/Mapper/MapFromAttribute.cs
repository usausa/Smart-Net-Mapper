namespace Smart.Mapper;

/// <summary>
/// Specifies that a property should be mapped from a source member or method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapFromAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the source member name (property path or method name).
    /// </summary>
    public string Member { get; }

    /// <summary>
    /// Gets or sets the order of this mapping. Lower values are processed first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapFromAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="member">The source member name.</param>
    public MapFromAttribute(string target, string member)
    {
        Target = target;
        Member = member;
    }
}
