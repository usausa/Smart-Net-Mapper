namespace Smart.Mapper;

// ジェネリックフォールバック Convert<TSource, TDestination> の数値マトリクス網羅テスト。
// Tests covering the numeric matrix of the generic fallback Convert<TSource, TDestination>.
// sbyte / ushort / uint / ulong ソースと float/double/decimal → 符号なしターゲットは
// 以前は boxing フォールバックに落ち、型不一致の unbox で InvalidCastException になっていた。
// sbyte / ushort / uint / ulong sources and float/double/decimal → unsigned targets previously
// fell through to the boxing fallback and failed with InvalidCastException on the mismatched unbox.
public class DefaultValueConverterTests
{
    [Fact]
    public void Convert_UnsignedAndSByteSources_ConvertWithoutBoxingFallback()
    {
        Assert.Equal(42, DefaultValueConverter.Convert<uint, int>(42u));
        Assert.Equal(42L, DefaultValueConverter.Convert<uint, long>(42u));
        Assert.Equal(42m, DefaultValueConverter.Convert<uint, decimal>(42u));
        Assert.Equal("42", DefaultValueConverter.Convert<uint, string>(42u));

        Assert.Equal(-5L, DefaultValueConverter.Convert<sbyte, long>(-5));
        Assert.Equal((short)-5, DefaultValueConverter.Convert<sbyte, short>(-5));

        Assert.Equal(7d, DefaultValueConverter.Convert<ushort, double>(7));
        Assert.Equal(7u, DefaultValueConverter.Convert<ushort, uint>(7));

        Assert.Equal(9f, DefaultValueConverter.Convert<ulong, float>(9UL));
        Assert.Equal(9, DefaultValueConverter.Convert<ulong, int>(9UL));
    }

    [Fact]
    public void Convert_FloatingToUnsignedTargets_ConvertWithoutBoxingFallback()
    {
        Assert.Equal(3u, DefaultValueConverter.Convert<float, uint>(3.7f));
        Assert.Equal((ushort)3, DefaultValueConverter.Convert<double, ushort>(3.7d));
        Assert.Equal(3UL, DefaultValueConverter.Convert<double, ulong>(3.7d));
        Assert.Equal((sbyte)3, DefaultValueConverter.Convert<decimal, sbyte>(3.7m));
        Assert.Equal(3u, DefaultValueConverter.Convert<decimal, uint>(3.7m));
    }
}
