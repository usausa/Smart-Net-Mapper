namespace Smart.Mapper.Generator.Tests;

using System.Globalization;

using Microsoft.CodeAnalysis;

// Without a culture or a format, DateTime goes to text through DefaultValueConverter.ConvertToString(DateTime), which
// writes the round-trip format (O) as it does for DateOnly, TimeOnly and DateTimeOffset, and text goes to DateTime through
// ConvertToDateTime(string), which keeps the kind the text gives (DateTimeStyles.RoundtripKind): UTC for a Z, local for an
// offset, and unspecified without either. A nullable DateTime goes the same way for a value. The generic conversion of the
// converter class, which a [ValueConverter] class of the user falls back to, does the same.
public class DateTimeDefaultFormatTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    [Fact]
    public void DefaultConversionCallsConverterWithoutCulture()
    {
        const string source =
            """
            #nullable enable
            using System;
            using Smart.Mapper;
            namespace Test;
            public class Src { public DateTime At { get; set; } public DateTime? NullableAt { get; set; } public string Text { get; set; } = ""; public string? NullableText { get; set; } }
            public class Dst { public string At { get; set; } = ""; public string? NullableAt { get; set; } public DateTime Text { get; set; } public DateTime? NullableText { get; set; } }
            public static partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnosticsAll(source), static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)));
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.At = global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.At);", generated, StringComparison.Ordinal);
        Assert.Contains("__d.NullableAt = src.NullableAt is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.NullableAt.GetValueOrDefault()) : null;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Text = global::Smart.Mapper.DefaultValueConverter.ConvertToDateTime(src.Text);", generated, StringComparison.Ordinal);
        Assert.Contains("__d.NullableText = src.NullableText is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToDateTime(src.NullableText) : null;", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc, "2024-01-02T03:04:05.6780000Z")]
    [InlineData(DateTimeKind.Unspecified, "2024-01-02T03:04:05.6780000")]
    public void DateTimeIsWrittenInRoundTripFormat(DateTimeKind kind, string expected)
    {
        var value = new DateTime(2024, 1, 2, 3, 4, 5, 678, kind);

        Assert.Equal(expected, DefaultValueConverter.ConvertToString(value));
        Assert.Equal(expected, DefaultValueConverter.Convert<DateTime, string>(value));
    }

    [Fact]
    public void LocalDateTimeIsWrittenWithOffset()
    {
        var value = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Local);

        Assert.Equal(value.ToString("O", CultureInfo.InvariantCulture), DefaultValueConverter.ConvertToString(value));
    }

    [Theory]
    [InlineData("2024-01-02T03:04:05.6780000Z", DateTimeKind.Utc)]
    [InlineData("2024-01-02T03:04:05.6780000", DateTimeKind.Unspecified)]
    [InlineData("2024-01-02T03:04:05.6780000+09:00", DateTimeKind.Local)]
    public void TextKeepsItsKind(string text, DateTimeKind kind)
    {
        var value = DefaultValueConverter.ConvertToDateTime(text);

        Assert.Equal(kind, value.Kind);
        Assert.Equal(value, DefaultValueConverter.Convert<string, DateTime>(text));
        Assert.Equal(value.Kind, DefaultValueConverter.Convert<string, DateTime>(text).Kind);
        if (kind == DateTimeKind.Local)
        {
            Assert.Equal(new DateTime(2024, 1, 1, 18, 4, 5, 678, DateTimeKind.Utc), value.ToUniversalTime());
        }
        else
        {
            Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5, 678, kind), value);
        }
    }

    // Text in another format still parses, as before
    [Fact]
    public void InvariantTextStillParses()
    {
        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5), DefaultValueConverter.ConvertToDateTime("01/02/2024 03:04:05"));
    }

    // With the round-trip format given (DateTimeFormat = "O" or "o"), text keeps the kind it gives, as it does without a
    // format, where it used to be read as local time for a Z. The generated code passes the format to the converter
    [Fact]
    public void FormatIsPassedToConverter()
    {
        const string source =
            """
            #nullable enable
            using System;
            using Smart.Mapper;
            namespace Test;
            public class Src { public string At { get; set; } = ""; public DateTime When { get; set; } }
            public class Dst { public DateTime At { get; set; } public string When { get; set; } = ""; }
            public static partial class M
            {
                [Mapper(Culture = "en-US", DateTimeFormat = "O")]
                public static partial Dst Map(Src src);
            }
            """;

        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnosticsAll(source), static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)));
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.At = global::Smart.Mapper.DefaultValueConverter.ConvertToDateTime(src.At, __culture_en_US, \"O\");", generated, StringComparison.Ordinal);
        Assert.Contains("__d.When = global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.When, __culture_en_US, \"O\");", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("O", "2024-01-02T03:04:05.6780000Z", DateTimeKind.Utc)]
    [InlineData("o", "2024-01-02T03:04:05.6780000Z", DateTimeKind.Utc)]
    [InlineData("O", "2024-01-02T03:04:05.6780000", DateTimeKind.Unspecified)]
    [InlineData("O", "2024-01-02T03:04:05.6780000+09:00", DateTimeKind.Local)]
    public void RoundTripFormatKeepsKind(string format, string text, DateTimeKind kind)
    {
        var value = DefaultValueConverter.ConvertToDateTime(text, CultureInfo.InvariantCulture, format);

        Assert.Equal(kind, value.Kind);
        Assert.Equal(DefaultValueConverter.ConvertToDateTime(text), value);
    }

    [Theory]
    [InlineData("O", DateTimeKind.Utc)]
    [InlineData("O", DateTimeKind.Unspecified)]
    [InlineData("o", DateTimeKind.Local)]
    public void RoundTripFormatRoundTrips(string format, DateTimeKind kind)
    {
        var value = new DateTime(2024, 1, 2, 3, 4, 5, 678, kind);

        var text = DefaultValueConverter.ConvertToString(value, CultureInfo.InvariantCulture, format);
        var back = DefaultValueConverter.ConvertToDateTime(text, CultureInfo.InvariantCulture, format);

        Assert.Equal(value, back);
        Assert.Equal(kind, back.Kind);
    }

    // The RFC 1123 format (R, r) reads the time as written, of an unspecified kind, as DateTime.ParseExact does: its GMT is
    // text of the format and not a time zone, and ToString writes the time as it is, whatever its kind
    [Theory]
    [InlineData("R")]
    [InlineData("r")]
    public void Rfc1123FormatReadsTimeAsWritten(string format)
    {
        var value = DefaultValueConverter.ConvertToDateTime("Tue, 02 Jan 2024 03:04:05 GMT", CultureInfo.InvariantCulture, format);

        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5), value);
        Assert.Equal(DateTimeKind.Unspecified, value.Kind);
        Assert.Equal("Tue, 02 Jan 2024 03:04:05 GMT", DefaultValueConverter.ConvertToString(value, CultureInfo.InvariantCulture, format));
    }
}
