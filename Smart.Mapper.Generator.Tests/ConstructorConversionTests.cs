namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// コンストラクタ引数およびオブジェクト初期化子が、通常のプロパティ代入と同じ変換パイプラインを
// Verifies that constructor arguments and object-initializer entries go through the same conversion
// 通ることを検証する。以前はソースアクセサをそのまま出力していたため、変換が必要な場合は
// pipeline as ordinary property assignments. They previously emitted a bare source accessor, so any
// 生成コードが CS1503 でコンパイルできなかった。
// case needing a conversion produced generated code that failed to compile with CS1503.
public class ConstructorConversionTests
{
    private static void AssertCompiles(string source)
    {
        var errors = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    // 変換が必要な型（int -> string）をコンストラクタ引数で受ける。
    // A constructor argument whose type requires a conversion (int -> string).
    [Fact]
    public void ConstructorParameterAppliesTypeConversion()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public record Dst(string Value);
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
    }

    // Converter がコンストラクタ引数にも適用される。
    // A Converter is applied to a constructor argument as well.
    [Fact]
    public void ConstructorParameterAppliesConverter()
    {
        var source = """
            using Smart.Mapper;
            using System.Globalization;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public record Dst(string Value);
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "Other", Converter = nameof(Conv))]
                public static partial Dst Map(Src src);
                public static string Conv(int v) => v.ToString(CultureInfo.InvariantCulture);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("Conv(src.Other)", generated, StringComparison.Ordinal);
    }

    // NullValue がコンストラクタ引数にも適用される。
    // A NullValue is applied to a constructor argument as well.
    [Fact]
    public void ConstructorParameterAppliesNullValue()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int? Value { get; set; } }
            public record Dst(int Value);
            public partial class M
            {
                [Mapper]
                [MapProperty<int>("Value", NullValue = 99)]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("99", generated, StringComparison.Ordinal);
    }

    // 暗黙変換で足りる場合は従来どおり素のアクセサを出力する。
    // When C#'s own implicit conversion suffices, a bare accessor is still emitted.
    [Fact]
    public void ConstructorParameterKeepsImplicitConversion()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public record Dst(long Value);
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("new global::Test.Dst(src.Value)", generated, StringComparison.Ordinal);
    }

    // null 許容ソース + 型変換をコンストラクタ引数で受ける場合、三項式に展開され、
    // A nullable source needing conversion expands to a conditional expression, falling back to the
    // null 時はターゲット型の default になる。
    // destination type's default when null.
    [Fact]
    public void ConstructorParameterConvertsNullableSourceWithDefaultFallback()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int? Value { get; set; } }
            public record Dst(string Value);
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Value is not null ?", generated, StringComparison.Ordinal);
        Assert.Contains(": default!", generated, StringComparison.Ordinal);
    }

    // get-only プロパティ + パラメータ付きコンストラクタでも変換が適用される。
    // Conversion also applies to a get-only property assigned through a parameterized constructor.
    [Fact]
    public void GetOnlyConstructorParameterAppliesConversion()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public class Dst
            {
                public string Value { get; }
                public Dst(string value) { Value = value; }
            }
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
    }

    // null 許容な中間セグメントを持つドット記法ソースは、コンストラクタ引数でも
    // A dotted source with a nullable intermediate segment is guarded in constructor arguments the
    // 文経路と同様に null ガード（三項式）で保護される。
    // same way the statement path guards it, via a conditional expression.
    [Fact]
    public void ConstructorParameterGuardsNullableIntermediateSegment()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class SrcC { public int Val { get; set; } }
            public class Src { public SrcC? Child { get; set; } }
            public record Dst(string Value);
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "Child.Val")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Child is not null ?", generated, StringComparison.Ordinal);
        Assert.Contains(": default!", generated, StringComparison.Ordinal);
    }

    // 対応するプロパティを持たないコンストラクタ引数にも変換が適用される。
    // Conversion also applies to a constructor parameter with no backing destination property.
    [Fact]
    public void ConstructorParameterWithoutPropertyAppliesConversion()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst
            {
                public Dst(string value) { Text = value; }
                public string Text { get; }
            }
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
    }

    // 対応するプロパティを持たない引数を [MapProperty] でリネームできる。
    // A parameter with no backing property can still be remapped with [MapProperty].
    [Fact]
    public void ConstructorParameterWithoutPropertyHonorsExplicitRename()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public class Dst
            {
                public Dst(string value) { Text = value; }
                public string Text { get; }
            }
            public partial class M
            {
                [Mapper]
                [MapProperty("value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Other", generated, StringComparison.Ordinal);
    }

    // 対応するプロパティを持たない引数にも Converter が適用される。
    // A Converter applies to a parameter with no backing property as well.
    [Fact]
    public void ConstructorParameterWithoutPropertyAppliesConverter()
    {
        var source = """
            using Smart.Mapper;
            using System.Globalization;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public class Dst
            {
                public Dst(string value) { Text = value; }
                public string Text { get; }
            }
            public partial class M
            {
                [Mapper]
                [MapProperty("value", "Other", Converter = nameof(Conv))]
                public static partial Dst Map(Src src);
                public static string Conv(int v) => v.ToString(CultureInfo.InvariantCulture);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("Conv(src.Other)", generated, StringComparison.Ordinal);
    }

    // init 専用メンバーをオブジェクト初期化子で代入する場合にも変換が適用される。
    // Conversion also applies to init-only members assigned in the object initializer.
    [Fact]
    public void InitOnlyInitializerAppliesTypeConversion()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Id { get; set; } public int Number { get; set; } }
            public class Dst
            {
                public int Id { get; }
                public string Number { get; init; } = "";
                public Dst(int id) { Id = id; }
            }
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
    }

    // オブジェクト初期化子でもドット記法のソースパスにパラメーター名が付与される。
    // A dotted source path in the object initializer is prefixed with the source parameter name.
    [Fact]
    public void InitOnlyInitializerPrefixesNestedSourcePath()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class SrcC { public int Val { get; set; } }
            public class Src { public SrcC Child { get; set; } = new(); public int Id { get; set; } }
            public class Dst
            {
                public int Id { get; }
                public int Value { get; init; }
                public Dst(int id) { Id = id; }
            }
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "Child.Val")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Child", generated, StringComparison.Ordinal);
    }
}
