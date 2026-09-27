#pragma warning disable SA1500
#pragma warning disable CA1024
#pragma warning disable CA1819
namespace Smart.Mapper.Models;

public class MapFromSource
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
}

public class MapFromDestination
{
    public string FullName { get; set; } = default!;
    public string UpperCaseName { get; set; } = default!;
}

public class MapFromContext
{
    public string Separator { get; set; } = " ";
}

public class MapFromMethodSource
{
    public int[] Items { get; set; } = [];

    public int GetItemCount() => Items.Length;
    public int GetItemSum() => Items.Sum();
}

public class MapFromMethodDestination
{
    public int ItemCount { get; set; }
    public int ItemSum { get; set; }
}

public class OrderTestSource
{
    public int Value { get; set; }
}

public class OrderTestDestination
{
    private readonly List<string> setOrder = [];

    public string Step1
    {
        get;
        set
        {
            field = value;
            setOrder.Add("Step1");
        }
    } = default!;

    public string Step2
    {
        get;
        set
        {
            field = value;
            setOrder.Add("Step2");
        }
    } = default!;

    public string Step3
    {
        get;
        set
        {
            field = value;
            setOrder.Add("Step3");
        }
    } = default!;

    public IReadOnlyList<string> GetSetOrder() => setOrder;
}

public class MapUsingContextSource
{
    public string BaseValue { get; set; } = default!;
}

public class MapUsingContextDestination
{
    public string ComputedValue { get; set; } = default!;
}

public class MapUsingContext
{
    public string Suffix { get; set; } = default!;
}

// D2: required member models
public class RequiredMemberSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class RequiredMemberDestination
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

// Regression G/H/I: NullBehavior.Skip without conversion, and return-mappers to init-only / required destinations.
public class SkipNoConvSource
{
    public int? Value { get; set; }
}

public class SkipNoConvDestination
{
    public int Value { get; set; }
}

public class InitReturnSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class InitReturnDestination
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
}

public class RequiredReturnSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class RequiredReturnDestination
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Extra { get; set; }
}

// MapConstant / MapExpression / MapUsing / MapFrom が init-only / required メンバーを対象にする場合、
// オブジェクト初期化子で代入される (return マッパー)。
// MapConstant / MapExpression / MapUsing / MapFrom targeting init-only / required members are
// assigned via the object initializer (return mapper).
public class FeatureInitSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class FeatureInitDestination
{
    public int Id { get; set; }
    public required string Fixed { get; set; }
    public string Upper { get; init; } = default!;
    public required string FromName { get; set; }
    public int Doubled { get; init; }
}

// E3: MapperProfile models
public class ProfileSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class ProfileDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

// A property whose setter the mapper cannot call is left out of the automatic mapping
public class PrivateSetterSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class PrivateSetterDestination
{
    public int Id { get; set; }
    public string Name { get; private set; } = "keep";
}

// Culture and formats resolved each on its own, from the method and then the profile
public class CultureProfileSource
{
    public decimal Amount { get; set; }
}

public class CultureProfileDestination
{
    public string Amount { get; set; } = default!;
}

// Constants the compiler converts implicitly
public class ConstantConversionDestination
{
    public long Big { get; set; }
    public byte Small { get; set; }
    public int Count { get; set; }
}

// Constants of every kind the generated code writes: members and casts of enums, types, arrays,
// floating-point numbers, and strings and chars that need escaping
public enum ConstantKind
{
    None,
    First,
    Second = 5
}

[Flags]
public enum ConstantAccess
{
    None = 0,
    Read = 1,
    Write = 2,
    Run = 4
}

public class ConstantKindSource
{
    public ConstantKind? Kind { get; set; }
    public Type? Type { get; set; }
    public int[]? Numbers { get; set; }
    public double? Ratio { get; set; }
    public string? Text { get; set; }
}

public class ConstantKindDestination
{
    public ConstantKind Kind { get; set; }
    public ConstantKind Undefined { get; set; }
    public ConstantAccess Access { get; set; }
    public Type? Type { get; set; }
    public int[] Numbers { get; set; } = default!;
    public string?[] Texts { get; set; } = default!;
    public object?[] Values { get; set; } = default!;
    public double Ratio { get; set; }
    public float Scale { get; set; }
    public double Zero { get; set; }
    public double Missing { get; set; }
    public char Quote { get; set; }
    public string Text { get; set; } = default!;
}

// Keywords used as the names of the parameters of the mapper and of the method it calls
public class KeywordSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class KeywordDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

// Constants of the integer types without a literal of their own keep their type when boxed
public class SmallIntegerDestination
{
    public object? Value { get; set; }
    public object?[] Values { get; set; } = default!;
    public byte[] Bytes { get; set; } = default!;
}

