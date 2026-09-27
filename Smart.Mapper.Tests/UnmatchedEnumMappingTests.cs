namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// A value no member of the target enum has the name of gives null to a nullable target: an enum value of another enum,
// and a string Enum.TryParse cannot parse, while a number in a string still gives its value
public class UnmatchedEnumMappingTests
{
    [Theory]
    [InlineData(UnmatchedSourceKind.First, UnmatchedTargetKind.First)]
    [InlineData(UnmatchedSourceKind.Second, UnmatchedTargetKind.Second)]
    [InlineData(UnmatchedSourceKind.Third, null)]
    public void EnumValueWithoutMatchGivesNull(UnmatchedSourceKind kind, UnmatchedTargetKind? expected)
    {
        var destination = TestMappers.MapUnmatchedEnum(new UnmatchedEnumSource { Kind = kind, Text = "First" });

        Assert.Equal(expected, destination.Kind);
    }

    [Theory]
    [InlineData("Third", UnmatchedSourceKind.Third)]
    [InlineData("1", UnmatchedSourceKind.Second)]
    [InlineData("Fourth", null)]
    public void StringWithoutMatchGivesNull(string text, UnmatchedSourceKind? expected)
    {
        var destination = TestMappers.MapUnmatchedEnum(new UnmatchedEnumSource { Text = text });

        Assert.Equal(expected, destination.Text);
    }
}
