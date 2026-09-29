namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The required members of the destination concern only the construction of a return mapper. A void mapper fills
// an instance that exists, so an unmapped required member is not reported for it (SMP0308 used to be). A
// required field is treated like a required property: a return mapper reports it unmapped (SMP0308) instead of
// failing in the generated code (CS9035), and assigns it in the object initializer when a constant, an
// expression or a method is mapped to it.
public class RequiredMemberMappingTests
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

    private static string Body(string source)
    {
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var start = generated.IndexOf(" Map(", StringComparison.Ordinal);
        return String.Join("\n", generated[start..].Split('\n').Skip(1).Select(static l => l.Trim()).Where(static l => l.Length > 0));
    }

    private static string Source(string destination, string attributes, bool returns) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Number { get; set; } public string Name { get; set; } = ""; }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
            private static string Label(Src src) => src.Name + "!";
        }
        """;

    [Theory]
    [InlineData("public class Dst { public required string Code { get; set; } public int Number { get; set; } }")]
    [InlineData("public class Dst { public required int Key; public int Number { get; set; } }")]
    public void VoidMapperDoesNotReportUnmappedRequiredMember(string destination)
    {
        var source = Source(destination, string.Empty, returns: false);

        AssertCompiles(source);
        Assert.Contains("dst.Number = src.Number;", Body(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public class Dst { public required int Key; public int Number { get; set; } }", "Key")]
    [InlineData("public class Base { public required string Code; } public class Dst : Base { public int Number { get; set; } }", "Code")]
    public void UnmappedRequiredFieldEmitsDiagnostic(string destination, string member)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(destination, string.Empty, returns: true));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0308", diagnostic.Id);
        Assert.Contains($"member=[{member}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public void RequiredFieldIsSetInInitializer()
    {
        var source = Source(
            "public class Dst { public required int Key; public required string Label; public required int Offset; public int Number { get; set; } }",
            """
            [MapConstant(nameof(Dst.Key), 7)]
            [MapUsing(nameof(Dst.Label), nameof(Label))]
            [MapExpression(nameof(Dst.Offset), "src.Number + 1")]
            """,
            returns: true);

        AssertCompiles(source);
        Assert.Contains(
            "var __d = new global::Test.Dst()\n{\nKey = 7,\nLabel = Label(src),\nOffset = __expression0(src),\n};\n__d.Number = src.Number;",
            Body(source),
            StringComparison.Ordinal);
    }

    // A void mapper assigns a required field after construction like any other
    [Fact]
    public void RequiredFieldInVoidMapperIsAssigned()
    {
        var source = Source(
            "public class Dst { public required int Key; public int Number { get; set; } }",
            "[MapConstant(nameof(Dst.Key), 7)]",
            returns: false);

        AssertCompiles(source);
        Assert.Contains("dst.Key = 7;", Body(source), StringComparison.Ordinal);
    }
}
