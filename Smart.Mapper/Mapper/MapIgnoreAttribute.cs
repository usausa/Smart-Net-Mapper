namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a target property to be ignored during mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapIgnoreAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name to ignore.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapIgnoreAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name to ignore.</param>
    public MapIgnoreAttribute(string target)
    {
        Target = target;
    }
}
