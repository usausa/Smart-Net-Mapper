namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a target property that is computed from source using a custom method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapFromAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the method name that computes the value from source.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapFromAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="methodName">The method name that computes the value.</param>
    public MapFromAttribute(string target, string methodName)
    {
        Target = target;
        MethodName = methodName;
    }
}
