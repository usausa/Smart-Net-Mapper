namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A member whose setter the mapper class cannot call (a private or protected one) is assigned through a constructor,
// as a get-only one is: the constructor setting it is called, when the mapping gives it every argument. It used to be
// constructed without arguments, and the member left unset without a word. Of the constructors, the longest the
// mapping gives every argument is called, so one it cannot fill no longer stops the mapping (SMP0305) when a shorter
// one can be called or the destination can be created without arguments, the members only a constructor assigns
// left unmapped then. A [MapIgnore] naming a parameter or its member leaves it without a value. A type whose setters
// the mapper class can all call keeps constructing as it did.
public class ConstructorChoiceTests
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

    private static string Source(string types, string attributes = "") =>
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

    private static void AssertGenerated(string source, string expected, string? unexpected)
    {
        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        if (unexpected is not null)
        {
            Assert.DoesNotContain(unexpected, generated, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public int A { get; private set; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public int A { get; protected set; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } public string Name { get; set; } = \"\"; } public class Dst { public Dst() { } public Dst(int a, string name) { A = a; Name = name; } public int A { get; private set; } public string Name { get; set; } = \"\"; }",
        "var __d = new global::Test.Dst(src.A, src.Name);")]
    public void ConstructorSettingMemberIsCalled(string types, string expected)
    {
        AssertGenerated(Source(types), expected, null);
    }

    // The longest constructor lacks a value; the shorter one the mapping fills is called
    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public Dst(int a, int b) { A = a; B = b; } public int A { get; private set; } public int B { get; private set; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a) { A = a; } public Dst(int a, int b) { A = a + b; } public int A { get; } }",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } } public record Dst(int A, int B) { public Dst(int a) : this(a, 0) { } }",
        "var __d = new global::Test.Dst(src.A);")]
    public void ShorterConstructorMappingFillsIsCalled(string types, string expected)
    {
        AssertGenerated(Source(types), expected, null);
    }

    // No constructor gets every argument, and the destination is created without arguments
    [Theory]
    [InlineData(
        "public class Src { public string Name { get; set; } = \"\"; } public class Dst { public Dst() { } public Dst(int id, string name) { Id = id; Name = name; } public int Id { get; private set; } public string Name { get; set; } = \"\"; }",
        "",
        "__d.Name = src.Name;")]
    [InlineData(
        "public class Src { public int X { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public int A { get; } public int X { get; set; } }",
        "",
        "__d.X = src.X;")]
    [InlineData(
        "public class Src { public int A { get; set; } } public record struct Dst(int A, int B);",
        "",
        "__d.A = src.A;")]
    [InlineData(
        "public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; private set; } public int B { get; private set; } }",
        "[MapIgnore(\"B\")]",
        "var __d = new global::Test.Dst();")]
    public void DestinationIsCreatedWithoutArguments(string types, string attributes, string expected)
    {
        AssertGenerated(Source(types, attributes), expected, "new global::Test.Dst(src");
    }

    // A [MapIgnore] of a parameter no member has keeps the constructor taking it from being called
    [Fact]
    public void IgnoredParameterChoosesAnotherConstructor()
    {
        AssertGenerated(
            Source(
                "public class Src { public int A { get; set; } public int X { get; set; } } public class Dst { public Dst(int a) { A = a; } public Dst(int a, int x) { A = a + x; } public int A { get; } }",
                "[MapIgnore(\"x\")]"),
            "var __d = new global::Test.Dst(src.A);",
            "src.X");
    }

    // A type whose setters the mapper class can all call does not change to a constructor taking arguments
    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; set; } public int B { get; set; } }",
        "__d.A = src.A;")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public int A { get; internal set; } }",
        "__d.A = src.A;")]
    [InlineData(
        "public class Src { public int A { get; set; } public int X { get; set; } } public class Dst { public Dst() { } public Dst(int x) { Text = x.ToString(); } public Dst(int a, int b) { A = a; B = b; } public int A { get; set; } public int B { get; set; } public string Text { get; set; } = \"\"; }",
        "__d.A = src.A;")]
    public void TypeWithCallableSettersKeepsConstruction(string types, string expected)
    {
        var source = Source(types);

        AssertGenerated(source, expected, "new global::Test.Dst(src");
        Assert.Contains("var __d = new global::Test.Dst();", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A type that cannot be created without arguments reports the parameter of the longest constructor that has no
    // value, as before; a [MapProperty] naming a member only a constructor the mapping cannot fill assigns is a target
    // that cannot be assigned
    [Theory]
    [InlineData(
        "public class Src { public string Name { get; set; } = \"\"; } public class Dst { public Dst(int id, string name) { Id = id; Name = name; } public int Id { get; private set; } public string Name { get; set; } }",
        "",
        "SMP0305",
        "parameter=[id]")]
    [InlineData(
        "public class Src { public int A { get; set; } } public record Dst(int A, int B);",
        "",
        "SMP0305",
        "parameter=[B]")]
    [InlineData(
        "public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "[MapIgnore(\"B\")]",
        "SMP0304",
        "target=[B]")]
    [InlineData(
        "public class Src { public int X { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "[MapProperty(\"A\", \"X\")]",
        "SMP0102",
        "target=[A]")]
    public void ParameterWithoutValueEmitsDiagnostic(string types, string attributes, string id, string part)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(types, attributes));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains(part, diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }
}
