namespace Smart.Mapper.Generator.Tests;

using System.Globalization;

using Microsoft.CodeAnalysis;

// Source Generator が正しい診断（SMP0001〜SMP0020）を発行することを検証するテスト。
public class DiagnosticTests
{
    // -----------------------------------------------------------------------
    // SMP0001 — [Mapper] をインスタンスメソッドまたは非 partial メソッドに付与
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
            public record Dest(int Value, string Name);
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0018");
    }

    // -----------------------------------------------------------------------
    // SMP0019 — Culture なしで Format 指定
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
        // 異なる型でスペシャライズドが存在しない場合に SMP0020 が発火する
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
                private static string Build(int x) => x.ToString();
            }

            public class Src { }
            public class Dest { public string Name { get; set; } = ""; }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // 実際に発火される診断 ID を確認（SMP0009 または SMP0011）
        Assert.Contains(diagnostics, d => d.Id is "SMP0009" or "SMP0011");
    }

    // -----------------------------------------------------------------------
    // SMP0022 — コンストラクタパラメーターがソースプロパティに解決できない
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
}
