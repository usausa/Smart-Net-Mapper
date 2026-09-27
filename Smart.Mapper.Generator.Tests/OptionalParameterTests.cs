namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// An optional parameter of the constructor a return mapper calls (with a default value, [Optional], or params) the
// mapping has no value for is left out to take its default, and the arguments after it are passed by name. It used to
// be reported as a parameter without a source (SMP0301). One with a value is passed as before, and a parameter that
// is not optional still needs one. A constructor whose call, with the parameters left out, another constructor takes
// as well is passed over, as the call would bind to that one or be ambiguous (CS0121), whether that one is obsolete
// as an error (CS0619) or takes a ref readonly parameter (CS9193).
public class OptionalParameterTests
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
        using System.Runtime.InteropServices;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        {{types}}
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
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, int b = 5) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } public int C { get; set; } } public class Dst { public Dst(int a, int b = 5, int c = 6) { A = a; B = b; C = c; } public int A { get; } public int B { get; } public int C { get; } }",
        "",
        "var __d = new global::Test.Dst(src.A, c: src.C);")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, params int[] rest) { A = a; Count = rest.Length; } public int A { get; } public int Count { get; } }",
        "",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, [Optional] int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int A { get; set; } public string Class { get; set; } = \"\"; } public class Dst { public Dst(int a, int b = 1, string @class = \"\") { A = a; Class = @class; } public int A { get; } public string Class { get; } }",
        "",
        "var __d = new global::Test.Dst(src.A, @class: src.Class);")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, int b = 5, int c = 6) { A = a; B = b; C = c; } public int A { get; } public int B { get; } public int C { get; } }",
        "[MapConstant(\"C\", 3)]",
        "var __d = new global::Test.Dst(src.A, c: 3);")]
    [InlineData(
        "public class Src { public int A { get; set; } public Child Item { get; set; } = new(); } public class Dst { public Dst(int a, int b = 5, Child? item = null) { A = a; Item = item; } public int A { get; } public Child? Item { get; } }",
        "[MapNested(\"Item\", Mapper = nameof(MapChild))]",
        "var __d = new global::Test.Dst(src.A, item: __arg1);")]
    [InlineData(
        "public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { public Dst(int a, int b = 5) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "[MapIgnore(\"B\")]",
        "var __d = new global::Test.Dst(src.A);")]
    [InlineData(
        "public class Src { public int X { get; set; } } public class Dst { public Dst(int a = 1) { A = a; } public int A { get; } }",
        "",
        "var __d = new global::Test.Dst();")]
    public void OptionalParameterWithoutValueIsLeftOut(string types, string attributes, string expected)
    {
        var source = Source(types, attributes);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { public Dst(int a, int b = 5) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "var __d = new global::Test.Dst(src.A, src.B);")]
    [InlineData(
        "public class Src { public int A { get; set; } public int[] Rest { get; set; } = []; } public class Dst { public Dst(int a, params int[] rest) { A = a; Count = rest.Length; } public int A { get; } public int Count { get; } }",
        "var __d = new global::Test.Dst(src.A, src.Rest);")]
    public void OptionalParameterWithValueIsPassed(string types, string expected)
    {
        var source = Source(types);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // The call new Dst(src.Name, code: src.Code) would bind to the shorter constructor, which is called instead
    [Fact]
    public void ConstructorAnotherCallTakesIsPassedOver()
    {
        var source = Source(
            "public class Src { public string Name { get; set; } = \"\"; public int Code { get; set; } } public class Dst { public Dst(string name, int code = 0) { Name = name; Code = code; } public Dst(string name, int age = 0, int code = 0) { Name = name; Code = code + age; } public string Name { get; } public int Code { get; } }");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst(src.Name, src.Code);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A parameter that is not optional still needs a value, and a required member an optional parameter left out
    // would have assigned needs a mapping of its own
    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "SMP0301",
        "parameter=[b]")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, int b = 5) { A = a; B = b; } public int A { get; } public required int B { get; set; } }",
        "SMP0303",
        "member=[B]")]
    [InlineData(
        "public class Src { public string Name { get; set; } = \"\"; } public class Dst { public Dst(string name, int age = 0) { Name = name; } public Dst(string name, bool active = false) { Name = name; } public string Name { get; } }",
        "SMP0301",
        "parameter=[age]")]
    [InlineData(
        "public class Src { public string Name { get; set; } = \"\"; } public class Dst { [System.Obsolete(\"Old\", true)] public Dst(string name) { Name = name; } public Dst(string name, int age = 0) { Name = name; } public string Name { get; } }",
        "SMP0301",
        "parameter=[age]")]
    [InlineData(
        "public class Src { public string Name { get; set; } = \"\"; } public class Dst { public Dst(ref readonly string name) { Name = name; } public Dst(string name, int age = 0) { Name = name; } public string Name { get; } }",
        "SMP0301",
        "parameter=[age]")]
    public void ParameterWithoutValueEmitsDiagnostic(string types, string id, string part)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(types));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains(part, diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }
}
