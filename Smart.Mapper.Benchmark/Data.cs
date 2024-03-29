namespace Smart.Mapper.Benchmark;

// Single
public sealed class SingleSource
{
    public int Value { get; set; }
}

public sealed class SingleDestination
{
    public int Value { get; set; }
}

// Simple
public sealed class SimpleSource
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
    public int Value4 { get; set; }
    public string? Value5 { get; set; }
    public string? Value6 { get; set; }
    public string? Value7 { get; set; }
    public string? Value8 { get; set; }
}

public sealed class SimpleDestination
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
    public int Value4 { get; set; }
    public string? Value5 { get; set; }
    public string? Value6 { get; set; }
    public string? Value7 { get; set; }
    public string? Value8 { get; set; }
}

// Mixed
public sealed class MixedSource
{
    public string? StringValue { get; set; }
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public int? NullableIntValue { get; set; }
    public float FloatValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public bool BoolValue { get; set; }
    public MyEnum EnumValue { get; set; }
}

public sealed class MixedDestination
{
    public string? StringValue { get; set; }
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public int? NullableIntValue { get; set; }
    public float FloatValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public bool BoolValue { get; set; }
    public MyEnum EnumValue { get; set; }
}

public enum MyEnum
{
    Zero,
    One
}
