namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// The constructor receiving the target of the attribute is called over the first declared of its length, and a
// parameterless constructor obsolete as a warning is left for when nothing else constructs
public class ConstructorPreferenceMappingTests
{
    [Fact]
    public void ConstructorReceivingTargetIsCalled()
    {
        var destination = TestMappers.MapPreferredConstructor(new PreferredConstructorSource { Code = 3, Level = 9, Name = "name" });

        Assert.Equal(3, destination.Code);
        Assert.Equal("name", destination.Label);
    }

    [Fact]
    public void ObsoleteParameterlessConstructorIsAvoided()
    {
        var destination = TestMappers.MapObsoleteParameterless(new PreferredConstructorSource { Code = 3 });

        Assert.Equal(3, destination.Code);
        Assert.True(destination.Constructed);
    }
}
