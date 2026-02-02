namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies an expression to be evaluated and set on the target property.
/// Use this for dynamic values like DateTime.Now.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapExpressionAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the expression string to evaluate.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Gets or sets the order of this mapping. Lower values are processed first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapExpressionAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="expression">The expression string to evaluate.</param>
    public MapExpressionAttribute(string target, string expression)
    {
        Target = target;
        Expression = expression;
    }
}
