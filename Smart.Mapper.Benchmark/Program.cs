namespace Smart.Mapper.Benchmark;

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
            BenchmarkConverter.TypeToBenchmarks(typeof(ConversionMapBenchmark)),
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
        AddDiagnoser(MemoryDiagnoser.Default, new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
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

    private SimpleSource _source = default!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new SimpleSource
        {
            Value1 = 1, Value2 = 2, Value3 = 3, Value4 = 4,
            Value5 = "a", Value6 = "b", Value7 = "c", Value8 = "d"
        };
    }

    /// <summary>手書きの直接代入（理論上の最速ベースライン）</summary>
    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public SimpleDestination Direct()
    {
        var src = _source;
        SimpleDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new SimpleDestination
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

    /// <summary>Smart.Mapper 生成コード（同型プロパティコピー）</summary>
    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination SmartMapper()
    {
        var src = _source;
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

    private NestedSource _source = default!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new NestedSource
        {
            Id = 1,
            Name = "Test",
            Address = new AddressSource { City = "Tokyo", ZipCode = "100-0001" }
        };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public NestedDestination Direct()
    {
        var src = _source;
        NestedDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new NestedDestination
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
        var src = _source;
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

    private CollectionSource _source = default!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new CollectionSource
        {
            Items = Enumerable.Range(1, ItemCount)
                .Select(i => new CollectionItemSource { Id = i, Label = $"Item{i}" })
                .ToList()
        };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public List<CollectionItemDestination> Direct()
    {
        var src = _source;
        List<CollectionItemDestination> ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new List<CollectionItemDestination>(src.Items.Count);
            foreach (var item in src.Items)
            {
                ret.Add(new CollectionItemDestination { Id = item.Id, Label = item.Label });
            }
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public CollectionWrapper SmartMapper()
    {
        var src = _source;
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

    private ConversionSource _source = default!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new ConversionSource
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
        var src = _source;
        ConversionDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = new ConversionDestination
            {
                IntValue = src.IntValue.ToString(),
                LongValue = src.LongValue.ToString(),
                DoubleValue = src.DoubleValue.ToString(),
                BoolValue = src.BoolValue.ToString()
            };
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public ConversionDestination SmartMapper()
    {
        var src = _source;
        ConversionDestination ret = default!;
        for (var i = 0; i < N; i++)
        {
            ret = BenchmarkMappers.MapConversion(src);
        }
        return ret;
    }
}
