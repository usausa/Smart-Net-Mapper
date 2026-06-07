#pragma warning disable CA1815
#pragma warning disable CA2225
namespace Smart.Mapper.Models;

// op_Implicit: UserId <-> int
public readonly struct UserId
{
    public int Value { get; init; }

    public static implicit operator int(UserId v) => v.Value;
    public static implicit operator UserId(int v) => new() { Value = v };
}

public sealed class ImplicitConversionSource
{
    public UserId Id { get; set; }
    public UserId? NullableId { get; set; }
}

public sealed class ImplicitConversionDestination
{
    public int Id { get; set; }
    public int NullableId { get; set; }
}

// op_Explicit: Celsius -> double (one direction only)
public readonly struct Celsius
{
    public double Value { get; init; }

    public static explicit operator double(Celsius v) => v.Value;
}

public sealed class ExplicitConversionSource
{
    public Celsius Temp { get; set; }
    public Celsius? NullableTemp { get; set; }
}

public sealed class ExplicitConversionDestination
{
    public double Temp { get; set; }
    public double NullableTemp { get; set; }
}

// op_Implicit takes priority over op_Explicit when both exist
public readonly struct DualOpStruct
{
    public int Value { get; init; }

    public static implicit operator long(DualOpStruct v) => v.Value;
    public static explicit operator int(DualOpStruct v) => v.Value;
}

public sealed class DualOpSource
{
    public DualOpStruct X { get; set; }
}

public sealed class DualOpImplicitDestination
{
    public long X { get; set; }
}

public sealed class DualOpExplicitDestination
{
    public int X { get; set; }
}

// IFormattable: Money -> string with Culture/Format
public readonly struct Money : IFormattable
{
    public decimal Amount { get; init; }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => Amount.ToString(format ?? "G", formatProvider);

    public override string ToString() => Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class FormattableSource
{
    public Money Price { get; set; }
}

public sealed class FormattableDestination
{
    public string Price { get; set; } = default!;
}

// __todo.md §3: op_Implicit を持つ型（value/class/struct/pair/cross-pair）
public sealed class OpClass
{
    public int Value { get; init; }

    public static implicit operator int(OpClass c) => c.Value;

    public static implicit operator OpClass(int v) => new() { Value = v };
}

public readonly struct OpStruct
{
    public int Value { get; init; }

    public static implicit operator int(OpStruct s) => s.Value;

    public static implicit operator OpStruct(int v) => new() { Value = v };
}

public readonly struct OpPairA
{
    public int V { get; init; }

    public static implicit operator OpPairB(OpPairA a) => new() { V = a.V };

    public static implicit operator OpPairA(OpPairB b) => new() { V = b.V };
}

public readonly struct OpPairB
{
    public int V { get; init; }
}

public sealed class OpCrossClass
{
    public int V { get; init; }

    public static implicit operator OpCrossStruct(OpCrossClass c) => new() { V = c.V };

    public static implicit operator OpCrossClass(OpCrossStruct s) => new() { V = s.V };
}

public readonly struct OpCrossStruct
{
    public int V { get; init; }
}
