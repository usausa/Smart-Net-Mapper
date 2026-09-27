namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A culture names the field the generated code gets it into. One that is not a culture name (such as "en US")
// broke that field's name in the generated code, and is reported (SMP0404), whether it comes from [Mapper],
// [MapProperty] or [MapperProfile]. The field name of a culture name is always an identifier, and different
// names get different fields: a hyphen becomes an underscore and the underscore of a sort order two.
public class CultureNameTests
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

    private static string Source(string attributes, string profile = "") =>
        $$"""
        using Smart.Mapper;
        namespace Test;
        public class Src { public int First { get; set; } public int Second { get; set; } }
        public class Dst { public string First { get; set; } = ""; public string Second { get; set; } = ""; }
        {{profile}}
        public static partial class M
        {
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    [Theory]
    [InlineData("[Mapper(Culture = \"en US\")]")]
    [InlineData("[Mapper(Culture = \"ja.JP\")]")]
    [InlineData("[Mapper(Culture = \"-JP\")]")]
    [InlineData("[Mapper(Culture = \"ja-\")]")]
    [InlineData("[Mapper(Culture = \"ja-JP-toolongsubtag\")]")]
    [InlineData("[Mapper(Culture = \"日本\")]")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.First), Culture = \"en US\")]")]
    public void InvalidCultureNameEmitsDiagnostic(string attributes)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(attributes));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0404", diagnostic.Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    [Fact]
    public void InvalidProfileCultureNameEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source("[Mapper]", "[MapperProfile(Culture = \"en US\")]"));

        Assert.Equal("SMP0404", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
    }

    [Theory]
    [InlineData("[Mapper(Culture = \"ja-JP\")]", "__culture_ja_JP")]
    [InlineData("[Mapper(Culture = \"zh-Hant-TW\")]", "__culture_zh_Hant_TW")]
    [InlineData("[Mapper(Culture = \"de-DE_phoneb\")]", "__culture_de_DE__phoneb")]
    [InlineData("[Mapper(Culture = \"en\")]", "__culture_en")]
    public void ValidCultureNameGetsField(string attributes, string field)
    {
        var source = Source(attributes);

        AssertCompiles(source);
        Assert.Contains($"global::System.Globalization.CultureInfo {field} = ", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // Names that would have met in one field get fields of their own
    [Fact]
    public void DifferentNamesGetDifferentFields()
    {
        var source = Source("[Mapper(Culture = \"de-DE\")] [MapProperty(nameof(Dst.Second), Culture = \"de_DE\")]");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__culture_de_DE = global::System.Globalization.CultureInfo.GetCultureInfo(\"de-DE\");", generated, StringComparison.Ordinal);
        Assert.Contains("__culture_de__DE = global::System.Globalization.CultureInfo.GetCultureInfo(\"de_DE\");", generated, StringComparison.Ordinal);
    }
}
