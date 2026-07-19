namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// コンストラクタ経由で代入されるメンバーに対する明示的 [MapProperty] リネームの解決を検証する。
// Verifies resolution of explicit [MapProperty] renames onto members assigned through a constructor.
//   - BuildPropertyMappings はセッターの無い対象を除外するため、get-only プロパティへの
//     BuildPropertyMappings skips targets without a setter, so a rename onto a get-only property
//     リネームはコンストラクタ解決時に失われていた（SMP0301 が誤発生していた）。
//     used to be lost by the time constructor parameters were resolved (spurious SMP0301).
public class ConstructorParameterMappingTests
{
    private static void AssertCompiles(string source)
    {
        var errors = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    // get-only プロパティをパラメータ付きコンストラクタで代入する場合のリネーム。
    // Rename onto a get-only property assigned through a parameterized constructor.
    [Fact]
    public void GetOnlyConstructorParameterHonorsExplicitRename()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public class Dst
            {
                public int Value { get; }
                public Dst(int value) { Value = value; }
            }
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Other", generated, StringComparison.Ordinal);
    }

    // レコードの位置指定パラメータ（init アクセサを持つ）でのリネーム。
    // Rename onto a record positional parameter, which carries an init accessor.
    [Fact]
    public void RecordConstructorParameterHonorsExplicitRename()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public record Dst(int Value);
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Other", generated, StringComparison.Ordinal);
    }

    // init-only プロパティをコンストラクタで代入する場合のリネーム。
    // Rename onto an init-only property assigned through a constructor.
    [Fact]
    public void InitOnlyConstructorParameterHonorsExplicitRename()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public class Dst
            {
                public int Value { get; init; }
                public Dst(int value) { Value = value; }
            }
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Other", generated, StringComparison.Ordinal);
    }

    // 明示マッピングが無い場合は従来どおり名前一致で解決されること。
    // Without an explicit mapping, resolution still falls back to name matching.
    [Fact]
    public void GetOnlyConstructorParameterFallsBackToNameMatching()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst
            {
                public int Value { get; }
                public Dst(int value) { Value = value; }
            }
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.Value", generated, StringComparison.Ordinal);
    }

    // 実際に呼ばれるコンストラクタ（最長のもの）に含まれない get-only プロパティは、
    // A get-only property not covered by the constructor construction actually calls (the longest
    // 従来どおりスキップされ、get-only への代入（CS0200）を生成しない。
    // one) stays skipped, so no assignment to a get-only property (CS0200) is emitted.
    [Fact]
    public void GetOnlyPropertyOutsideBestConstructorIsSkipped()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int A { get; set; } public int B { get; set; } public int Extra { get; set; } }
            public class Dst
            {
                public Dst(int extra) { Extra = extra; }
                public Dst(int a, int b) { A = a; B = b; }
                public int A { get; set; }
                public int B { get; set; }
                public int Extra { get; }
            }
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("new global::Test.Dst(src.A, src.B)", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("destination.Extra", generated, StringComparison.Ordinal);
    }

    // void マッパーはコンストラクタを呼ばないため、便宜的なパラメータ付きコンストラクタが
    // A void mapper never constructs, so a convenience parameterized constructor on the destination
    // あっても SMP0302 にならず通常どおりマッピングされる。
    // does not trigger SMP0302 and mapping proceeds normally.
    [Fact]
    public void VoidMapperIgnoresConvenienceConstructor()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst
            {
                public Dst() { }
                public Dst(int value) { Value = value; }
                public int Value { get; set; }
            }
            public partial class M
            {
                [Mapper]
                public static partial void Map(Src src, Dst dst);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("dst.Value = src.Value", generated, StringComparison.Ordinal);
    }

    // パラメータレスコンストラクタが無くても、void マッパーは構築しないため成立する。
    // Even without a parameterless constructor a void mapper works - it never constructs.
    [Fact]
    public void VoidMapperWorksWithoutParameterlessConstructor()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst
            {
                public Dst(int value) { Value = value; }
                public int Value { get; set; }
            }
            public partial class M
            {
                [Mapper]
                public static partial void Map(Src src, Dst dst);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("dst.Value = src.Value", generated, StringComparison.Ordinal);
    }

    // コンストラクタを使わない構築でも、init 専用メンバーはオブジェクト初期化子で代入される。
    // Init-only members are assigned via the object initializer even when construction is
    // parameterless (constructor parameters all match settable properties).
    [Fact]
    public void InitOnlyMemberUsesInitializerWhenConstructorNotRequired()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class Src { public int A { get; set; } public int B { get; set; } }
            public class Dst
            {
                public Dst() { }
                public Dst(int a) { A = a; }
                public int A { get; set; }
                public int B { get; init; }
            }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("B = src.B", generated, StringComparison.Ordinal);
        Assert.Contains("destination.A = src.A", generated, StringComparison.Ordinal);
    }

    // 何もコンストラクタを要求しない場合はパラメータレス構築が選ばれる。
    // Parameterless construction is preferred when nothing requires the constructor.
    [Fact]
    public void ParameterlessConstructionPreferredWhenNothingRequiresConstructor()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst
            {
                public Dst() { }
                public Dst(int value) { Value = value; }
                public int Value { get; set; }
            }
            public partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("new global::Test.Dst()", generated, StringComparison.Ordinal);
        Assert.Contains("destination.Value = src.Value", generated, StringComparison.Ordinal);
    }

    // パラメータレスコンストラクタが無い型は、引き続きパラメータ付きコンストラクタで構築される。
    // A type without a parameterless constructor keeps constructing through the parameterized one.
    [Fact]
    public void ParameterizedConstructorUsedWhenNoParameterlessExists()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst
            {
                public Dst(int value) { Value = value; }
                public int Value { get; set; }
            }
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

    // 解決できないソースを指定した明示マッピングは、名前一致で代替せず SMP0213 で報告する。
    // An explicit mapping with an unresolvable source is reported as SMP0213 rather than silently
    // falling back to name matching, even when a same-named source property happens to exist.
    [Fact]
    public void UnresolvableExplicitRenameIsReported()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst
            {
                public int Value { get; }
                public Dst(int value) { Value = value; }
            }
            public partial class M
            {
                [Mapper]
                [MapProperty("Value", "NoSuchSource")]
                public static partial Dst Map(Src src);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "SMP0213");
    }
}
