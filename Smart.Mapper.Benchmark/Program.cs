namespace Smart.Mapper.Benchmark;

using AutoMapper;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using Nelibur.ObjectMapper;

using Smart.Mapper.Benchmark.Mappers;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<MapperBenchmark>();
    }
}

public class BenchmarkConfig : ManualConfig
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
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class MapperBenchmark
{
    private const int N = 1000;

    private IMapper autoMapper = default!;

    private readonly DirectMapper directMapper = new();

    private readonly ActionMapperFactory instantActionMapperFactory = new();    // Boxed

    private readonly ActionMapperFactory rawActionMapperFactory = new();

    private IActionMapper<SimpleSource, SimpleDestination> instantSimpleMapper = default!;

    private IActionMapper<SimpleSource, SimpleDestination> rawSimpleMapper = default!;

    private SmartMapper smartMapper = default!;

    private Func<SimpleSource, SimpleDestination> smartSimpleMapper = default!;

    [GlobalSetup]
    public void Setup()
    {
        // AutoMapper
        var autoMapperConfig = new MapperConfiguration(c =>
        {
            c.CreateMap<SingleSource, SingleDestination>();
            c.CreateMap<SimpleSource, SimpleDestination>();
            c.CreateMap<MixedSource, MixedDestination>();
        });
        autoMapper = autoMapperConfig.CreateMapper();

        // TinyMapper
        TinyMapper.Bind<SingleSource, SingleDestination>();
        TinyMapper.Bind<SimpleSource, SimpleDestination>();
        TinyMapper.Bind<MixedSource, MixedDestination>();

        // Action based
        instantActionMapperFactory.AddMapper(typeof(SingleSource), typeof(SingleDestination), InstantMapperFactory.Create<SingleSource, SingleDestination>());
        instantSimpleMapper = InstantMapperFactory.Create<SimpleSource, SimpleDestination>();
        instantActionMapperFactory.AddMapper(typeof(SimpleSource), typeof(SimpleDestination), instantSimpleMapper);
        instantActionMapperFactory.AddMapper(typeof(MixedSource), typeof(MixedDestination), InstantMapperFactory.Create<MixedSource, MixedDestination>());

        rawActionMapperFactory.AddMapper(typeof(SingleSource), typeof(SingleDestination), RawChainMapperFactory.CreateSingleMapper());
        rawSimpleMapper = RawChainMapperFactory.CreateSimpleMapper();
        rawActionMapperFactory.AddMapper(typeof(SimpleSource), typeof(SimpleDestination), rawSimpleMapper);
        rawActionMapperFactory.AddMapper(typeof(MixedSource), typeof(MixedDestination), RawChainMapperFactory.CreateMixedMapper());

        // Smart
        var smartConfig = new MapperConfig()
            .AddDefaultMapper();
        smartConfig.CreateMap<SimpleSource, SimpleDestination>();
        smartConfig.CreateMap<MixedSource, MixedDestination>();
        smartMapper = smartConfig.ToMapper();

        smartSimpleMapper = smartMapper.GetMapperFunc<SimpleSource, SimpleDestination>();
    }

    //--------------------------------------------------------------------------------
    // Simple
    //--------------------------------------------------------------------------------

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleAutoMapper()
    {
        var m = autoMapper;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SimpleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleAutoMapper2()
    {
        var m = autoMapper;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SimpleSource, SimpleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleTinyMapper()
    {
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = TinyMapper.Map<SimpleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleInstantMapper()
    {
        var m = instantActionMapperFactory;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SimpleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleRawMapper()
    {
        var m = rawActionMapperFactory;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SimpleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleSmartMapper()
    {
        var m = smartMapper;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SimpleSource, SimpleDestination>(source);
        }
        return ret;
    }

    //--------------------------------------------------------------------------------
    // Without lookup
    //--------------------------------------------------------------------------------

    // Slow (object based/boxed & delegate getter/setter)
    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleInstantMapperWoLookup()
    {
        var m = instantSimpleMapper;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map(source);
        }
        return ret;
    }

    // Fast (No loop, No boxed)
    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleRawMapperWoLookup()
    {
        var m = rawSimpleMapper;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleSmartMapperWoLookup()
    {
        var m = smartSimpleMapper;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleDirect()
    {
        var m = directMapper;
        var source = new SimpleSource();
        var ret = default(SimpleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map(source);
        }
        return ret;
    }

    // Max
    [Benchmark(OperationsPerInvoke = N)]
    public SimpleDestination? SimpleInline()
    {
        var source = new SimpleSource();
        var ret = default(SimpleDestination);

        // Without Lookup
        for (var i = 0; i < N; i++)
        {
            ret = new SimpleDestination
            {
                Value1 = source.Value1,
                Value2 = source.Value2,
                Value3 = source.Value3,
                Value4 = source.Value4,
                Value5 = source.Value5,
                Value6 = source.Value6,
                Value7 = source.Value7,
                Value8 = source.Value8
            };
        }

        return ret;
    }

    //--------------------------------------------------------------------------------
    // Mixed
    //--------------------------------------------------------------------------------

    [Benchmark(OperationsPerInvoke = N)]
    public MixedDestination? MixedAutoMapper()
    {
        var m = autoMapper;
        var source = new MixedSource();
        var ret = default(MixedDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<MixedDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public MixedDestination? MixedAutoMapper2()
    {
        var m = autoMapper;
        var source = new MixedSource();
        var ret = default(MixedDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<MixedSource, MixedDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public MixedDestination? MixedTinyMapper()
    {
        var source = new MixedSource();
        var ret = default(MixedDestination);
        for (var i = 0; i < N; i++)
        {
            ret = TinyMapper.Map<MixedDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public MixedDestination? MixedInstantMapper()
    {
        var m = instantActionMapperFactory;
        var source = new MixedSource();
        var ret = default(MixedDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<MixedDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public MixedDestination? MixedRawMapper()
    {
        var m = rawActionMapperFactory;
        var source = new MixedSource();
        var ret = default(MixedDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<MixedDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public MixedDestination? MixedSmartMapper()
    {
        var m = smartMapper;
        var source = new MixedSource();
        var ret = default(MixedDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<MixedSource, MixedDestination>(source);
        }
        return ret;
    }

    //--------------------------------------------------------------------------------
    // Single
    //--------------------------------------------------------------------------------

    [Benchmark(OperationsPerInvoke = N)]
    public SingleDestination? SingleAutoMapper()
    {
        var m = autoMapper;
        var source = new SingleSource();
        var ret = default(SingleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SingleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SingleDestination? SingleAutoMapper2()
    {
        var m = autoMapper;
        var source = new SingleSource();
        var ret = default(SingleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SingleSource, SingleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SingleDestination? SingleTinyMapper()
    {
        var source = new SingleSource();
        var ret = default(SingleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = TinyMapper.Map<SingleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SingleDestination? SingleInstantMapper()
    {
        var m = instantActionMapperFactory;
        var source = new SingleSource();
        var ret = default(SingleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SingleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SingleDestination? SingleRawMapper()
    {
        var m = rawActionMapperFactory;
        var source = new SingleSource();
        var ret = default(SingleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SingleDestination>(source);
        }
        return ret;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public SingleDestination? SingleSmartMapper()
    {
        var m = smartMapper;
        var source = new SingleSource();
        var ret = default(SingleDestination);
        for (var i = 0; i < N; i++)
        {
            ret = m.Map<SingleSource, SingleDestination>(source);
        }
        return ret;
    }
}

public sealed class DirectMapper
{
    public SimpleDestination Map(SimpleSource source)
    {
        return new SimpleDestination
        {
            Value1 = source.Value1,
            Value2 = source.Value2,
            Value3 = source.Value3,
            Value4 = source.Value4,
            Value5 = source.Value5,
            Value6 = source.Value6,
            Value7 = source.Value7,
            Value8 = source.Value8
        };
    }
}
