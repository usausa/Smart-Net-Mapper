namespace Smart.Mapper;

// B3: IParsable<T> / ISpanParsable<T> 自動変換テスト
public class ParsableMappingTests
{
    // T1: string → TestParsableId (IParsable のみ実装)
    [Fact]
    public void Map_StringToIParsable_ConvertsCorrectly()
    {
        var source = new ParsableSource { IdText = "42" };
        var destination = new ParsableDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(42, destination.IdText.Value);
    }

    // T2: string → TestSpanParsableId (ISpanParsable 実装、IParsable より優先)
    [Fact]
    public void Map_StringToISpanParsable_ConvertsCorrectly()
    {
        var source = new SpanParsableSource { IdText = "99" };
        var destination = new SpanParsableDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(99, destination.IdText.Value);
    }

    // T3: Culture 指定あり + IParsable
    [Fact]
    public void Map_StringToIParsableWithCulture_UsesSpecifiedCulture()
    {
        var source = new ParsableCultureSource { IdText = "123" };
        var destination = new ParsableCultureDestination();

        TestMappers.MapCulture(source, destination);

        Assert.Equal(123, destination.IdText.Value);
    }

    // T4: Culture 指定あり + ISpanParsable
    [Fact]
    public void Map_StringToISpanParsableWithCulture_UsesSpecifiedCulture()
    {
        var source = new SpanParsableCultureSource { IdText = "456" };
        var destination = new SpanParsableCultureDestination();

        TestMappers.MapCulture(source, destination);

        Assert.Equal(456, destination.IdText.Value);
    }

    // T6: string → int は既存の ConvertToInt32 が使われる (B3 に到達しない)
    [Fact]
    public void Map_StringToInt_UsesSpecializedConverter()
    {
        var source = new TypeConversionSource { StringValue = "777" };
        var destination = new TypeConversionDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(777, destination.StringValue);
    }

    // T7: string? → TestSpanParsableId (nullable source: null ハンドリング後に Parse が呼ばれる)
    [Fact]
    public void Map_NullableStringToISpanParsable_WhenNotNull_ConvertsCorrectly()
    {
        var source = new NullableSpanParsableSource { IdText = "55" };
        var destination = new NullableSpanParsableDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(55, destination.IdText.Value);
    }

    [Fact]
    public void Map_NullableStringToISpanParsable_WhenNull_UsesDefault()
    {
        var source = new NullableSpanParsableSource { IdText = null };
        var destination = new NullableSpanParsableDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(default, destination.IdText);
    }
}
