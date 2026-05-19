#pragma warning disable CA1819
#pragma warning disable CA2227
namespace Smart.Mapper;

using System.Collections.Immutable;

public class CollectionSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
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
    public string Name { get; set; } = string.Empty;
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
    public string Name { get; set; } = string.Empty;
}

public class CustomCollectionConverterSource
{
    public List<CustomCollectionConverterSourceChild>? Children { get; set; }
}

public class CustomCollectionConverterDestChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
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
    public string Extra { get; set; } = string.Empty;
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
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
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
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
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
    public string Name { get; set; } = string.Empty;
}

public class ImmutableCollectionDestinationChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
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
    public string Name { get; set; } = string.Empty;
}

public class InPlaceDestinationChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
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
    public string Name { get; set; } = string.Empty;
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
    public System.Collections.Immutable.ImmutableArray<MatrixDstItem> Items { get; set; }
}

public class MatrixToHashSetDst
{
    public System.Collections.Generic.HashSet<MatrixDstItem>? Items { get; set; }
}

public class MatrixVoidDst
{
    public List<MatrixDstItem>? Items { get; set; }
}
