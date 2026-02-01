namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a method to be called after the mapping process.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterMapAttribute : Attribute
{
    /// <summary>
    /// Gets the method name to call after mapping.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AfterMapAttribute"/> class.
    /// </summary>
    /// <param name="methodName">The method name to call after mapping.</param>
    public AfterMapAttribute(string methodName)
    {
        MethodName = methodName;
    }
}
