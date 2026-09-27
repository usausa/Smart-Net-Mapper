namespace Smart.Mapper.Generator.Models;

// What supplies an argument of the constructor a return mapper calls: the property mapping of the member the
// parameter assigns (or one made for a parameter without a member), or the attribute that assigns the member.
internal enum ConstructorArgumentKind
{
    Property,
    Constant,
    Expression,
    MapUsing,
    MapFrom,
    MapNested,
    MapCollection
}
