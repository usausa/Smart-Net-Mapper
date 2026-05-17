namespace Smart.Mapper;

// B4: Culture / Format mapping models

public class CultureFormatSource
{
    public double Amount { get; set; }
    public DateTime EventDate { get; set; }
    public decimal Price { get; set; }
}

public class CultureFormatDestination
{
    public string Amount { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
}

public class CultureParseSource
{
    public string Amount { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
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
    public string ValueA { get; set; } = string.Empty;
    public string ValueB { get; set; } = string.Empty;
}
