namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Mappers in nested types, a constructor with [SetsRequiredMembers], and the profile's name comparison for the
// names written in the attributes, fields as well
public class ContainingTypeAndNameMappingTests
{
    [Fact]
    public void NestedTypeMapperMaps()
    {
        var destination = NestedTypeMappers.Inner.Map(new BasicSource { Id = 1, Name = "name", Description = "text" });

        Assert.Equal(1, destination.Id);
        Assert.Equal("name", destination.Name);
        Assert.Equal("text", destination.Description);
    }

    // The culture field is declared in the nested type
    [Fact]
    public void NestedStructMapperUsesItsCulture()
    {
        var destination = NestedTypeMappers.Formatting.Map(new CultureProfileSource { Amount = 1234.5m });

        Assert.Equal("1.234,50", destination.Name);
    }

    [Fact]
    public void SetsRequiredMembersKeepsWhatConstructorSet()
    {
        var destination = TestMappers.MapSetsRequired(new BasicSource { Id = 2, Name = "name" });

        Assert.Equal(2, destination.Id);
        Assert.Equal("name", destination.Name);
        Assert.Equal("default", destination.Code);
    }

    [Fact]
    public void ProfileNameComparisonAppliesToAttributeNames()
    {
        var destination = NameComparisonProfileMappers.Map(new NameComparisonSource { Value = 5, Other = 4 });

        Assert.Equal(3, destination.Field);
        Assert.Equal(new NameComparisonDestination { Target = 5, Field = 3, Count = 8 }, destination);
    }
}
