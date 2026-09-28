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

// Dotted target paths through members the mapper cannot assign write into the instances they hold
public class HeldPathSource
{
    public int Value { get; set; }
    public string? Text { get; set; }
}

public class HeldPathChild
{
    public int Value { get; set; }
    public string? Text { get; set; }
    public HeldPathLeaf? Leaf { get; set; }
}

public class HeldPathLeaf
{
    public int Value { get; set; }
}

public class HeldPathDestination
{
    public HeldPathDestination(HeldPathChild? missing = null)
    {
        Missing = missing;
    }

    public HeldPathChild Held { get; } = new();
    public HeldPathChild? Missing { get; }
    public HeldPathChild Private { get; private set; } = new();
    public HeldPathChild Init { get; init; } = new();
}

// Dotted target paths through a struct property, types the mapper cannot create, and init-only members an
// object initializer sets
public record struct PathPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

public abstract class PathAbstractChild
{
    public int Value { get; set; }
}

public class PathConcreteChild : PathAbstractChild;

public class PathInitChild
{
    public int Value { get; init; }
    public int Other { get; init; }
    public int Plain { get; set; }
}

public class TargetPathSource
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class TargetPathDestination
{
    public PathPoint Point { get; set; }
    public PathAbstractChild? Abstract { get; set; }
    public PathInitChild? Created { get; set; }
    public PathInitChild Held { get; } = new();
}

public class PathInitDestination
{
    public PathInitChild Child { get; set; } = default!;
    public int Top { get; set; }
}

// [MapFrom] through members that may be null, and values of [MapUsing] / [MapFrom] a type converts to implicitly
public class NullablePathLeaf
{
    public string? Zip { get; set; }
    public int Code { get; set; }
}

public class NullablePathMid
{
    public NullablePathLeaf? Leaf { get; set; }
}

public class NullablePathSource
{
    public NullablePathMid? Mid { get; set; }
    public IReadOnlyList<int>? Items { get; set; }
    public string? Nick { get; set; }

    public string? GetAlias() => Nick;
}

public class NullablePathDestination
{
    public string? Zip { get; set; } = "keep";
    public int Code { get; set; } = -1;
    public long Count { get; set; } = -1;
    public string Nick { get; set; } = "keep";
    public decimal? Total { get; set; }
}

public record NullablePathRecord(string Zip, int? Code);
