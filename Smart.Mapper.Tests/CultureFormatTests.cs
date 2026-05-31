namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

public class CultureFormatMappingTests
{
    [Fact]
    public void Map_WithCultureAndNumberFormat_FormatsDoubleWithFrenchLocale()
    {
        // fr-FR uses comma as decimal separator, N2 gives 2 decimal places
        var source = new CultureFormatSource { Amount = 1234.56, EventDate = new DateTime(2024, 6, 15), Price = 99.9m };

        var destination = TestMappers.MapWithCultureFormat(source);

        // fr-FR N2: "1\u00a0234,56" (non-breaking space as thousands separator)
        Assert.Contains("1", destination.Amount, StringComparison.Ordinal);
        Assert.Contains("234", destination.Amount, StringComparison.Ordinal);
        Assert.Contains(",56", destination.Amount, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_WithCultureParse_ParsesDoubleWithFrenchLocale()
    {
        // fr-FR decimal separator is comma
        var source = new CultureParseSource { Amount = "1234,56", EventDate = "15/06/2024" };

        var destination = TestMappers.MapWithCultureParse(source);

        Assert.Equal(1234.56, destination.Amount, precision: 2);
    }

    [Fact]
    public void Map_WithCultureParse_ParsesDateWithFrenchLocale()
    {
        // fr-FR date format is dd/MM/yyyy
        var source = new CultureParseSource { Amount = "0", EventDate = "15/06/2024" };

        var destination = TestMappers.MapWithCultureParse(source);

        Assert.Equal(new DateTime(2024, 6, 15), destination.EventDate);
    }

    [Fact]
    public void Map_WithPropertyCultureOverride_UsesPropertyCultureForValueB()
    {
        // ValueA should use en-US (method-level), ValueB should use de-DE (property-level)
        var source = new CultureOverrideSource { ValueA = 1234.5, ValueB = 1234.5 };

        var destination = TestMappers.MapWithPropertyCultureOverride(source);

        // en-US: dot as decimal separator
        Assert.Contains(".", destination.ValueA, StringComparison.Ordinal);
        // de-DE: comma as decimal separator
        Assert.Contains(",", destination.ValueB, StringComparison.Ordinal);
    }
}
