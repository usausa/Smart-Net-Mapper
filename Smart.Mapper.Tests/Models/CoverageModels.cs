namespace Smart.Mapper.Models;

// =====================================================================
// __todo.md カバレッジ用モデル
// =====================================================================

// ---------------------------------------------------------------------
// §1 数値変換
// ---------------------------------------------------------------------

// 基本（拡大 / 縮小 / decimal）
public sealed class NumericCovSource
{
    public short WidenS2I { get; set; } // short -> int
    public long NarrowL2I { get; set; } // long -> int
    public int Int2Dec { get; set; } // int -> decimal
    public decimal Dec2Int { get; set; } // decimal -> int
}

public sealed class NumericCovDestination
{
    public int WidenS2I { get; set; }
    public int NarrowL2I { get; set; }
    public decimal Int2Dec { get; set; }
    public int Dec2Int { get; set; }
}

// Nullable 数値（名前一致 / 型違いで変換を強制）
public sealed class NullableNumCovSource
{
    public int? IntQ2Int { get; set; } // int? -> int
    public int Int2IntQ { get; set; } // int -> int?
    public int? NullIntQ2Int { get; set; } // int?(null) -> int (default)
    public int? IntQ2Long { get; set; } // int? -> long
    public int Int2LongQ { get; set; } // int -> long?
    public int? IntQ2LongQ { get; set; } // int? -> long?
    public int? IntQ2Short { get; set; } // int? -> short
    public int Int2ShortQ { get; set; } // int -> short?
    public int? IntQ2ShortQ { get; set; } // int? -> short?
    public int? IntQ2Dec { get; set; } // int? -> decimal
    public int Int2DecQ { get; set; } // int -> decimal?
    public decimal Dec2IntQ { get; set; } // decimal -> int?
    public decimal? DecQ2Int { get; set; } // decimal? -> int
}

public sealed class NullableNumCovDestination
{
    public int IntQ2Int { get; set; }
    public int? Int2IntQ { get; set; }
    public int NullIntQ2Int { get; set; }
    public long IntQ2Long { get; set; }
    public long? Int2LongQ { get; set; }
    public long? IntQ2LongQ { get; set; }
    public short IntQ2Short { get; set; }
    public short? Int2ShortQ { get; set; }
    public short? IntQ2ShortQ { get; set; }
    public decimal IntQ2Dec { get; set; }
    public decimal? Int2DecQ { get; set; }
    public int? Dec2IntQ { get; set; }
    public int DecQ2Int { get; set; }
}

// ---------------------------------------------------------------------
// §2 Enum 変換（幅 16/32/64 × int × enum × Nullable）
// ---------------------------------------------------------------------
public enum CovE16 : short
{
    None = 0,
    A = 1,
    B = 2,
    C = 3
}

// ReSharper disable once EnumUnderlyingTypeIsInt
public enum CovE32 : int
{
    None = 0,
    A = 1,
    B = 2,
    C = 3
}

public enum CovE64 : long
{
    None = 0,
    A = 1,
    B = 2,
    C = 3
}

// enum <-> int, enum <-> enum（非 Nullable）
public sealed class EnumCovSource
{
    public CovE16 E16ToInt { get; set; }
    public CovE32 E32ToInt { get; set; }
    public CovE64 E64ToInt { get; set; }
    public int IntToE16 { get; set; }
    public int IntToE32 { get; set; }
    public int IntToE64 { get; set; }
    public CovE32 E32ToE16 { get; set; }
    public CovE32 E32ToE64 { get; set; }
}

public sealed class EnumCovDestination
{
    public int E16ToInt { get; set; }
    public int E32ToInt { get; set; }
    public int E64ToInt { get; set; }
    public CovE16 IntToE16 { get; set; }
    public CovE32 IntToE32 { get; set; }
    public CovE64 IntToE64 { get; set; }
    public CovE16 E32ToE16 { get; set; }
    public CovE64 E32ToE64 { get; set; }
}

// Nullable 絡み
public sealed class NullableEnumCovSource
{
    public CovE16? E16QToInt { get; set; } // enum? -> int
    public int? IntQToE32 { get; set; } // int? -> enum
    public CovE32? E32QToE16 { get; set; } // enum? -> enum
    public CovE32 E32ToE16Q { get; set; } // enum -> enum?
    public CovE32? E32QToE16Q { get; set; } // enum? -> enum?
    public CovE32? NullE32QToInt { get; set; } // enum?(null) -> int (default)
}

public sealed class NullableEnumCovDestination
{
    public int E16QToInt { get; set; }
    public CovE32 IntQToE32 { get; set; }
    public CovE16 E32QToE16 { get; set; }
    public CovE16? E32ToE16Q { get; set; }
    public CovE16? E32QToE16Q { get; set; }
    public int NullE32QToInt { get; set; }
}

// ---------------------------------------------------------------------
// §3 ユーザー定義変換演算子（op_Implicit）
// 変換演算子を持つ型は UserDefinedConversionModels.cs に定義（CA1815/CA2225 抑制済み）
// ---------------------------------------------------------------------
public sealed class OperatorCovSource
{
    public int IntToClass { get; set; } // int -> class (op)
    public OpClass ClassToInt { get; set; } = default!; // class -> int (op)
    public int IntToStruct { get; set; } // int -> struct (op)
    public OpStruct StructToInt { get; set; } // struct -> int (op)
    public OpStruct? StructQToInt { get; set; } // struct? -> int (op)
    public int IntToStructQ { get; set; } // int -> struct? (op)
    public OpPairA PairAToB { get; set; } // struct pair A -> B (op)
    public OpCrossClass CrossClassToStruct { get; set; } = default!; // class -> struct (op)
    public OpCrossStruct CrossStructToClass { get; set; } // struct -> class (op)
}

public sealed class OperatorCovDestination
{
    public OpClass IntToClass { get; set; } = default!;
    public int ClassToInt { get; set; }
    public OpStruct IntToStruct { get; set; }
    public int StructToInt { get; set; }
    public int StructQToInt { get; set; }
    public OpStruct? IntToStructQ { get; set; }
    public OpPairB PairAToB { get; set; }
    public OpCrossStruct CrossClassToStruct { get; set; }
    public OpCrossClass CrossStructToClass { get; set; } = default!;
}

// ---------------------------------------------------------------------
// §4 カスタムコンバータ（ValueConverter × specialized × Nullable source）
// ---------------------------------------------------------------------
public static class CovConverter
{
    // specialized: int -> string
    public static string ConvertToString(int value) => $"#{value}";

    // specialized: string -> int
    public static int ConvertToInt32(string value) =>
        int.Parse(value.TrimStart('#'), System.Globalization.CultureInfo.InvariantCulture);

    // generic fallback
    public static TDestination Convert<TSource, TDestination>(TSource source) =>
        DefaultValueConverter.Convert<TSource, TDestination>(source);
}

public sealed class ConverterCovSource
{
    public int IntToStr { get; set; } // int -> string (specialized)
    public int? NIntToStr { get; set; } // int? -> string (nullable source + converter)
    public string StrToInt { get; set; } = default!; // string -> int (specialized)
}

public sealed class ConverterCovDestination
{
    public string IntToStr { get; set; } = default!;
    public string NIntToStr { get; set; } = default!;
    public int StrToInt { get; set; }
}
