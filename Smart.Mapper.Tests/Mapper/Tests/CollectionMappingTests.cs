namespace Smart.Mapper;

public class MapCollectionTests
{
    [Fact]
    public void MapCollection_ArrayToList_MapsElements()
    {
        var source = new CollectionSource
        {
            Children =
            [
                new CollectionSourceChild { Id = 1, Name = "Child1" },
                new CollectionSourceChild { Id = 2, Name = "Child2" }
            ],
            DirectValue = 100
        };
        var destination = new CollectionDestination();

        TestMappers.Map(source, destination);

        Assert.NotNull(destination.Children);
        Assert.Equal(2, destination.Children.Count);
        Assert.Equal(1, destination.Children[0].Id);
        Assert.Equal("Child1", destination.Children[0].Name);
        Assert.Equal(2, destination.Children[1].Id);
        Assert.Equal("Child2", destination.Children[1].Name);
        Assert.Equal(100, destination.DirectValue);
    }

    [Fact]
    public void MapCollection_ListToArray_MapsElements()
    {
        var source = new CollectionSource
        {
            Items =
            [
                new CollectionSourceChild { Id = 10, Name = "Item1" },
                new CollectionSourceChild { Id = 20, Name = "Item2" },
                new CollectionSourceChild { Id = 30, Name = "Item3" }
            ]
        };
        var destination = new CollectionDestination();

        TestMappers.Map(source, destination);

        Assert.NotNull(destination.Items);
        Assert.Equal(3, destination.Items.Length);
        Assert.Equal(10, destination.Items[0].Id);
        Assert.Equal(20, destination.Items[1].Id);
        Assert.Equal(30, destination.Items[2].Id);
    }

    [Fact]
    public void MapCollection_NullSource_SetsDefault()
    {
        var source = new CollectionSource { Children = null, DirectValue = 50 };
        var destination = new CollectionDestination
        {
            Children = [new CollectionDestinationChild { Id = 999 }]
        };

        TestMappers.Map(source, destination);

        Assert.Null(destination.Children);
        Assert.Equal(50, destination.DirectValue);
    }

    [Fact]
    public void MapCollection_WithReturnType_ReturnsNewObject()
    {
        var source = new CollectionSource
        {
            Children = [new CollectionSourceChild { Id = 5, Name = "Test" }]
        };

        var destination = TestMappers.MapToNew(source);

        Assert.NotNull(destination);
        Assert.NotNull(destination.Children);
        Assert.Single(destination.Children);
        Assert.Equal(5, destination.Children[0].Id);
    }

    [Fact]
    public void MapCollection_WithVoidMapper_MapsElements()
    {
        var source = new VoidMapperSource
        {
            Children =
            [
                new VoidMapperSourceChild { Id = 100 },
                new VoidMapperSourceChild { Id = 200 }
            ]
        };
        var destination = new VoidMapperDestination();

        TestMappers.Map(source, destination);

        Assert.NotNull(destination.Children);
        Assert.Equal(2, destination.Children.Count);
        Assert.Equal(100, destination.Children[0].Id);
        Assert.Equal(200, destination.Children[1].Id);
    }

    [Fact]
    public void MapCollection_WithCustomConverter_UsesSpecifiedMethod()
    {
        var source = new CustomCollectionConverterSource
        {
            Children =
            [
                new CustomCollectionConverterSourceChild { Id = 1, Name = "Item1" },
                new CustomCollectionConverterSourceChild { Id = 2, Name = "Item2" },
                new CustomCollectionConverterSourceChild { Id = 3, Name = "Item3" }
            ]
        };
        var destination = new CustomCollectionConverterDestination();

        TestMappers.MapWithCustomCollectionConverter2(source, destination);

        Assert.NotNull(destination.Children);
        Assert.IsAssignableFrom<IReadOnlyList<CustomCollectionConverterDestChild>>(destination.Children);
        Assert.Equal(3, destination.Children.Count);
        Assert.Equal(1, destination.Children[0].Id);
        Assert.Equal("Item1", destination.Children[0].Name);
        Assert.Equal(2, destination.Children[1].Id);
        Assert.Equal(3, destination.Children[2].Id);
    }

    [Fact]
    public void MapCollection_WithCustomConverter_NullSource_ReturnsNull()
    {
        var source = new CustomCollectionConverterSource { Children = null };
        var destination = new CustomCollectionConverterDestination();

        TestMappers.MapWithCustomCollectionConverter2(source, destination);

        Assert.NotNull(destination.Children);
        Assert.Empty(destination.Children);
    }
}

