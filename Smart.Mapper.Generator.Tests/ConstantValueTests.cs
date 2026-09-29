namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A [MapConstant] value, and the NullValue of [MapProperty] where the generated code writes it, have to
// convert to the type they are assigned to the way the compiler converts the literal. One that does not is
// reported (SMP0216) instead of failing in the generated code with CS0029 / CS0019 / CS0664; a literal the
// compiler converts implicitly (an int to a long or a byte, null to a reference) keeps compiling.
public class ConstantValueTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static void AssertDiagnostic(string source, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
    }

    private static string Source(string attributes, string members = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src
        {
            public int? Num { get; set; }
            public string? Name { get; set; }
            public string? Text { get; set; }
            public int? Code { get; set; }
        }
        public class Dst
        {
            public int Num { get; set; }
            public string Name { get; set; } = "";
            public int Text { get; set; }
            public object? Code { get; set; }
            public long Big { get; set; }
            public byte Small { get; set; }
            public int? Maybe { get; set; }
            public string? Label { get; set; }
            public float Ratio { get; set; }
            public decimal Money { get; set; }
            public char Letter { get; set; }
            public object? Any { get; set; }
            public double Dbl { get; set; }
        }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            public static partial Dst Map(Src src);
            {{members}}
        }
        """;

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Num), \"abc\")]")]
    [InlineData("[MapConstant(nameof(Dst.Ratio), 1.5)]")]
    [InlineData("[MapConstant(nameof(Dst.Money), 2.5)]")]
    [InlineData("[MapConstant(nameof(Dst.Num), null)]")]
    [InlineData("[MapConstant(nameof(Dst.Label), true)]")]
    [InlineData("[MapConstant<string>(nameof(Dst.Big), \"1\")]")]
    public void ConstantThatCannotBeAssignedEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute), "SMP0216");
    }

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Big), 1)]", "__d.Big = 1;")]
    [InlineData("[MapConstant(nameof(Dst.Small), 1)]", "__d.Small = 1;")]
    [InlineData("[MapConstant(nameof(Dst.Maybe), null)]", "__d.Maybe = null;")]
    [InlineData("[MapConstant(nameof(Dst.Label), null)]", "__d.Label = null;")]
    [InlineData("[MapConstant(nameof(Dst.Letter), 'x')]", "__d.Letter = 'x';")]
    [InlineData("[MapConstant(nameof(Dst.Any), 3)]", "__d.Any = 3;")]
    [InlineData("[MapConstant<float>(nameof(Dst.Dbl), 1.25f)]", "__d.Dbl = 1.25f;")]
    public void ConstantThatConvertsImplicitlyCompiles(string attribute, string assignment)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(assignment, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    // Written after ?? on a nullable source copied as it is
    [InlineData("[MapProperty(nameof(Dst.Num), NullValue = \"x\")]")]
    [InlineData("[MapProperty(nameof(Dst.Code), NullValue = \"x\")]")]
    // Written as the fallback of a nullable source that is converted
    [InlineData("[MapProperty(nameof(Dst.Text), NullValue = \"x\")]")]
    public void NullValueThatCannotBeAssignedEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute), "SMP0216");
    }

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Num), NullValue = -1)]", "__d.Num = src.Num ?? -1;")]
    [InlineData("[MapProperty(nameof(Dst.Name), NullValue = \"none\")]", "__d.Name = src.Name ?? \"none\";")]
    [InlineData("[MapProperty(nameof(Dst.Text), NullValue = 0)]", ": 0;")]
    [InlineData("[MapProperty(nameof(Dst.Code), NullValue = 5)]", "__d.Code = src.Code ?? 5;")]
    public void NullValueThatConvertsImplicitlyCompiles(string attribute, string assignment)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(assignment, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // Written as the fallback of a nullable source a converter method takes, which is called for a value only
    [Fact]
    public void NullValueWithConverterThatCannotBeAssignedEmitsDiagnostic()
    {
        AssertDiagnostic(
            Source("[MapProperty(nameof(Dst.Num), Converter = nameof(ToNum), NullValue = \"x\")]", "static int ToNum(int? value) => value ?? 0;"),
            "SMP0216");
    }

    // A NullValue the generated code does not write, with NullBehavior.Skip, is not checked, as before.
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Num), NullBehavior = NullBehavior.Skip, NullValue = \"x\")]", "")]
    [InlineData("[MapProperty(nameof(Dst.Num), NullBehavior = NullBehavior.Skip, Converter = nameof(ToNum), NullValue = \"x\")]", "static int ToNum(int? value) => value ?? 0;")]
    public void NullValueNotWrittenIsNotChecked(string attribute, string members)
    {
        AssertCompiles(Source(attribute, members));
    }
}
