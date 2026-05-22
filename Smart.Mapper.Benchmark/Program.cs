namespace Smart.Mapper.Benchmark;

using System.Globalization;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "simple": BenchmarkRunner.Run<SimpleMapBenchmark>(); return;
                case "nested": BenchmarkRunner.Run<NestedMapBenchmark>(); return;
                case "collection": BenchmarkRunner.Run<CollectionMapBenchmark>(); return;
                case "conversion": BenchmarkRunner.Run<ConversionMapBenchmark>(); return;
            }
        }

        BenchmarkRunner.Run(
        [
            BenchmarkConverter.TypeToBenchmarks(typeof(SimpleMapBenchmark)),
            BenchmarkConverter.TypeToBenchmarks(typeof(NestedMapBenchmark)),
            BenchmarkConverter.TypeToBenchmarks(typeof(CollectionMapBenchmark)),
            BenchmarkConverter.TypeToBenchmarks(typeof(ConversionMapBenchmark))
        ]);
    }
}

public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddDiagnoser(MemoryDiagnoser.Default, new DisassemblyDiagnoser(new(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
        AddJob(Job.MediumRun);
    }
}

// ==========================================================================
// Scenario 1: Simple flat property copy (8 props, same types)
// Comparison: Direct handwritten assignment vs Smart.Mapper generated code
// ==========================================================================

#pragma warning disable IDE0320
[Config(typeof(BenchmarkConfig))]
[BenchmarkCategory("Simple")]
public class SimpleMapBenchmark
{
    private const int N = 1000;

    private SimpleSource source = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new()
        {
            Value1 = 1, Value2 = 2, Value3 = 3, Value4 = 4,
            Value5 = "a", Value6 = "b", Value7 = "c", Value8 = "d"
        };
    }

    // 手書きの直接代入（理論上の最速ベースライン）
    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public SimpleDestination Direct()
    {
        var src = source;
        SimpleDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new()
            {
                Value1 = src.Value1,
                Value2 = src.Value2,
                Value3 = src.Value3,
                Value4 = src.Value4,
                Value5 = src.Value5,
                Value6 = src.Value6,
                Value7 = src.Value7,
                Value8 = src.Value8
            };
        }
        return ret;
    }

    // Smart.Mapper 生成コード（同型プロパティコピー）
    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination SmartMapper()
    {
        var src = source;
        SimpleDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = BenchmarkMappers.MapSimple(src);
        }
        return ret;
    }
}

// ==========================================================================
// Scenario 2: Nested object mapping
// Comparison: Direct handwritten vs Smart.Mapper (MapNested)
// ==========================================================================

[Config(typeof(BenchmarkConfig))]
[BenchmarkCategory("Nested")]
public class NestedMapBenchmark
{
    private const int N = 1000;

    private NestedSource source = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new()
        {
            Id = 1,
            Name = "Test",
            Address = new() { City = "Tokyo", ZipCode = "100-0001" }
        };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public NestedDestination Direct()
    {
        var src = source;
        NestedDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new()
            {
                Id = src.Id,
                Name = src.Name,
                Address = src.Address is null ? null : new AddressDestination
                {
                    City = src.Address.City,
                    ZipCode = src.Address.ZipCode
                }
            };
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public NestedDestination SmartMapper()
    {
        var src = source;
        NestedDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = BenchmarkMappers.MapNested(src);
        }
        return ret;
    }
}

// ==========================================================================
// Scenario 3: Collection mapping (List<T> -> List<T>)
// Comparison: Direct foreach vs Smart.Mapper (inline-expanded CollectionsMarshal)
// ==========================================================================

[Config(typeof(BenchmarkConfig))]
[BenchmarkCategory("Collection")]
public class CollectionMapBenchmark
{
    private const int N = 100;

    [Params(10, 100)]
    public int ItemCount { get; set; }

    private CollectionSource source = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new()
        {
            Items = Enumerable.Range(1, ItemCount)
                .Select(i => new CollectionItemSource { Id = i, Label = $"Item{i}" })
                .ToList()
        };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public List<CollectionItemDestination> Direct()
    {
        var src = source;
        List<CollectionItemDestination> ret = default!;
        for (var i = 0; i < N; i++)
        {
#pragma warning disable IDE0028
            ret = new(src.Items.Count);
#pragma warning restore IDE0028
            foreach (var item in src.Items)
            {
                ret.Add(new() { Id = item.Id, Label = item.Label });
            }
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public CollectionWrapper SmartMapper()
    {
        var src = source;
        CollectionWrapper ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = BenchmarkMappers.MapWrapper(src);
        }
        return ret;
    }
}

// ==========================================================================
// Scenario 4: Type conversion mapping (numeric -> string)
// Comparison: Direct .ToString() vs Smart.Mapper specialized converter
// ==========================================================================

[Config(typeof(BenchmarkConfig))]
[BenchmarkCategory("Conversion")]
public class ConversionMapBenchmark
{
    private const int N = 1000;

    private ConversionSource source = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new()
        {
            IntValue = 42,
            LongValue = 123456789L,
            DoubleValue = 3.14159,
            BoolValue = true
        };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public ConversionDestination Direct()
    {
        var src = source;
        ConversionDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new()
            {
                IntValue = src.IntValue.ToString(CultureInfo.InvariantCulture),
                LongValue = src.LongValue.ToString(CultureInfo.InvariantCulture),
                DoubleValue = src.DoubleValue.ToString(CultureInfo.InvariantCulture),
                BoolValue = src.BoolValue.ToString()
            };
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public ConversionDestination SmartMapper()
    {
        var src = source;
        ConversionDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = BenchmarkMappers.MapConversion(src);
        }
        return ret;
    }
}
