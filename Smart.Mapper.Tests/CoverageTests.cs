namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// =====================================================================
// __todo.md §1 数値変換
// =====================================================================
public sealed class NumericConversionCoverageTests
{
    [Fact]
    public void BasicNumericConversions()
    {
        var source = new NumericCovSource { WidenS2I = 300, NarrowL2I = 1234L, Int2Dec = 42, Dec2Int = 3.9m };
        var destination = new NumericCovDestination();

        TestMappers.MapNumericCov(source, destination);

        Assert.Equal(300, destination.WidenS2I);
        Assert.Equal(1234, destination.NarrowL2I);
        Assert.Equal(42m, destination.Int2Dec);
        Assert.Equal(3, destination.Dec2Int);
    }

    [Fact]
    public void NullableNumericConversions_WithValues()
    {
        var source = new NullableNumCovSource
        {
            IntQ2Int = 5,
            Int2IntQ = 6,
            NullIntQ2Int = null,
            IntQ2Long = 7,
            Int2LongQ = 8,
            IntQ2LongQ = 9,
            IntQ2Short = 10,
            Int2ShortQ = 11,
            IntQ2ShortQ = 12,
            IntQ2Dec = 13,
            Int2DecQ = 14,
            Dec2IntQ = 15.7m,
            DecQ2Int = 16.2m
        };
        var destination = new NullableNumCovDestination();

        TestMappers.MapNullableNumCov(source, destination);

        Assert.Equal(5, destination.IntQ2Int);
        Assert.Equal(6, destination.Int2IntQ!.Value);
        Assert.Equal(0, destination.NullIntQ2Int);
        Assert.Equal(7L, destination.IntQ2Long);
        Assert.Equal(8L, destination.Int2LongQ!.Value);
        Assert.Equal(9L, destination.IntQ2LongQ!.Value);
        Assert.Equal((short)10, destination.IntQ2Short);
        Assert.Equal((short)11, destination.Int2ShortQ!.Value);
        Assert.Equal((short)12, destination.IntQ2ShortQ!.Value);
        Assert.Equal(13m, destination.IntQ2Dec);
        Assert.Equal(14m, destination.Int2DecQ!.Value);
        Assert.Equal(15, destination.Dec2IntQ!.Value);
        Assert.Equal(16, destination.DecQ2Int);
    }
}

// =====================================================================
// __todo.md §2 Enum 変換
// =====================================================================
public sealed class EnumConversionCoverageTests
{
    [Fact]
    public void EnumIntAndEnumEnumConversions()
    {
        var source = new EnumCovSource
        {
            E16ToInt = CovE16.B,
            E32ToInt = CovE32.B,
            E64ToInt = CovE64.C,
            IntToE16 = 2,
            IntToE32 = 3,
            IntToE64 = 1,
            E32ToE16 = CovE32.C,
            E32ToE64 = CovE32.B
        };
        var destination = new EnumCovDestination();

        TestMappers.MapEnumCov(source, destination);

        Assert.Equal(2, destination.E16ToInt);
        Assert.Equal(2, destination.E32ToInt);
        Assert.Equal(3, destination.E64ToInt);
        Assert.Equal(CovE16.B, destination.IntToE16);
        Assert.Equal(CovE32.C, destination.IntToE32);
        Assert.Equal(CovE64.A, destination.IntToE64);
        Assert.Equal(CovE16.C, destination.E32ToE16); // by name
        Assert.Equal(CovE64.B, destination.E32ToE64); // by name
    }

    [Fact]
    public void NullableEnumConversions()
    {
        var source = new NullableEnumCovSource
        {
            E16QToInt = CovE16.C,
            IntQToE32 = 2,
            E32QToE16 = CovE32.B,
            E32ToE16Q = CovE32.C,
            E32QToE16Q = CovE32.A,
            NullE32QToInt = null
        };
        var destination = new NullableEnumCovDestination();

        TestMappers.MapNullableEnumCov(source, destination);

        Assert.Equal(3, destination.E16QToInt);
        Assert.Equal(CovE32.B, destination.IntQToE32);
        Assert.Equal(CovE16.B, destination.E32QToE16);
        Assert.Equal(CovE16.C, destination.E32ToE16Q!.Value);
        Assert.Equal(CovE16.A, destination.E32QToE16Q!.Value);
        Assert.Equal(0, destination.NullE32QToInt); // null -> default
    }
}

// =====================================================================
// __todo.md §3 ユーザー定義変換演算子
// =====================================================================
public sealed class OperatorConversionCoverageTests
{
    [Fact]
    public void UserDefinedOperatorConversions()
    {
        var source = new OperatorCovSource
        {
            IntToClass = 10,
            ClassToInt = new OpClass { Value = 20 },
            IntToStruct = 30,
            StructToInt = new OpStruct { Value = 40 },
            StructQToInt = new OpStruct { Value = 50 },
            IntToStructQ = 60,
            PairAToB = new OpPairA { V = 70 },
            CrossClassToStruct = new OpCrossClass { V = 80 },
            CrossStructToClass = new OpCrossStruct { V = 90 }
        };
        var destination = new OperatorCovDestination();

        TestMappers.MapOperatorCov(source, destination);

        Assert.Equal(10, destination.IntToClass.Value); // int -> class
        Assert.Equal(20, destination.ClassToInt); // class -> int
        Assert.Equal(30, destination.IntToStruct.Value); // int -> struct
        Assert.Equal(40, destination.StructToInt); // struct -> int
        Assert.Equal(50, destination.StructQToInt); // struct? -> int
        Assert.Equal(60, destination.IntToStructQ!.Value.Value); // int -> struct?
        Assert.Equal(70, destination.PairAToB.V); // pair A -> B
        Assert.Equal(80, destination.CrossClassToStruct.V); // class -> struct
        Assert.Equal(90, destination.CrossStructToClass.V); // struct -> class
    }
}

// =====================================================================
// __todo.md §4 カスタムコンバータ
// =====================================================================
public sealed class ConverterConversionCoverageTests
{
    [Fact]
    public void CustomConverterWithSpecializedMethods()
    {
        var source = new ConverterCovSource { IntToStr = 5, NIntToStr = 7, StrToInt = "#42" };
        var destination = new ConverterCovDestination();

        TestMappers.MapConverterCov(source, destination);

        Assert.Equal("#5", destination.IntToStr); // int -> string
        Assert.Equal("#7", destination.NIntToStr); // int? (value) -> string
        Assert.Equal(42, destination.StrToInt); // string -> int
    }

    [Fact]
    public void CustomConverter_NullableSourceNull_UsesDefault()
    {
        var source = new ConverterCovSource { IntToStr = 1, NIntToStr = null, StrToInt = "#0" };
        var destination = new ConverterCovDestination();

        TestMappers.MapConverterCov(source, destination);

        Assert.Equal("#1", destination.IntToStr);
        Assert.Null(destination.NIntToStr); // int? (null) -> default!
        Assert.Equal(0, destination.StrToInt);
    }
}
