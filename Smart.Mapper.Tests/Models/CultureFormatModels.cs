namespace Smart.Mapper.Models;

// B4: Culture / Format mapping models

public class CultureFormatSource
{
    public double Amount { get; set; }
    public DateTime EventDate { get; set; }
    public decimal Price { get; set; }
}

public class CultureFormatDestination
{
    public string Amount { get; set; } = default!;
    public string EventDate { get; set; } = default!;
    public string Price { get; set; } = default!;
}

public class CultureParseSource
{
    public string Amount { get; set; } = default!;
    public string EventDate { get; set; } = default!;
}

public class CultureParseDestination
{
    public double Amount { get; set; }
    public DateTime EventDate { get; set; }
}

// Property-level culture override
public class CultureOverrideSource
{
    public double ValueA { get; set; }
    public double ValueB { get; set; }
}

public class CultureOverrideDestination
{
    public string ValueA { get; set; } = default!;
    public string ValueB { get; set; } = default!;
}

// Regression: Culture combined with specialized converters that previously lacked
// 3-argument (source, culture, format) overloads: bool / Guid / Half / Int128 / UInt128 / BigInteger.
public class CultureSpecialParseSource
{
    public string HalfValue { get; set; } = default!;
    public string Int128Value { get; set; } = default!;
    public string UInt128Value { get; set; } = default!;
    public string BigIntegerValue { get; set; } = default!;
    public string BoolValue { get; set; } = default!;
    public string GuidValue { get; set; } = default!;
}

public class CultureSpecialParseDestination
{
    public Half HalfValue { get; set; }
    public Int128 Int128Value { get; set; }
    public UInt128 UInt128Value { get; set; }
    public System.Numerics.BigInteger BigIntegerValue { get; set; }
    public bool BoolValue { get; set; }
    public Guid GuidValue { get; set; }
}

public class CultureSpecialFormatSource
{
    public Half HalfValue { get; set; }
    public Int128 Int128Value { get; set; }
    public UInt128 UInt128Value { get; set; }
    public System.Numerics.BigInteger BigIntegerValue { get; set; }
    public bool BoolValue { get; set; }
    public Guid GuidValue { get; set; }
}

public class CultureSpecialFormatDestination
{
    public string HalfValue { get; set; } = default!;
    public string Int128Value { get; set; } = default!;
    public string UInt128Value { get; set; } = default!;
    public string BigIntegerValue { get; set; } = default!;
    public string BoolValue { get; set; } = default!;
    public string GuidValue { get; set; } = default!;
}
