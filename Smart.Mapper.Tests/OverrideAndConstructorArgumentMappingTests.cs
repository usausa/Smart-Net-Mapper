namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Properties hiding or overriding one of a base type, a required member the dotted paths write into, and a
// constructor taking the values of the attributes
public class OverrideAndConstructorArgumentMappingTests
{
    // The most derived property of the name is assigned, of the type it declares
    [Fact]
    public void HidingPropertyIsAssigned()
    {
        var destination = TestMappers.MapHiding(new BasicSource { Id = 1, Name = "name" });

        Assert.Equal("name", destination.Name);
        Assert.Equal(0, ((HidingBaseDestination)destination).Name);
        Assert.Equal(1, destination.Id);
    }

    [Fact]
    public void OverridingRequiredPropertyIsSet()
    {
        var destination = TestMappers.MapRequiredOverride(new BasicSource { Id = 2, Name = "name" });

        Assert.Equal(2, destination.Id);
        Assert.Equal("name", destination.Name);
    }

    // A property overriding the getter only is assigned through the setter it inherits
    [Fact]
    public void GetterOverrideIsAssignedThroughInheritedSetter()
    {
        var destination = TestMappers.MapGetterOverride(new BasicSource { Id = 3 });

        Assert.Equal(3, destination.Id);
        Assert.Equal("label", destination.Label);
    }

    // The object initializer creates the required member, which the path then writes into
    [Fact]
    public void RequiredMemberIsCreatedForPath()
    {
        var destination = TestMappers.MapRequiredPath(new BasicSource { Id = 4 });

        Assert.Equal(4, destination.Child.Value);
        Assert.Equal(0, destination.Child.Other);
        Assert.Equal(4, destination.Id);
    }

    [Fact]
    public void ConstructorTakesAttributeValues()
    {
        var source = new ConstructorArgumentSource
        {
            Child = new DottedPathChild { Value = 1, Other = 2 },
            Children = [new DottedPathChild { Value = 3 }, new DottedPathChild { Value = 4 }],
            Number = 5
        };

        var destination = TestMappers.MapConstructorArguments(source);

        Assert.NotSame(source.Child, destination.Child);
        Assert.Equal(1, destination.Child.Value);
        Assert.Equal(2, destination.Child.Other);
        Assert.Equal(7, destination.Code);
        Assert.Equal("#5", destination.Label);
        Assert.Collection(
            destination.Children,
            static x => Assert.Equal(3, x.Value),
            static x => Assert.Equal(4, x.Value));
        Assert.NotSame(source.Children[0], destination.Children[0]);
    }
}
