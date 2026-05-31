namespace Smart.Mapper.Models;

// Record types (D1)
public record RecordSource(int Id, string Name, int Age);

public record RecordDestination(int Id, string Name, int Age);

public record RecordDestinationPartial(int Id, string Name);

// Class with primary constructor (D3)
public class PrimaryCtorSource
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public int Age { get; init; }
}

public class PrimaryCtorDestination(int id, string name, int age)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int Age { get; } = age;
}

// Record with extra settable property
public record RecordWithExtra(int Id, string Name)
{
    public string? Extra { get; set; }
}

public class RecordWithExtraSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Extra { get; set; }
}

// Source for [MapProperty] override test
public class MapPropertyOverrideSource
{
    public int Identifier { get; set; }
    public string FullName { get; set; } = default!;
}

public record MapPropertyOverrideDestination(int Id, string Name);
