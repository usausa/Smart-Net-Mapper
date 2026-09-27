namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Dotted paths into a member the automatic mapping leaves to them, required members that are internal, and
// members inherited from a base class or from the interface an interface extends
public class InheritedMemberAndPathMappingTests
{
    // The member the paths go into is created, not copied from the source and written into
    [Fact]
    public void DottedPathsLeaveSourceMemberUnchanged()
    {
        var source = new DottedPathSource { Child = new DottedPathChild { Value = 1, Other = 2 }, Number = 5 };

        var destination = TestMappers.MapDottedPaths(source);

        Assert.NotSame(source.Child, destination.Child);
        Assert.Equal(3, destination.Child!.Value);
        Assert.Equal(10, destination.Child.Other);
        Assert.Equal(5, destination.Number);
        Assert.Equal(1, source.Child.Value);
        Assert.Equal(2, source.Child.Other);
    }

    [Fact]
    public void DottedPathsWithNullSourceMemberCreateIt()
    {
        var destination = TestMappers.MapDottedPaths(new DottedPathSource { Number = 4 });

        Assert.Equal(3, destination.Child!.Value);
        Assert.Equal(8, destination.Child.Other);
    }

    [Fact]
    public void InternalRequiredMembersAreSet()
    {
        var destination = TestMappers.MapInternalRequired(new BasicSource { Id = 7, Name = "name" });

        Assert.Equal("#7", destination.Code);
        Assert.Equal(2, destination.Level);
        Assert.Equal(7, destination.Id);
    }

    // A void mapper fills an instance that exists, and leaves its required members as they are
    [Fact]
    public void VoidMapperKeepsInternalRequiredMembers()
    {
        var destination = new InternalRequiredDestination { Code = "keep", Level = 1 };

        TestMappers.MapInternalRequiredInto(new BasicSource { Id = 3 }, destination);

        Assert.Equal("keep", destination.Code);
        Assert.Equal(1, destination.Level);
        Assert.Equal(3, destination.Id);
    }

    [Fact]
    public void MapFromCallsBaseClassMethod()
    {
        var destination = TestMappers.MapInherited(new InheritedLookupSource { First = 2, Second = 3, Name = "name" });

        Assert.Equal(5, destination.Total);
        Assert.Equal(2, destination.First);
        Assert.Equal("name", destination.Name);
    }

    // The property and the method the source interface inherits
    [Fact]
    public void InterfaceSourceMapsInheritedMembers()
    {
        var destination = TestMappers.MapInherited(new LookupSource(4, "name"));

        Assert.Equal(4, destination.First);
        Assert.Equal("name", destination.Name);
        Assert.Equal(8, destination.Total);
    }

    private sealed class LookupSource : IInheritedLookupSource
    {
        public LookupSource(int first, string name)
        {
            First = first;
            Name = name;
        }

        public int First { get; }

        public string Name { get; }

        public int Twice() => First * 2;
    }
}
