namespace Smart.Mapper.Models;

public class BasicSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
}

public class BasicDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
}

public class DifferentPropertySource
{
    public int SourceId { get; set; }
    public string SourceName { get; set; } = default!;
}

public class DifferentPropertyDestination
{
    public int DestId { get; set; }
    public string DestName { get; set; } = default!;
}

public class IgnoreSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Secret { get; set; } = default!;
}

public class IgnoreDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Secret { get; set; } = default!;
}

public class MultiPropertySource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int Value { get; set; }
    public string Description { get; set; } = default!;
}

public class MultiPropertyDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int Value { get; set; }
    public string Description { get; set; } = default!;
}

public class ConstantSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class ConstantDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BeforeAfterSource
{
    public int Value { get; set; }
    public string Text { get; set; } = default!;
}

public class BeforeAfterDestination
{
    public int Value { get; set; }
    public string Text { get; set; } = default!;
    public bool BeforeMapCalled { get; set; }
    public bool AfterMapCalled { get; set; }
}

public class AutoMapSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int Value { get; set; }
}

public class AutoMapDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int Value { get; set; }
}

public class CustomMappingContext
{
    public bool BeforeMapCalled { get; set; }
    public bool AfterMapCalled { get; set; }
    public string ContextValue { get; set; } = default!;
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
