#pragma warning disable CA1002
#pragma warning disable CA1815
#pragma warning disable CA1819
#pragma warning disable CA2227
namespace Smart.Mapper.Models;

using System.Collections.Frozen;
using System.Collections.Immutable;

public class CollectionSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class CollectionSource
{
    public CollectionSourceChild[]? Children { get; set; }
    public List<CollectionSourceChild>? Items { get; set; }
    public int DirectValue { get; set; }
}

public class CollectionDestinationChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class CollectionDestination
{
    public List<CollectionDestinationChild>? Children { get; set; }
    public CollectionDestinationChild[]? Items { get; set; }
    public int DirectValue { get; set; }
}

public class CustomCollectionConverterSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class CustomCollectionConverterSource
{
    public List<CustomCollectionConverterSourceChild>? Children { get; set; }
}

public class CustomCollectionConverterDestChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class CustomCollectionConverterDestination
{
    public IReadOnlyList<CustomCollectionConverterDestChild>? Children { get; set; }
}

public static class TestCollectionConverter
{
    public static TDest[] ToArray<TSource, TDest>(IEnumerable<TSource>? source, Func<TSource, TDest> mapper)
    {
        return DefaultCollectionConverter.ToArray(source, mapper)!;
    }

    public static List<TDest> ToList<TSource, TDest>(IEnumerable<TSource>? source, Func<TSource, TDest> mapper)
    {
        return DefaultCollectionConverter.ToList(source, mapper)!;
    }

    public static IReadOnlyList<TDest> ToReadOnlyList<TSource, TDest>(IEnumerable<TSource>? source, Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return [];
        }
        return source.Select(mapper).ToList().AsReadOnly();
    }
}

public class VoidMapperSourceChild
{
    public int Id { get; set; }
}

public class VoidMapperSource
{
    public VoidMapperSourceChild[]? Children { get; set; }
}

public class VoidMapperDestinationChild
{
    public int Id { get; set; }
    public string Extra { get; set; } = default!;
}

public class VoidMapperDestination
{
    public List<VoidMapperDestinationChild>? Children { get; set; }
}

public class CustomCollectionSource
{
    public CollectionSourceChild[]? Numbers { get; set; }
}

public class CustomCollectionDestination
{
    public List<CollectionDestinationChild>? Numbers { get; set; }
}

public static class TestCustomCollectionConverter
{
    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        return source?.Select(x =>
        {
            var mapped = mapper(x);
            if (mapped is CollectionDestinationChild child)
            {
                child.Id *= 2;
            }
            return mapped;
        }).ToArray();
    }

    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        return source?.Select(x =>
        {
            var mapped = mapper(x);
            if (mapped is CollectionDestinationChild child)
            {
                child.Id *= 2;
            }
            return mapped;
        }).ToList();
    }
}

// C3: ImmutableArray / ImmutableList / HashSet collection targets
public class ImmutableCollectionSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class ImmutableCollectionDestinationChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class ImmutableCollectionSource
{
    public List<ImmutableCollectionSourceChild>? Items { get; set; }
    public List<ImmutableCollectionSourceChild>? ListItems { get; set; }
    public List<ImmutableCollectionSourceChild>? SetItems { get; set; }
}

public class ImmutableCollectionDestination
{
    public ImmutableArray<ImmutableCollectionDestinationChild> Items { get; set; }
    public ImmutableList<ImmutableCollectionDestinationChild>? ListItems { get; set; }
    public HashSet<ImmutableCollectionDestinationChild>? SetItems { get; set; }
}

// C2: InPlace collection update
public class InPlaceSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class InPlaceDestinationChild
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class InPlaceSource
{
    public List<InPlaceSourceChild>? Items { get; set; }
}

public class InPlaceDestination
{
    public List<InPlaceDestinationChild>? Items { get; set; }
}

// D4: readonly struct
public readonly struct ReadOnlyStructSource
{
    public int Id { get; init; }
    public string Name { get; init; }
}

public class ReadOnlyStructDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

// Parameter modifiers: a mutable struct source passed by in, and a struct destination passed by ref
public struct MutableStructSource
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public struct MutableStructDestination
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Total { get; set; }
}

// Custom parameter handed to hooks by in / ref readonly, or declared nullable
public class HookContext
{
    public string Prefix { get; set; } = default!;
    public bool CopyDescription { get; set; }
}

// C4-δ: Custom CollectionConverter + array destination
public class MatrixConverterArrayDst
{
    public MatrixDstItem[]? Items { get; set; }
}

