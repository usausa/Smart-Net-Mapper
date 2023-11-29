namespace Smart.Mapper.Benchmark.Mappers;

public static class RawChainMapperFactory
{
    public static IActionMapper<SingleSource, SingleDestination> CreateSingleMapper()
    {
        return new ActionMapper<SingleSource, SingleDestination>(
            static () => new SingleDestination(),
            [
                static (s, d) => d.Value = s.Value
            ]);
    }

    public static IActionMapper<SimpleSource, SimpleDestination> CreateSimpleMapper()
    {
        return new ActionMapper<SimpleSource, SimpleDestination>(
            static () => new SimpleDestination(),
            [
                static (s, d) => d.Value1 = s.Value1,
                static (s, d) => d.Value2 = s.Value2,
                static (s, d) => d.Value3 = s.Value3,
                static (s, d) => d.Value4 = s.Value4,
                static (s, d) => d.Value5 = s.Value5,
                static (s, d) => d.Value6 = s.Value6,
                static (s, d) => d.Value7 = s.Value7,
                static (s, d) => d.Value8 = s.Value8
            ]);
    }

    public static IActionMapper<MixedSource, MixedDestination> CreateMixedMapper()
    {
        return new ActionMapper<MixedSource, MixedDestination>(
            static () => new MixedDestination(),
            [
                static (s, d) => d.StringValue = s.StringValue,
                static (s, d) => d.IntValue = s.IntValue,
                static (s, d) => d.LongValue = s.LongValue,
                static (s, d) => d.NullableIntValue = s.NullableIntValue,
                static (s, d) => d.FloatValue = s.FloatValue,
                static (s, d) => d.DateTimeValue = s.DateTimeValue,
                static (s, d) => d.BoolValue = s.BoolValue,
                static (s, d) => d.EnumValue = s.EnumValue
            ]);
    }
}