public class CustomConverterTests
{
    [Fact]
    public void MapWithCustomConverter_UsesCustomConversion()
    {
        var source = new CustomConverterSource { IntValue = 42, StringValue = "NUM_100" };
        var destination = new CustomConverterDestination();

        TestMappers.MapWithCustomConverter(source, destination);

        Assert.Equal("PREFIX_42", destination.IntValue);
        Assert.Equal(100, destination.StringValue);
    }

    [Fact]
    public void MapWithCustomCollectionConverter_UsesCustomCollectionConversion()
    {
        var source = new CustomCollectionSource
        {
            Numbers =
            [
                new CollectionSourceChild { Id = 1, Name = "One" },
                new CollectionSourceChild { Id = 2, Name = "Two" },
                new CollectionSourceChild { Id = 3, Name = "Three" }
            ]
        };
        var destination = new CustomCollectionDestination();

        TestMappers.MapWithCustomCollectionConverter(source, destination);

        Assert.NotNull(destination.Numbers);
        Assert.Equal(3, destination.Numbers.Count);
        Assert.Equal(2, destination.Numbers[0].Id);
        Assert.Equal(4, destination.Numbers[1].Id);
        Assert.Equal(6, destination.Numbers[2].Id);
    }
}

public class ImmutableCollectionMappingTests
{
    [Fact]
    public void MapImmutableCollections_ToImmutableArray_MapsElements()
    {
        var source = new ImmutableCollectionSource
        {
            Items = [new ImmutableCollectionSourceChild { Id = 1, Name = "A" }, new ImmutableCollectionSourceChild { Id = 2, Name = "B" }],
            ListItems = [new ImmutableCollectionSourceChild { Id = 3, Name = "C" }],
            SetItems = [new ImmutableCollectionSourceChild { Id = 4, Name = "D" }]
        };
        var destination = new ImmutableCollectionDestination();

        TestMappers.MapImmutableCollections(source, destination);

        Assert.Equal(2, destination.Items.Length);
        Assert.Equal(1, destination.Items[0].Id);
        Assert.Equal("A", destination.Items[0].Name);
        Assert.NotNull(destination.ListItems);
        Assert.Single(destination.ListItems!);
        Assert.Equal(3, destination.ListItems![0].Id);
        Assert.NotNull(destination.SetItems);
        Assert.Single(destination.SetItems!);
    }
}

public class InPlaceCollectionMappingTests
{
    [Fact]
    public void MapInPlace_NullDestination_CreatesNewList()
    {
        var source = new InPlaceSource
        {
            Items = [new InPlaceSourceChild { Id = 1, Name = "A" }, new InPlaceSourceChild { Id = 2, Name = "B" }]
        };
        var destination = new InPlaceDestination();

        TestMappers.MapInPlace(source, destination);

        Assert.NotNull(destination.Items);
        Assert.Equal(2, destination.Items!.Count);
        Assert.Equal(1, destination.Items[0].Id);
        Assert.Equal("A", destination.Items[0].Name);
        Assert.Equal(2, destination.Items[1].Id);
    }

    [Fact]
    public void MapInPlace_ExistingList_ClearsAndRefills()
    {
        var source = new InPlaceSource
        {
            Items = [new InPlaceSourceChild { Id = 10, Name = "New" }]
        };
        var existingList = new List<InPlaceDestinationChild>
        {
            new() { Id = 99, Name = "Old1" },
            new() { Id = 98, Name = "Old2" }
        };
        var destination = new InPlaceDestination { Items = existingList };
        var originalReference = destination.Items;

        TestMappers.MapInPlace(source, destination);

        // Reference must be preserved
        Assert.Same(originalReference, destination.Items);
        Assert.Single(destination.Items!);
        Assert.Equal(10, destination.Items![0].Id);
        Assert.Equal("New", destination.Items[0].Name);
    }

    [Fact]
    public void MapInPlace_NullSource_LeavesDestinationUnchanged()
    {
        var source = new InPlaceSource { Items = null };
        var existingList = new List<InPlaceDestinationChild>
        {
            new() { Id = 1, Name = "Existing" }
        };
        var destination = new InPlaceDestination { Items = existingList };

        TestMappers.MapInPlace(source, destination);

        Assert.Same(existingList, destination.Items);
        Assert.Single(destination.Items!);
    }
}

public class ReadOnlyStructMappingTests
{
    [Fact]
    public void MapReadOnlyStruct_MapsAllProperties()
    {
        var source = new ReadOnlyStructSource { Id = 42, Name = "Test" };

        var destination = TestMappers.MapReadOnlyStruct(source);

        Assert.Equal(42, destination.Id);
        Assert.Equal("Test", destination.Name);
    }
}
