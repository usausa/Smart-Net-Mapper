namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A dotted source or target path converts an enum at its end as a member the path does not go through does: to and
// from another enum by member name, to and from text, and to and from a number. It used to be reported as having no
// conversion (SMP0402), or to go to text through Enum.ToString(format, provider), which is obsolete (CS0618). Strict mode
// reports the members of the source enum without a member of the same name (SMP0503), and the culture and the formats of
// the method apply to a path as they do to a member.
public class DottedPathConversionTests
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

    private static string Source(string attributes, string mapperAttribute = "[Mapper(AutoMap = false)]") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public enum Status { Active, Suspended }
        public enum StatusDto { Active, Suspended }
        public enum Narrow { Active }
        public class Inner
        {
            public Status Status { get; set; }
            public Status? Maybe { get; set; }
            public string Text { get; set; } = "Active";
            public int Code { get; set; }
            public System.DateTime When { get; set; }
            public decimal Amount { get; set; }
        }
        public class Src { public Inner Inner { get; set; } = new(); public Status Status { get; set; } }
        public class Out { public string Text { get; set; } = ""; public StatusDto Status { get; set; } }
        public class Dst
        {
            public string Text { get; set; } = "";
            public StatusDto Status { get; set; }
            public Status FromText { get; set; }
            public int Number { get; set; }
            public Status FromNumber { get; set; }
            public string? MaybeText { get; set; }
            public Narrow Narrow { get; set; }
            public string When { get; set; } = "";
            public string Amount { get; set; } = "";
            public Out Out { get; set; } = new();
        }
        public static partial class M
        {
            {{mapperAttribute}}
            {{attributes}}
            public static partial Dst Map(Src source);
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Text), \"Inner.Status\")]", "__d.Text = source.Inner!.Status switch", "global::Test.Status.Suspended => \"Suspended\",")]
    [InlineData("[MapProperty(nameof(Dst.Status), \"Inner.Status\")]", "__d.Status = source.Inner!.Status switch", "global::Test.Status.Suspended => global::Test.StatusDto.Suspended,")]
    [InlineData("[MapProperty(nameof(Dst.FromText), \"Inner.Text\")]", "__d.FromText = source.Inner!.Text switch", "\"Suspended\" => global::Test.Status.Suspended,")]
    [InlineData("[MapProperty(nameof(Dst.Number), \"Inner.Status\")]", "__d.Number = (int)source.Inner!.Status;", "__d.Number = (int)source.Inner!.Status;")]
    [InlineData("[MapProperty(nameof(Dst.FromNumber), \"Inner.Code\")]", "__d.FromNumber = (global::Test.Status)source.Inner!.Code;", "__d.FromNumber = (global::Test.Status)source.Inner!.Code;")]
    [InlineData("[MapProperty(nameof(Dst.MaybeText), \"Inner.Maybe\")]", "__d.MaybeText = source.Inner!.Maybe is not null ? source.Inner!.Maybe.GetValueOrDefault() switch", "global::Test.Status.Active => \"Active\",")]
    [InlineData("[MapProperty(\"Out.Text\", nameof(Src.Status))]", "__d.Out.Text = source.Status switch", "global::Test.Status.Suspended => \"Suspended\",")]
    [InlineData("[MapProperty(\"Out.Status\", nameof(Src.Status))]", "__d.Out.Status = source.Status switch", "global::Test.Status.Suspended => global::Test.StatusDto.Suspended,")]
    public void EnumAtEndOfPathIsConverted(string attributes, string assignment, string arm)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(assignment, generated, StringComparison.Ordinal);
        Assert.Contains(arm, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("ToString(null, ", generated, StringComparison.Ordinal);
    }

    // Strict mode reports the members no member of the target enum has the name of, as for a member
    [Fact]
    public void StrictReportsUnmatchedMemberOfPath()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            "[MapProperty(nameof(Dst.Narrow), \"Inner.Status\")]",
            "[Mapper(AutoMap = false, Strict = true)]"));

        var warning = Assert.Single(diagnostics, static d => d.Id == "SMP0503");
        Assert.Contains("target=[Narrow]", warning.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Contains("Suspended", warning.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // The culture and the formats of the method apply to a path as they do to a member
    [Fact]
    public void MethodCultureAndFormatsApplyToPath()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.When), \"Inner.When\")] [MapProperty(nameof(Dst.Amount), \"Inner.Amount\")]",
            "[Mapper(AutoMap = false, Culture = \"de-DE\", DateTimeFormat = \"yyyy/MM/dd\", NumberFormat = \"N2\")]"));

        Assert.Empty(problems);
        Assert.Contains("__d.When = global::Smart.Mapper.DefaultValueConverter.ConvertToString(source.Inner!.When, __culture_de_DE, \"yyyy/MM/dd\");", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Amount = global::Smart.Mapper.DefaultValueConverter.ConvertToString(source.Inner!.Amount, __culture_de_DE, \"N2\");", generated, StringComparison.Ordinal);
    }

    // A format of the path itself goes with the culture of the method, as for a member
    [Fact]
    public void PathFormatTakesMethodCulture()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Amount), \"Inner.Amount\", NumberFormat = \"N0\")]",
            "[Mapper(AutoMap = false, Culture = \"de-DE\")]"));

        Assert.Empty(problems);
        Assert.Contains("ConvertToString(source.Inner!.Amount, __culture_de_DE, \"N0\")", generated, StringComparison.Ordinal);
    }
}
