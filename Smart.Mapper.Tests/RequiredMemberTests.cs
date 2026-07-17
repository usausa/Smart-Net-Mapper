namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// D2: required メンバー対応のテスト。
// D2: Tests for required member support.
// required プロパティが自動マッピングで設定されることを確認する。
// Verifies that required properties are set via automatic mapping.
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

    // Regression H: return-mapper to an init-only destination without a parameterized constructor.
    // Previously generated `new Dst(); dst.Id = ...;` which failed with CS8852.
    [Fact]
    public void Map_InitOnlyReturnMapper_SetsInitMembers()
    {
        var dest = TestMappers.MapInitReturn(new InitReturnSource { Id = 7, Name = "seven" });

        Assert.Equal(7, dest.Id);
        Assert.Equal("seven", dest.Name);
    }

    // Regression I: return-mapper to a required-member destination.
    // Previously generated `new Dst();` which failed with CS9035 (required members unset).
    [Fact]
    public void Map_RequiredReturnMapper_SetsRequiredMembers()
    {
        var dest = TestMappers.MapRequiredReturn(new RequiredReturnSource { Id = 9, Name = "nine" });

        Assert.Equal(9, dest.Id);
        Assert.Equal("nine", dest.Name);
        Assert.Null(dest.Extra);
    }

    // MapConstant / MapExpression / MapUsing / MapFrom が init-only / required メンバーを対象とする場合、
    // オブジェクト初期化子経由で代入される (以前は構築後代入で CS8852 / CS9035)。
    // MapConstant / MapExpression / MapUsing / MapFrom targeting init-only / required members are
    // assigned via the object initializer (previously post-construction assignment: CS8852 / CS9035).
    [Fact]
    public void Map_FeatureMappings_SetInitOnlyAndRequiredTargets()
    {
        var dest = TestMappers.MapFeatureInit(new FeatureInitSource { Id = 3, Name = "abc" });

        Assert.Equal(3, dest.Id);
        Assert.Equal("F", dest.Fixed);
        Assert.Equal("ABC", dest.Upper);
        Assert.Equal("abc", dest.FromName);
        Assert.Equal(6, dest.Doubled);
    }
}
