namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

public class OrderTests
{
    [Fact]
    public void MapWithOrderSetsPropertiesInCorrectOrder()
    {
        var source = new OrderTestSource { Value = 42 };
        var destination = new OrderTestDestination();

        TestMappers.MapWithOrder(source, destination);

        var setOrder = destination.GetSetOrder();
        Assert.Equal(3, setOrder.Count);
        Assert.Equal("Step1", setOrder[0]);
        Assert.Equal("Step2", setOrder[1]);
        Assert.Equal("Step3", setOrder[2]);
    }

    [Fact]
    public void MapWithReversedOrderSetsPropertiesInOrderedSequence()
    {
        var source = new OrderTestSource { Value = 42 };
        var destination = new OrderTestDestination();

        TestMappers.MapWithReversedOrder(source, destination);

        var setOrder = destination.GetSetOrder();
        Assert.Equal(3, setOrder.Count);
        Assert.Equal("Step3", setOrder[0]);
        Assert.Equal("Step1", setOrder[1]);
        Assert.Equal("Step2", setOrder[2]);
    }
}

public class SpecializedConverterTests
{
    [Fact]
    public void MapWithSpecializedConverterUsesSpecializedMethodsWhenAvailable()
    {
        var source = new SpecializedConverterSource { StringValue = "42", IntValue = 100, DoubleValue = 3.14 };
        var destination = new SpecializedConverterDestination();

        TestMappers.MapWithSpecializedConverter(source, destination);

        Assert.Equal(1042, destination.StringValue);
        Assert.Equal("SPEC_100", destination.IntValue);
        Assert.Equal(3.14m, destination.DoubleValue);
    }
}

public class DefaultValueConverterSpecializedTests
{
    [Fact]
    public void DefaultValueConverterStringToIntUsesSpecializedMethod()
    {
        var result = DefaultValueConverter.ConvertToInt32("123");
        Assert.Equal(123, result);
    }

    [Fact]
    public void DefaultValueConverterIntToStringUsesSpecializedMethod()
    {
        var result = DefaultValueConverter.ConvertToString(456);
        Assert.Equal("456", result);
    }

    [Fact]
    public void DefaultValueConverterGenericConvertStillWorks()
    {
        var intResult = DefaultValueConverter.Convert<string, int>("789");
        Assert.Equal(789, intResult);

        var stringResult = DefaultValueConverter.Convert<int, string>(321);
        Assert.Equal("321", stringResult);
    }
}
