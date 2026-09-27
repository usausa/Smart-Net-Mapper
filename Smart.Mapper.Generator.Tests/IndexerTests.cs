namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// An indexer is not a member a name reaches. The automatic mapping used to assign one as a property named this[]
// (__d.this[] = src.this[]), which does not compile, and Strict to report one as unmapped. Indexers are left out,
// on the source and on the destination.
public class IndexerTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static string Source(string types, string mapper = "[Mapper]", string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{signature}}
        }
        """;

    private const string Indexed = """
        public class Src { public int this[int index] => index; public int Y { get; set; } }
        public class Dst { public int this[int index] { get => index; set { } } public int Y { get; set; } }
        """;

    [Theory]
    [InlineData("public static partial Dst Map(Src src);", "__d.Y = src.Y;")]
    [InlineData("public static partial void Map(Src src, Dst dst);", "dst.Y = src.Y;")]
    public void IndexersAreLeftOut(string signature, string expected)
    {
        var source = Source(Indexed, signature: signature);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("this[", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void StrictDoesNotReportIndexer()
    {
        AssertCompiles(Source(Indexed, "[Mapper(Strict = true)]"));
    }
}