// C4-β/γ: Collection matrix test models
public class MatrixSrcItem
{
    public int Value { get; set; }
}

public class MatrixDstItem
{
    public int Value { get; set; }
}

#pragma warning disable CA1819
public class MatrixArraySource
{
    public MatrixSrcItem[]? Items { get; set; }
}

public class MatrixListSource
{
    public List<MatrixSrcItem>? Items { get; set; }
}

public class MatrixEnumerableSource
{
    public IEnumerable<MatrixSrcItem>? Items { get; set; }
}

public class MatrixToListDst
{
    public List<MatrixDstItem>? Items { get; set; }
}

public class MatrixToArrayDst
{
    public MatrixDstItem[]? Items { get; set; }
}

public class MatrixToImmutableArrayDst
{
    public ImmutableArray<MatrixDstItem> Items { get; set; }
}

public class MatrixToHashSetDst
{
    public HashSet<MatrixDstItem>? Items { get; set; }
}

// Regression: Memory<T> source (previously rejected as non-collection) and FrozenSet target
// (previously emitted a bare .ToFrozenSet() that did not compile).
public class MatrixMemorySource
{
    public Memory<MatrixSrcItem> Items { get; set; }
}

// IReadOnlyList ソースはインデクサベースの反復 (IndexedList 形状) で処理される。
// IReadOnlyList sources are iterated via the indexer (IndexedList shape).
public class MatrixReadOnlyListSource
{
    public IReadOnlyList<MatrixSrcItem>? Items { get; set; }
}

// IReadOnlyCollection ソースは Count を使った presize 付き foreach で処理される。
// IReadOnlyCollection sources are iterated via foreach with Count-based presizing.
public class MatrixReadOnlyCollectionSource
{
    public IReadOnlyCollection<MatrixSrcItem>? Items { get; set; }
}

public class MatrixToFrozenSetDst
{
    public FrozenSet<MatrixDstItem>? Items { get; set; }
}

public class MatrixVoidDst
{
    public List<MatrixDstItem>? Items { get; set; }
}

// Regression: several collections that are never null in one mapper (the generated locals were
// redeclared in the same scope, CS0128), next to a nullable collection and a nested object.
public class MultiCollectionSource
{
    public IReadOnlyList<MatrixSrcItem> Lines { get; set; } = [];
    public List<MatrixSrcItem> Items { get; set; } = [];
    public MatrixSrcItem[] Values { get; set; } = [];
    public IEnumerable<MatrixSrcItem> Sequence { get; set; } = [];
    public List<MatrixSrcItem>? Optional { get; set; }
    public NestedObjectSourceChild? Child { get; set; }
}

public class MultiCollectionDestination
{
    public IReadOnlyList<MatrixDstItem> Lines { get; set; } = [];
    public List<MatrixDstItem> Items { get; set; } = [];
    public MatrixDstItem[] Values { get; set; } = [];
    public HashSet<MatrixDstItem> Sequence { get; set; } = [];
    public List<MatrixDstItem>? Optional { get; set; }
    public NestedObjectDestinationChild? Child { get; set; }
}

// Element and nested mappers filling a struct instance through ref
public struct PointSource
{
    public int X { get; set; }
    public int Y { get; set; }
}

public struct PointDestination
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class PathSource
{
    public List<PointSource> Points { get; set; } = [];
    public IEnumerable<PointSource> Route { get; set; } = [];
    public PointSource Origin { get; set; }
}

public class PathDestination
{
    public List<PointDestination> Points { get; set; } = default!;
    public PointDestination[] Route { get; set; } = [];
    public PointDestination Origin { get; set; }
}

// Collection converter taking the source collection and the element mapper by in
internal static class InCollectionConverter
{
    public static List<TDest> ToList<TSource, TDest>(in IEnumerable<TSource>? source, in Func<TSource, TDest> mapper) =>
        source is null ? [] : [.. source.Select(mapper)];
}

// Converter classes nested in another class, found through the typeof of the attribute
public class NestedConverterSource
{
    public int Value { get; set; }
    public List<int> Items { get; set; } = [];
}

public class NestedConverterDestination
{
    public string Value { get; set; } = default!;
    public List<string> Items { get; set; } = default!;
}

internal static class ConverterHost
{
    internal static class NestedValueConverter
    {
        public static string ConvertToString(int source) => $"N:{source.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        public static TDestination Convert<TSource, TDestination>(TSource source) => DefaultValueConverter.Convert<TSource, TDestination>(source);
    }

