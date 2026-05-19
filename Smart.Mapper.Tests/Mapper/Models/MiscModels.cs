#pragma warning disable SA1500
#pragma warning disable CA1024
#pragma warning disable CA1819
namespace Smart.Mapper;

public class MapFromSource
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class MapFromDestination
{
    public string FullName { get; set; } = string.Empty;
    public string UpperCaseName { get; set; } = string.Empty;
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
    } = string.Empty;

    public string Step2
    {
        get;
        set
        {
            field = value;
            setOrder.Add("Step2");
        }
    } = string.Empty;

    public string Step3
    {
        get => step3;
        set { step3 = value; setOrder.Add("Step3"); }
    }
    private string step3 = string.Empty;

    public IReadOnlyList<string> GetSetOrder() => setOrder;
}

public class MapUsingContextSource
{
    public string BaseValue { get; set; } = string.Empty;
}

public class MapUsingContextDestination
{
    public string ComputedValue { get; set; } = string.Empty;
}

public class MapUsingContext
{
    public string Suffix { get; set; } = string.Empty;
}

// D2: required member models
public class RequiredMemberSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RequiredMemberDestination
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

// E3: MapperProfile models
public class ProfileSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProfileDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
