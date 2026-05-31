namespace Smart.Mapper.Models;

public class TypeConversionSource
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = default!;
}

public class TypeConversionDestination
{
    public string IntValue { get; set; } = default!;
    public int StringValue { get; set; }
}

public class ExtendedTypeConversionSource
{
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }
    public bool BoolValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public string GuidString { get; set; } = default!;
}

public class ExtendedTypeConversionDestination
{
    public string LongValue { get; set; } = default!;
    public string DoubleValue { get; set; } = default!;
    public string DecimalValue { get; set; } = default!;
    public string BoolValue { get; set; } = default!;
    public string DateTimeValue { get; set; } = default!;
    public Guid GuidString { get; set; }
}

public class NumericConversionSource
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
}

public class NumericConversionDestination
{
    public long IntValue { get; set; }
    public int LongValue { get; set; }
    public decimal DoubleValue { get; set; }
}

// B1: DateOnly, TimeOnly, DateTimeOffset, TimeSpan conversions
public class DateTimeTypeConversionSource
{
    public DateOnly DateOnlyValue { get; set; }
    public TimeOnly TimeOnlyValue { get; set; }
    public DateTimeOffset DateTimeOffsetValue { get; set; }
    public TimeSpan TimeSpanValue { get; set; }
}

public class DateTimeTypeToStringDestination
{
    public string DateOnlyValue { get; set; } = default!;
    public string TimeOnlyValue { get; set; } = default!;
    public string DateTimeOffsetValue { get; set; } = default!;
    public string TimeSpanValue { get; set; } = default!;
}

// B2: Half, Int128, UInt128 conversions
public class ModernNumericConversionSource
{
    public Half HalfValue { get; set; }
    public int IntValue { get; set; }
}

public class ModernNumericConversionDestination
{
    public string HalfValue { get; set; } = default!;
    public Half IntValue { get; set; }
}

// E2: Case-insensitive name comparison
public class CaseInsensitiveSource
{
    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable SA1300
    public int userid { get; set; }
    public string username { get; set; } = default!;
#pragma warning restore SA1300
#pragma warning restore IDE1006
    // ReSharper restore InconsistentNaming
}

public class CaseInsensitiveDestination
{
    public int UserId { get; set; }
    public string UserName { get; set; } = default!;
}
