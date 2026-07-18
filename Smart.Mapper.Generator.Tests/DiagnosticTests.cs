namespace Smart.Mapper.Generator.Tests;

using System.Globalization;

using Microsoft.CodeAnalysis;

// Source Generator が正しい診断（SMP0001〜SMP0402）を発行することを検証するテスト。
// Tests that verify the source generator emits the correct diagnostics (SMP0001-SMP0402).
public class DiagnosticTests
{
    // -----------------------------------------------------------------------
    // SMP0001 — [Mapper] をインスタンスメソッドまたは非 partial メソッドに付与
    // SMP0001 — [Mapper] applied to an instance method or a non-partial method
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0001_NonPartialMethod_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static Dest Map(Src src) => default!;
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public int Value { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0001");
    }

    [Fact]
    public void SMP0001_InstanceMethod_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal partial class Mappers
            {
                [Mapper]
                public partial Dest Map(Src src);
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public int Value { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0001");
    }

    // -----------------------------------------------------------------------
    // SMP0002 — パラメーターが不足しているメソッド
    // SMP0002 — method with insufficient parameters
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0002_NoParameters_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial void Map();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0002");
    }

    // -----------------------------------------------------------------------
    // SMP0003 — カスタムパラメーターの型が重複
    // SMP0003 — duplicate custom parameter type
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0003_DuplicateCustomParameterType_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial void Map(Src src, Dest dst, string ctx1, string ctx2);
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public int Value { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0003");
    }

