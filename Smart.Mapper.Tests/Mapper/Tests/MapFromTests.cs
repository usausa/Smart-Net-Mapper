namespace Smart.Mapper;

public class MapFromTests
{
    [Fact]
    public void MapFrom_ComputesValueFromMethod()
    {
        var source = new MapFromSource { FirstName = "John", LastName = "Doe" };
        var destination = new MapFromDestination();

        TestMappers.Map(source, destination);

        Assert.Equal("John Doe", destination.FullName);
        Assert.Equal("JOHN DOE", destination.UpperCaseName);
    }

    [Fact]
    public void MapFrom_WithCustomParameters_UsesContext()
    {
        var source = new MapFromSource { FirstName = "Jane", LastName = "Smith" };
        var context = new MapFromContext { Separator = " - " };

        var destination = TestMappers.MapWithContext(source, context);

        Assert.Equal("Jane - Smith", destination.FullName);
    }
}

public class MapFromMethodTests
{
    [Fact]
    public void MapFromMethod_CallsSourceMethod()
    {
        var source = new MapFromMethodSource { Items = [1, 2, 3, 4, 5] };
        var destination = new MapFromMethodDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(5, destination.ItemCount);
        Assert.Equal(15, destination.ItemSum);
    }
}

public class MapFromPropertyPathTests
{
    [Fact]
    public void MapWithPropertyPath_MapsMethodCallAndPropertyPath()
    {
        var source = new MapFromPathSource { Nested = new MapFromPathNested { Value = 123 } };
        var destination = new MapFromPathDestination();

        TestMappers.MapWithPropertyPath(source, destination);

        Assert.Equal(42, destination.ItemCount);
        Assert.Equal(123, destination.NestedValue);
    }
}

public class MapUsingContextTests
{
    [Fact]
    public void MapWithUsingContext_PassesCustomParametersToMethod()
    {
        var source = new MapUsingContextSource { BaseValue = "Hello" };
        var destination = new MapUsingContextDestination();
        var context = new MapUsingContext { Suffix = "World" };

        TestMappers.MapWithUsingContext(source, destination, context);

        Assert.Equal("Hello_World", destination.ComputedValue);
    }
}
