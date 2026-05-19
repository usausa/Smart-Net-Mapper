namespace Smart.Mapper;

using System.Collections.Immutable;

/// <summary>
/// Source-shape × Target-shape matrix tests for C4 inline collection emit.
/// </summary>
public class CollectionMatrixMappingTests
{
    public static TheoryData<int> ElementCounts => [0, 1, 10];

    private static MatrixSrcItem[] MakeArray(int count) =>
        Enumerable.Range(1, count).Select(i => new MatrixSrcItem { Value = i }).ToArray();

    private static List<MatrixSrcItem> MakeList(int count) =>
        Enumerable.Range(1, count).Select(i => new MatrixSrcItem { Value = i }).ToList();

    // ── Array source ─────────────────────────────────────────────────────────

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ArrayToList_PreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToListDst();
        TestMappers.MapArrayToList(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
        for (var i = 0; i < count; i++) Assert.Equal(i + 1, dst.Items[i].Value);
    }

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ArrayToArray_PreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToArrayDst();
        TestMappers.MapArrayToArray(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++) Assert.Equal(i + 1, dst.Items[i].Value);
    }

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ArrayToImmutableArray_PreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToImmutableArrayDst();
        TestMappers.MapArrayToImmutableArray(src, dst);
        Assert.Equal(count, dst.Items.Length);
        for (var i = 0; i < count; i++) Assert.Equal(i + 1, dst.Items[i].Value);
    }

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ArrayToHashSet_PreservesElements(int count)
    {
        var src = new MatrixArraySource { Items = MakeArray(count) };
        var dst = new MatrixToHashSetDst();
        TestMappers.MapArrayToHashSet(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
    }

    // ── List source ──────────────────────────────────────────────────────────

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ListToList_PreservesElements(int count)
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

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ListToArray_PreservesElements(int count)
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

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ListToImmutableArray_PreservesElements(int count)
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

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ListToHashSet_PreservesElements(int count)
    {
        var src = new MatrixListSource { Items = MakeList(count) };
        var dst = new MatrixToHashSetDst();
        TestMappers.MapListToHashSet(src, dst);
        Assert.NotNull(dst.Items);
        Assert.Equal(count, dst.Items.Count);
    }

    // ── Null source ──────────────────────────────────────────────────────────

    [Fact]
    public void Map_NullArrayToList_SetsDefault()
    {
        var src = new MatrixArraySource { Items = null };
        var dst = new MatrixToListDst();
        TestMappers.MapArrayToList(src, dst);
        Assert.Null(dst.Items);
    }

    [Fact]
    public void Map_NullListToList_SetsDefault()
    {
        var src = new MatrixListSource { Items = null };
        var dst = new MatrixToListDst();
        TestMappers.MapListToList(src, dst);
        Assert.Null(dst.Items);
    }

    // ── Void (Action) mapper ─────────────────────────────────────────────────

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ArrayToListVoid_PreservesElements(int count)
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

    [Theory, MemberData(nameof(ElementCounts))]
    public void Map_ListToListVoid_PreservesElements(int count)
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