// Required fields a return mapper sets in the object initializer, and a void mapper leaves to the instance
internal record struct RequiredFieldDestination
{
    public required int Key;
    public required string Label;

    public int Id { get; set; }
}

// A constructor with [SetsRequiredMembers] sets the required members the mapper leaves out
public class SetsRequiredDestination
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public SetsRequiredDestination()
    {
        Code = "default";
    }

    public required string Code { get; set; }

    public required int Id { get; set; }

    public string Name { get; set; } = default!;
}

// Names written in the attributes are matched under the profile's name comparison, fields as well
public class NameComparisonSource
{
    public int Value { get; set; }

    public int Other { get; set; }

    public int GetCount() => Other * 2;
}

internal record struct NameComparisonDestination
{
    public int Field;

    public int Target { get; set; }

    public int Count { get; set; }
}

// A member the dotted paths of attributes go into is not copied from the source as a whole
public class DottedPathChild
{
    public int Value { get; set; }

    public int Other { get; set; }
}

public class DottedPathSource
{
    public DottedPathChild? Child { get; set; }

    public int Number { get; set; }
}

public class DottedPathDestination
{
    public DottedPathChild? Child { get; set; }

    public int Number { get; set; }
}

// Required members that are internal, one of them inherited, set in the object initializer
internal abstract class InternalRequiredBase
{
    internal required string Code { get; set; }
}

internal sealed class InternalRequiredDestination : InternalRequiredBase
{
    internal required int Level { get; init; }

    public int Id { get; set; }
}

// Members inherited from a base class, and from the interface an interface extends
public class InheritedLookupBase
{
    public int First { get; set; }

    public int Second { get; set; }

    public int Sum() => First + Second;
}

public class InheritedLookupSource : InheritedLookupBase
{
    public string Name { get; set; } = default!;
}

public interface IInheritedLookupBase
{
    int First { get; }

    int Twice();
}

public interface IInheritedLookupSource : IInheritedLookupBase
{
    string Name { get; }
}

public class InheritedLookupDestination
{
    public int First { get; set; }

    public string Name { get; set; } = default!;

    public int Total { get; set; }
}

// A property hiding one of a base type (new), one overriding a required one, and ones overriding the getter only
public class HidingBaseDestination
{
    public int Name { get; set; }
}

public class HidingDestination : HidingBaseDestination
{
    public new string Name { get; set; } = default!;

    public int Id { get; set; }
}

public class RequiredOverrideBase
{
    public virtual required int Id { get; set; }
}

public class RequiredOverrideDestination : RequiredOverrideBase
{
    public override required int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class GetterOverrideBase
{
    public virtual int Id { get; set; }

    public virtual string Label { get; set; } = default!;
}

public class GetterOverrideDestination : GetterOverrideBase
{
    public override int Id => base.Id;

    public override string Label => base.Label;
}

// A required member the dotted paths write into after construction, which the object initializer creates
public class RequiredPathDestination
{
    public required DottedPathChild Child { get; set; }

    public int Id { get; set; }
}

// A record whose constructor takes the values of the attributes
public class ConstructorArgumentSource
{
    public DottedPathChild Child { get; set; } = new();

    public DottedPathChild[] Children { get; set; } = [];

    public int Number { get; set; }
}

public record ConstructorArgumentDestination(DottedPathChild Child, int Code, string Label, IReadOnlyList<DottedPathChild> Children);

// Constructors whose parameters differ from the members: in type, by a name no member has, and by the spelling of
// the member a parameter assigns
public class ConstructorSource
{
    public int Code { get; set; }

    public int Other { get; set; }

    public int Level { get; set; }
}

public class ParameterTypeDestination
{
    public ParameterTypeDestination(string code)
    {
        Code = Int32.Parse(code, System.Globalization.CultureInfo.InvariantCulture);
    }

    public int Code { get; }
}

public class ParameterOnlyDestination
{
    public ParameterOnlyDestination(int number, string label)
    {
        Text = label + number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public string Text { get; }
}

public class ParameterNameDestination
{
    public ParameterNameDestination(int value)
    {
        Value = value;
    }

    // A constructor the mapper class cannot call is not taken, although longer
    protected ParameterNameDestination(int value, int other)
    {
        Value = value + other;
    }

    public int Value { get; }
}

// A property hiding one of a base type with a member the mapping does not take
public class HidingInternalBase
{
    public int Level { get; set; }
}

public class HidingInternalDestination : HidingInternalBase
{
    internal new string Level { get; set; } = "none";

