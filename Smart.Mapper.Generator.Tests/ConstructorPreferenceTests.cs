namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A target of an attribute giving a value that only a constructor receives, a parameter name no member has or a
// member without a setter the mapper class can call, chooses the constructor: of the candidates the mapping gives
// every argument, the one receiving the most of these targets, then the longest, then the first declared. It used to
// be the longest (the first declared), or construction without arguments, whatever the attributes named, so the
// value had nowhere to go (SMP0102). A target a setter receives does not choose one, and one no candidate the mapping
// fills receives is reported as before.
public class ConstructorPreferenceTests
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

    private static string Source(string types, string attributes) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        {{types}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    [Theory]
    // Two constructors of a length, the attribute naming a parameter of the second
    [InlineData(
        "public class Src { public int A { get; set; } public int B { get; set; } public string Y { get; set; } = \"\"; } public class Dst { public Dst(int a, int b) { A = a; B = b; } public Dst(int a, string x) { A = a; B = x.Length; } public int A { get; } public int B { get; } }",
        "[MapProperty(\"x\", \"Y\")]",
        "var __d = new global::Test.Dst(src.A, src.Y);")]
    // A shorter constructor takes the get-only member the constant is for
    [InlineData(
        "public class Src { public int B { get; set; } public int C { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public Dst(int b, int c) { B = b; C = c; } public int A { get; } public int B { get; set; } public int C { get; set; } }",
        "[MapConstant(\"A\", 5)]",
        "var __d = new global::Test.Dst(5);")]
    // The one receiving more of the targets, although shorter
    [InlineData(
        "public class Src { public int B { get; set; } public int C { get; set; } } public class Dst { public Dst(int a, int b, int c) { A = a; B = b + c; } public Dst(int a, string x) { A = a; B = x.Length; } public int A { get; } public int B { get; } }",
        "[MapConstant(\"A\", 1)] [MapConstant(\"x\", \"text\")]",
        "var __d = new global::Test.Dst(1, \"text\");")]
    // Of those receiving as many, the longest
    [InlineData(
        "public class Src { public int B { get; set; } } public class Dst { public Dst(int a) { A = a; } public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "[MapConstant(\"A\", 1)]",
        "var __d = new global::Test.Dst(1, src.B);")]
    // A private setter as well
    [InlineData(
        "public class Src { public int X { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public int A { get; private set; } }",
        "[MapProperty(\"A\", \"X\")]",
        "var __d = new global::Test.Dst(src.X);")]
    public void ConstructorReceivingTargetIsCalled(string types, string attributes, string expected)
    {
        var source = Source(types, attributes);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A target a setter receives keeps the construction of a type whose setters the mapper class can all call
    [Fact]
    public void TargetSetterReceivesDoesNotChooseConstructor()
    {
        var source = Source(
            "public class Src { public int X { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public int A { get; set; } }",
            "[MapProperty(\"A\", \"X\")]");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("var __d = new global::Test.Dst();", generated, StringComparison.Ordinal);
        Assert.Contains("__d.A = src.X;", generated, StringComparison.Ordinal);
    }

    // No candidate the mapping fills receives the target
    [Fact]
    public void TargetNoConstructorReceivesEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(
            "public class Src { public int X { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
            "[MapProperty(\"A\", \"X\")]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0102", diagnostic.Id);
        Assert.Contains("target=[A]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }
}
