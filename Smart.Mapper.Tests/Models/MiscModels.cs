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
