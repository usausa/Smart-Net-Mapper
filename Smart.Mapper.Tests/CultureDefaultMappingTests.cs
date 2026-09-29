namespace Smart.Mapper;

using System.Globalization;

// The culture of the conversions: the invariant one by default, the current one under DefaultCulture Current, and the
// one a CultureInfo parameter gives, which a null one falls back from to the culture the method takes without it
public sealed partial class CultureDefaultMappingTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void InvariantIsDefault()
    {
        var destination = InvariantMappers.Map(new Amount { Value = 1234.5m, At = new DateTime(2026, 9, 28, 11, 55, 0, DateTimeKind.Utc) });

        Assert.Equal("1234.5", destination.Value);
        Assert.Equal("2026-09-28T11:55:00.0000000Z", destination.At);
    }

    [Fact]
    public void FormatWithoutCultureAppliesWithInvariantCulture()
    {
        var destination = InvariantMappers.MapFormatted(new Amount { Value = 1234.5m });

        Assert.Equal("1,234.50", destination.Value);
    }

    [Fact]
    public void CurrentUsesCurrentCultureOfConversion()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = German;
            var german = CurrentMappers.Map(new Amount { Value = 1234.5m });
            CultureInfo.CurrentCulture = English;
            var english = CurrentMappers.Map(new Amount { Value = 1234.5m });

            Assert.Equal("1234,5", german.Value);
            Assert.Equal("1234.5", english.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void CultureNameWinsOverCurrent()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = English;

            var destination = CurrentMappers.MapGerman(new Amount { Value = 1234.5m });

            Assert.Equal("1234,5", destination.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void CultureParameterGivesCulture()
    {
        Assert.Equal("1234,5", InvariantMappers.MapWith(new Amount { Value = 1234.5m }, German).Value);
        Assert.Equal("1234.5", InvariantMappers.MapWith(new Amount { Value = 1234.5m }, English).Value);
    }

    [Fact]
    public void CultureParameterFormatsWithFormat()
    {
        var destination = InvariantMappers.MapFormattedWith(new Amount { Value = 1234.5m }, German);

        Assert.Equal("1.234,50", destination.Value);
    }

    [Fact]
    public void CultureOfPropertyWinsOverParameter()
    {
        var destination = InvariantMappers.MapWithEnglishValue(new Amount { Value = 1234.5m, Other = 1234.5m }, German);

        Assert.Equal("1234.5", destination.Value);
        Assert.Equal("1234,5", destination.Other);
    }

    [Fact]
    public void NullCultureParameterFallsBackToCultureOfMethod()
    {
        Assert.Equal("1234,5", InvariantMappers.MapWithOptional(new Amount { Value = 1234.5m }, German).Value);
        Assert.Equal("1234.5", InvariantMappers.MapWithOptional(new Amount { Value = 1234.5m }, null).Value);
        Assert.Equal("1234,5", GermanMappers.MapWithOptional(new Amount { Value = 1234.5m }, null).Value);
    }

    [Fact]
    public void CultureParameterIsPassedOnToCallbacks()
    {
        var destination = InvariantMappers.MapWithCallback(new Amount { Value = 1.5m }, German);

        Assert.Equal("de-DE:1,5", destination.Culture);
    }

    // The Parse of IParsable<T> takes no format, so a format leaves the text to it
    [Fact]
    public void FormatLeavesParsableToParse()
    {
        var destination = InvariantMappers.ParseFormatted(new AmountText { Value = "1234.5" });

        Assert.Equal(1234.5m, destination.Value.Value);
    }

    // A nested mapper taking a culture not null gets the one the conversions go with, the culture of the method for null
    [Fact]
    public void NullCultureParameterGoesToNestedMapperAsCultureOfMethod()
    {
        var source = new AmountHolder { Price = new Amount { Value = 1234.5m } };

        Assert.Equal("1234,5", GermanMappers.MapHolder(source, null).Price.Value);
        Assert.Equal("1234.5", GermanMappers.MapHolder(source, English).Price.Value);
    }

    public sealed class Amount
    {
        public decimal Value { get; set; }

        public decimal Other { get; set; }

        public DateTime At { get; set; }
    }

    public sealed class AmountHolder
    {
        public Amount Price { get; set; } = new();
    }

    public sealed class AmountTextHolder
    {
        public AmountText Price { get; set; } = new();
    }

    public sealed class AmountText
    {
        public string Value { get; set; } = string.Empty;

        public string Other { get; set; } = string.Empty;

        public string At { get; set; } = string.Empty;

        public string Culture { get; set; } = string.Empty;
    }

    public sealed class Money : IParsable<Money>
    {
        public decimal Value { get; init; }

        public static Money Parse(string s, IFormatProvider? provider) => new() { Value = Decimal.Parse(s, provider) };

        public static bool TryParse(string? s, IFormatProvider? provider, out Money result)
        {
            var parsed = Decimal.TryParse(s, provider, out var value);
            result = new Money { Value = value };
            return parsed;
        }
    }

    public sealed class ParsedAmount
    {
        public Money Value { get; set; } = new();
    }

    internal static partial class InvariantMappers
    {
        [Mapper]
        public static partial AmountText Map(Amount source);

        [Mapper(NumberFormat = "N2")]
        public static partial AmountText MapFormatted(Amount source);

        [Mapper]
        public static partial AmountText MapWith(Amount source, CultureInfo culture);

        [Mapper(NumberFormat = "N2")]
        public static partial AmountText MapFormattedWith(Amount source, CultureInfo culture);

        [Mapper]
        [MapProperty(nameof(AmountText.Value), Culture = "en-US")]
        public static partial AmountText MapWithEnglishValue(Amount source, CultureInfo culture);

        [Mapper]
        public static partial AmountText MapWithOptional(Amount source, CultureInfo? culture);

        [Mapper]
        [AfterMap(nameof(SetCulture))]
        public static partial AmountText MapWithCallback(Amount source, CultureInfo culture);

        private static void SetCulture(Amount source, AmountText destination, CultureInfo culture) => destination.Culture = culture.Name + ":" + source.Value.ToString(culture);

        [Mapper(NumberFormat = "N2")]
        public static partial ParsedAmount ParseFormatted(AmountText source);
    }

    [MapperProfile(DefaultCulture = MapperCulture.Current)]
    internal static partial class CurrentMappers
    {
        [Mapper]
        public static partial AmountText Map(Amount source);

        [Mapper(Culture = "de-DE")]
        public static partial AmountText MapGerman(Amount source);
    }

    [MapperProfile(Culture = "de-DE")]
    internal static partial class GermanMappers
    {
        [Mapper]
        public static partial AmountText MapWithOptional(Amount source, CultureInfo? culture);

        [Mapper]
        [MapNested(nameof(AmountTextHolder.Price), Mapper = nameof(MapAmount))]
        public static partial AmountTextHolder MapHolder(AmountHolder source, CultureInfo? culture);

        [Mapper]
        private static partial AmountText MapAmount(Amount source, CultureInfo culture);
    }
}
