namespace Smart.Mapper.Models;

public class BasicSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BasicDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DifferentPropertySource
{
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
}

public class DifferentPropertyDestination
{
    public int DestId { get; set; }
    public string DestName { get; set; } = string.Empty;
}

public class IgnoreSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public class IgnoreDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public class MultiPropertySource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class MultiPropertyDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ConstantSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ConstantDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BeforeAfterSource
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class BeforeAfterDestination
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool BeforeMapCalled { get; set; }
    public bool AfterMapCalled { get; set; }
}

public class AutoMapSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class AutoMapDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class CustomMappingContext
{
    public bool BeforeMapCalled { get; set; }
    public bool AfterMapCalled { get; set; }
    public string ContextValue { get; set; } = string.Empty;
}

public class ConditionSource
{
    public int Value { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}

public class ConditionDestination
{
    public int Value { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}
