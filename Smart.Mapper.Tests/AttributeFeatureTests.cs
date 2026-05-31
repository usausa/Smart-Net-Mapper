namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

public class ConstantMappingTests
{
    [Fact]
    public void Map_ConstantValues_SetsConstantsCorrectly()
    {
        var source = new ConstantSource { Id = 1, Name = "Test" };
        var destination = new ConstantDestination();
        var beforeMap = DateTime.Now;

        TestMappers.Map(source, destination);

        Assert.Equal(1, destination.Id);
        Assert.Equal("Test", destination.Name);
        Assert.Equal("Active", destination.Status);
        Assert.Equal(1, destination.Version);
        Assert.True(destination.CreatedAt >= beforeMap);
        Assert.True(destination.CreatedAt <= DateTime.Now);
    }
}

public class BeforeAfterMapTests
{
    [Fact]
    public void Map_BeforeAfterMap_CallsBothMethods()
    {
        var source = new BeforeAfterSource { Value = 42, Text = "Hello" };
        var destination = new BeforeAfterDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(42, destination.Value);
        Assert.Equal("Hello", destination.Text);
        Assert.True(destination.BeforeMapCalled);
        Assert.True(destination.AfterMapCalled);
    }
}

public class CustomParameterTests
{
    [Fact]
    public void MapWithContext_CallsBeforeMapAndAfterMapWithContext()
    {
        var source = new BasicSource { Id = 1, Name = "Test", Description = "Desc" };
        var destination = new BasicDestination();
        var context = new CustomMappingContext { ContextValue = "TestContext" };

        TestMappers.MapWithContext(source, destination, context);

        Assert.True(context.BeforeMapCalled);
        Assert.True(context.AfterMapCalled);
        Assert.Equal(1, destination.Id);
        Assert.Equal("Test", destination.Name);
    }

    [Fact]
    public void MapWithContextMixed_CallsBeforeMapWithoutContextAndAfterMapWithContext()
    {
        var source = new BasicSource { Id = 2, Name = "Mixed", Description = "Desc" };
        var destination = new BasicDestination();
        var context = new CustomMappingContext();

        TestMappers.MapWithContextMixed(source, destination, context);

        Assert.False(context.BeforeMapCalled);
        Assert.True(context.AfterMapCalled);
        Assert.Equal(2, destination.Id);
    }

    [Fact]
    public void MapToNewWithContext_ReturnsNewObjectAndCallsAfterMapWithContext()
    {
        var source = new BasicSource { Id = 3, Name = "Return", Description = "Original" };
        var context = new CustomMappingContext { ContextValue = "ReturnContext" };

        var destination = TestMappers.MapToNewWithContext(source, context);

        Assert.True(context.AfterMapCalled);
        Assert.Equal(3, destination.Id);
        Assert.Equal("Return", destination.Name);
        Assert.Equal("Modified by AfterMap: ReturnContext", destination.Description);
    }
}

public class ConverterTests
{
    [Fact]
    public void MapWithConverter_UsesCustomConverter()
    {
        var source = new ConverterSource { Value = 42, Text = "Hello" };
        var destination = new ConverterDestination();

        TestMappers.MapWithConverter(source, destination);

        Assert.Equal("Value: 42", destination.ConvertedValue);
    }

    [Fact]
    public void MapWithConverterAndContext_UsesCustomConverterWithContext()
    {
        var source = new ConverterSource { Value = 100, Text = "Hello" };
        var destination = new ConverterDestination();
        var context = new CustomMappingContext { ContextValue = "TestContext" };

        TestMappers.MapWithConverterAndContext(source, destination, context);

        Assert.Equal("Value: 100, Context: TestContext", destination.ConvertedValue);
        Assert.Equal("Hello (formatted with TestContext)", destination.FormattedText);
    }
}

public class ConditionTests
{
    [Fact]
    public void MapWithPropertyCondition_WhenNameNotNull_MapsName()
    {
        var source = new ConditionSource { Value = 42, Name = "Test", IsActive = true };
        var destination = new ConditionDestination();

        TestMappers.MapWithPropertyCondition(source, destination);

        Assert.Equal(42, destination.Value);
        Assert.Equal("Test", destination.Name);
    }

    [Fact]
    public void MapWithPropertyCondition_WhenNameNull_DoesNotMapName()
    {
        var source = new ConditionSource { Value = 42, Name = null, IsActive = true };
        var destination = new ConditionDestination { Name = "Original" };

        TestMappers.MapWithPropertyCondition(source, destination);

        Assert.Equal(42, destination.Value);
        Assert.Equal("Original", destination.Name);
    }

    [Fact]
    public void MapWithGenericConstant_SetsGenericConstantValues()
    {
        var source = new ConstantSource { Name = "Test" };
        var destination = new ConstantDestination();

        TestMappers.MapWithGenericConstant(source, destination);

        Assert.Equal("Test", destination.Name);
        Assert.Equal(2, destination.Version);
        Assert.Equal("Pending", destination.Status);
    }
}
