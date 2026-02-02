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
    public string Method { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AfterMapAttribute"/> class.
    /// </summary>
    /// <param name="method">The method name to call after mapping.</param>
    public AfterMapAttribute(string method)
    {
        Method = method;
    }
}
