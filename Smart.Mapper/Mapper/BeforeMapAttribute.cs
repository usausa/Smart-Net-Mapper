namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a method to be called before the mapping process.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeMapAttribute : Attribute
{
    /// <summary>
    /// Gets the method name to call before mapping.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BeforeMapAttribute"/> class.
    /// </summary>
    /// <param name="method">The method name to call before mapping.</param>
    public BeforeMapAttribute(string method)
    {
        Method = method;
    }
}
