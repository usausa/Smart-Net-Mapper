namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// 明示的なマッピング属性のソース解決が Mapper の NameComparison に従うことを検証する。
// Verifies that source resolution for explicit mapping attributes honours the mapper's NameComparison.
//   - 完全一致（Ordinal）を優先し、一致しない場合のみ NameComparison にフォールバックする。
//     An exact ordinal match wins; the configured comparison is only a fallback.
//   - 解決した実際の宣言名が生成コードに出力される（属性の綴りではない）。
//     The declared name of the resolved property is emitted, not the spelling used in the attribute.
public class NameComparisonResolutionTests
{
    private static void AssertCompiles(string source)
    {
        var errors = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    // Source 省略時、ターゲット名の綴りでソース側を大文字小文字無視で解決する。
    // With Source omitted, the target spelling resolves case-insensitively against the source.
    [Fact]
    public void MapNestedResolvesSourceCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class SrcC { public int X { get; set; } }
            public class DstC { public int X { get; set; } }
            public class Src { public SrcC? child { get; set; } }
            public class Dst { public DstC? Child { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapNested("Child", Mapper = nameof(MapChild))]
                public static partial Dst Map(Src src);
                [Mapper]
                public static partial DstC MapChild(SrcC s);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.child", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MapCollectionResolvesSourceCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            using System.Collections.Generic;
            namespace Test;
            public class SrcI { public int X { get; set; } }
            public class DstI { public int X { get; set; } }
            public class Src { public List<SrcI> items { get; set; } = new(); }
            public class Dst { public List<DstI> Items { get; set; } = new(); }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapCollection("Items", Mapper = nameof(MapItem))]
                public static partial Dst Map(Src src);
                [Mapper]
                public static partial DstI MapItem(SrcI s);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.items", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MapPropertyResolvesSourceCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class Src { public int other { get; set; } }
            public class Dst { public int Value { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapProperty("Value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.other", generated, StringComparison.Ordinal);
    }

    // ドット記法でも各セグメントが解決され、実際の宣言名で出力される。
    // Every segment of a dotted path is resolved and emitted with its declared name.
    [Fact]
    public void MapPropertyResolvesNestedSourcePathCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class SrcC { public int val { get; set; } }
            public class Src { public SrcC child { get; set; } = new(); }
            public class Dst { public int Value { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapProperty("Value", "Child.Val")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        // セグメント間には null 免除の "!" が挿入される。
        // A null-forgiving "!" is inserted between the segments.
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.child!.val", generated, StringComparison.Ordinal);
    }

    // 完全一致が存在する場合は、大小文字違いの候補より優先される。
    // An exact match takes precedence over a candidate differing only by case.
    [Fact]
    public void ExactMatchWinsOverCaseInsensitiveCandidate()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class Src { public int Value { get; set; } public int value { get; set; } }
            public class Dst { public int Target { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapProperty("Target", "value")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("src.value", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.Value", generated, StringComparison.Ordinal);
    }

    // ターゲット側も NameComparison に従い、宣言名に正規化されて出力される。
    // The target side honours NameComparison too, and is emitted using the declared name.
    [Fact]
    public void MapPropertyResolvesTargetCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public class Dst { public int Value { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapProperty("value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("destination.Value = src.Other", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MapNestedResolvesTargetCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class SrcC { public int X { get; set; } }
            public class DstC { public int X { get; set; } }
            public class Src { public SrcC? Child { get; set; } }
            public class Dst { public DstC? Child { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapNested("child", Mapper = nameof(MapChild))]
                public static partial Dst Map(Src src);
                [Mapper]
                public static partial DstC MapChild(SrcC s);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("destination.Child", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MapCollectionResolvesTargetCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            using System.Collections.Generic;
            namespace Test;
            public class SrcI { public int X { get; set; } }
            public class DstI { public int X { get; set; } }
            public class Src { public List<SrcI> Items { get; set; } = new(); }
            public class Dst { public List<DstI> Items { get; set; } = new(); }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapCollection("items", Mapper = nameof(MapItem))]
                public static partial Dst Map(Src src);
                [Mapper]
                public static partial DstI MapItem(SrcI s);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("destination.Items", generated, StringComparison.Ordinal);
    }

    // [MapIgnore] などターゲット名のみを取る属性も同様に一致する。
    // Attributes that only take a target name, such as [MapIgnore], match the same way.
    [Fact]
    public void MapIgnoreResolvesTargetCaseInsensitively()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class Src { public int Value { get; set; } public int Keep { get; set; } }
            public class Dst { public int Value { get; set; } public int Keep { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapIgnore("value")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("destination.Keep", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("destination.Value", generated, StringComparison.Ordinal);
    }

    // 綴り違いで同一メンバーを二重指定した場合は重複として検出される。
    // Two attributes naming the same member with different casing are detected as duplicates.
    [Fact]
    public void DuplicateTargetIsDetectedAcrossCasing()
    {
        var source = """
            using Smart.Mapper;
            using System;
            namespace Test;
            public class Src { public int A { get; set; } public int B { get; set; } }
            public class Dst { public int Value { get; set; } }
            public partial class M
            {
                [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapProperty("Value", "A")]
                [MapProperty("value", "B")]
                public static partial Dst Map(Src src);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0101");
    }

    // 既定（Ordinal）ではターゲット側の大小文字違いも従来どおり診断となる。
    // Under the default comparison a target-side case mismatch is still diagnosed.
    [Fact]
    public void DefaultComparisonStillRejectsTargetCaseMismatch()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Other { get; set; } }
            public class Dst { public int Value { get; set; } }
            public partial class M
            {
                [Mapper]
                [MapProperty("value", "Other")]
                public static partial Dst Map(Src src);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0214");
    }

    // 既定（Ordinal）では大小文字違いは解決されず、従来どおり診断となる。
    // Under the default (Ordinal) comparison a case difference stays unresolved and is diagnosed.
    [Fact]
    public void DefaultComparisonStillRejectsCaseMismatch()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class SrcC { public int X { get; set; } }
            public class DstC { public int X { get; set; } }
            public class Src { public SrcC? child { get; set; } }
            public class Dst { public DstC? Child { get; set; } }
            public partial class M
            {
                [Mapper]
                [MapNested("Child", Mapper = nameof(MapChild))]
                public static partial Dst Map(Src src);
                [Mapper]
                public static partial DstC MapChild(SrcC s);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SMP0206");
    }
}
