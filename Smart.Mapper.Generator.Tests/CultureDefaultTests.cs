namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The default culture of the profiles, of the class or of the assembly, applies to the conversions without a culture
// name: Invariant, the default, goes without one, and Current with the current culture of the time of the conversion.
// A profile of the assembly gives every setting the method and the profile of the class do not set. A CultureInfo
// parameter of the mapper gives the culture of its conversions, over the culture of the method and the profiles and
// under the one of [MapProperty], and one that may be null falls back to the culture the method takes without it. The
// culture of [Mapper] on such a method is not used, which is reported (SMP0405).
public class CultureDefaultTests
{
    private const string Current = "global::System.Globalization.CultureInfo.CurrentCulture";
    private const string Invariant = "global::System.Globalization.CultureInfo.InvariantCulture";

    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string assemblyAttributes, string classAttributes, string members) =>
        $$"""
        #nullable enable
        using System;
        using System.Globalization;
        using Smart.Mapper;
        {{assemblyAttributes}}
        namespace Test;
        public class Src { public decimal Amount { get; set; } public DateTime At { get; set; } public string name { get; set; } = ""; public int Extra { get; set; } }
        public class Dst { public string Amount { get; set; } = ""; public string At { get; set; } = ""; public string Name { get; set; } = ""; }
        public class Dst2 { public string Amount { get; set; } = ""; public int Unmapped { get; set; } }
        {{classAttributes}}
        public static partial class M
        {
            {{members}}
        }
        """;

    [Theory]
    // Invariant, the default, goes without a culture
    [InlineData("", "", "[Mapper]", "ConvertToString(src.Amount)")]
    [InlineData("", "[MapperProfile(DefaultCulture = MapperCulture.Invariant)]", "[Mapper]", "ConvertToString(src.Amount)")]
    // Current, of the class or the assembly, goes with the current culture
    [InlineData("", "[MapperProfile(DefaultCulture = MapperCulture.Current)]", "[Mapper]", "ConvertToString(src.Amount, " + Current + ", null)")]
    [InlineData("[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]", "", "[Mapper]", "ConvertToString(src.Amount, " + Current + ", null)")]
    [InlineData("[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]", "", "[Mapper]", "ConvertToString(src.At, " + Current + ", null)")]
    // The class over the assembly
    [InlineData("[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]", "[MapperProfile(DefaultCulture = MapperCulture.Invariant)]", "[Mapper]", "ConvertToString(src.Amount)")]
    // A culture name over the default
    [InlineData("[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]", "", "[Mapper(Culture = \"ja-JP\")]", "ConvertToString(src.Amount, __culture_ja_JP, null)")]
    // A format applies with the default culture
    [InlineData("", "[MapperProfile(DefaultCulture = MapperCulture.Current)]", "[Mapper(NumberFormat = \"N2\")]", "ConvertToString(src.Amount, " + Current + ", \"N2\")")]
    [InlineData("", "", "[Mapper(DateTimeFormat = \"yyyy\")]", "ConvertToString(src.At, " + Invariant + ", \"yyyy\")")]
    public void DefaultCultureAppliesWithoutCultureName(string assemblyAttributes, string classAttributes, string mapper, string call)
    {
        var (generated, problems) = Build(Source(assemblyAttributes, classAttributes, mapper + "\n    [MapProperty(nameof(Dst.Name), \"name\")]\n    public static partial Dst Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains(call, generated, StringComparison.Ordinal);
    }

    [Theory]
    // The culture and the formats of the assembly profile, under those of the class and the method
    [InlineData("[assembly: MapperProfile(Culture = \"ja-JP\")]", "", "[Mapper]", "ConvertToString(src.Amount, __culture_ja_JP, null)")]
    [InlineData("[assembly: MapperProfile(Culture = \"ja-JP\")]", "[MapperProfile(Culture = \"en-US\")]", "[Mapper]", "ConvertToString(src.Amount, __culture_en_US, null)")]
    [InlineData("[assembly: MapperProfile(Culture = \"ja-JP\")]", "[MapperProfile(Culture = \"en-US\")]", "[Mapper(Culture = \"de-DE\")]", "ConvertToString(src.Amount, __culture_de_DE, null)")]
    [InlineData("[assembly: MapperProfile(NumberFormat = \"N1\")]", "", "[Mapper]", "ConvertToString(src.Amount, " + Invariant + ", \"N1\")")]
    [InlineData("[assembly: MapperProfile(NumberFormat = \"N1\")]", "[MapperProfile(NumberFormat = \"N3\")]", "[Mapper]", "ConvertToString(src.Amount, " + Invariant + ", \"N3\")")]
    // Each setting on its own: the culture of the assembly with the format of the class
    [InlineData("[assembly: MapperProfile(Culture = \"ja-JP\")]", "[MapperProfile(NumberFormat = \"N3\")]", "[Mapper]", "ConvertToString(src.Amount, __culture_ja_JP, \"N3\")")]
    public void AssemblyProfileGivesWhatMethodAndClassDoNotSet(string assemblyAttributes, string classAttributes, string mapper, string call)
    {
        var (generated, problems) = Build(Source(assemblyAttributes, classAttributes, mapper + "\n    [MapProperty(nameof(Dst.Name), \"name\")]\n    public static partial Dst Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains(call, generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[assembly: MapperProfile(Strict = true)]", "", true)]
    [InlineData("[assembly: MapperProfile(Strict = true)]", "[MapperProfile(Strict = false)]", false)]
    [InlineData("", "", false)]
    public void AssemblyProfileGivesStrictMode(string assemblyAttributes, string classAttributes, bool reported)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(assemblyAttributes, classAttributes, "[Mapper]\n    public static partial Dst2 Map(Src src);"));

        Assert.Equal(reported, diagnostics.Any(static d => d.Id == "SMP0501"));
    }

    [Theory]
    [InlineData("[assembly: MapperProfile(NameComparison = StringComparison.OrdinalIgnoreCase)]", "", true)]
    [InlineData("[assembly: MapperProfile(NameComparison = StringComparison.OrdinalIgnoreCase)]", "[MapperProfile(NameComparison = StringComparison.Ordinal)]", false)]
    public void AssemblyProfileGivesNameComparison(string assemblyAttributes, string classAttributes, bool matched)
    {
        var (generated, _) = Build(Source(assemblyAttributes, classAttributes, "[Mapper]\n    public static partial Dst Map(Src src);"));

        Assert.Equal(matched, generated.Contains("__d.Name = src.name;", StringComparison.Ordinal));
    }

    [Theory]
    // The parameter gives the culture of the conversions, return and void mappers alike
    [InlineData("", "[Mapper] public static partial Dst Map(Src src, CultureInfo culture);", "ConvertToString(src.Amount, culture, null)")]
    [InlineData("", "[Mapper] public static partial void Map(Src src, Dst dst, CultureInfo culture);", "ConvertToString(src.Amount, culture, null)")]
    [InlineData("[MapperProfile(Culture = \"ja-JP\", DefaultCulture = MapperCulture.Current)]", "[Mapper] public static partial Dst Map(Src src, CultureInfo culture);", "ConvertToString(src.At, culture, null)")]
    // The culture of [MapProperty] over the parameter
    [InlineData("", "[Mapper] [MapProperty(nameof(Dst.Amount), Culture = \"en-US\")] public static partial Dst Map(Src src, CultureInfo culture);", "ConvertToString(src.Amount, __culture_en_US, null)")]
    [InlineData("", "[Mapper] [MapProperty(nameof(Dst.Amount), Culture = \"en-US\")] public static partial Dst Map(Src src, CultureInfo culture);", "ConvertToString(src.At, culture, null)")]
    // A format applies with the parameter
    [InlineData("", "[Mapper(NumberFormat = \"N2\")] public static partial Dst Map(Src src, CultureInfo culture);", "ConvertToString(src.Amount, culture, \"N2\")")]
    // One that may be null falls back to the culture of the method without it
    [InlineData("", "[Mapper] public static partial Dst Map(Src src, CultureInfo? culture);", "ConvertToString(src.Amount, (culture ?? " + Invariant + "), null)")]
    [InlineData("[MapperProfile(Culture = \"ja-JP\")]", "[Mapper] public static partial Dst Map(Src src, CultureInfo? culture);", "ConvertToString(src.Amount, (culture ?? __culture_ja_JP), null)")]
    [InlineData("[MapperProfile(Culture = \"ja-JP\")]", "[Mapper] public static partial Dst Map(Src src, CultureInfo? culture);", "__culture_ja_JP = global::System.Globalization.CultureInfo.GetCultureInfo(\"ja-JP\");")]
    [InlineData("[MapperProfile(DefaultCulture = MapperCulture.Current)]", "[Mapper] public static partial Dst Map(Src src, CultureInfo? culture);", "ConvertToString(src.Amount, (culture ?? " + Current + "), null)")]
    public void CultureParameterGivesCulture(string classAttributes, string members, string call)
    {
        var (generated, problems) = Build(Source(string.Empty, classAttributes, members));

        Assert.Empty(problems);
        Assert.Contains(call, generated, StringComparison.Ordinal);
    }

    // The parameter is passed on to the methods taking the custom parameters, as before
    [Fact]
    public void CultureParameterIsPassedOnToCallbacks()
    {
        var (generated, problems) = Build(Source(
            string.Empty,
            string.Empty,
            "[Mapper] [AfterMap(nameof(After))] public static partial Dst Map(Src src, CultureInfo culture); private static void After(Src src, Dst dst, CultureInfo culture) { }"));

        Assert.Empty(problems);
        Assert.Contains("After(src, __d, culture);", generated, StringComparison.Ordinal);
        Assert.Contains("ConvertToString(src.Amount, culture, null)", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void CultureOfMapperWithParameterIsReported()
    {
        const string members = "[Mapper(Culture = \"ja-JP\")] /*here*/\n    public static partial Dst Map(Src src, CultureInfo culture);";
        var source = Source(string.Empty, string.Empty, members);

        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(source), static d => d.Id == "SMP0405");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        var markedLine = Array.FindIndex(source.Split('\n'), static line => line.Contains("/*here*/", StringComparison.Ordinal));
        Assert.Equal(markedLine, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.Contains("ConvertToString(src.Amount, culture, null)", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // The culture of a profile is a default, which the parameter takes over without a warning
    [Fact]
    public void CultureOfProfileWithParameterIsNotReported()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source("[assembly: MapperProfile(Culture = \"en-US\")]", "[MapperProfile(Culture = \"ja-JP\")]", "[Mapper] public static partial Dst Map(Src src, CultureInfo culture);"));

        Assert.DoesNotContain(diagnostics, static d => d.Id == "SMP0405");
    }

    // A value converter of its own is called through the overload taking the culture under Current and with the
    // parameter, as with a culture name
    [Theory]
    [InlineData("[MapperProfile(DefaultCulture = MapperCulture.Current)]", "[Mapper] public static partial Dst2 Map(Src src);", false, true)]
    [InlineData("", "[Mapper] public static partial Dst2 Map(Src src, CultureInfo culture);", false, true)]
    [InlineData("[MapperProfile(DefaultCulture = MapperCulture.Current)]", "[Mapper] public static partial Dst2 Map(Src src);", true, false)]
    [InlineData("", "[Mapper] public static partial Dst2 Map(Src src);", false, false)]
    public void ValueConverterNeedsCultureOverload(string classAttributes, string members, bool hasCultureOverload, bool reported)
    {
        var converter = """
            public static class Conv
            {
                public static string ConvertToString(decimal source) => source.ToString(CultureInfo.InvariantCulture);
            """ +
            (hasCultureOverload ? "\n    public static string ConvertToString(decimal source, IFormatProvider culture, string? format) => source.ToString(format, culture);" : string.Empty) +
            "\n    public static TDestination Convert<TSource, TDestination>(TSource source) => default!;\n}";
        var source = Source(string.Empty, classAttributes + "\n[ValueConverter(typeof(Conv))]", members) + "\n" + converter;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Equal(reported, diagnostics.Any(static d => d.Id == "SMP0104"));
    }
}
