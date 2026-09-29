namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Culture, DateTimeFormat and NumberFormat are each taken from the mapper method, or else from the profile.
// The profile used to replace all three of the method's unless the method set Culture, so a format the
// method set was lost under a profile that set one, and a profile format was dropped once the method set
// its own culture. A format without a culture applies with the invariant culture.
public class CultureFormatPrecedenceTests
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

    private static string Source(string profile, string mapper) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public decimal Amount { get; set; } public System.DateTime At { get; set; } }
        public class Dst { public string Amount { get; set; } = ""; public string At { get; set; } = ""; }
        {{profile}}
        public static partial class M
        {
            {{mapper}}
            public static partial Dst Map(Src src);
        }
        """;

    [Theory]
    // The method's format wins over the profile's, with the profile's culture
    [InlineData("[MapperProfile(Culture = \"ja-JP\", NumberFormat = \"N0\")]", "[Mapper(NumberFormat = \"N2\")]", "ConvertToString(src.Amount, __culture_ja_JP, \"N2\")")]
    // The profile's format applies under the method's culture
    [InlineData("[MapperProfile(Culture = \"ja-JP\", NumberFormat = \"N0\")]", "[Mapper(Culture = \"en-US\")]", "ConvertToString(src.Amount, __culture_en_US, \"N0\")")]
    [InlineData("[MapperProfile(NumberFormat = \"N0\")]", "[Mapper(Culture = \"en-US\")]", "ConvertToString(src.Amount, __culture_en_US, \"N0\")")]
    [InlineData("[MapperProfile(Culture = \"ja-JP\", DateTimeFormat = \"yyyy/MM/dd\")]", "[Mapper(Culture = \"en-US\")]", "ConvertToString(src.At, __culture_en_US, \"yyyy/MM/dd\")")]
    // Each setting on its own
    [InlineData("[MapperProfile(Culture = \"ja-JP\", NumberFormat = \"N0\", DateTimeFormat = \"yyyy\")]", "[Mapper(DateTimeFormat = \"MM\")]", "ConvertToString(src.At, __culture_ja_JP, \"MM\")")]
    [InlineData("[MapperProfile(Culture = \"ja-JP\", NumberFormat = \"N0\", DateTimeFormat = \"yyyy\")]", "[Mapper(DateTimeFormat = \"MM\")]", "ConvertToString(src.Amount, __culture_ja_JP, \"N0\")")]
    public void EachSettingComesFromMethodThenProfile(string profile, string mapper, string call)
    {
        var source = Source(profile, mapper);

        AssertCompiles(source);
        Assert.Contains(call, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    // A format with no culture from either applies with the invariant culture
    [InlineData("[MapperProfile(NumberFormat = \"N0\")]", "[Mapper]", "ConvertToString(src.Amount, global::System.Globalization.CultureInfo.InvariantCulture, \"N0\")")]
    [InlineData("", "[Mapper(NumberFormat = \"N2\")]", "ConvertToString(src.Amount, global::System.Globalization.CultureInfo.InvariantCulture, \"N2\")")]
    // A culture from the profile serves the method's format, and the other way round
    [InlineData("[MapperProfile(Culture = \"ja-JP\")]", "[Mapper(NumberFormat = \"N2\")]", "ConvertToString(src.Amount, __culture_ja_JP, \"N2\")")]
    [InlineData("[MapperProfile(NumberFormat = \"N0\")]", "[Mapper(Culture = \"en-US\")]", "ConvertToString(src.Amount, __culture_en_US, \"N0\")")]
    // A numeric format leaves a date to the conversion without a culture
    [InlineData("", "[Mapper(NumberFormat = \"N2\")]", "ConvertToString(src.At)")]
    public void FormatWithoutCultureAppliesWithInvariantCulture(string profile, string mapper, string call)
    {
        var source = Source(profile, mapper);

        AssertCompiles(source);
        Assert.Contains(call, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    // The Parse of IParsable<T> takes no format, so a format of the method or a profile leaves it to parse the text
    [InlineData("", "[Mapper(NumberFormat = \"N2\")]", "global::Test.Money.Parse(src.Price, global::System.Globalization.CultureInfo.InvariantCulture)")]
    [InlineData("", "[Mapper(DateTimeFormat = \"yyyy-MM-dd\")]", "global::Test.Money.Parse(src.Price, global::System.Globalization.CultureInfo.InvariantCulture)")]
    [InlineData("[assembly: MapperProfile(DateTimeFormat = \"yyyy-MM-dd\")]", "[Mapper]", "global::Test.Money.Parse(src.Price, global::System.Globalization.CultureInfo.InvariantCulture)")]
    [InlineData("", "[Mapper(Culture = \"de-DE\", NumberFormat = \"N2\")]", "global::Test.Money.Parse(src.Price, __culture_de_DE)")]
    // A number is parsed by the value converter, with the format
    [InlineData("", "[Mapper(NumberFormat = \"N2\")]", "ConvertToDecimal(src.Amount, global::System.Globalization.CultureInfo.InvariantCulture, \"N2\")")]
    public void FormatLeavesParsableToParse(string assemblyAttributes, string mapper, string call)
    {
        var source = $$"""
            #nullable enable
            using System;
            using Smart.Mapper;
            {{assemblyAttributes}}
            namespace Test;
            public readonly struct Money : IParsable<Money>
            {
                public decimal Value { get; init; }
                public static Money Parse(string s, IFormatProvider? provider) => new() { Value = decimal.Parse(s, provider) };
                public static bool TryParse(string? s, IFormatProvider? provider, out Money result) { result = default; return false; }
            }
            public class Src { public string Price { get; set; } = ""; public string Amount { get; set; } = ""; }
            public class Dst { public Money Price { get; set; } public decimal Amount { get; set; } }
            public static partial class M
            {
                {{mapper}}
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnostics(source), static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Contains(call, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
