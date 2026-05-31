#pragma warning disable CA1024
#pragma warning disable CA1822
namespace Smart.Mapper.Models;

public class FlatSource
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
}

public class DestinationChild
{
    public int Value { get; set; }
}

public class NestedDestination
{
    public DestinationChild? Child1 { get; set; }
    public DestinationChild? Child2 { get; set; }
    public DestinationChild? Child3 { get; set; }
}

public class NestedSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class NestedSource
{
    public NestedSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class FlatDestination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; } = default!;
    public int DirectValue { get; set; }
}

public class DeepNestedChild
{
    public int Value { get; set; }
}

public class DeepNestedParent
{
    public DeepNestedChild? Inner { get; set; }
}

public class DeepNestedDestination
{
    public DeepNestedParent? Outer { get; set; }
}

public class DeepSource
{
    public int DeepValue { get; set; }
}

public class DeepSourceInner
{
    public int Value { get; set; }
    public string Name { get; set; } = default!;
}

public class DeepSourceOuter
{
    public DeepSourceInner? Inner { get; set; }
}

public class DeepNestedSource
{
    public DeepSourceOuter? Outer { get; set; }
    public int DirectValue { get; set; }
}

public class DeepFlatDestination
{
    public int OuterInnerValue { get; set; }
    public string OuterInnerName { get; set; } = default!;
    public int DirectValue { get; set; }
}

public class NestedObjectSourceChild
{
    public int Value { get; set; }
    public string Text { get; set; } = default!;
}

public class NestedObjectSource
{
    public NestedObjectSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class NestedObjectDestinationChild
{
    public int Value { get; set; }
    public string Text { get; set; } = default!;
}

public class NestedObjectDestination
{
    public NestedObjectDestinationChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class MapFromPathNested
{
    public int Value { get; set; }
}

public class MapFromPathSource
{
    public MapFromPathNested Nested { get; set; } = new();

    public int GetItemCount() => 42;
}

public class MapFromPathDestination
{
    public int ItemCount { get; set; }
    public int NestedValue { get; set; }
}
