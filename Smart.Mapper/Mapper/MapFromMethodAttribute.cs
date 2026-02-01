namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies that the target property is set from the result of calling a method on the source object.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapFromMethodAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the method name to call on the source object.
    /// </summary>
    public string SourceMethod { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapFromMethodAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="sourceMethod">The method name to call on the source object.</param>
    public MapFromMethodAttribute(string target, string sourceMethod)
    {
        Target = target;
        SourceMethod = sourceMethod;
    }
}
