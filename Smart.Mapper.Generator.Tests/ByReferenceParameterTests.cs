namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A constructor with a ref, out or ref readonly parameter takes a variable, which the generated code does not pass:
// such a constructor used to be called with a value (CS1620, or CS9192 / CS9193 for ref readonly) and is no longer
// called, another one is. A destination no other constructor creates is reported (SMP0303). An in parameter takes a
// value as a parameter without a modifier does.
public class ByReferenceParameterTests
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

    private static string Source(string destination, string attributes = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        public class Src { public int A { get; set; } public int B { get; set; } public Child Item { get; set; } = new(); }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            public static partial Dst Map(Src src);

            [Mapper]
            public static partial Child MapChild(Child source);
        }
        """;

    [Theory]
    [InlineData("public class Dst { public Dst(ref int a) { A = a; } public int A { get; } }")]
    [InlineData("public class Dst { public Dst(out int a) { a = 1; A = a; } public int A { get; } }")]
    [InlineData("public class Dst { public Dst(ref readonly int a) { A = a; } public int A { get; } }")]
    public void ConstructorTakingVariableOnlyEmitsDiagnostic(string destination)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(destination));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0303", diagnostic.Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    [Theory]
    [InlineData(
        "public class Dst { public Dst(int a) { A = a; } public Dst(int a, ref int b) { A = a + b; } public int A { get; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Dst { public Dst(int a) { A = a; } public Dst(int a, ref readonly int b) { A = a + b; } public int A { get; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Dst { public Dst() { } public Dst(ref int a) { A = a; } public int A { get; set; } }",
        "__d.A = src.A;")]
    public void AnotherConstructorIsCalled(string destination, string expected)
    {
        var source = Source(destination);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public class Dst { public Dst(in int a) { A = a; } public int A { get; } }", "", "var __d = new global::Test.Dst(src.A);")]
    [InlineData("public class Dst { public Dst(in long a) { A = a; } public long A { get; } }", "", "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Dst { public Dst(in Child item) { Item = item; } public Child Item { get; } }",
        "[MapNested(\"Item\", Mapper = nameof(MapChild))]",
        "var __d = new global::Test.Dst(__arg0);")]
    public void InParameterTakesValue(string destination, string attributes, string expected)
    {
        var source = Source(destination, attributes);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
