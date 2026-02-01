namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a constant value to be set on the target property.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConstantAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the constant value to set.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets or sets an expression string to evaluate instead of a constant value.
    /// When set, this takes precedence over <see cref="Value"/>.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConstantAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="value">The constant value to set.</param>
    public MapConstantAttribute(string target, object? value)
    {
        Target = target;
        Value = value;
    }
}

/// <summary>
/// Specifies a constant value of type <typeparamref name="T"/> to be set on the target property.
/// </summary>
/// <typeparam name="T">The type of the constant value.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConstantAttribute<T> : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the constant value to set.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConstantAttribute{T}"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="value">The constant value to set.</param>
    public MapConstantAttribute(string target, T value)
    {
        Target = target;
        Value = value;
    }
}
