namespace Smart.Mapper;

// op_Implicit: UserId <-> int
public struct UserId
{
    public int Value { get; init; }

    public static implicit operator int(UserId v) => v.Value;
    public static implicit operator UserId(int v) => new() { Value = v };
}

public class ImplicitConversionSource
{
    public UserId Id { get; set; }
    public UserId? NullableId { get; set; }
}

public class ImplicitConversionDestination
{
    public int Id { get; set; }
    public int NullableId { get; set; }
}

// op_Explicit: Celsius -> double (one direction only)
public struct Celsius
{
    public double Value { get; init; }

    public static explicit operator double(Celsius v) => v.Value;
}

public class ExplicitConversionSource
{
    public Celsius Temp { get; set; }
    public Celsius? NullableTemp { get; set; }
}

public class ExplicitConversionDestination
{
    public double Temp { get; set; }
    public double NullableTemp { get; set; }
}

// op_Implicit takes priority over op_Explicit when both exist
public struct DualOpStruct
{
    public int Value { get; init; }

    public static implicit operator long(DualOpStruct v) => v.Value;
    public static explicit operator int(DualOpStruct v) => v.Value;
}

public class DualOpSource
{
    public DualOpStruct X { get; set; }
}

public class DualOpImplicitDestination
{
    public long X { get; set; }
}

public class DualOpExplicitDestination
{
    public int X { get; set; }
}

// IFormattable: Money -> string with Culture/Format
public struct Money : IFormattable
{
    public decimal Amount { get; init; }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => Amount.ToString(format ?? "G", formatProvider);

    public override string ToString() => Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public class FormattableSource
{
    public Money Price { get; set; }
}

public class FormattableDestination
{
    public string Price { get; set; } = string.Empty;
}
