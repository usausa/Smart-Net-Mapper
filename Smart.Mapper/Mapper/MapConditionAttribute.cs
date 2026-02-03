namespace Smart.Mapper;

/// <summary>
/// Specifies a condition method that determines whether a property should be mapped.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConditionAttribute : Attribute
{
    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the condition method name.
    /// The method should return bool and accept the source value (and optionally custom parameters).
    /// </summary>
    public string Condition { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapConditionAttribute"/> class.
    /// </summary>
    /// <param name="target">The target property name.</param>
    /// <param name="condition">The condition method name.</param>
    public MapConditionAttribute(string target, string condition)
    {
        Target = target;
        Condition = condition;
    }
}
