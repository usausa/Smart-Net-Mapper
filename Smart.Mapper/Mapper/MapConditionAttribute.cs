namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a condition method that determines whether to execute the mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MapConditionAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the condition method.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConditionAttribute"/> class.
    /// </summary>
    /// <param name="methodName">The name of the condition method that returns bool.</param>
    public MapConditionAttribute(string methodName)
    {
        MethodName = methodName;
    }
}

/// <summary>
/// Specifies a condition for a specific property mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapPropertyConditionAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the name of the condition method.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapPropertyConditionAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="methodName">The name of the condition method that returns bool.</param>
    public MapPropertyConditionAttribute(string target, string methodName)
    {
        Target = target;
        MethodName = methodName;
    }
}
