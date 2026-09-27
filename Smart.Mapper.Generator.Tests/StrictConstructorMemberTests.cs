namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// In strict mode, a property only a constructor can set (get-only, or a setter the mapper class cannot call) that a
// parameter of a constructor a return mapper can call takes is reported as not mapped (SMP0501) when the
// construction chosen passes it no argument and no attribute maps or ignores it, whether the source has it or not.
// It used to be left out without a word. A property no constructor takes and a void mapper are not concerned, and a
// member the constructor sets through the mapping made for its parameter is no longer reported.
public class StrictConstructorMemberTests
{
    private static string Source(string types, string attributes = "", string signature = "public static partial Dst Map(Src src);", string mapper = "[Mapper(Strict = true)]") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            {{signature}}
        }
        """;

    private static List<string> Unmapped(string source)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);
        Assert.DoesNotContain(diagnostics, static d => d.Severity == DiagnosticSeverity.Error);

        return diagnostics
            .Where(static d => d.Id == "SMP0501")
            .Select(static d => d.GetMessage(CultureInfo.InvariantCulture))
            .Select(static m => m[(m.IndexOf("property=[", StringComparison.Ordinal) + 10)..].TrimEnd(']'))
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToList();
    }

    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "A,B")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; private set; } public int B { get; private set; } }",
        "A,B")]
    [InlineData(
        "public class Src { public int B { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public Dst(int b, int c) { B = b; C = c; } public int A { get; private set; } public int B { get; set; } public int C { get; set; } }",
        "A,C")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, int b = 5) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "B")]
    public void MemberOnlyConstructorSetsIsReported(string types, string expected)
    {
        Assert.Equal(expected, String.Join(",", Unmapped(Source(types))));
    }

    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public int A { get; set; } public int Twice => A * 2; }",
        "")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "[MapIgnore(\"A\")] [MapIgnore(\"B\")]")]
    [InlineData(
        "public class Src { public int A { get; set; } } public record Dst(int A, int B = 2);",
        "[MapConstant(\"B\", 3)]")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a) { A = a; } public int A { get; set; } }",
        "")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public Dst(int a, int b = 5) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "[MapIgnore(\"B\")]")]
    public void MemberMappedOrNoConstructorTakesIsNotReported(string types, string attributes)
    {
        Assert.Empty(Unmapped(Source(types, attributes)));
    }

    // The mapping made for the parameter, named after it, sets the member through the constructor
    [Fact]
    public void MemberConstructorSetsWithoutAutomaticMappingIsNotReported()
    {
        Assert.Empty(Unmapped(Source(
            "public class Src { public int A { get; set; } } public class Dst { public Dst(int a) { A = a; } public int A { get; set; } }",
            mapper: "[Mapper(Strict = true, AutoMap = false)]")));
    }

    [Fact]
    public void VoidMapperIsNotConcerned()
    {
        Assert.Equal(
            "X",
            String.Join(",", Unmapped(Source(
                "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a) { A = a; } public int A { get; } public int X { get; set; } }",
                signature: "public static partial void Map(Src src, Dst dst);"))));
    }

    [Fact]
    public void WithoutStrictNothingIsReported()
    {
        Assert.Empty(Unmapped(Source(
            "public class Src { public int A { get; set; } } public class Dst { public Dst() { } public Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
            mapper: "[Mapper]")));
    }
}
