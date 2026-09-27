namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Keywords used as names, integer constants that keep their type, required fields, and dotted target paths
// through a struct property, a type the mapper cannot create, and init-only members an object initializer sets
public class IdentifierAndTargetPathMappingTests
{
    [Fact]
    public void MapKeywordsPassesKeywordParameters()
    {
        var destination = TestMappers.MapKeywords(new KeywordSource { Id = 1, Name = "name" }, 10);

        Assert.Equal(11, destination.Id);
        Assert.Equal("name", destination.Name);
    }

    [Fact]
    public void MapSmallIntegersBoxesConstantsWithTheirType()
    {
        var destination = TestMappers.MapSmallIntegers(new TargetPathSource());

        Assert.Equal((byte)1, Assert.IsType<byte>(destination.Value));
        Assert.Collection(
            destination.Values,
            static x => Assert.Equal((short)-2, Assert.IsType<short>(x)),
            static x => Assert.Equal((sbyte)-3, Assert.IsType<sbyte>(x)),
            static x => Assert.Equal((ushort)4, Assert.IsType<ushort>(x)));
        Assert.Equal<byte>([5, 6], destination.Bytes);
    }

    [Fact]
    public void MapRequiredFieldsSetsThemInInitializer()
    {
        var destination = TestMappers.MapRequiredFields(new TargetPathSource { X = 3, Y = 4 });

        Assert.Equal(7, destination.Key);
        Assert.Equal("#4", destination.Label);
        Assert.Equal(3, destination.Id);
    }

    // A void mapper fills the instance it is given, whose required fields are set already
    [Fact]
    public void MapRequiredFieldsIntoKeepsThem()
    {
        var destination = new RequiredFieldDestination { Key = 1, Label = "keep" };

        TestMappers.MapRequiredFieldsInto(new TargetPathSource { X = 5 }, ref destination);

        Assert.Equal(1, destination.Key);
        Assert.Equal("keep", destination.Label);
        Assert.Equal(5, destination.Id);
    }

    // The struct property is written back, a member the mapper cannot create is written into when it holds an
    // instance, and one it can create is created
    [Fact]
    public void MapPathsWritesThroughIntermediateMembers()
    {
        var abstractChild = new PathConcreteChild();
        var destination = new TargetPathDestination { Abstract = abstractChild };

        TestMappers.MapPaths(new TargetPathSource { X = 2, Y = 3 }, destination);

        Assert.Equal(new PathPoint { X = 2, Y = 5 }, destination.Point);
        Assert.Equal(2, abstractChild.Value);
        Assert.Equal(5, destination.Created?.Plain);
        Assert.Equal(30, destination.Held.Plain);
    }

    [Fact]
    public void MapPathsLeavesNullMemberItCannotCreate()
    {
        var destination = new TargetPathDestination();

        TestMappers.MapPaths(new TargetPathSource { X = 2, Y = 3 }, destination);

        Assert.Null(destination.Abstract);
        Assert.Equal(new PathPoint { X = 2, Y = 5 }, destination.Point);
    }

    // The init-only members are set in the object initializer, which creates the member they go through
    [Fact]
    public void MapInitPathsSetsInitOnlyMembers()
    {
        var destination = TestMappers.MapInitPaths(new TargetPathSource { X = 2, Y = 3 });

        Assert.Equal(2, destination.Child.Value);
        Assert.Equal(9, destination.Child.Other);
        Assert.Equal(3, destination.Child.Plain);
        Assert.Equal(3, destination.Top);
    }
}
