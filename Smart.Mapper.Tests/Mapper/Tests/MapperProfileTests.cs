namespace Smart.Mapper;

/// <summary>
/// E3: MapperProfile 集約のテスト。
/// クラス属性の設定がメソッドに継承されること、およびメソッドレベルで上書きできることを確認する。
/// </summary>
public class MapperProfileTests
{
    [Fact]
    public void Map_WithProfile_MapsPropertiesCorrectly()
    {
        var source = new ProfileSource { Id = 10, Name = "Profile" };
        var destination = new ProfileDestination();

        ProfileMappers.Map(source, destination);

        Assert.Equal(10, destination.Id);
        Assert.Equal("Profile", destination.Name);
    }

    [Fact]
    public void MapNoStrict_WithProfileOverride_MapsPropertiesCorrectly()
    {
        var source = new ProfileSource { Id = 20, Name = "Override" };
        var destination = new ProfileDestination();

        ProfileMappers.MapNoStrict(source, destination);

        Assert.Equal(20, destination.Id);
        Assert.Equal("Override", destination.Name);
    }
}
