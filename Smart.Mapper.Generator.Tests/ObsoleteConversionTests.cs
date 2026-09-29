namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A member the generated code calls on its own to convert a value, obsolete as an error, is not called (CS0619):
// a conversion operator, a method of the [ValueConverter] / [CollectionConverter] class, the ToString(format,
// provider) or Parse of the conversions, and the constructor a collection is created with. Another conversion takes
// over when there is one, and otherwise the diagnostic of a conversion that is not there is reported. One obsolete as
// a warning is still called, warning as C# does (CS0618).
public class ObsoleteConversionTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string types, string mapper = "[Mapper]", string classAttributes = "", string members = "") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Collections.ObjectModel;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        {{types}}
        {{classAttributes}}
        public static partial class M
        {
            {{mapper}}
            public static partial Dst Map(Src src);
            {{members}}
        }
        """;

    // Nothing else converts the value, so the conversion is reported as not there
    [Theory]
    [InlineData("public readonly struct Money { public decimal Value { get; init; } [Obsolete(\"Old\", true)] public static implicit operator decimal(Money m) => m.Value; } public class Src { public Money Amount { get; set; } } public class Dst { public decimal Amount { get; set; } }")]
    [InlineData("public readonly struct Money { public decimal Value { get; init; } [Obsolete(\"Old\", true)] public static explicit operator Money(decimal v) => new() { Value = v }; } public class Src { public decimal Amount { get; set; } } public class Dst { public Money Amount { get; set; } }")]
    [InlineData("public readonly struct Code : IParsable<Code> { [Obsolete(\"Old\", true)] public static Code Parse(string s, IFormatProvider? provider) => default; public static bool TryParse(string? s, IFormatProvider? provider, out Code result) { result = default; return true; } } public class Src { public string Code { get; set; } = \"\"; } public class Dst { public Code Code { get; set; } }")]
    public void ConversionObsoleteAsErrorIsNotCalled(string types)
    {
        var (_, problems) = Build(Source(types));

        Assert.Equal("SMP0402", Assert.Single(problems));
    }

    [Fact]
    public void FormatToStringObsoleteAsErrorIsNotCalled()
    {
        var (_, problems) = Build(Source(
            "public class Point2 { [Obsolete(\"Old\", true)] public string ToString(string? format, IFormatProvider? provider) => \"\"; } public class Src { public Point2 Where { get; set; } = new(); } public class Dst { public string Where { get; set; } = \"\"; }",
            "[Mapper(Culture = \"en-US\")]"));

        Assert.Equal("SMP0402", Assert.Single(problems));
    }

    // Another conversion takes over: the generic method of the converter class, or the Parse taking a string
    [Fact]
    public void ConverterMethodObsoleteAsErrorGivesWayToGenericMethod()
    {
        var (generated, problems) = Build(Source(
            "public class Src { public int Amount { get; set; } } public class Dst { public string Amount { get; set; } = \"\"; } public static class MyConverter { [Obsolete(\"Old\", true)] public static string ConvertToString(int value) => \"\"; public static TD Convert<TS, TD>(TS value) => default!; }",
            classAttributes: "[ValueConverter(typeof(MyConverter))]"));

        Assert.Empty(problems);
        Assert.Contains("__d.Amount = global::Test.MyConverter.Convert<int, string>(src.Amount);", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void SpanParseObsoleteAsErrorGivesWayToStringParse()
    {
        var (generated, problems) = Build(Source(
            "public readonly struct Code : ISpanParsable<Code> { public static Code Parse(string s, IFormatProvider? provider) => default; public static bool TryParse(string? s, IFormatProvider? provider, out Code result) { result = default; return true; } [Obsolete(\"Old\", true)] public static Code Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => default; public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Code result) { result = default; return true; } } public class Src { public string Code { get; set; } = \"\"; } public class Dst { public Code Code { get; set; } }"));

        Assert.Empty(problems);
        Assert.Contains("global::Test.Code.Parse(src.Code, global::System.Globalization.CultureInfo.InvariantCulture)", generated, StringComparison.Ordinal);
    }

    // The collection converter method, the converter method taking the culture, and the constructor of a collection
    // class are reported with the diagnostics of a method or a collection that does not fit
    [Theory]
    [InlineData(
        "public class Src { public List<Child> Items { get; set; } = []; } public class Dst { public List<Child> Items { get; set; } = []; } public static class MyCollections { [Obsolete(\"Old\", true)] public static List<TD> ToList<TS, TD>(IEnumerable<TS> source, Func<TS, TD> map) => new(); }",
        "[Mapper(AutoMap = false)] [MapCollection(\"Items\", Mapper = nameof(MapChild))]",
        "[CollectionConverter(typeof(MyCollections))]",
        "SMP0110")]
    [InlineData(
        "public class Src { public List<Child> Items { get; set; } = []; } public class Dst { public Bag Items { get; set; } = new(0); } public class Bag : Collection<Child> { [Obsolete(\"Old\", true)] public Bag() { } public Bag(int capacity) { } }",
        "[Mapper(AutoMap = false)] [MapCollection(\"Items\", Mapper = nameof(MapChild))]",
        "",
        "SMP0212")]
    [InlineData(
        "public class Src { public int Amount { get; set; } } public class Dst { public string Amount { get; set; } = \"\"; } public static class MyConverter { public static string ConvertToString(int value) => \"\"; [Obsolete(\"Old\", true)] public static string ConvertToString(int value, IFormatProvider provider, string? format) => \"\"; public static TD Convert<TS, TD>(TS value) => default!; }",
        "[Mapper(Culture = \"en-US\")]",
        "[ValueConverter(typeof(MyConverter))]",
        "SMP0110")]
    public void ConverterOrCollectionObsoleteAsErrorEmitsDiagnostic(string types, string mapper, string classAttributes, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(types, mapper, classAttributes, "[Mapper] public static partial Child MapChild(Child source);"));

        Assert.Equal(id, Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // One obsolete as a warning is still called
    [Fact]
    public void ConversionObsoleteAsWarningIsCalled()
    {
        var (generated, problems) = Build(Source(
            "public readonly struct Money { public decimal Value { get; init; } [Obsolete(\"Old\")] public static implicit operator decimal(Money m) => m.Value; } public class Src { public Money Amount { get; set; } } public class Dst { public decimal Amount { get; set; } }"));

        Assert.Equal("CS0618", Assert.Single(problems));
        Assert.Contains("__d.Amount = src.Amount;", generated, StringComparison.Ordinal);
    }
}
