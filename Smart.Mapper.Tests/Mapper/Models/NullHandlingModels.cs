namespace Smart.Mapper.Models;

public class NullableNestedSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NullableNestedSource
{
    public NullableNestedSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class NullableNestedFlatDestination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int DirectValue { get; set; }
}

public class NullablePropertySource
{
    public string? NullableName { get; set; }
    public int? NullableInt { get; set; }
    public string NonNullableName { get; set; } = string.Empty;
}

public class NullablePropertyDestination
{
    public string? NullableName { get; set; }
    public int? NullableInt { get; set; }
    public string NonNullableName { get; set; } = "default";
}

public class NullableToNonNullableSource
{
    public string? Name { get; set; }
}

public class NullableToNonNullableDestination
{
    public string Name { get; set; } = "original";
}

public class NullableIntToStringSource
{
    public int? IntValue { get; set; }
}

public class NullableIntToStringDestination
{
    public string IntValue { get; set; } = "original";
}

// A1: NullSubstitute
public class NullSubstituteSource
{
    public string? Name { get; set; }
    public int? Count { get; set; }
}

public class NullSubstituteDestination
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
