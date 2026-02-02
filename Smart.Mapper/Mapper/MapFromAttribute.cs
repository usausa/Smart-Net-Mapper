namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies that the target property is set from a source expression (method call or property path).
/// </summary>
/// <remarks>
/// Supports:
/// - Method calls: <c>[MapFrom("Count", "GetCount")]</c> generates <c>d.Count = s.GetCount()</c>
/// - Property paths: <c>[MapFrom("Count", "Items.Length")]</c> generates <c>d.Count = s.Items.Length</c>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapFromAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the source expression (method name or property path on the source object).
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Gets or sets the order of this mapping. Lower values are processed first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapFromAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="from">The source expression (method name or property path).</param>
    public MapFromAttribute(string target, string from)
    {
        Target = target;
        From = from;
    }
}
