#pragma warning disable CA2227
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

// -------------------------------------------------------------------------
// Nested object mapping
// -------------------------------------------------------------------------

public sealed class NestedSource
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public AddressSource? Address { get; set; }
}

public sealed class AddressSource
{
    public string? City { get; set; }
    public string? ZipCode { get; set; }
}

public sealed class NestedDestination
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public AddressDestination? Address { get; set; }
}

public sealed class AddressDestination
{
    public string? City { get; set; }
    public string? ZipCode { get; set; }
}

// -------------------------------------------------------------------------
// Collection mapping
// -------------------------------------------------------------------------

public sealed class CollectionItemSource
{
    public int Id { get; set; }
    public string? Label { get; set; }
}

public sealed class CollectionItemDestination
{
    public int Id { get; set; }
    public string? Label { get; set; }
}

// MapCollection 属性はプロパティ単位のため、コレクションをラップするオブジェクトを用意する
public sealed class CollectionSource
{
    public List<CollectionItemSource> Items { get; set; } = [];
}

public sealed class CollectionWrapper
{
    public List<CollectionItemDestination> Items { get; set; } = [];
}

// -------------------------------------------------------------------------
// Type conversion mapping (int → string, string → int)
// -------------------------------------------------------------------------

public sealed class ConversionSource
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public bool BoolValue { get; set; }
}

public sealed class ConversionDestination
{
    public string? IntValue { get; set; }
    public string? LongValue { get; set; }
    public string? DoubleValue { get; set; }
    public string? BoolValue { get; set; }
}
