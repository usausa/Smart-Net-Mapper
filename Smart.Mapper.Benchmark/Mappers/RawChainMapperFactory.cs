namespace Smart.Mapper.Benchmark.Mappers;

using System;

public static class RawChainMapperFactory
{
    public static IActionMapper<SingleSource, SingleDestination> CreateSingleMapper()
    {
        return new ActionMapper<SingleSource, SingleDestination>(
            () => new SingleDestination(),
            new Action<SingleSource, SingleDestination>[]
            {
                (s, d) => d.Value = s.Value
            });
    }

    public static IActionMapper<SimpleSource, SimpleDestination> CreateSimpleMapper()
    {
        return new ActionMapper<SimpleSource, SimpleDestination>(
            () => new SimpleDestination(),
            new Action<SimpleSource, SimpleDestination>[]
            {
                (s, d) => d.Value1 = s.Value1,
                (s, d) => d.Value2 = s.Value2,
                (s, d) => d.Value3 = s.Value3,
                (s, d) => d.Value4 = s.Value4,
                (s, d) => d.Value5 = s.Value5,
                (s, d) => d.Value6 = s.Value6,
                (s, d) => d.Value7 = s.Value7,
                (s, d) => d.Value8 = s.Value8
            });
    }

    public static IActionMapper<MixedSource, MixedDestination> CreateMixedMapper()
    {
        return new ActionMapper<MixedSource, MixedDestination>(
            () => new MixedDestination(),
            new Action<MixedSource, MixedDestination>[]
            {
                (s, d) => d.StringValue = s.StringValue,
                (s, d) => d.IntValue = s.IntValue,
                (s, d) => d.LongValue = s.LongValue,
                (s, d) => d.NullableIntValue = s.NullableIntValue,
                (s, d) => d.FloatValue = s.FloatValue,
                (s, d) => d.DateTimeValue = s.DateTimeValue,
                (s, d) => d.BoolValue = s.BoolValue,
                (s, d) => d.EnumValue = s.EnumValue
            });
    }
}
