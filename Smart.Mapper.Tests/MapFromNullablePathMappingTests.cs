namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// [MapFrom] reads a property path through members that may be null under their null check: an assignment leaves the
// target as it is when one is null, and a constructor argument takes null or default. The values of [MapUsing] and
// [MapFrom] go to a target of a type they convert to implicitly (int to long, decimal to decimal?)
public class MapFromNullablePathMappingTests
{
    [Fact]
    public void NullMembersLeaveTargets()
    {
        var destination = new NullablePathDestination();

        TestMappers.MapNullablePath(new NullablePathSource { Nick = "nick" }, destination);

        Assert.Equal("keep", destination.Zip);
        Assert.Equal(-1, destination.Code);
        Assert.Equal(-1, destination.Count);
        Assert.Equal("nick", destination.Nick);
        Assert.Equal(0m, destination.Total);
    }

    [Fact]
    public void ValuesAreReadThroughMembers()
    {
        var destination = new NullablePathDestination();

        TestMappers.MapNullablePath(
            new NullablePathSource { Mid = new NullablePathMid { Leaf = new NullablePathLeaf { Zip = "100", Code = 7 } }, Items = [1, 2, 3] },
            destination);

        Assert.Equal("100", destination.Zip);
        Assert.Equal(7, destination.Code);
        Assert.Equal(3, destination.Count);
        Assert.Equal(6m, destination.Total);
    }

    [Fact]
    public void ConstructorArgumentsTakeDefaultForNullMembers()
    {
        var empty = TestMappers.MapNullablePathRecord(new NullablePathSource { Mid = new NullablePathMid() });
        var filled = TestMappers.MapNullablePathRecord(new NullablePathSource { Mid = new NullablePathMid { Leaf = new NullablePathLeaf { Zip = "200", Code = 8 } } });

        Assert.Null(empty.Code);
        Assert.Equal("200", filled.Zip);
        Assert.Equal(8, filled.Code);
    }
}
