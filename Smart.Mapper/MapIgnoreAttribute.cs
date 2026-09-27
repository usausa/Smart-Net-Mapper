namespace Smart.Mapper;

// Leaves the target, a member of the destination as a whole, out of the automatic mapping; a dotted target is
// reported (SMP0223), as the automatic mapping never assigns a member of a member on its own. An attribute mapping
// the same target contradicts it (SMP0101), while the dotted paths into it are applied. A member a constructor
// parameter of a return mapper assigns, or a parameter no member has, leaves the parameter without a value: another
// constructor is called, an optional parameter is left out, or the destination is created without arguments, and
// one that cannot be created otherwise is reported (SMP0216). A required member of the destination a return mapper
// creates cannot be left out (SMP0216) unless the constructor called has SetsRequiredMembers.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapIgnoreAttribute : Attribute
{
    public string Target { get; }

    public MapIgnoreAttribute(string target)
    {
        Target = target;
    }
}