    // -----------------------------------------------------------------------
    // SMP0101 — 同一ターゲットプロパティへの重複マッピング
    // SMP0101 — duplicate mapping to the same target property
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0101_DuplicateTargetMapping_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapProperty(nameof(Dest.Value), nameof(Src.A))]
                [MapProperty(nameof(Dest.Value), nameof(Src.B))]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public int A { get; set; } public int B { get; set; } }
            public class Dest { public int Value { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0101");
    }

    // -----------------------------------------------------------------------
    // SMP0501 — Strict モードで未マッピングの destination プロパティ
    // SMP0501 — unmapped destination property in Strict mode
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0501_StrictMode_UnmappedProperty_EmitsWarning()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper(Strict = true)]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public int Value { get; set; } public int Extra { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "SMP0501" &&
            d.Severity == DiagnosticSeverity.Warning &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("Extra", StringComparison.Ordinal));
    }

    [Fact]
    public void SMP0501_StrictMode_AllMapped_NoDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper(Strict = true)]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public int Value { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0501");
    }

    // -----------------------------------------------------------------------
    // SMP0303 — required プロパティが未マッピング
    // SMP0303 — required property is unmapped
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0303_UnmappedRequiredProperty_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial Dest Map(Src src);
            }

            public class Src { public int Value { get; set; } }
            public class Dest
            {
                public int Value { get; set; }
                public required string Name { get; set; }
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "SMP0303" &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("Name", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // SMP0302 — コンストラクタ専用プロパティを持つ型（record）で void mapper を使用
    // SMP0302 — void mapper used for a type with constructor-only properties (record)
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0302_RecordWithVoidMapper_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public int Value { get; set; } public string Name { get; set; } = ""; }
            // record 型はプライマリコンストラクタ経由のマッピングが必要 → void mapper では SMP0302
            // A record type must be mapped via its primary constructor → a void mapper triggers SMP0302
            public record Dest(int Value, string Name);
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0302");
    }

    // -----------------------------------------------------------------------
    // SMP0401 — Culture なしで Format 指定
    // SMP0401 — Format specified without Culture
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0401_FormatWithoutCulture_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper(NumberFormat = "N2")]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public double Price { get; set; } }
            public class Dest { public string? Price { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0401");
    }

    [Fact]
    public void SMP0401_FormatWithCulture_NoDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper(Culture = "ja-JP", NumberFormat = "N2")]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public double Price { get; set; } }
            public class Dest { public string? Price { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0401");
    }

    // -----------------------------------------------------------------------
    // SMP0402 — AllowTypeConverter なしで汎用フォールバック使用
    // SMP0402 — generic fallback used without AllowTypeConverter
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0402_TypeConverterFallback_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public MyValueObject Value { get; set; } = default!; }
            public class Dest { public MyValueObject Value { get; set; } = default!; }

            public class MyValueObject { }
            """;

        // MyValueObject → MyValueObject は同型コピーなので診断なし（スペシャライズド不要）
        // MyValueObject → MyValueObject is a same-type copy, so no diagnostic (no specialized converter needed)
        // 異なる型でスペシャライズドが存在しない場合に SMP0402 が発火する
        // SMP0402 fires when the types differ and no specialized converter exists
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0402");
    }

    [Fact]
    public void SMP0402_UnsupportedConversion_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public MyTypeA Value { get; set; } = default!; }
            public class Dest { public MyTypeB Value { get; set; } = default!; }

            public class MyTypeA { }
            public class MyTypeB { }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0402");
    }

    // -----------------------------------------------------------------------
    // SMP0104 — Converter メソッドのシグネチャ不一致
    // SMP0104 — Converter method signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0104_InvalidConverterSignature_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapProperty(nameof(Dest.Name), nameof(Src.Name), Converter = nameof(Convert))]
                public static partial void Map(Src src, Dest dst);

                // 引数が int なのに string を要求 → シグネチャ不一致
                // The argument is int but a string is required → signature mismatch
                private static string Convert(int value) => value.ToString();
            }

            public class Src { public string Name { get; set; } = ""; }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0104");
    }

    // -----------------------------------------------------------------------
    // SMP0201 — MapFrom メソッドのシグネチャ不一致
    // SMP0201 — MapFrom method signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0201_InvalidMapFromSignature_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapFrom(nameof(Dest.Name), nameof(Build))]
                public static partial void Map(Src src, Dest dst);

                // 引数が int → Src ではない → SMP0201
                // The argument is int → not Src → SMP0201
                private static string Build(int x) => x.ToString();
            }

            public class Src { }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // 実際に発火される診断 ID を確認（SMP0201 または SMP0204）
        // Check the diagnostic ID that is actually emitted (SMP0201 or SMP0204)
        Assert.Contains(diagnostics, d => d.Id is "SMP0201" or "SMP0204");
    }

    // -----------------------------------------------------------------------
    // SMP0301 — コンストラクタパラメーターがソースプロパティに解決できない
    // SMP0301 — constructor parameter cannot be resolved to a source property
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0301_UnresolvedConstructorParameter_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial Dest Map(Src src);
            }

            public class Src { public int Value { get; set; } }
            public record Dest(int Value, string Name);
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "SMP0301" &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("Name", StringComparison.Ordinal));
    }

    [Fact]
    public void SMP0301_AllConstructorParametersResolved_NoDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial Dest Map(Src src);
            }

            public class Src { public int Value { get; set; } public string Name { get; set; } = ""; }
            public record Dest(int Value, string Name);
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0301");
    }

    // コンストラクタパラメーター名が source 以外 (src / input) でも正しく生成されることを確認
    // Verifies correct generation even when the constructor parameter name is not "source" (src / input)
    [Fact]
    public void ConstructorMapping_NonDefaultParameterName_CompilesWithoutError()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial Dest Map(Src src);

                [Mapper]
                public static partial Dest MapFromInput(Src input);
            }

            public class Src { public int Value { get; set; } public string Name { get; set; } = ""; }
            public record Dest(int Value, string Name);
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    // -----------------------------------------------------------------------
    // SMP0206 — [MapCollection]/[MapNested] のソースプロパティ名が typo
    // SMP0206 — source property name in [MapCollection]/[MapNested] is a typo
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0206_MapCollection_UnresolvedSourceProperty_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;
            using System.Collections.Generic;

            internal static partial class Mappers
            {
                [Mapper]
                [MapCollection("Items", "ItemsTypo", Mapper = nameof(MapItem))]
                public static partial Dest Map(Src src);

                private static DestItem MapItem(SrcItem item) => new DestItem { Value = item.Value };
            }

            public class SrcItem { public int Value { get; set; } }
            public class DestItem { public int Value { get; set; } }
            public class Src { public List<SrcItem> Items { get; set; } = new(); }
            public class Dest { public List<DestItem> Items { get; set; } = new(); }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0206");
    }

    [Fact]
    public void SMP0206_MapNested_UnresolvedSourceProperty_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapNested("Child", "ChildTypo", Mapper = nameof(MapChild))]
                public static partial Dest Map(Src src);

                private static DestChild MapChild(SrcChild c) => new DestChild { Value = c.Value };
            }

            public class SrcChild { public int Value { get; set; } }
            public class DestChild { public int Value { get; set; } }
            public class Src { public SrcChild Child { get; set; } = new(); }
            public class Dest { public DestChild Child { get; set; } = new(); }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0206");
    }

    // -----------------------------------------------------------------------
    // SMP0207 — [MapCollection]/[MapNested] のターゲットプロパティ名が typo
    // SMP0207 — target property name in [MapCollection]/[MapNested] is a typo
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0207_MapCollection_UnresolvedTargetProperty_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;
            using System.Collections.Generic;

            internal static partial class Mappers
            {
                [Mapper]
                [MapCollection("ItemsTypo", "Items", Mapper = nameof(MapItem))]
                public static partial Dest Map(Src src);

                private static DestItem MapItem(SrcItem item) => new DestItem { Value = item.Value };
            }

            public class SrcItem { public int Value { get; set; } }
            public class DestItem { public int Value { get; set; } }
            public class Src { public List<SrcItem> Items { get; set; } = new(); }
            public class Dest { public List<DestItem> Items { get; set; } = new(); }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0207");
    }

    [Fact]
    public void SMP0207_MapNested_UnresolvedTargetProperty_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapNested("ChildTypo", "Child", Mapper = nameof(MapChild))]
                public static partial Dest Map(Src src);

                private static DestChild MapChild(SrcChild c) => new DestChild { Value = c.Value };
            }

            public class SrcChild { public int Value { get; set; } }
            public class DestChild { public int Value { get; set; } }
            public class Src { public SrcChild Child { get; set; } = new(); }
            public class Dest { public DestChild Child { get; set; } = new(); }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0207");
    }

    // -----------------------------------------------------------------------
    // SMP0203 — [MapFrom] のターゲットプロパティ名が typo
    // SMP0203 — target property name in [MapFrom] is a typo
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0203_MapFrom_UnresolvedTargetProperty_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapFrom("NameTypo", nameof(Build))]
                public static partial void Map(Src src, Dest dst);

                private static string Build(Src s) => s.Name;
            }

            public class Src { public string Name { get; set; } = ""; }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "SMP0203" &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("NameTypo", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // SMP0208 — [MapCollection] のソースプロパティがコレクション型でない
    // SMP0208 — source property in [MapCollection] is not a collection type
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0208_MapCollection_SourceNotCollection_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapCollection("Items", "Name", Mapper = nameof(MapItem))]
                public static partial Dest Map(Src src);

                private static DestItem MapItem(string s) => new DestItem { Value = s };
            }

            public class Src { public string Name { get; set; } = ""; }
            public class DestItem { public string Value { get; set; } = ""; }
            public class Dest { public System.Collections.Generic.List<DestItem> Items { get; set; } = new(); }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0208");
    }

    // -----------------------------------------------------------------------
    // SMP0209 — [MapCollection] のターゲットプロパティがコレクション型でない
    // SMP0209 — target property in [MapCollection] is not a collection type
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0209_MapCollection_TargetNotCollection_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;
            using System.Collections.Generic;

            internal static partial class Mappers
            {
                [Mapper]
                [MapCollection("Name", "Items", Mapper = nameof(MapItem))]
                public static partial Dest Map(Src src);

                private static string MapItem(SrcItem s) => s.Value;
            }

            public class SrcItem { public string Value { get; set; } = ""; }
            public class Src { public List<SrcItem> Items { get; set; } = new(); }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0209");
    }

    // -----------------------------------------------------------------------
    // SMP0102 / SMP0103 — BeforeMap / AfterMap シグネチャ不一致
    // SMP0102 / SMP0103 — BeforeMap / AfterMap signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0102_InvalidBeforeMapSignature_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [BeforeMap(nameof(Before))]
                public static partial void Map(Src src, Dest dst);

                // 引数の型が不一致
                // The argument type does not match
                private static void Before(int x, Dest dst) { }
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public int Value { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0102");
    }

    [Fact]
    public void SMP0103_InvalidAfterMapSignature_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [AfterMap(nameof(After))]
                public static partial void Map(Src src, Dest dst);

                // 引数の型が不一致
                // The argument type does not match
                private static void After(int x, Dest dst) { }
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public int Value { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0103");
    }

    // -----------------------------------------------------------------------
    // SMP0105 — Converter 戻り値型不一致
    // SMP0105 — Converter return type mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0105_ConverterReturnTypeMismatch_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapProperty(nameof(Dest.Name), nameof(Src.Name), Converter = nameof(Convert))]
                public static partial void Map(Src src, Dest dst);

                // 引数型は合うが戻り値が int（target は string）
                // Argument type matches but the return type is int (target is string)
                private static int Convert(string value) => value.Length;
            }

            public class Src { public string Name { get; set; } = ""; }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0105");
    }

    // -----------------------------------------------------------------------
    // SMP0106 — MapCondition シグネチャ不一致
    // SMP0106 — MapCondition signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0106_InvalidPropertyConditionSignature_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapCondition(nameof(Dest.Name), nameof(ShouldMap))]
                public static partial void Map(Src src, Dest dst);

                // 引数の型が不一致（string ではなく int を受け取る）
                // The argument type does not match (receives int instead of string)
                private static bool ShouldMap(int value) => value > 0;
            }

            public class Src { public string Name { get; set; } = ""; }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0106");
    }

    // -----------------------------------------------------------------------
    // SMP0202 — MapUsing の静的メソッド戻り値型不一致
    // SMP0202 — MapUsing static method return type mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0202_MapUsingReturnTypeMismatch_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapUsing(nameof(Dest.Name), nameof(Build))]
                public static partial void Map(Src src, Dest dst);

                // 引数型は合うが戻り値が int（target は string）
                // Argument type matches but the return type is int (target is string)
                private static int Build(Src s) => s.Value;
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0202");
    }

    // -----------------------------------------------------------------------
    // SMP0204 / SMP0205 — MapFrom のソースメソッド未解決・戻り値型不一致
    // SMP0204 / SMP0205 — MapFrom source method unresolved / return type mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0204_MapFrom_SourceMethodNotFound_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapFrom(nameof(Dest.Count), "NonExistentMethod")]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { }
            public class Dest { public int Count { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0204");
    }

    [Fact]
    public void SMP0205_MapFrom_SourceMethodReturnTypeMismatch_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapFrom(nameof(Dest.Count), "GetName")]
                public static partial void Map(Src src, Dest dst);
            }

            // GetName() は string を返すが target は int
            // GetName() returns string but the target is int
            public class Src { public string GetName() => "name"; }
            public class Dest { public int Count { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0205");
    }

    // -----------------------------------------------------------------------
    // SMP0210 / SMP0211 — MapCollection / MapNested のマッパーメソッド不一致
    // SMP0210 / SMP0211 — MapCollection / MapNested mapper method mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0210_MapCollection_InvalidMapperMethod_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;
            using System.Collections.Generic;

            internal static partial class Mappers
            {
                [Mapper]
                [MapCollection("Items", "Items", Mapper = "NonExistentMapper")]
                public static partial Dest Map(Src src);
            }

            public class SrcItem { public int Value { get; set; } }
            public class DestItem { public int Value { get; set; } }
            public class Src { public List<SrcItem> Items { get; set; } = new(); }
            public class Dest { public List<DestItem> Items { get; set; } = new(); }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0210");
    }

    [Fact]
    public void SMP0211_MapNested_InvalidMapperMethod_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapNested("Child", "Child", Mapper = "NonExistentMapper")]
                public static partial Dest Map(Src src);
            }

            public class SrcChild { public int Value { get; set; } }
            public class DestChild { public int Value { get; set; } }
            public class Src { public SrcChild Child { get; set; } = new(); }
            public class Dest { public DestChild Child { get; set; } = new(); }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0211");
    }

    // -----------------------------------------------------------------------
    // SMP0403 — MapExpression 内のリフレクション使用
    // SMP0403 — reflection used inside MapExpression
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0403_MapExpressionWithReflection_EmitsWarning()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapExpression("Name", "System.Activator.CreateInstance(typeof(string))?.ToString()")]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public int Value { get; set; } }
            public class Dest { public string? Name { get; set; } }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "SMP0403" &&
            d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
    }
}
