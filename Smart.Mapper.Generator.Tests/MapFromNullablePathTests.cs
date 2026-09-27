namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The property path of [MapFrom] goes through members that may be null the way the source path of [MapProperty]
// does: in an assignment, the target is assigned under their null check and left as it is when one is null, and
// as a constructor argument or an object initializer entry, the value is a conditional giving null to a target
// taking it, or default. A nullable reference, of a property or of a method, going to a target not annotated as
// one is taken with !, as [MapProperty] takes it. The path used to be read as it is, which warned in the generated
// code (CS8602 / CS8601 / CS8604) and threw for a null member. A path through members that cannot be null is read
// as before.
public class MapFromNullablePathTests
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
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Lines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Source(string attributes, string mapper = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Leaf { public string? Zip { get; set; } public string City { get; set; } = ""; public int Code { get; set; } public int? MaybeCode { get; set; } }
        public class Mid { public Leaf? Leaf { get; set; } public Leaf Solid { get; set; } = new(); }
        public class Src
        {
            public Mid? Mid { get; set; }
            public Mid Solid { get; set; } = new();
            public List<int>? Items { get; set; }
            public string? Nick { get; set; }
            public string? GetNick() => Nick;
        }
        public class Dst
        {
            public string Zip { get; set; } = "";
            public string? ZipOrNull { get; set; }
            public int Code { get; set; }
            public int? CodeOrNull { get; set; }
            public int Count { get; set; }
            public string Nick { get; set; } = "";
        }
        public record DstRecord(string Zip, int Code, int? CodeOrNull);
        public class InitDst { public string Zip { get; init; } = ""; public int? CodeOrNull { get; init; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}
        }
        """;

    [Fact]
    public void AssignmentThroughNullableMembersIsGuarded()
    {
        var (generated, problems) = Build(Source(
            "[MapFrom(nameof(Dst.ZipOrNull), \"Mid.Leaf.Zip\")] [MapFrom(nameof(Dst.Code), \"Mid.Solid.Code\")] [MapFrom(nameof(Dst.Count), \"Items.Count\")]"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    if (src.Mid is not null && src.Mid.Leaf is not null)
                    {
                        dst.ZipOrNull = src.Mid.Leaf.Zip;
                    }
                    if (src.Mid is not null)
                    {
                        dst.Code = src.Mid.Solid.Code;
                    }
                    if (src.Items is not null)
                    {
                        dst.Count = src.Items.Count;
                    }
            """),
            generated,
            StringComparison.Ordinal);
    }

    // A nullable reference going to a target not annotated as one is taken with !, whether the path can be null or not
    [Fact]
    public void NullableReferenceIsForgiven()
    {
        var (generated, problems) = Build(Source(
            "[MapFrom(nameof(Dst.Zip), \"Solid.Solid.Zip\")] [MapFrom(nameof(Dst.Nick), nameof(Src.GetNick))] [MapFrom(nameof(Dst.ZipOrNull), \"Solid.Leaf.Zip\")]"));

        Assert.Empty(problems);
        Assert.Contains("dst.Zip = src.Solid.Solid.Zip!;", generated, StringComparison.Ordinal);
        Assert.Contains("dst.Nick = src.GetNick()!;", generated, StringComparison.Ordinal);
        Assert.Contains("dst.ZipOrNull = src.Solid.Leaf.Zip;", generated, StringComparison.Ordinal);
    }

    // A path through members that cannot be null is read as before
    [Fact]
    public void PathThroughMembersNotNullIsReadAsItIs()
    {
        var (generated, problems) = Build(Source("[MapFrom(nameof(Dst.Code), \"Solid.Solid.Code\")] [MapFrom(nameof(Dst.Nick), nameof(Src.Nick))]"));

        Assert.Empty(problems);
        Assert.Contains("dst.Code = src.Solid.Solid.Code;", generated, StringComparison.Ordinal);
        Assert.Contains("dst.Nick = src.Nick!;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("if (", generated, StringComparison.Ordinal);
    }

    // A constructor argument and an object initializer entry take null for a target taking it, or default
    [Fact]
    public void ExpressionsTakeNullOrDefault()
    {
        var (record, recordProblems) = Build(Source(
            "[MapFrom(nameof(DstRecord.Zip), \"Mid.Leaf.Zip\")] [MapFrom(nameof(DstRecord.Code), \"Mid.Solid.Code\")] [MapFrom(nameof(DstRecord.CodeOrNull), \"Mid.Leaf.Code\")]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(recordProblems);
        Assert.Contains(
            "new global::Test.DstRecord(src.Mid is not null && src.Mid.Leaf is not null ? src.Mid.Leaf.Zip! : default!, src.Mid is not null ? src.Mid.Solid.Code : default!, src.Mid is not null && src.Mid.Leaf is not null ? src.Mid.Leaf.Code : null)",
            record,
            StringComparison.Ordinal);

        var (init, initProblems) = Build(Source(
            "[MapFrom(nameof(InitDst.Zip), \"Mid.Leaf.Zip\")] [MapFrom(nameof(InitDst.CodeOrNull), \"Mid.Leaf.MaybeCode\")]",
            "public static partial InitDst Map(Src src);"));

        Assert.Empty(initProblems);
        Assert.Contains("Zip = src.Mid is not null && src.Mid.Leaf is not null ? src.Mid.Leaf.Zip! : default!,", init, StringComparison.Ordinal);
        Assert.Contains("CodeOrNull = src.Mid is not null && src.Mid.Leaf is not null ? src.Mid.Leaf.MaybeCode : null,", init, StringComparison.Ordinal);
    }
}
