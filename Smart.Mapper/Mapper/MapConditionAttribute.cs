namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a condition that determines whether to execute the mapping.
/// When Target is null, the condition applies to the entire mapping.
/// When Target is specified, the condition applies only to that specific property.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConditionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the target property name.
    /// When null, the condition applies to the entire mapping.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets the name of the condition property or method.
    /// The generator will search for a matching property or method that returns bool.
    /// </summary>
    public string Condition { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConditionAttribute"/> class
    /// for a global condition that applies to the entire mapping.
    /// </summary>
    /// <param name="condition">The name of the condition property or method that returns bool.</param>
    public MapConditionAttribute(string condition)
    {
        Condition = condition;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConditionAttribute"/> class
    /// for a property-level condition.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="condition">The name of the condition property or method that returns bool.</param>
    public MapConditionAttribute(string target, string condition)
    {
        Target = target;
        Condition = condition;
    }
}