    public int Code { get; set; }
}

// Members only a constructor assigns, as their setters are private, and a constructor with optional parameters
public class ConstructorChoiceSource
{
    public int Code { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class ConstructorSetterDestination
{
    public ConstructorSetterDestination()
    {
    }

    public ConstructorSetterDestination(int code)
    {
        Code = code;
    }

    public int Code { get; private set; }

    public string Name { get; set; } = string.Empty;
}

// The source has no Id, so no constructor gets every argument
public class ConstructorIdDestination
{
    public ConstructorIdDestination()
    {
    }

    public ConstructorIdDestination(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; private set; } = -1;

    public string Name { get; set; } = string.Empty;
}

public class OptionalParameterDestination
{
    public OptionalParameterDestination(int code, int level = 5, string name = "none", params int[] extra)
    {
        Code = code;
        Level = level;
        Name = name;
        ExtraCount = extra.Length;
    }

    public int Code { get; }

    public int Level { get; }

    public string Name { get; }

    public int ExtraCount { get; }
}

// Constructors of a length, the attribute naming a member only the second sets, and a parameterless constructor
// obsolete as a warning
public class PreferredConstructorSource
{
    public int Code { get; set; }

    public int Level { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PreferredConstructorDestination
{
    public PreferredConstructorDestination(int code, int level)
    {
        Code = code;
        Label = "level " + level.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public PreferredConstructorDestination(int code, string label)
    {
        Code = code;
        Label = label;
    }

    public int Code { get; }

    public string Label { get; }
}

public class ObsoleteParameterlessDestination
{
    [Obsolete("For serializers")]
    public ObsoleteParameterlessDestination()
    {
    }

    public ObsoleteParameterlessDestination(int code)
    {
        Code = code;
        Constructed = true;
    }

    public int Code { get; set; }

    public bool Constructed { get; }
}

// Constructors receiving the target of the attribute alike, one of them obsolete, and obsolete properties the
// automatic mapping leaves out
public class ObsoleteMemberSource
{
    public int Code { get; set; }

    public int Other { get; set; }

    public int Level { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Current { get; set; }

    [Obsolete("Use Current")]
    public int Previous => Current;

    public int Legacy { get; set; }
}

public class ObsoleteTieDestination
{
    [Obsolete("Use the constructor taking the name")]
    public ObsoleteTieDestination(int code, int level)
    {
        Code = code;
        Via = "level " + level.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public ObsoleteTieDestination(int code, string name)
    {
        Code = code;
        Via = name;
    }

    public int Code { get; }

    public string Via { get; }
}

public class ObsoleteMemberDestination
{
    public int Code { get; set; }

    public int Previous { get; set; }

    [Obsolete("Use Code")]
    public int Legacy
    {
        get;
        set
        {
            field = value;
            LegacyAssigned = true;
        }
    }

    public bool LegacyAssigned { get; private set; }
}

// Enum members obsolete as an error and as a warning, which the generated code writes as casts of their numbers
public enum ObsoleteSourceColor
{
    Red,
    Blue,
    [Obsolete("Use Blue", true)]
    Green
}

public enum ObsoleteDestinationColor
{
    Red,
    Blue,
    [Obsolete("Use Blue")]
    Green = 20
}

public class ObsoleteEnumSource
{
    public ObsoleteSourceColor Color { get; set; }

    public ObsoleteSourceColor Name { get; set; }

    public string Text { get; set; } = string.Empty;
}

public class ObsoleteEnumDestination
{
    public ObsoleteDestinationColor Color { get; set; }

    public string Name { get; set; } = string.Empty;

    public ObsoleteDestinationColor Text { get; set; }

    public ObsoleteDestinationColor Fixed { get; set; }
}

// NullBehavior.Skip with a converter taking the nullable source, which is called only for a value
public class SkipConverterSource
{
    public int? Value { get; set; }
}

public class SkipConverterDestination
{
    public string Value { get; set; } = "kept";
}

// Types a generic mapper method maps, and the entity it creates as its type parameter
public class GenericBox<T>
{
    public T Value { get; set; } = default!;

    public int Count { get; set; }
}

public class GenericWrap<T>
{
    public T Value { get; set; } = default!;

    public long Count { get; set; }
}

public class GenericEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class GenericDerivedEntity : GenericEntity
{
    public bool Derived { get; } = true;
}

public class GenericEntitySource
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

// A struct a mapper returns as nullable
public record struct NullableStructValue
{
    public int Id { get; set; }

    public string Name { get; set; }
}

// NullValue with a converter taking the nullable source: a null source takes NullValue, and a value is converted
public class NullValueConverterSource
{
    public int? Value { get; set; }
}

public class NullValueConverterDestination
{
    public string Value { get; set; } = string.Empty;
}

// A dotted source whose intermediate member is null gives NullValue to the target that has one, and leaves the other
public class IntermediateLeaf
{
    public string? Name { get; set; }

    public int Code { get; set; }
}

public class IntermediateSource
{
    public IntermediateLeaf? Leaf { get; set; }
}

public class IntermediateDestination
{
    public string Name { get; set; } = "old";

    public int Code { get; set; } = 7;
}
