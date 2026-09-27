namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Constants and NullValues of every kind written as expressions of their own type, dotted target paths through
// members the mapper cannot assign, and a void mapper into a type whose constructor takes members without a
// setter
public class ConstantAndTargetMappingTests
{
    [Fact]
    public void MapConstantKindsAssignsEveryKind()
    {
        var destination = TestMappers.MapConstantKinds(new ConstantKindSource());

        Assert.Equal(ConstantKind.Second, destination.Kind);
        Assert.Equal((ConstantKind)9, destination.Undefined);
        Assert.Equal(ConstantAccess.Read | ConstantAccess.Run, destination.Access);
        Assert.Equal(typeof(Dictionary<string, int>), destination.Type);
        Assert.Equal<int>([1, -2, 3], destination.Numbers);
        Assert.Equal<string?>(["a\"b", null, "c\\d\r\n"], destination.Texts);
        Assert.Equal<object?>([1, "x", null, typeof(int), ConstantKind.First], destination.Values);
        Assert.Equal(0.1, destination.Ratio);
        Assert.Equal(1.5e-7f, destination.Scale);
        Assert.True((destination.Zero == 0) && Double.IsNegative(destination.Zero));
        Assert.True(Double.IsNaN(destination.Missing));
        Assert.Equal('\'', destination.Quote);
        Assert.Equal("tab\tline\U00002028end\U0001F600", destination.Text);
    }

    // Each mapping creates the arrays anew, so the destinations do not share them
    [Fact]
    public void MapConstantKindsCreatesArraysEachTime()
    {
        var first = TestMappers.MapConstantKinds(new ConstantKindSource());
        var second = TestMappers.MapConstantKinds(new ConstantKindSource());

        Assert.NotSame(first.Numbers, second.Numbers);
        Assert.NotSame(first.Values, second.Values);
    }

    [Fact]
    public void MapNullValueKindsAssignsEveryKindForNullSource()
    {
        var destination = TestMappers.MapNullValueKinds(new ConstantKindSource());

        Assert.Equal(ConstantKind.Second, destination.Kind);
        Assert.Equal(typeof(string), destination.Type);
        Assert.Equal<int>([7, 8], destination.Numbers);
        Assert.True(Double.IsNaN(destination.Ratio));
        Assert.Equal("none\t\"x\"", destination.Text);
    }

    [Fact]
    public void MapNullValueKindsKeepsSourceValues()
    {
        var source = new ConstantKindSource
        {
            Kind = ConstantKind.First,
            Type = typeof(int),
            Numbers = [1],
            Ratio = 2.5,
            Text = "text"
        };

        var destination = TestMappers.MapNullValueKinds(source);

        Assert.Equal(ConstantKind.First, destination.Kind);
        Assert.Equal(typeof(int), destination.Type);
        Assert.Same(source.Numbers, destination.Numbers);
        Assert.Equal(2.5, destination.Ratio);
        Assert.Equal("text", destination.Text);
    }

    // The instances held by get-only, private-set and init-only members are written into, a member the mapper
    // can assign below them is created, and a null one is left as it is
    [Fact]
    public void MapHeldPathWritesIntoHeldInstances()
    {
        var destination = new HeldPathDestination();
        var held = destination.Held;

        TestMappers.MapHeldPath(new HeldPathSource { Value = 3, Text = "text" }, destination);

        Assert.Same(held, destination.Held);
        Assert.Equal(3, held.Value);
        Assert.Equal("text", held.Text);
        Assert.Equal(3, held.Leaf?.Value);
        Assert.Null(destination.Missing);
        Assert.Equal(3, destination.Private.Value);
        Assert.Equal(3, destination.Init.Value);
    }

    [Fact]
    public void MapHeldPathToNewWritesIntoHeldInstances()
    {
        var destination = TestMappers.MapHeldPathToNew(new HeldPathSource { Value = 4, Text = "text" });

        Assert.Equal(4, destination.Held.Value);
        Assert.Equal("text", destination.Init.Text);
    }

    // The members only the constructor assigns keep their values, and the get-only collection is refilled
    [Fact]
    public void MapConstructedTargetFillsGivenInstance()
    {
        var items = new List<MatrixDstItem> { new() { Value = -1 } };
        var destination = new ConstructedTargetDestination("keep", items);
        var source = new ConstructedTargetSource
        {
            Id = "other",
            Name = "name",
            Items = [new() { Value = 1 }, new() { Value = 2 }]
        };

        TestMappers.MapConstructedTarget(source, destination);

        Assert.Equal("keep", destination.Id);
        Assert.Equal("name", destination.Name);
        Assert.Same(items, destination.Items);
        Assert.Collection(items, static x => Assert.Equal(1, x.Value), static x => Assert.Equal(2, x.Value));
    }
}
