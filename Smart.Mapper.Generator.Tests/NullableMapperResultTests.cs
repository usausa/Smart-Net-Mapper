namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The mapper of [MapNested] / [MapCollection] returning a nullable reference, as one declared to take null does
// (Map(Src? source)), is taken with ! into a target or elements not annotated as nullable, as a null source member
// is taken as default!, unless it gets a value that is not null and returns one for it ([return: NotNullIfNotNull],
// which a generated mapper declares, see NotNullIfNotNullTests). The collections the generated code creates keep the
// nullable annotations of the elements of the target (List<Item?>), and the local of a constructor argument takes a
// nullable result. The generated code used to warn in each of these (CS8601, CS8604, CS8619, CS8620, CS8621, CS8600).
public class NullableMapperResultTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string types, string attributes, string mapper = "public static partial Dst Map(Src src);", string classAttributes = "") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Collections.ObjectModel;
        using Smart.Mapper;
        namespace Test;
        public class SrcChild { public int V { get; set; } }
        public class DstChild { public int V { get; set; } }
        {{types}}
        {{classAttributes}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{mapper}}

            [Mapper]
            public static partial DstChild? MapChild(SrcChild? src);

            [Mapper]
            public static partial DstChild MapPlain(SrcChild src);
        }
        """;

    // A source that is not null gets a result that is not null from the generated mapper, which declares it so
    [Theory]
    [InlineData("public class Src { public SrcChild C { get; set; } = new(); } public class Dst { public DstChild C { get; set; } = new(); }", "__d.C = MapChild(src.C);")]
    [InlineData("public class Src { public SrcChild? C { get; set; } } public class Dst { public DstChild C { get; set; } = new(); }", "__d.C = MapChild(src.C)!;")]
    [InlineData("public class Src { public SrcChild C { get; set; } = new(); } public class Dst { public DstChild? C { get; set; } }", "__d.C = MapChild(src.C);")]
    public void NestedNullableResultIsForgivenForNonNullableTarget(string types, string expected)
    {
        var (generated, problems) = Build(Source(types, "[MapNested(\"C\", Mapper = nameof(MapChild))]"));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The local of a constructor argument takes the nullable result for a nullable parameter; with a mapper returning
    // a value that is not null, it is declared as before
    [Theory]
    [InlineData("MapChild", "global::Test.DstChild? __arg0;")]
    [InlineData("MapPlain", "global::Test.DstChild __arg0;")]
    public void NestedConstructorArgumentTakesNullableResult(string mapper, string expected)
    {
        var (generated, problems) = Build(Source(
            "public class Src { public SrcChild C { get; set; } = new(); } public record Dst(DstChild? C);",
            $"[MapNested(\"C\", Mapper = nameof({mapper}))]"));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public List<DstChild?> C { get; set; } = [];", "new global::System.Collections.Generic.List<global::Test.DstChild?>(", "__dst[__i] = MapChild(__src[__i]);")]
    [InlineData("public DstChild?[] C { get; set; } = [];", "new global::Test.DstChild?[", "__arr[__i] = MapChild(__src[__i]);")]
    [InlineData("public HashSet<DstChild?> C { get; set; } = [];", "new global::System.Collections.Generic.HashSet<global::Test.DstChild?>(", "__set.Add(MapChild(__src[__i]));")]
    [InlineData("public ImmutableArray<DstChild?> C { get; set; } = [];", "ImmutableArray.CreateBuilder<global::Test.DstChild?>(", "__ib.Add(MapChild(__src[__i]));")]
    [InlineData("public ObservableCollection<DstChild?> C { get; set; } = [];", "new global::System.Collections.ObjectModel.ObservableCollection<global::Test.DstChild?>();", "__items.Add(MapChild(__src[__i]));")]
    [InlineData("public List<DstChild> C { get; set; } = [];", "new global::System.Collections.Generic.List<global::Test.DstChild>(", "__dst[__i] = MapChild(__src[__i])!;")]
    [InlineData("public DstChild[] C { get; set; } = [];", "new global::Test.DstChild[", "__arr[__i] = MapChild(__src[__i])!;")]
    [InlineData("public HashSet<DstChild> C { get; set; } = [];", "new global::System.Collections.Generic.HashSet<global::Test.DstChild>(", "__set.Add(MapChild(__src[__i])!);")]
    public void CollectionKeepsElementAnnotations(string target, string creation, string element)
    {
        var (generated, problems) = Build(Source(
            "public class Src { public List<SrcChild?> C { get; set; } = []; } public class Dst { " + target + " }",
            "[MapCollection(\"C\", Mapper = nameof(MapChild))]"));

        Assert.Empty(problems);
        Assert.Contains(creation, generated, StringComparison.Ordinal);
        Assert.Contains(element, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void EnumerableSourceKeepsElementAnnotations()
    {
        var (generated, problems) = Build(Source(
            "public class Src { public IEnumerable<SrcChild?> C { get; set; } = []; public IReadOnlyCollection<SrcChild?> D { get; set; } = []; } " +
            "public class Dst { public DstChild?[] C { get; set; } = []; public List<DstChild> D { get; set; } = []; }",
            "[MapCollection(\"C\", Mapper = nameof(MapChild))] [MapCollection(\"D\", Mapper = nameof(MapChild))]"));

        Assert.Empty(problems);
        Assert.Contains("new global::System.Collections.Generic.List<global::Test.DstChild?>();", generated, StringComparison.Ordinal);
        Assert.Contains("__list.Add(MapChild(__item)!);", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void InPlaceKeepsElementAnnotations()
    {
        var (generated, problems) = Build(Source(
            "public class Src { public List<SrcChild?> C { get; set; } = []; } public class Dst { public IList<DstChild?>? C { get; set; } }",
            "[MapCollection(\"C\", Mapper = nameof(MapChild), Strategy = CollectionStrategy.InPlace)]",
            "public static partial void Map(Src src, Dst dst);"));

        Assert.Empty(problems);
        Assert.Contains("dst.C = new global::System.Collections.Generic.List<global::Test.DstChild?>(src.C.Count);", generated, StringComparison.Ordinal);
        Assert.Contains("((global::System.Collections.Generic.ICollection<global::Test.DstChild?>)dst.C).Clear();", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ConverterTakesElementAnnotations()
    {
        var (generated, problems) = Build(Source(
            "public class Src { public List<SrcChild?> C { get; set; } = []; } public class Dst { public List<DstChild?> C { get; set; } = []; }",
            "[MapCollection(\"C\", Mapper = nameof(MapChild))]",
            classAttributes: "[CollectionConverter(typeof(DefaultCollectionConverter))]"));

        Assert.Empty(problems);
        Assert.Contains("ToList<global::Test.SrcChild?, global::Test.DstChild?>(src.C, MapChild)!", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorArgumentCollectionKeepsElementAnnotations()
    {
        var (generated, problems) = Build(Source(
            "public class Src { public List<SrcChild?> C { get; set; } = []; } public record Dst(ObservableCollection<DstChild?> C);",
            "[MapCollection(\"C\", Mapper = nameof(MapChild))]"));

        Assert.Empty(problems);
        Assert.Contains("global::System.Collections.ObjectModel.ObservableCollection<global::Test.DstChild?> __arg0;", generated, StringComparison.Ordinal);
    }
}
