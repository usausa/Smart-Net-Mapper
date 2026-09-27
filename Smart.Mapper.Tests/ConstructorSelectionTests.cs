namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// A member whose setter the mapper cannot call is set through the constructor taking it, a destination no
// constructor gets every argument for is created without arguments, and an optional parameter without a value takes
// its default
public class ConstructorSelectionTests
{
    [Fact]
    public void PrivateSetterIsSetThroughConstructor()
    {
        var destination = TestMappers.MapConstructorSetter(new ConstructorChoiceSource { Code = 7, Name = "a" });

        Assert.Equal(7, destination.Code);
        Assert.Equal("a", destination.Name);
    }

    [Fact]
    public void DestinationIsCreatedWithoutArguments()
    {
        var destination = TestMappers.MapConstructorId(new ConstructorChoiceSource { Code = 7, Name = "a" });

        Assert.Equal(-1, destination.Id);
        Assert.Equal("a", destination.Name);
    }

    // The level and the extra values are left out, and the name after them is passed by name
    [Fact]
    public void OptionalParameterTakesDefault()
    {
        var destination = TestMappers.MapOptionalParameter(new ConstructorChoiceSource { Code = 7, Name = "a" });

        Assert.Equal(7, destination.Code);
        Assert.Equal(5, destination.Level);
        Assert.Equal("a", destination.Name);
        Assert.Equal(0, destination.ExtraCount);
    }
}
