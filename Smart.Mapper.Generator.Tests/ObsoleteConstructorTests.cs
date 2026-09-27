namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A constructor obsolete as a warning is called only when nothing else constructs the destination, a constructor the
// mapping fills or one callable without arguments; a parameterless one obsolete as a warning does not count as a way
// to construct without arguments then. It used to be called as any other, and the generated code warned (CS0618). A
// type that constructs only through one still calls it, warning as before. The targets of the attributes choose before
// an obsolete constructor is avoided, so one alone receiving the target of an attribute is called, warning as well,
// where it used to be passed over for the target to be reported (SMP0214).
public class ObsoleteConstructorTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static string Source(string types, string attributes = "") =>
        $$"""
        #nullable enable
        using System;
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

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { [Obsolete(\"For serializers\")] public Dst() { } public Dst(int a) { A = a; } public int A { get; set; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } [Obsolete(\"Old\")] public Dst(int a) { A = a; } public int A { get; } }",
        "var __d = new global::Test.Dst();")]
    [InlineData(
        "public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { [Obsolete(\"Old\")] public Dst(int a, int b) { A = a; B = b; } public Dst(int a) { A = a; } public int A { get; } public int B { get; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { public Dst() { } [Obsolete(\"Old\")] public Dst(int a, int b) { A = a; B = b; } public int A { get; set; } public int B { get; set; } }",
        "var __d = new global::Test.Dst();")]
    public void ObsoleteConstructorIsAvoided(string types, string expected)
    {
        var (generated, problems) = Build(Source(types));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // Nothing else constructs, so the obsolete one is called, warning as before
    [Theory]
    [InlineData(
        "public class Src { public int X { get; set; } } public class Dst { [Obsolete(\"For serializers\")] public Dst() { } public Dst(int a) { A = a; } public int A { get; set; } }",
        "var __d = new global::Test.Dst();")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { [Obsolete(\"Old\")] public Dst(int a) { A = a; } public int A { get; } }",
        "var __d = new global::Test.Dst(src.A);")]
    public void OnlyObsoleteConstructorIsCalled(string types, string expected)
    {
        var (generated, problems) = Build(Source(types));

        Assert.Equal("CS0618", Assert.Single(problems));
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The attribute targets choose before the obsolete constructor is avoided
    [Theory]
    [InlineData("public class Src { public int X { get; set; } } public class Dst { public Dst() { } [Obsolete(\"Old\")] public Dst(int a) { A = a; } public int A { get; } }")]
    [InlineData("public class Src { public int X { get; set; } public string Name { get; set; } = \"\"; } public class Dst { [Obsolete(\"Old\")] public Dst(int a) { A = a; } public Dst(string name) { Name = name; } public int A { get; } public string Name { get; } = \"\"; }")]
    public void ObsoleteConstructorAloneReceivingTargetIsCalled(string types)
    {
        var (generated, problems) = Build(Source(types, "[MapProperty(\"A\", \"X\")]"));

        Assert.Equal("CS0618", Assert.Single(problems));
        Assert.Contains("var __d = new global::Test.Dst(src.X);", generated, StringComparison.Ordinal);
    }

    // Of the constructors receiving as many targets, one not obsolete
    [Fact]
    public void ConstructorReceivingTargetNotObsoleteIsCalled()
    {
        var (generated, problems) = Build(Source(
            "public class Src { public int X { get; set; } public int B { get; set; } public string C { get; set; } = \"\"; } public class Dst { [Obsolete(\"Old\")] public Dst(int a, int b) { A = a + b; } public Dst(int a, string c) { A = a; } public int A { get; } }",
            "[MapProperty(\"A\", \"X\")]"));

        Assert.Empty(problems);
        Assert.Contains("var __d = new global::Test.Dst(src.X, src.C);", generated, StringComparison.Ordinal);
    }
}
