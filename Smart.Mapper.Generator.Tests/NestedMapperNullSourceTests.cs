namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The mapper of [MapNested] whose parameter takes null gets a null source member as well, and decides what the target
// gets for it, as the converter of a property and the mapper of the elements of a collection do; a void one fills the
// instance created for it. One whose parameter does not take null is called for a value only, the target getting
// default. A null source used to give default whatever the mapper took.
public class NestedMapperNullSourceTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class SrcChild { public int V { get; set; } }
        public class DstChild { public int V { get; set; } }
        public struct Point { public int X { get; set; } }
        public class Src { public SrcChild? C { get; set; } public Point? P { get; set; } }
        public class Dst { public DstChild C { get; set; } = new(); public DstChild P { get; set; } = new(); }
        public record Rec(DstChild C);
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            [Mapper]
            public static partial DstChild MapPlain(SrcChild source);

            private static DstChild MapAny(SrcChild? source) => new() { V = source?.V ?? -1 };

            private static void FillAny(SrcChild? source, DstChild target) => target.V = source?.V ?? -1;

            private static DstChild MapPoint(Point source) => new() { V = source.X };
        }
        """;

    [Theory]
    [InlineData("[MapNested(nameof(Dst.C), Mapper = nameof(MapAny))]", "__d.C = MapAny(src.C);")]
    [InlineData("[MapNested(nameof(Dst.C), Mapper = nameof(MapPlain))]", "__d.C = src.C is not null ? MapPlain(src.C!) : default!;")]
    [InlineData("[MapNested(nameof(Dst.P), Mapper = nameof(MapPoint))]", "__d.P = src.P is not null ? MapPoint(src.P.Value) : default!;")]
    public void NullSourceGoesToMapperTakingNull(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullSourceGoesToVoidMapperTakingNull()
    {
        var (generated, problems) = Build(Source("[MapNested(nameof(Dst.C), Mapper = nameof(FillAny))]"));

        Assert.Empty(problems);
        Assert.Contains("FillAny(src.C, __nested_C);", generated, StringComparison.Ordinal);
        Assert.Contains("__d.C = __nested_C;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("is not null", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullSourceGoesToMapperTakingNullForConstructorArgument()
    {
        var (generated, problems) = Build(Source("[MapNested(nameof(Rec.C), Mapper = nameof(MapAny))]", "public static partial Rec Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("__arg0 = MapAny(src.C);", generated, StringComparison.Ordinal);
    }
}
