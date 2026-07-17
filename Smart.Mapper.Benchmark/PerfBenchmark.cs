#pragma warning disable CA2227
namespace Smart.Mapper.Benchmark;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;

// emit 改善(enum switch 化 / InPlace 直接呼び出し / IReadOnlyList インデクサ反復)の
// 前後比較用ベンチマーク。OldEmit 系メソッドは改善前の生成コードを手書きで再現したもの。
// Before/after benchmarks for the emit improvements (enum switch, InPlace direct calls,
// IReadOnlyList indexer iteration). OldEmit methods reproduce the previous generated code by hand.

// -- Models --

public enum PerfStatus
{
    None = 0,
    Active = 1,
    Inactive = 2,
    Pending = 3,
    Archived = 4
}

public sealed class EnumPerfSource
{
    public PerfStatus Status { get; set; }
    public string StatusText { get; set; } = default!;
}

public sealed class EnumPerfDestination
{
    public string Status { get; set; } = default!;
    public PerfStatus StatusText { get; set; }
}

public sealed class PerfItemSource
{
    public int Id { get; set; }
}

public sealed class PerfItemDestination
{
    public int Id { get; set; }
}

public sealed class InPlacePerfSource
{
    public List<PerfItemSource> Items { get; set; } = [];
}

public sealed class InPlacePerfDestination
{
    public List<PerfItemDestination>? Items { get; set; }
}

public sealed class RoListPerfSource
{
    public IReadOnlyList<PerfItemSource>? Items { get; set; }
}

public sealed class RoListPerfDestination
{
    public List<PerfItemDestination>? Items { get; set; }
}

// -- Mappers --

internal static partial class PerfBenchmarkMappers
{
    // enum → string / string → enum (switch emit)
    [Mapper]
    public static partial EnumPerfDestination MapEnum(EnumPerfSource source);

    [Mapper]
    public static partial PerfItemDestination MapPerfItem(PerfItemSource source);

    // InPlace: concrete List<T> destination (direct Clear/Add emit)
    [Mapper]
    [MapCollection(nameof(InPlacePerfDestination.Items), nameof(InPlacePerfSource.Items), Mapper = nameof(MapPerfItem), Strategy = CollectionStrategy.InPlace)]
    public static partial void MapInPlace(InPlacePerfSource source, InPlacePerfDestination destination);

    // IReadOnlyList source (IndexedList shape: indexer iteration + SetCount list build)
    [Mapper]
    [MapCollection(nameof(RoListPerfDestination.Items), nameof(RoListPerfSource.Items), Mapper = nameof(MapPerfItem))]
    public static partial void MapReadOnlyList(RoListPerfSource source, RoListPerfDestination destination);
}

// ==========================================================================
// Perf 1: enum ⇔ string conversion emit
// ==========================================================================

[Config(typeof(BenchmarkConfig))]
[BenchmarkCategory("PerfEnum")]
public class EnumMapBenchmark
{
    private const int N = 1000;

    private EnumPerfSource source = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new() { Status = PerfStatus.Pending, StatusText = "Archived" };
    }

    // 旧 emit: Enum.ToString / Enum.Parse
    // Old emit: Enum.ToString / Enum.Parse
    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public EnumPerfDestination OldEmit()
    {
        var src = source;
        EnumPerfDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new()
            {
                Status = src.Status.ToString(),
                StatusText = Enum.Parse<PerfStatus>(src.StatusText)
            };
        }
        return ret;
    }

    // 新 emit: switch (定数文字列 / 文字列 switch)
    // New emit: switch (constant strings / string switch)
    [Benchmark(OperationsPerInvoke = N)]
    public EnumPerfDestination Generated()
    {
        var src = source;
        EnumPerfDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = PerfBenchmarkMappers.MapEnum(src);
        }
        return ret;
    }
}

// ==========================================================================
// Perf 2: InPlace collection emit (interface dispatch vs direct List calls)
// ==========================================================================

[Config(typeof(BenchmarkConfig))]
[BenchmarkCategory("PerfInPlace")]
public class InPlaceMapBenchmark
{
    private const int N = 100;

    private InPlacePerfSource source = default!;
    private InPlacePerfDestination destinationOld = default!;
    private InPlacePerfDestination destinationNew = default!;

    [Params(100)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = new() { Items = [.. Enumerable.Range(1, Count).Select(static i => new PerfItemSource { Id = i })] };
        destinationOld = new() { Items = [] };
        destinationNew = new() { Items = [] };
    }

    // 旧 emit: ICollection<T> キャスト経由の Clear/Add
    // Old emit: Clear/Add through an ICollection<T> cast
    // CA1859: the interface-typed local is the point of this baseline (it reproduces the old emit).
#pragma warning disable CA1859
    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public InPlacePerfDestination OldEmit()
    {
        var src = source;
        var dst = destinationOld;
        for (var i = 0; i < N; i++)
        {
            dst.Items ??= [];
            ((ICollection<PerfItemDestination>)dst.Items).Clear();
            var srcSpan = CollectionsMarshal.AsSpan(src.Items);
            var dstColl = (ICollection<PerfItemDestination>)dst.Items;
            for (var j = 0; j < srcSpan.Length; j++)
            {
                dstColl.Add(PerfBenchmarkMappers.MapPerfItem(srcSpan[j]));
            }
        }
        return dst;
    }
#pragma warning restore CA1859

    // 新 emit: List<T> 直接の Clear/Add
    // New emit: direct List<T> Clear/Add
    [Benchmark(OperationsPerInvoke = N)]
    public InPlacePerfDestination Generated()
    {
        var src = source;
        var dst = destinationNew;
        for (var i = 0; i < N; i++)
        {
            PerfBenchmarkMappers.MapInPlace(src, dst);
        }
        return dst;
    }
}

// ==========================================================================
// Perf 3: IReadOnlyList source emit (enumerator vs indexer + SetCount)
// ==========================================================================

[Config(typeof(BenchmarkConfig))]
[BenchmarkCategory("PerfRoList")]
public class ReadOnlyListMapBenchmark
{
    private const int N = 100;

    private RoListPerfSource source = default!;

    [Params(100)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = new() { Items = [.. Enumerable.Range(1, Count).Select(static i => new PerfItemSource { Id = i })] };
    }

    // 旧 emit: IReadOnlyCollection 形状 (foreach + Add)
    // Old emit: ReadOnlyCollection shape (foreach + Add)
    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public RoListPerfDestination OldEmit()
    {
        var src = source;
        RoListPerfDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new();
            if (src.Items is null)
            {
                ret.Items = default!;
            }
            else
            {
                var srcColl = src.Items!;
                var list = new List<PerfItemDestination>(srcColl.Count);
                foreach (var item in srcColl)
                {
                    list.Add(PerfBenchmarkMappers.MapPerfItem(item));
                }
                ret.Items = list;
            }
        }
        return ret;
    }

    // 新 emit: IndexedList 形状 (インデクサ + SetCount + span 書き込み)
    // New emit: IndexedList shape (indexer + SetCount + span write)
    [Benchmark(OperationsPerInvoke = N)]
    public RoListPerfDestination Generated()
    {
        var src = source;
        RoListPerfDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new();
            PerfBenchmarkMappers.MapReadOnlyList(src, ret);
        }
        return ret;
    }
}
