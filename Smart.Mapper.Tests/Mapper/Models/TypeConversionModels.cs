namespace Smart.Mapper;

public class TypeConversionSource
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
}

public class TypeConversionDestination
{
    public string IntValue { get; set; } = string.Empty;
    public int StringValue { get; set; }
}

public class ExtendedTypeConversionSource
{
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }
    public bool BoolValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public string GuidString { get; set; } = string.Empty;
}

public class ExtendedTypeConversionDestination
{
    public string LongValue { get; set; } = string.Empty;
    public string DoubleValue { get; set; } = string.Empty;
    public string DecimalValue { get; set; } = string.Empty;
    public string BoolValue { get; set; } = string.Empty;
    public string DateTimeValue { get; set; } = string.Empty;
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
    public string DateOnlyValue { get; set; } = string.Empty;
    public string TimeOnlyValue { get; set; } = string.Empty;
    public string DateTimeOffsetValue { get; set; } = string.Empty;
    public string TimeSpanValue { get; set; } = string.Empty;
}

// B2: Half, Int128, UInt128 conversions
public class ModernNumericConversionSource
{
    public Half HalfValue { get; set; }
    public int IntValue { get; set; }
}

public class ModernNumericConversionDestination
{
    public string HalfValue { get; set; } = string.Empty;
    public Half IntValue { get; set; }
}

// E2: Case-insensitive name comparison
public class CaseInsensitiveSource
{
    public int userid { get; set; }
    public string username { get; set; } = string.Empty;
}

public class CaseInsensitiveDestination
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}
