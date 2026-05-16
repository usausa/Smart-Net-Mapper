namespace Smart.Mapper;

public class NullHandlingTests
{
    [Fact]
    public void Map_NestedSourceWithNullChild_SkipsCopyForNullSource()
    {
        var source = new NullableNestedSource { Child = null, DirectValue = 100 };
        var destination = new NullableNestedFlatDestination { ChildId = 999, ChildName = "Original", DirectValue = 0 };

        TestMappers.Map(source, destination);

        Assert.Equal(100, destination.DirectValue);
        Assert.Equal(999, destination.ChildId);
        Assert.Equal("Original", destination.ChildName);
    }

    [Fact]
    public void Map_NestedSourceWithNonNullChild_CopiesNestedProperties()
    {
        var source = new NullableNestedSource
        {
            Child = new NullableNestedSourceChild { Id = 42, Name = "Test" },
            DirectValue = 100
        };
        var destination = new NullableNestedFlatDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(100, destination.DirectValue);
        Assert.Equal(42, destination.ChildId);
        Assert.Equal("Test", destination.ChildName);
    }

    [Fact]
    public void Map_NullableProperties_CopiesNullValues()
    {
        var source = new NullablePropertySource { NullableName = null, NullableInt = null, NonNullableName = "Test" };
        var destination = new NullablePropertyDestination { NullableName = "Original", NullableInt = 999 };

        TestMappers.Map(source, destination);

        Assert.Null(destination.NullableName);
        Assert.Null(destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void Map_NullableProperties_CopiesNonNullValues()
    {
        var source = new NullablePropertySource { NullableName = "NewName", NullableInt = 42, NonNullableName = "Test" };
        var destination = new NullablePropertyDestination();

        TestMappers.Map(source, destination);

        Assert.Equal("NewName", destination.NullableName);
        Assert.Equal(42, destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void Map_NullableToNonNullable_WithNullSource_SetsDefault()
    {
        var source = new NullableToNonNullableSource { Name = null };
        var destination = new NullableToNonNullableDestination { Name = "Original" };

        TestMappers.Map(source, destination);

        Assert.Null(destination.Name);
    }

    [Fact]
    public void Map_NullableToNonNullable_WithNonNullSource_CopiesValue()
    {
        var source = new NullableToNonNullableSource { Name = "NewValue" };
        var destination = new NullableToNonNullableDestination { Name = "Original" };

        TestMappers.Map(source, destination);

        Assert.Equal("NewValue", destination.Name);
    }

    [Fact]
    public void Map_NullableIntToString_WithNullSource_SetsDefault()
    {
        var source = new NullableIntToStringSource { IntValue = null };
        var destination = new NullableIntToStringDestination { IntValue = "Original" };

        TestMappers.Map(source, destination);

        Assert.Null(destination.IntValue);
    }

    [Fact]
    public void Map_NullableIntToString_WithNonNullSource_ConvertsValue()
    {
        var source = new NullableIntToStringSource { IntValue = 42 };
        var destination = new NullableIntToStringDestination { IntValue = "Original" };

        TestMappers.Map(source, destination);

        Assert.Equal("42", destination.IntValue);
    }

    [Fact]
    public void MapWithNullSubstitute_WhenSourceIsNull_UsesFallbackValues()
    {
        var source = new NullSubstituteSource { Name = null, Count = null };
        var destination = new NullSubstituteDestination();

        TestMappers.MapWithNullSubstitute(source, destination);

        Assert.Equal("(none)", destination.Name);
        Assert.Equal(-1, destination.Count);
    }

    [Fact]
    public void MapWithNullSubstitute_WhenSourceHasValues_UsesSourceValues()
    {
        var source = new NullSubstituteSource { Name = "hello", Count = 5 };
        var destination = new NullSubstituteDestination();

        TestMappers.MapWithNullSubstitute(source, destination);

        Assert.Equal("hello", destination.Name);
        Assert.Equal(5, destination.Count);
    }
}
