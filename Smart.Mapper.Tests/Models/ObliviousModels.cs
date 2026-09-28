#pragma warning disable CA1002
#pragma warning disable CA2227
namespace Smart.Mapper.Models;

// Types declared with nullable annotations disabled, whose references may hold null all the same, so they are taken as
// nullable: NullValue and NullBehavior.Skip apply to them, and a null one goes to a converter, a condition or a mapper
// that does not take null no more than a nullable one does
#nullable disable
public class ObliviousSource
{
    public string Name { get; set; }

    public string Note { get; set; }
}

public class ObliviousDestination
{
    public string Name { get; set; }

    public string Note { get; set; }
}

public class ObliviousItem
{
    public int Value { get; set; }
}

public class ObliviousGraphSource
{
    public string Name { get; set; }

    public ObliviousItem Child { get; set; }

    public List<ObliviousItem> Items { get; set; }

    public ObliviousGraphSource Parent { get; set; }
}
#nullable restore

public class ObliviousItemDestination
{
    public int Value { get; set; }
}

public class ObliviousGraphDestination
{
    public string Name { get; set; } = "init";

    public ObliviousItemDestination? Child { get; set; } = new() { Value = -1 };

    public List<ObliviousItemDestination?>? Items { get; set; }

    public string? ParentName { get; set; } = "init";
}

// The source of [MapNested] may be null, and the mapper takes null
public class NestedNullChildSource
{
    public int Value { get; set; }
}

public class NestedNullChildDestination
{
    public int Value { get; set; }
}

public class NestedNullSource
{
    public NestedNullChildSource? Child { get; set; }

    public NestedNullChildSource? Other { get; set; }
}

public class NestedNullDestination
{
    public NestedNullChildDestination Child { get; set; } = new() { Value = -2 };

    public NestedNullChildDestination Other { get; set; } = new() { Value = -2 };
}
