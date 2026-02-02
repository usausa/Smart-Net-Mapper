namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a target property that is computed from source using a custom method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapMethodAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the method name that computes the value from source.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets or sets the order of this mapping. Lower values are processed first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapMethodAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="method">The method name that computes the value.</param>
    public MapMethodAttribute(string target, string method)
    {
        Target = target;
        Method = method;
    }
}
