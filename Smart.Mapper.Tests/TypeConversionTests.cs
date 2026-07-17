namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

public class TypeConversionMappingTests
{
    [Fact]
    public void Map_TypeConversion_ConvertsTypes()
    {
        var source = new TypeConversionSource { IntValue = 999, StringValue = "123" };
        var destination = new TypeConversionDestination();

        TestMappers.Map(source, destination);

        Assert.Equal("999", destination.IntValue);
        Assert.Equal(123, destination.StringValue);
    }

    [Fact]
    public void Map_CharAndStringInterconvert()
    {
        var source = new ScalarCharSource { CharValue = 'A', StringValue = "B" };

        var destination = TestMappers.MapScalarChar(source);

        Assert.Equal("A", destination.CharValue);
        Assert.Equal('B', destination.StringValue);
    }

    [Fact]
    public void Map_NumericToHalf_WithCulture()
    {
        var source = new ScalarHalfSource { IntValue = 3 };

        var destination = TestMappers.MapScalarHalf(source);

        Assert.Equal((Half)3, destination.IntValue);
    }
}

public class ExtendedTypeConversionTests
{
    [Fact]
    public void Map_ExtendedTypeConversions_ConvertsCorrectly()
    {
        var guid = Guid.NewGuid();
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0);
        var source = new ExtendedTypeConversionSource
        {
            LongValue = 1234567890L,
            DoubleValue = 3.14159,
            DecimalValue = 99.99m,
            BoolValue = true,
            DateTimeValue = dateTime,
            GuidString = guid.ToString()
        };
        var destination = new ExtendedTypeConversionDestination();

        TestMappers.Map(source, destination);

        Assert.Equal("1234567890", destination.LongValue);
        Assert.Contains("3.14159", destination.DoubleValue, StringComparison.Ordinal);
        Assert.Contains("99.99", destination.DecimalValue, StringComparison.Ordinal);
        Assert.Equal("True", destination.BoolValue);
        Assert.Contains("01/15/2024", destination.DateTimeValue, StringComparison.Ordinal);
        Assert.Contains("10:30:00", destination.DateTimeValue, StringComparison.Ordinal);
        Assert.Equal(guid, destination.GuidString);
    }
}

public class NumericConversionTests
{
    [Fact]
    public void Map_NumericConversions_ConvertsCorrectly()
    {
        var source = new NumericConversionSource { IntValue = 100, LongValue = 200L, DoubleValue = 3.5 };
        var destination = new NumericConversionDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(100L, destination.IntValue);
        Assert.Equal(200, destination.LongValue);
        Assert.Equal(3.5m, destination.DoubleValue);
    }
}

public class DateTimeTypeConversionTests
{
    [Fact]
    public void MapDateTimeTypes_ConvertsToString()
    {
        var date = new DateOnly(2024, 6, 15);
        var time = new TimeOnly(12, 30, 0);
        var dto = new DateTimeOffset(2024, 6, 15, 12, 30, 0, TimeSpan.Zero);
        var ts = new TimeSpan(1, 2, 3);
        var source = new DateTimeTypeConversionSource
        {
            DateOnlyValue = date,
            TimeOnlyValue = time,
            DateTimeOffsetValue = dto,
            TimeSpanValue = ts
        };
        var destination = new DateTimeTypeToStringDestination();

        TestMappers.MapDateTimeTypes(source, destination);

        Assert.Equal(date.ToString("O", System.Globalization.CultureInfo.InvariantCulture), destination.DateOnlyValue);
        Assert.Equal(time.ToString("O", System.Globalization.CultureInfo.InvariantCulture), destination.TimeOnlyValue);
        Assert.False(String.IsNullOrEmpty(destination.DateTimeOffsetValue));
        Assert.False(String.IsNullOrEmpty(destination.TimeSpanValue));
    }
}

public class ModernNumericConversionTests
{
    [Fact]
    public void MapModernNumericTypes_ConvertsHalfAndInt()
    {
        var source = new ModernNumericConversionSource { HalfValue = (Half)3.14, IntValue = 42 };
        var destination = new ModernNumericConversionDestination();

        TestMappers.MapModernNumericTypes(source, destination);

        Assert.False(String.IsNullOrEmpty(destination.HalfValue));
        Assert.Equal((Half)42, destination.IntValue);
    }
}

public class CaseInsensitiveMappingTests
{
    [Fact]
    public void MapCaseInsensitive_MapsLowercaseSourceToTitleCaseDestination()
    {
        var source = new CaseInsensitiveSource { userid = 7, username = "alice" };
        var destination = new CaseInsensitiveDestination();

        TestMappers.MapCaseInsensitive(source, destination);

        Assert.Equal(7, destination.UserId);
        Assert.Equal("alice", destination.UserName);
    }
}
