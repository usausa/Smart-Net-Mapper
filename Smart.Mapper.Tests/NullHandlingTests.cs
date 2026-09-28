namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

public class NullHandlingTests
{
    [Fact]
    public void MapNestedSourceWithNullChildSkipsCopyForNullSource()
    {
        var source = new NullableNestedSource { Child = null, DirectValue = 100 };
        var destination = new NullableNestedFlatDestination { ChildId = 999, ChildName = "Original", DirectValue = 0 };

        TestMappers.Map(source, destination);

        Assert.Equal(100, destination.DirectValue);
        Assert.Equal(999, destination.ChildId);
        Assert.Equal("Original", destination.ChildName);
    }

    [Fact]
    public void MapNestedSourceWithNonNullChildCopiesNestedProperties()
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
    public void MapNullablePropertiesCopiesNullValues()
    {
        var source = new NullablePropertySource { NullableName = null, NullableInt = null, NonNullableName = "Test" };
        var destination = new NullablePropertyDestination { NullableName = "Original", NullableInt = 999 };

        TestMappers.Map(source, destination);

        Assert.Null(destination.NullableName);
        Assert.Null(destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void MapNullablePropertiesCopiesNonNullValues()
    {
        var source = new NullablePropertySource { NullableName = "NewName", NullableInt = 42, NonNullableName = "Test" };
        var destination = new NullablePropertyDestination();

        TestMappers.Map(source, destination);

        Assert.Equal("NewName", destination.NullableName);
        Assert.Equal(42, destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void MapNullableToNonNullableWithNullSourceSetsDefault()
    {
        var source = new NullableToNonNullableSource { Name = null };
        var destination = new NullableToNonNullableDestination { Name = "Original" };

        TestMappers.Map(source, destination);

        Assert.Null(destination.Name);
    }

    [Fact]
    public void MapNullableToNonNullableWithNonNullSourceCopiesValue()
    {
        var source = new NullableToNonNullableSource { Name = "NewValue" };
        var destination = new NullableToNonNullableDestination { Name = "Original" };

        TestMappers.Map(source, destination);

        Assert.Equal("NewValue", destination.Name);
    }

    [Fact]
    public void MapNullableIntToStringWithNullSourceSetsDefault()
    {
        var source = new NullableIntToStringSource { IntValue = null };
        var destination = new NullableIntToStringDestination { IntValue = "Original" };

        TestMappers.Map(source, destination);

        Assert.Null(destination.IntValue);
    }

    [Fact]
    public void MapNullableIntToStringWithNonNullSourceConvertsValue()
    {
        var source = new NullableIntToStringSource { IntValue = 42 };
        var destination = new NullableIntToStringDestination { IntValue = "Original" };

        TestMappers.Map(source, destination);

        Assert.Equal("42", destination.IntValue);
    }

    [Fact]
    public void MapWithNullValueWhenSourceIsNullUsesFallbackValues()
    {
        var source = new NullValueSource { Name = null, Count = null };
        var destination = new NullValueDestination();

        TestMappers.MapWithNullValue(source, destination);

        Assert.Equal("(none)", destination.Name);
        Assert.Equal(-1, destination.Count);
    }

    [Fact]
    public void MapWithNullValueWhenSourceHasValuesUsesSourceValues()
    {
        var source = new NullValueSource { Name = "hello", Count = 5 };
        var destination = new NullValueDestination();

        TestMappers.MapWithNullValue(source, destination);

        Assert.Equal("hello", destination.Name);
        Assert.Equal(5, destination.Count);
    }

    // Regression G: NullBehavior.Skip on a nullable value type -> non-nullable target with no conversion.
    // Previously generated `dst.Value = src.Value;` (int? -> int) which did not compile and ignored Skip.
    [Fact]
    public void MapSkipNoConversionCopiesValueButSkipsNull()
    {
        var whenValue = new SkipNoConvDestination { Value = 99 };
        TestMappers.MapSkipNoConv(new SkipNoConvSource { Value = 5 }, whenValue);
        Assert.Equal(5, whenValue.Value);

        var whenNull = new SkipNoConvDestination { Value = 99 };
        TestMappers.MapSkipNoConv(new SkipNoConvSource { Value = null }, whenNull);
        Assert.Equal(99, whenNull.Value); // skipped: destination preserved
    }

    // NullBehavior.Skip with a converter: the converter, which takes null as well, is called only for a value
    [Fact]
    public void MapSkipConverterCallsConverterForValueOnly()
    {
        var whenValue = new SkipConverterDestination();
        TestMappers.MapSkipConverter(new SkipConverterSource { Value = 5 }, whenValue);
        Assert.Equal("value 5", whenValue.Value);

        var whenNull = new SkipConverterDestination();
        TestMappers.MapSkipConverter(new SkipConverterSource { Value = null }, whenNull);
        Assert.Equal("kept", whenNull.Value);
    }

    // NullValue with a converter: a null source takes NullValue, and the converter is called for a value only
    [Fact]
    public void MapNullValueConverterTakesNullValueForNull()
    {
        Assert.Equal("none", TestMappers.MapNullValueConverter(new NullValueConverterSource()).Value);
        Assert.Equal("value 5", TestMappers.MapNullValueConverter(new NullValueConverterSource { Value = 5 }).Value);
    }

    // A null intermediate member gives NullValue to the target that has one, and leaves the other as it is
    [Fact]
    public void MapIntermediateTakesNullValueForNullIntermediate()
    {
        var whenNull = new IntermediateDestination();
        TestMappers.MapIntermediate(new IntermediateSource(), whenNull);
        Assert.Equal("none", whenNull.Name);
        Assert.Equal(7, whenNull.Code);

        var whenValue = new IntermediateDestination();
        TestMappers.MapIntermediate(new IntermediateSource { Leaf = new IntermediateLeaf { Name = "name", Code = 3 } }, whenValue);
        Assert.Equal("name", whenValue.Name);
        Assert.Equal(3, whenValue.Code);
    }

    // A reference declared with nullable annotations disabled takes NullValue and NullBehavior.Skip as well
    [Fact]
    public void MapObliviousAppliesNullHandling()
    {
        var whenNull = new ObliviousDestination { Name = "old", Note = "kept" };
        TestMappers.MapOblivious(new ObliviousSource(), whenNull);
        Assert.Equal("Unknown", whenNull.Name);
        Assert.Equal("kept", whenNull.Note);

        var whenValue = new ObliviousDestination { Name = "old", Note = "old" };
        TestMappers.MapOblivious(new ObliviousSource { Name = "name", Note = "note" }, whenValue);
        Assert.Equal("name", whenValue.Name);
        Assert.Equal("note", whenValue.Note);
    }

    // A null reference declared with nullable annotations disabled does not get to a converter, a mapper of [MapNested]
    // or an element mapper not taking null, nor is it read through: the converter leaves the target as it is, the
    // nested target and a null element get null, a null collection gives null, and the path is read under its check
    [Fact]
    public void MapObliviousGraphChecksNull()
    {
        var whenNull = new ObliviousGraphDestination();
        TestMappers.MapObliviousGraph(new ObliviousGraphSource { Items = [new ObliviousItem { Value = 1 }, null!] }, whenNull);
        Assert.Equal("init", whenNull.Name);
        Assert.Null(whenNull.Child);
        int?[] expectedItems = [1, null];
        Assert.Equal(expectedItems, whenNull.Items!.Select(static x => x?.Value));
        Assert.Equal("init", whenNull.ParentName);

        var whenNullItems = new ObliviousGraphDestination { Items = [] };
        TestMappers.MapObliviousGraph(new ObliviousGraphSource(), whenNullItems);
        Assert.Null(whenNullItems.Items);

        var whenValue = new ObliviousGraphDestination();
        TestMappers.MapObliviousGraph(
            new ObliviousGraphSource { Name = "name", Child = new ObliviousItem { Value = 2 }, Items = [], Parent = new ObliviousGraphSource { Name = "parent" } },
            whenValue);
        Assert.Equal("NAME", whenValue.Name);
        Assert.Equal(2, whenValue.Child!.Value);
        Assert.Empty(whenValue.Items!);
        Assert.Equal("parent", whenValue.ParentName);
    }

    // A null source parameter declared with nullable annotations disabled maps nothing and gives null
    [Fact]
    public void MapObliviousOrNullReturnsNullForNull()
    {
        Assert.Null(TestMappers.MapObliviousOrNull(null));
        Assert.Equal("name", TestMappers.MapObliviousOrNull(new ObliviousSource { Name = "name" }).Name);
    }

    // A null source of [MapNested] goes to a mapper taking null, which decides what the target gets
    [Fact]
    public void MapNestedNullCallsMapperTakingNull()
    {
        var whenNull = TestMappers.MapNestedNull(new NestedNullSource());
        Assert.Equal(-1, whenNull.Child.Value);
        Assert.Equal(-1, whenNull.Other.Value);

        var whenValue = TestMappers.MapNestedNull(new NestedNullSource { Child = new NestedNullChildSource { Value = 3 }, Other = new NestedNullChildSource { Value = 4 } });
        Assert.Equal(3, whenValue.Child.Value);
        Assert.Equal(4, whenValue.Other.Value);
    }
}
