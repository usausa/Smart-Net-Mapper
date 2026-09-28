namespace Smart.Mapper;

// Assigns the value of the expression to the target, or passes it to the constructor of a return mapper for a member
// the constructor assigns, or a parameter the target names when no member has its name, as a value of the parameter's
// type. A dotted target (Child.Value) writes into that member, which the automatic mapping then leaves out, cannot go
// into a member the constructor assigns (SMP0222), nor through a nullable struct, whose Value is a copy no setter takes
// back (SMP0214).
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
