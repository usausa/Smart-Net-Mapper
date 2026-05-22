namespace Smart.Mapper.Tests;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// D2: required メンバー対応のテスト。
// required プロパティが自動マッピングで設定されることを確認する。
public class RequiredMemberTests
{
    [Fact]
    public void Map_RequiredMembers_MapsAllRequiredProperties()
    {
        var source = new RequiredMemberSource { Id = 1, Name = "Test" };
        var destination = new RequiredMemberDestination { Id = 0, Name = string.Empty };

        TestMappers.MapRequiredMembers(source, destination);

        Assert.Equal(1, destination.Id);
        Assert.Equal("Test", destination.Name);
    }

    [Fact]
    public void Map_RequiredMembers_OptionalPropertyMapped()
    {
        var source = new RequiredMemberSource { Id = 42, Name = "Hello" };
        var destination = new RequiredMemberDestination { Id = 0, Name = string.Empty };

        TestMappers.MapRequiredMembers(source, destination);

        // Description is not in source, so it stays null
        Assert.Null(destination.Description);
    }
}
