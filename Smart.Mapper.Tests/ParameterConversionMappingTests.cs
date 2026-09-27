namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// A converter, a condition and a [MapUsing] method take the value as an interface it implements, and a nullable struct
// goes to a method taking the struct it holds for a value only
public class ParameterConversionMappingTests
{
    [Fact]
    public void ValueGoesThroughConversion()
    {
        var destination = new ParameterConversionDestination();

        TestMappers.MapParameterConversion(
            new ParameterConversionSource { Name = "a", Owner = new ConversionPerson { Name = "b" }, Tags = ["x", "y"], Quantity = 2, Score = 3 },
            destination);

        Assert.Equal("[a]", destination.Title);
        Assert.Equal("[b]", destination.OwnerName);
        Assert.Equal("x,y", destination.TagText);
        Assert.Equal("2", destination.QuantityText);
        Assert.Equal("3", destination.ScoreText);
        Assert.Equal(3, destination.Score);
    }

    [Fact]
    public void NullIsNotPassed()
    {
        var destination = new ParameterConversionDestination();

        TestMappers.MapParameterConversion(new ParameterConversionSource { Name = "a" }, destination);

        Assert.Equal("[a]", destination.Title);
        Assert.Equal("keep", destination.OwnerName);
        Assert.Equal(string.Empty, destination.TagText);
        Assert.Equal("keep", destination.QuantityText);
        Assert.Equal("-", destination.ScoreText);
        Assert.Equal(-1, destination.Score);
    }

    [Fact]
    public void ConditionIsCheckedWithValue()
    {
        var destination = new ParameterConversionDestination();

        TestMappers.MapParameterConversion(new ParameterConversionSource { Score = -5 }, destination);

        Assert.Equal("-5", destination.ScoreText);
        Assert.Equal(-1, destination.Score);
    }

    [Fact]
    public void ConstructorArgumentTakesNullOrDefault()
    {
        var empty = TestMappers.MapParameterConversionRecord(new ParameterConversionSource());
        var filled = TestMappers.MapParameterConversionRecord(new ParameterConversionSource { Quantity = 4, Score = 5 });

        Assert.Null(empty.QuantityText);
        Assert.Null(empty.ScoreText);
        Assert.Equal("4", filled.QuantityText);
        Assert.Equal("5", filled.ScoreText);
    }
}
