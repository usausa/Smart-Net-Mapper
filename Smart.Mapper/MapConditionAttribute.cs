namespace Smart.Mapper;

// Assigns the target only when the condition method returns true. It guards the property mapping of the target, the
// automatic one or a MapProperty; a target no property mapping assigns is reported (SMP0221). The method is a static
// method found as a converter is (of the mapper class, a base class of it, a class containing it, or a type a global
// using static directive imports), taking the source value as its type, by value as a type the value converts to
// implicitly, or, from a nullable struct, as the struct it holds or a type that struct converts to implicitly (its
// Value); of overloads, the one the call binds to is used, and an ambiguous call is reported (SMP0106). A condition
// method whose parameter does not take null, or takes the value a nullable struct holds, is not given a null source,
// which does not meet it.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapConditionAttribute : Attribute
{
    public string Target { get; }

    public string Condition { get; }

    public MapConditionAttribute(string target, string condition)
    {
        Target = target;
        Condition = condition;
    }
}
