namespace Smart.Mapper;

// Assigns the value of a C# expression to the target; the expression can refer to the parameters of the mapper
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapExpressionAttribute : Attribute
{
    public string Target { get; }

    public string Expression { get; }

    public int Order { get; set; }

    public MapExpressionAttribute(string target, string expression)
    {
        Target = target;
        Expression = expression;
    }
}
