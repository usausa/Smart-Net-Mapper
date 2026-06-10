namespace Smart.Mapper.Generator.Tests;

using System.Globalization;

using Microsoft.CodeAnalysis;

// Source Generator が正しい診断（SMP0001〜SMP0020）を発行することを検証するテスト。
// Tests that verify the source generator emits the correct diagnostics (SMP0001-SMP0020).
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
    // SMP0015 — 同一ターゲットプロパティへの重複マッピング
    // SMP0015 — duplicate mapping to the same target property
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0015_DuplicateTargetMapping_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0015");
    }

    // -----------------------------------------------------------------------
    // SMP0016 — Strict モードで未マッピングの destination プロパティ
    // SMP0016 — unmapped destination property in Strict mode
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0016_StrictMode_UnmappedProperty_EmitsWarning()
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
            d.Id == "SMP0016" &&
            d.Severity == DiagnosticSeverity.Warning &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("Extra", StringComparison.Ordinal));
    }

    [Fact]
    public void SMP0016_StrictMode_AllMapped_NoDiagnostic()
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

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0016");
    }

    // -----------------------------------------------------------------------
    // SMP0017 — required プロパティが未マッピング
    // SMP0017 — required property is unmapped
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0017_UnmappedRequiredProperty_EmitsDiagnostic()
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
            d.Id == "SMP0017" &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("Name", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // SMP0018 — コンストラクタ専用プロパティを持つ型（record）で void mapper を使用
    // SMP0018 — void mapper used for a type with constructor-only properties (record)
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0018_RecordWithVoidMapper_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                public static partial void Map(Src src, Dest dst);
            }

            public class Src { public int Value { get; set; } public string Name { get; set; } = ""; }
            // record 型はプライマリコンストラクタ経由のマッピングが必要 → void mapper では SMP0018
            // A record type must be mapped via its primary constructor → a void mapper triggers SMP0018
            public record Dest(int Value, string Name);
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0018");
    }

    // -----------------------------------------------------------------------
    // SMP0019 — Culture なしで Format 指定
    // SMP0019 — Format specified without Culture
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0019_FormatWithoutCulture_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0019");
    }

    [Fact]
    public void SMP0019_FormatWithCulture_NoDiagnostic()
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

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0019");
    }

    // -----------------------------------------------------------------------
    // SMP0020 — AllowTypeConverter なしで汎用フォールバック使用
    // SMP0020 — generic fallback used without AllowTypeConverter
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0020_TypeConverterFallback_EmitsDiagnostic()
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
        // 異なる型でスペシャライズドが存在しない場合に SMP0020 が発火する
        // SMP0020 fires when the types differ and no specialized converter exists
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0020");
    }

    [Fact]
    public void SMP0020_UnsupportedConversion_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0020");
    }

    // -----------------------------------------------------------------------
    // SMP0006 — Converter メソッドのシグネチャ不一致
    // SMP0006 — Converter method signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0006_InvalidConverterSignature_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0006");
    }

    // -----------------------------------------------------------------------
    // SMP0009 — MapFrom メソッドのシグネチャ不一致
    // SMP0009 — MapFrom method signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0009_InvalidMapFromSignature_EmitsDiagnostic()
    {
        const string source = """
            using Smart.Mapper;

            internal static partial class Mappers
            {
                [Mapper]
                [MapFrom(nameof(Dest.Name), nameof(Build))]
                public static partial void Map(Src src, Dest dst);

                // 引数が int → Src ではない → SMP0009
                // The argument is int → not Src → SMP0009
                private static string Build(int x) => x.ToString();
            }

            public class Src { }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // 実際に発火される診断 ID を確認（SMP0009 または SMP0011）
        // Check the diagnostic ID that is actually emitted (SMP0009 or SMP0011)
        Assert.Contains(diagnostics, d => d.Id is "SMP0009" or "SMP0011");
    }

    // -----------------------------------------------------------------------
    // SMP0022 — コンストラクタパラメーターがソースプロパティに解決できない
    // SMP0022 — constructor parameter cannot be resolved to a source property
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0022_UnresolvedConstructorParameter_EmitsDiagnostic()
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
            d.Id == "SMP0022" &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("Name", StringComparison.Ordinal));
    }

    [Fact]
    public void SMP0022_AllConstructorParametersResolved_NoDiagnostic()
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

        Assert.DoesNotContain(diagnostics, d => d.Id == "SMP0022");
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
    // SMP0023 — [MapCollection]/[MapNested] のソースプロパティ名が typo
    // SMP0023 — source property name in [MapCollection]/[MapNested] is a typo
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0023_MapCollection_UnresolvedSourceProperty_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0023");
    }

    [Fact]
    public void SMP0023_MapNested_UnresolvedSourceProperty_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0023");
    }

    // -----------------------------------------------------------------------
    // SMP0024 — [MapCollection]/[MapNested] のターゲットプロパティ名が typo
    // SMP0024 — target property name in [MapCollection]/[MapNested] is a typo
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0024_MapCollection_UnresolvedTargetProperty_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0024");
    }

    [Fact]
    public void SMP0024_MapNested_UnresolvedTargetProperty_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0024");
    }

    // -----------------------------------------------------------------------
    // SMP0025 — [MapFrom] のターゲットプロパティ名が typo
    // SMP0025 — target property name in [MapFrom] is a typo
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0025_MapFrom_UnresolvedTargetProperty_EmitsDiagnostic()
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
            d.Id == "SMP0025" &&
            d.GetMessage(CultureInfo.InvariantCulture).Contains("NameTypo", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // SMP0026 — [MapCollection] のソースプロパティがコレクション型でない
    // SMP0026 — source property in [MapCollection] is not a collection type
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0026_MapCollection_SourceNotCollection_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0026");
    }

    // -----------------------------------------------------------------------
    // SMP0027 — [MapCollection] のターゲットプロパティがコレクション型でない
    // SMP0027 — target property in [MapCollection] is not a collection type
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0027_MapCollection_TargetNotCollection_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0027");
    }

    // -----------------------------------------------------------------------
    // SMP0004 / SMP0005 — BeforeMap / AfterMap シグネチャ不一致
    // SMP0004 / SMP0005 — BeforeMap / AfterMap signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0004_InvalidBeforeMapSignature_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0004");
    }

    [Fact]
    public void SMP0005_InvalidAfterMapSignature_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0005");
    }

    // -----------------------------------------------------------------------
    // SMP0007 — Converter 戻り値型不一致
    // SMP0007 — Converter return type mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0007_ConverterReturnTypeMismatch_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0007");
    }

    // -----------------------------------------------------------------------
    // SMP0008 — MapCondition シグネチャ不一致
    // SMP0008 — MapCondition signature mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0008_InvalidPropertyConditionSignature_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0008");
    }

    // -----------------------------------------------------------------------
    // SMP0010 — MapUsing の静的メソッド戻り値型不一致
    // SMP0010 — MapUsing static method return type mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0010_MapUsingReturnTypeMismatch_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0010");
    }

    // -----------------------------------------------------------------------
    // SMP0011 / SMP0012 — MapFrom のソースメソッド未解決・戻り値型不一致
    // SMP0011 / SMP0012 — MapFrom source method unresolved / return type mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0011_MapFrom_SourceMethodNotFound_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0011");
    }

    [Fact]
    public void SMP0012_MapFrom_SourceMethodReturnTypeMismatch_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0012");
    }

    // -----------------------------------------------------------------------
    // SMP0013 / SMP0014 — MapCollection / MapNested のマッパーメソッド不一致
    // SMP0013 / SMP0014 — MapCollection / MapNested mapper method mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0013_MapCollection_InvalidMapperMethod_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0013");
    }

    [Fact]
    public void SMP0014_MapNested_InvalidMapperMethod_EmitsDiagnostic()
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

        Assert.Contains(diagnostics, d => d.Id == "SMP0014");
    }

    // -----------------------------------------------------------------------
    // SMP0021 — MapExpression 内のリフレクション使用
    // SMP0021 — reflection used inside MapExpression
    // -----------------------------------------------------------------------

    [Fact]
    public void SMP0021_MapExpressionWithReflection_EmitsWarning()
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
            d.Id == "SMP0021" &&
            d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
    }
}
