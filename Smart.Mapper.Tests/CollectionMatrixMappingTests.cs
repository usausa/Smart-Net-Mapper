namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Source-shape × Target-shape matrix tests for C4 inline collection emit.
public class CollectionMatrixMappingTests
{
    public static TheoryData<int> ElementCounts => [0, 1, 10];

#pragma warning disable IDE0028
    private static MatrixSrcItem[] MakeArray(int count) =>
        Enumerable.Range(1, count).Select(i => new MatrixSrcItem { Value = i }).ToArray();
#pragma warning restore IDE0028

#pragma warning disable IDE0028
    private static List<MatrixSrcItem> MakeList(int count) =>
        Enumerable.Range(1, count).Select(i => new MatrixSrcItem { Value = i }).ToList();
#pragma warning restore IDE0028

    // ── Array source ─────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapArrayToListPreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToListDst();
        TestMappers.MapArrayToList(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapArrayToArrayPreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToArrayDst();
        TestMappers.MapArrayToArray(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapArrayToImmutableArrayPreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToImmutableArrayDst();
        TestMappers.MapArrayToImmutableArray(src, dst);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapArrayToHashSetPreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToHashSetDst();
        TestMappers.MapArrayToHashSet(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapArrayToFrozenSetPreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToFrozenSetDst();
        TestMappers.MapArrayToFrozenSet(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        var values = dst.Items.Select(static x => x.Value).OrderBy(static x => x).ToArray();
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, values[i]);
        }
    }

    // ── Memory source ────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapMemoryToListPreservesElements(int count)
    {
        var src = new MatrixMemorySource { Items = MakeArray(count).AsMemory() };
        var dst = new MatrixToListDst();
        TestMappers.MapMemoryToList(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    // ── IReadOnlyList source (IndexedList shape) ─────────────────────────────

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapReadOnlyListToListPreservesElements(int count)
    {
        var src = new MatrixReadOnlyListSource { Items = MakeList(count) };
        var dst = new MatrixToListDst();
        TestMappers.MapReadOnlyListToList(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Fact]
    public void MapNullReadOnlyListToListSetsDefault()
    {
        var src = new MatrixReadOnlyListSource { Items = null };
        var dst = new MatrixToListDst { Items = [] };
        TestMappers.MapReadOnlyListToList(src, dst);
        Assert.Null(dst.Items);
    }

    // ── IReadOnlyCollection source (Count-presized targets) ──────────────────

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapReadOnlyCollectionToImmutableArrayPreservesElements(int count)
    {
        var src = new MatrixReadOnlyCollectionSource { Items = MakeList(count) };
        var dst = new MatrixToImmutableArrayDst();
        TestMappers.MapReadOnlyCollectionToImmutableArray(src, dst);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapReadOnlyCollectionToHashSetPreservesElements(int count)
    {
        var src = new MatrixReadOnlyCollectionSource { Items = MakeList(count) };
        var dst = new MatrixToHashSetDst();
        TestMappers.MapReadOnlyCollectionToHashSet(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
    }

    // ── List source ──────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapListToListPreservesElements(int count)
    {
        var src = new MatrixListSource { Items = MakeList(count) };
        var dst = new MatrixToListDst();
        TestMappers.MapListToList(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapListToArrayPreservesElements(int count)
    {
        var src = new MatrixListSource { Items = MakeList(count) };
        var dst = new MatrixToArrayDst();
        TestMappers.MapListToArray(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapListToImmutableArrayPreservesElements(int count)
    {
        var src = new MatrixListSource { Items = MakeList(count) };
        var dst = new MatrixToImmutableArrayDst();
        TestMappers.MapListToImmutableArray(src, dst);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapListToHashSetPreservesElements(int count)
    {
        var src = new MatrixListSource { Items = MakeList(count) };
        var dst = new MatrixToHashSetDst();
        TestMappers.MapListToHashSet(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
    }

    // ── IEnumerable source (EmitInlineTargetBuildFromEnumerable) ────────────

    private static IEnumerable<MatrixSrcItem> MakeEnumerable(int count) =>
        Enumerable.Range(1, count).Select(i => new MatrixSrcItem { Value = i });

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapEnumerableToArrayPreservesElements(int count)
    {
        var src = new MatrixEnumerableSource { Items = MakeEnumerable(count) };
        var dst = new MatrixToArrayDst();
        TestMappers.MapEnumerableToArray(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapEnumerableToListPreservesElements(int count)
    {
        var src = new MatrixEnumerableSource { Items = MakeEnumerable(count) };
        var dst = new MatrixToListDst();
        TestMappers.MapEnumerableToList(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapEnumerableToImmutableArrayPreservesElements(int count)
    {
        var src = new MatrixEnumerableSource { Items = MakeEnumerable(count) };
        var dst = new MatrixToImmutableArrayDst();
        TestMappers.MapEnumerableToImmutableArray(src, dst);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapEnumerableToHashSetPreservesElements(int count)
    {
        var src = new MatrixEnumerableSource { Items = MakeEnumerable(count) };
        var dst = new MatrixToHashSetDst();
        TestMappers.MapEnumerableToHashSet(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
    }

    [Fact]
    public void MapNullEnumerableToArraySetsDefault()
    {
        var src = new MatrixEnumerableSource { Items = null };
        var dst = new MatrixToArrayDst();
        TestMappers.MapEnumerableToArray(src, dst);
        Assert.Null(dst.Items);
    }

    // ── Custom CollectionConverter + array destination (helper path) ─────────

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapListToArrayWithConverterPreservesElements(int count)
    {
        var src = new MatrixListSource { Items = MakeList(count) };
        var dst = new MatrixConverterArrayDst();
        TestMappers.MapListToArrayWithConverter(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Fact]
    public void MapNullListToArrayWithConverterSetsDefault()
    {
        var src = new MatrixListSource { Items = null };
        var dst = new MatrixConverterArrayDst();
        TestMappers.MapListToArrayWithConverter(src, dst);
        Assert.Null(dst.Items);
    }

    // ── Null source ──────────────────────────────────────────────────────────

    [Fact]
    public void MapNullArrayToListSetsDefault()
    {
        var src = new MatrixArraySource { Items = null };
        var dst = new MatrixToListDst();
        TestMappers.MapArrayToList(src, dst);
        Assert.Null(dst.Items);
    }

    [Fact]
    public void MapNullListToListSetsDefault()
    {
        var src = new MatrixListSource { Items = null };
        var dst = new MatrixToListDst();
        TestMappers.MapListToList(src, dst);
        Assert.Null(dst.Items);
    }

    // ── Void (Action) mapper ─────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapArrayToListVoidPreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixVoidDst();
        TestMappers.MapArrayToListVoid(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ElementCounts))]
    public void MapListToListVoidPreservesElements(int count)
    {
        var src = new MatrixListSource { Items = MakeList(count) };
        var dst = new MatrixVoidDst();
        TestMappers.MapListToListVoid(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i + 1, dst.Items[i].Value);
        }
    }
}
