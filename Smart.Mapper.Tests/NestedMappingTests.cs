namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

public class NestedMappingTests
{
    [Fact]
    public void MapFlatToNestedMapsToNestedProperties()
    {
        var source = new FlatSource { Value1 = 10, Value2 = 20, Value3 = 30 };
        var destination = new NestedDestination();

        TestMappers.Map(source, destination);

        Assert.NotNull(destination.Child1);
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
        Assert.Equal(10, destination.Child1.Value);
        Assert.Equal(20, destination.Child2.Value);
        Assert.Equal(30, destination.Child3.Value);
    }

    [Fact]
    public void MapToNewFlatToNestedReturnsNestedObject()
    {
        var source = new FlatSource { Value1 = 100, Value2 = 200, Value3 = 300 };

        var destination = TestMappers.MapToNew(source);

        Assert.NotNull(destination);
        Assert.NotNull(destination.Child1);
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
        Assert.Equal(100, destination.Child1.Value);
        Assert.Equal(200, destination.Child2.Value);
        Assert.Equal(300, destination.Child3.Value);
    }

    [Fact]
    public void MapNestedToFlatFlattensProperties()
    {
        var source = new NestedSource
        {
            Child = new NestedSourceChild { Id = 42, Name = "Test" },
            DirectValue = 999
        };
        var destination = new FlatDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(42, destination.ChildId);
        Assert.Equal("Test", destination.ChildName);
        Assert.Equal(999, destination.DirectValue);
    }

    [Fact]
    public void MapDeepNestedMapsToDeepNestedProperties()
    {
        var source = new DeepSource { DeepValue = 12345 };
        var destination = new DeepNestedDestination();

        TestMappers.Map(source, destination);

        Assert.NotNull(destination.Outer);
        Assert.NotNull(destination.Outer.Inner);
        Assert.Equal(12345, destination.Outer.Inner.Value);
    }

    [Fact]
    public void MapToNewDeepNestedReturnsDeepNestedObject()
    {
        var source = new DeepSource { DeepValue = 67890 };

        var destination = TestMappers.MapToNew(source);

        Assert.NotNull(destination);
        Assert.NotNull(destination.Outer);
        Assert.NotNull(destination.Outer.Inner);
        Assert.Equal(67890, destination.Outer.Inner.Value);
    }

    [Fact]
    public void MapFlatToNestedPreservesExistingNestedObjects()
    {
        var source = new FlatSource { Value1 = 10, Value2 = 20, Value3 = 30 };
        var existingChild1 = new DestinationChild { Value = 999 };
        var destination = new NestedDestination { Child1 = existingChild1 };

        TestMappers.Map(source, destination);

        Assert.Same(existingChild1, destination.Child1);
        Assert.Equal(10, destination.Child1.Value);
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
    }

    [Fact]
    public void MapDeepNestedSourceToFlatFlattensMultipleLevels()
    {
        var source = new DeepNestedSource
        {
            Outer = new DeepSourceOuter { Inner = new DeepSourceInner { Value = 12345, Name = "DeepName" } },
            DirectValue = 100
        };
        var destination = new DeepFlatDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(12345, destination.OuterInnerValue);
        Assert.Equal("DeepName", destination.OuterInnerName);
        Assert.Equal(100, destination.DirectValue);
    }

    [Fact]
    public void MapDeepNestedSourceWithNullOuterSkipsNestedProperties()
    {
        var source = new DeepNestedSource { Outer = null, DirectValue = 200 };
        var destination = new DeepFlatDestination { OuterInnerValue = 999, OuterInnerName = "Original" };

        TestMappers.Map(source, destination);

        Assert.Equal(200, destination.DirectValue);
        Assert.Equal(999, destination.OuterInnerValue);
        Assert.Equal("Original", destination.OuterInnerName);
    }

    [Fact]
    public void MapDeepNestedSourceWithNullInnerSkipsNestedProperties()
    {
        var source = new DeepNestedSource
        {
            Outer = new DeepSourceOuter { Inner = null },
            DirectValue = 300
        };
        var destination = new DeepFlatDestination { OuterInnerValue = 888, OuterInnerName = "Original" };

        TestMappers.Map(source, destination);

        Assert.Equal(300, destination.DirectValue);
        Assert.Equal(888, destination.OuterInnerValue);
        Assert.Equal("Original", destination.OuterInnerName);
    }
}

public class MapNestedTests
{
    [Fact]
    public void MapNestedWithValueMapsNestedObject()
    {
        var source = new NestedObjectSource
        {
            Child = new NestedObjectSourceChild { Value = 42, Text = "Hello" },
            DirectValue = 100
        };
        var destination = new NestedObjectDestination();

        TestMappers.Map(source, destination);

        Assert.NotNull(destination.Child);
        Assert.Equal(42, destination.Child.Value);
        Assert.Equal("Hello", destination.Child.Text);
        Assert.Equal(100, destination.DirectValue);
    }

    [Fact]
    public void MapNestedNullSourceSetsDefault()
    {
        var source = new NestedObjectSource { Child = null, DirectValue = 50 };
        var destination = new NestedObjectDestination { Child = new NestedObjectDestinationChild { Value = 999 } };

        TestMappers.Map(source, destination);

        Assert.Null(destination.Child);
        Assert.Equal(50, destination.DirectValue);
    }

    [Fact]
    public void MapNestedWithReturnTypeReturnsNewObject()
    {
        var source = new NestedObjectSource
        {
            Child = new NestedObjectSourceChild { Value = 123, Text = "Test" },
            DirectValue = 456
        };

        var destination = TestMappers.MapToNew(source);

        Assert.NotNull(destination);
        Assert.NotNull(destination.Child);
        Assert.Equal(123, destination.Child.Value);
        Assert.Equal("Test", destination.Child.Text);
        Assert.Equal(456, destination.DirectValue);
    }

    [Fact]
    public void MapNestedWithVoidMapperMapsNestedObject()
    {
        var source = new NestedObjectSource
        {
            Child = new NestedObjectSourceChild { Value = 77, Text = "VoidTest" },
            DirectValue = 88
        };
        var destination = new NestedObjectDestination();

        TestMappers.MapWithVoidNested(source, destination);

        Assert.NotNull(destination.Child);
        Assert.Equal(77, destination.Child.Value);
        Assert.Equal("VoidTest", destination.Child.Text);
        Assert.Equal(88, destination.DirectValue);
    }
}
