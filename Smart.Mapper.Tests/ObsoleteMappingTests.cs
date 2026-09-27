namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Of the constructors receiving the target of the attribute alike, the one not obsolete is called, the automatic
// mapping leaves obsolete properties out, as a source and as a destination, a conversion method obsolete as an error
// gives way to another conversion, and an obsolete enum member is written as a cast of its number
public class ObsoleteMappingTests
{
    [Fact]
    public void ConstructorNotObsoleteIsCalled()
    {
        var destination = TestMappers.MapObsoleteTie(new ObsoleteMemberSource { Other = 3, Level = 9, Name = "name" });

        Assert.Equal(3, destination.Code);
        Assert.Equal("name", destination.Via);
    }

    [Fact]
    public void ObsoletePropertiesAreLeftOut()
    {
        var destination = TestMappers.MapObsoleteMember(new ObsoleteMemberSource { Code = 1, Current = 5, Legacy = 7 });

        Assert.Equal(1, destination.Code);
        Assert.Equal(0, destination.Previous);
        Assert.False(destination.LegacyAssigned);
    }

    // The specialized method of the converter class is obsolete as an error, so its generic method converts
    [Fact]
    public void ConverterMethodObsoleteAsErrorGivesWayToGenericMethod()
    {
        var destination = TestMappers.MapObsoleteConverter(new ObsoleteConverterSource { Amount = 5 });

        Assert.Equal("generic 5", destination.Amount);
    }

    // Green is obsolete as an error on the source and as a warning on the destination, and still maps by name
    [Fact]
    public void ObsoleteEnumMembersMapByName()
    {
        var destination = TestMappers.MapObsoleteEnum(new ObsoleteEnumSource { Color = (ObsoleteSourceColor)2, Name = (ObsoleteSourceColor)2, Text = "Green" });

        Assert.Equal((ObsoleteDestinationColor)20, destination.Color);
        Assert.Equal("Green", destination.Name);
        Assert.Equal((ObsoleteDestinationColor)20, destination.Text);
        Assert.Equal((ObsoleteDestinationColor)20, destination.Fixed);
    }
}