    // Builds the list in the order its type argument arranges, the reverse one, so that a test can tell it was called
    internal static class NestedCollectionConverter<TOrder>
        where TOrder : IElementOrder
    {
        public static List<TDest> ToList<TSource, TDest>(IEnumerable<TSource> source, Func<TSource, TDest> mapper) =>
            [.. TOrder.Arrange(source.Select(mapper))];
    }

    // The order a generic collection converter builds its list in, given as its type argument
    internal interface IElementOrder
    {
        static abstract IEnumerable<T> Arrange<T>(IEnumerable<T> items);
    }

    internal readonly struct ReverseElementOrder : IElementOrder
    {
        public static IEnumerable<T> Arrange<T>(IEnumerable<T> items) => items.Reverse();
    }
}

// InPlace targets: a get-only one filled when it holds an instance, and a collection class of its own
// created with its constructor when null
public class InPlaceTargetSource
{
    public List<MatrixSrcItem> Items { get; set; } = [];
}

public class InPlaceGetOnlyDestination
{
    public List<MatrixDstItem> Items { get; } = [new() { Value = -1 }];
}

public class InPlaceNullGetOnlyDestination
{
    public InPlaceNullGetOnlyDestination(List<MatrixDstItem>? items = null)
    {
        Items = items;
    }

    public List<MatrixDstItem>? Items { get; }
}

public class InPlaceObservableDestination
{
    public System.Collections.ObjectModel.ObservableCollection<MatrixDstItem>? Items { get; set; }
}

// Collection classes of their own, as the source and as the target
public class MatrixSrcItemList : List<MatrixSrcItem>;

public class MatrixDstItemList : List<MatrixDstItem>;

public class CollectionClassSource
{
    public MatrixSrcItemList Items { get; set; } = [];
}

public class CollectionClassDestination
{
    public MatrixDstItemList Items { get; set; } = [];
    public System.Collections.ObjectModel.ObservableCollection<MatrixDstItem> Observed { get; set; } = [];
}

// A void mapper never constructs: a constructor taking members without a setter does not keep it from
// filling the others, and the get-only collection the constructor takes is refilled in place
public class ConstructedTargetSource
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public List<MatrixSrcItem> Items { get; set; } = [];
}

public class ConstructedTargetDestination
{
    public ConstructedTargetDestination(string id, List<MatrixDstItem> items)
    {
        Id = id;
        Items = items;
    }

    public string Id { get; }
    public string Name { get; set; } = default!;
    public List<MatrixDstItem> Items { get; }
}

// Elements that may be null, mapped by a mapper declared to take null, and a member it maps into a target not
// annotated as nullable
public class NullableElementSource
{
    public List<MatrixSrcItem?> Items { get; set; } = [];

    public MatrixSrcItem Head { get; set; } = new();
}

public class NullableElementDestination
{
    public List<MatrixDstItem?> Items { get; set; } = default!;

    public MatrixDstItem?[] Array { get; set; } = [];

    public MatrixDstItem Head { get; set; } = new();
}

// Dictionary interface targets, which get a Dictionary<TKey, TValue>
public class DictionarySource
{
    public Dictionary<string, MatrixSrcItem> Items { get; set; } = [];
}

public class DictionaryDestination
{
    public IReadOnlyDictionary<string, MatrixDstItem> Items { get; set; } = new Dictionary<string, MatrixDstItem>();

    public IDictionary<string, MatrixDstItem> Editable { get; set; } = default!;
}

// Mappers matched through conversions: the result into an interface it implements, the source into a base class,
// and a nullable struct as the value it holds
public interface IConvertedChild
{
    int Value { get; }
}

public class ConvertedChild : IConvertedChild
{
    public int Value { get; set; }
}

public class ConvertedBase
{
    public int Value { get; set; }
}

public class ConvertedDerived : ConvertedBase;

public record struct ConvertedPoint(int X);

public record struct ConvertedPointDto(int X);

public class ConversionSource
{
    public ConvertedDerived Child { get; set; } = new();

    public ConvertedPoint? Point { get; set; }

    public List<ConvertedDerived> Children { get; set; } = [];

    public List<ConvertedPoint?> Points { get; set; } = [];
}

public class ConversionDestination
{
    public IConvertedChild? Child { get; set; }

    public ConvertedPointDto Point { get; set; }

    public List<IConvertedChild> Children { get; set; } = default!;

    public List<ConvertedPointDto?> Points { get; set; } = default!;
}
