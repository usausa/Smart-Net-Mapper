namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// B3: IParsable<T> / ISpanParsable<T> 自動変換テスト
// B3: Automatic conversion tests for IParsable<T> / ISpanParsable<T>.
public class ParsableMappingTests
{
    // T1: string → TestParsableId (IParsable のみ実装)
    // T1: string → TestParsableId (implements only IParsable)
    [Fact]
    public void Map_StringToIParsable_ConvertsCorrectly()
    {
        var source = new ParsableSource { IdText = "42" };
        var destination = new ParsableDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(42, destination.IdText.Value);
    }

    // T2: string → TestSpanParsableId (ISpanParsable 実装、IParsable より優先)
    // T2: string → TestSpanParsableId (implements ISpanParsable; preferred over IParsable)
    [Fact]
    public void Map_StringToISpanParsable_ConvertsCorrectly()
    {
        var source = new SpanParsableSource { IdText = "99" };
        var destination = new SpanParsableDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(99, destination.IdText.Value);
    }

    // T3: Culture 指定あり + IParsable
    // T3: with specified Culture + IParsable
    [Fact]
    public void Map_StringToIParsableWithCulture_UsesSpecifiedCulture()
    {
        var source = new ParsableCultureSource { IdText = "123" };
        var destination = new ParsableCultureDestination();

        TestMappers.MapCulture(source, destination);

        Assert.Equal(123, destination.IdText.Value);
    }

    // T4: Culture 指定あり + ISpanParsable
    // T4: with specified Culture + ISpanParsable
    [Fact]
    public void Map_StringToISpanParsableWithCulture_UsesSpecifiedCulture()
    {
        var source = new SpanParsableCultureSource { IdText = "456" };
        var destination = new SpanParsableCultureDestination();

        TestMappers.MapCulture(source, destination);

        Assert.Equal(456, destination.IdText.Value);
    }

    // T6: string → int は既存の ConvertToInt32 が使われる (B3 に到達しない)
    // T6: string → int uses the existing ConvertToInt32 (does not reach B3)
    [Fact]
    public void Map_StringToInt_UsesSpecializedConverter()
    {
        var source = new TypeConversionSource { StringValue = "777" };
        var destination = new TypeConversionDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(777, destination.StringValue);
    }

    // T7: string? → TestSpanParsableId (nullable source: null ハンドリング後に Parse が呼ばれる)
    // T7: string? → TestSpanParsableId (nullable source: Parse is called after null handling)
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
