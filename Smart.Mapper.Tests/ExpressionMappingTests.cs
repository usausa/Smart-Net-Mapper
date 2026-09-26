namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Several [MapExpression] in one mapper declaring the same variable names (out var n, is int n)
public class ExpressionMappingTests
{
    [Fact]
    public void MapExpressionToNewEvaluatesEachExpression()
    {
        var source = new ExpressionSource { First = "12", Second = "x", Boxed = 3, OtherBoxed = "4" };

        var destination = TestMappers.MapExpressionToNew(source);

        Assert.Equal(12, destination.First);
        Assert.Equal(-1, destination.Second);
        Assert.Equal(3, destination.Boxed);
        Assert.Equal(-1, destination.OtherBoxed);
    }

    [Fact]
    public void MapExpressionIntoReadsDestinationParameter()
    {
        var source = new ExpressionSource { First = "1", Second = "2", Boxed = 3, OtherBoxed = 4 };
        var destination = new ExpressionDestination { Label = "keep" };
        var empty = new ExpressionDestination();

        TestMappers.MapExpressionInto(source, destination);
        TestMappers.MapExpressionInto(source, empty);

        Assert.Equal(1, destination.First);
        Assert.Equal(2, destination.Second);
        Assert.Equal(3, destination.Boxed);
        Assert.Equal(4, destination.OtherBoxed);
        Assert.Equal("keep", destination.Label);
        Assert.Equal("(none)", empty.Label);
    }

    [Fact]
    public void MapExpressionInitAssignsInitOnlyAndRequiredTargets()
    {
        var source = new ExpressionSource { First = "5", Second = "6", Boxed = "7" };

        var destination = TestMappers.MapExpressionInit(source);

        Assert.Equal(5, destination.First);
        Assert.Equal(6, destination.Second);
        Assert.Equal(-1, destination.Boxed);
    }

    [Fact]
    public void MapExpressionWithContextPassesInSourceAndContext()
    {
        var source = new ReadOnlyStructSource { Id = 1, Name = "20" };
        var context = new ExpressionContext { Offset = 100 };

        var destination = TestMappers.MapExpressionWithContext(source, context);

        Assert.Equal(120, destination.First);
        Assert.Equal(2, destination.Second);
        // Order = 1 is evaluated before Order = 2
        Assert.Equal(1, destination.OtherBoxed);
        Assert.Equal(2, destination.Boxed);
    }
}
