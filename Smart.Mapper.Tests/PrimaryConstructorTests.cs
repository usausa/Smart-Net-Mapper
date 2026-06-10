namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// D1/D3: record・プライマリコンストラクタ対応のテスト。
// D1/D3: Tests for record and primary-constructor support.
public class PrimaryConstructorTests
{
    // D1: record → record (全パラメータ)
    // D1: record → record (all parameters)
    [Fact]
    public void MapRecord_AllParameters_MapsCorrectly()
    {
        var source = new RecordSource(1, "Alice", 30);

        var destination = PrimaryConstructorMappers.MapRecord(source);

        Assert.Equal(1, destination.Id);
        Assert.Equal("Alice", destination.Name);
        Assert.Equal(30, destination.Age);
    }

    // D1: record → record (パラメータ数の少ない宛先)
    // D1: record → record (destination with fewer parameters)
    [Fact]
    public void MapRecordPartial_FewerParameters_MapsAvailableProperties()
    {
        var source = new RecordSource(2, "Bob", 25);

        var destination = PrimaryConstructorMappers.MapRecordPartial(source);

        Assert.Equal(2, destination.Id);
        Assert.Equal("Bob", destination.Name);
    }

    // D3: クラスのプライマリコンストラクタ
    // D3: primary constructor of a class
    [Fact]
    public void MapPrimaryCtorClass_AllParameters_MapsCorrectly()
    {
        var source = new PrimaryCtorSource { Id = 3, Name = "Carol", Age = 40 };

        var destination = PrimaryConstructorMappers.MapPrimaryCtorClass(source);

        Assert.Equal(3, destination.Id);
        Assert.Equal("Carol", destination.Name);
        Assert.Equal(40, destination.Age);
    }

    // D1: record + 通常 settable プロパティ
    // D1: record + a regular settable property
    [Fact]
    public void MapRecordWithExtra_ConstructorAndSettableProperty_MapsAll()
    {
        var source = new RecordWithExtraSource { Id = 4, Name = "Dave", Extra = "extra-value" };

        var destination = PrimaryConstructorMappers.MapRecordWithExtra(source);

        Assert.Equal(4, destination.Id);
        Assert.Equal("Dave", destination.Name);
        Assert.Equal("extra-value", destination.Extra);
    }

    // D1: record + settable プロパティが null の場合
    // D1: record + settable property is null
    [Fact]
    public void MapRecordWithExtra_NullExtra_MapsNullCorrectly()
    {
        var source = new RecordWithExtraSource { Id = 5, Name = "Eve", Extra = null };

        var destination = PrimaryConstructorMappers.MapRecordWithExtra(source);

        Assert.Equal(5, destination.Id);
        Assert.Equal("Eve", destination.Name);
        Assert.Null(destination.Extra);
    }

    // D1: [MapProperty] でコンストラクタ引数を上書き
    // D1: override a constructor argument with [MapProperty]
    [Fact]
    public void MapWithPropertyOverride_MapPropertyTakesPriority_MapsCorrectly()
    {
        var source = new MapPropertyOverrideSource { Identifier = 99, FullName = "Overridden" };

        var destination = PrimaryConstructorMappers.MapWithPropertyOverride(source);

        Assert.Equal(99, destination.Id);
        Assert.Equal("Overridden", destination.Name);
    }
}
