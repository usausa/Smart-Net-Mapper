namespace Smart.Mapper;

public class BasicMappingTests
{
    [Fact]
    public void Map_BasicProperties_CopiesAllProperties()
    {
        var source = new BasicSource { Id = 42, Name = "Test Name", Description = "Test Description" };
        var destination = new BasicDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(42, destination.Id);
        Assert.Equal("Test Name", destination.Name);
        Assert.Equal("Test Description", destination.Description);
    }

    [Fact]
    public void MapToNew_BasicProperties_ReturnsNewObjectWithCopiedProperties()
    {
        var source = new BasicSource { Id = 100, Name = "New Object", Description = "Created via MapToNew" };

        var destination = TestMappers.MapToNew(source);

        Assert.NotNull(destination);
        Assert.Equal(100, destination.Id);
        Assert.Equal("New Object", destination.Name);
        Assert.Equal("Created via MapToNew", destination.Description);
    }
}

public class DifferentPropertyMappingTests
{
    [Fact]
    public void Map_DifferentPropertyNames_MapsCorrectly()
    {
        var source = new DifferentPropertySource { SourceId = 123, SourceName = "Different Name" };
        var destination = new DifferentPropertyDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(123, destination.DestId);
        Assert.Equal("Different Name", destination.DestName);
    }

    [Fact]
    public void MapToNew_DifferentPropertyNames_ReturnsCorrectlyMappedObject()
    {
        var source = new DifferentPropertySource { SourceId = 456, SourceName = "Another Name" };

        var destination = TestMappers.MapToNew(source);

        Assert.NotNull(destination);
        Assert.Equal(456, destination.DestId);
        Assert.Equal("Another Name", destination.DestName);
    }
}

public class IgnorePropertyMappingTests
{
    [Fact]
    public void Map_IgnoredProperty_DoesNotCopyIgnoredProperty()
    {
        var source = new IgnoreSource { Id = 1, Name = "Public", Secret = "TopSecret" };
        var destination = new IgnoreDestination { Secret = "Original" };

        TestMappers.Map(source, destination);

        Assert.Equal(1, destination.Id);
        Assert.Equal("Public", destination.Name);
        Assert.Equal("Original", destination.Secret);
    }
}

public class AutoMapFalseTests
{
    [Fact]
    public void AutoMapFalse_OnlyMapsExplicitProperties()
    {
        var source = new AutoMapSource { Id = 42, Name = "Test", Value = 100 };
        var destination = new AutoMapDestination { Id = 0, Name = "Original", Value = 0 };

        TestMappers.MapExplicit(source, destination);

        Assert.Equal(42, destination.Id);
        Assert.Equal("Original", destination.Name);
        Assert.Equal(0, destination.Value);
    }

    [Fact]
    public void AutoMapFalse_WithMapProperty_OnlyMapsSpecified()
    {
        var source = new AutoMapSource { Id = 10, Name = "Explicit", Value = 200 };

        var destination = TestMappers.MapExplicitToNew(source);

        Assert.Equal(10, destination.Id);
        Assert.Equal("Explicit", destination.Name);
        Assert.Equal(0, destination.Value);
    }
}

public class AutoMapTrueTests
{
    [Fact]
    public void AutoMapTrue_MapsAllSameNameProperties()
    {
        var source = new BasicSource { Id = 42, Name = "Test" };
        var destination = new BasicDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(42, destination.Id);
        Assert.Equal("Test", destination.Name);
    }

    [Fact]
    public void AutoMapTrue_WithMapProperty_MapsExplicitAndAutoProperties()
    {
        var source = new MultiPropertySource { Id = 100, Name = "Multi", Value = 50, Description = "Test Description" };
        var destination = new MultiPropertyDestination();

        TestMappers.Map(source, destination);

        Assert.Equal(100, destination.Id);
        Assert.Equal("Multi", destination.Name);
        Assert.Equal(50, destination.Value);
        Assert.Equal("Test Description", destination.Description);
    }
}
