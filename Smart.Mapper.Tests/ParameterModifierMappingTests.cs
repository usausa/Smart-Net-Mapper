namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// The implementation repeats the modifier each parameter is declared with
public class ParameterModifierMappingTests
{
    [Fact]
    public void MapReadOnlyStructByValueCopiesAllProperties()
    {
        var source = new ReadOnlyStructSource { Id = 1, Name = "Value" };

        var destination = TestMappers.MapReadOnlyStructByValue(source);

        Assert.Equal(1, destination.Id);
        Assert.Equal("Value", destination.Name);
    }

    [Fact]
    public void MapMutableStructInPassesSourceToExpression()
    {
        var source = new MutableStructSource { Id = 2, Name = "In" };

        var destination = TestMappers.MapMutableStructIn(source);

        Assert.Equal(2, destination.Id);
        Assert.Equal("In!", destination.Name);
    }

    [Fact]
    public void MapIntoStructFillsCallerInstance()
    {
        var source = new MutableStructSource { Id = 5, Name = "Ref" };
        var destination = new MutableStructDestination { Total = 10 };

        TestMappers.MapIntoStruct(source, ref destination);

        Assert.Equal(5, destination.Id);
        Assert.Equal("Ref", destination.Name);
        Assert.Equal(15, destination.Total);
    }

    [Fact]
    public void MapWithReadOnlyContextPassesContextToExpressionAndMethod()
    {
        var source = new ExpressionSource { First = "7", Second = "second" };
        var context = new ExpressionContext { Offset = 100 };

        var destination = TestMappers.MapWithReadOnlyContext(source, in context);

        Assert.Equal(107, destination.First);
        Assert.Equal("second:100", destination.Label);
    }
}
