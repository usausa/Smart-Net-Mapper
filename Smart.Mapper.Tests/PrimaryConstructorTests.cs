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

    // 対応5: コンストラクタ引数に型変換・Converter・NullValue が適用されること。
    // Fix 5: type conversion, Converter and NullValue are applied to constructor arguments.
    [Fact]
    public void MapCtorConversion_AppliesConversionPipeline()
    {
        var source = new CtorConversionSource { Value = 42, Raw = 7, Quantity = null };

        var destination = PrimaryConstructorMappers.MapCtorConversion(source);

        Assert.Equal("42", destination.Value);
        Assert.Equal("#7", destination.Raw);
        Assert.Equal(99, destination.Quantity);
    }

    // 対応5: NullValue を使わない場合は元の値が渡ること。
    // Fix 5: the original value is passed through when the substitute is not needed.
    [Fact]
    public void MapCtorConversion_KeepsValueWhenNotNull()
    {
        var source = new CtorConversionSource { Value = 1, Raw = 2, Quantity = 5 };

        var destination = PrimaryConstructorMappers.MapCtorConversion(source);

        Assert.Equal(5, destination.Quantity);
    }

    // 対応4/5: セッターの無いプロパティへのリネームと型変換。
    // Fix 4/5: rename plus type conversion onto a get-only property.
    [Fact]
    public void MapCtorGetOnly_AppliesRenameAndConversion()
    {
        var source = new CtorGetOnlySource { Other = 123 };

        var destination = PrimaryConstructorMappers.MapCtorGetOnly(source);

        Assert.Equal("123", destination.Value);
    }

    // 対応5: null 許容ソース + 型変換。値がある場合は変換される。
    // Fix 5: a nullable source needing conversion is converted when it has a value.
    [Fact]
    public void MapCtorNullableConversion_ConvertsWhenNotNull()
    {
        var source = new CtorNullableConversionSource { Value = 7, Text = "42" };

        var destination = PrimaryConstructorMappers.MapCtorNullableConversion(source);

        Assert.Equal("7", destination.Value);
        Assert.Equal(42, destination.Text);
    }

    // 対応5: null の場合はターゲット型の default になる。
    // Fix 5: null yields the destination type's default.
    [Fact]
    public void MapCtorNullableConversion_UsesDestinationDefaultWhenNull()
    {
        var source = new CtorNullableConversionSource { Value = null, Text = null };

        var destination = PrimaryConstructorMappers.MapCtorNullableConversion(source);

        Assert.Null(destination.Value);
        Assert.Equal(0, destination.Text);
    }

    // 残作業1: 対応する destination プロパティを持たない引数にもリネームと型変換が適用される。
    // Remaining item 1: rename and conversion apply to a parameter with no backing property.
    [Fact]
    public void MapCtorNoProperty_AppliesRenameAndConversion()
    {
        var source = new CtorNoPropertySource { Other = 55 };

        var destination = PrimaryConstructorMappers.MapCtorNoProperty(source);

        Assert.Equal("55", destination.Text);
    }

    // null 許容な中間セグメントは、値がある場合は辿って変換される。
    // A nullable intermediate segment is traversed and converted when it has a value.
    [Fact]
    public void MapCtorNestedGuard_ConvertsWhenIntermediateNotNull()
    {
        var source = new CtorNestedGuardSource { Child = new CtorNestedGuardSourceChild { Val = 7 } };

        var destination = PrimaryConstructorMappers.MapCtorNestedGuard(source);

        Assert.Equal("7", destination.Value);
    }

    // null 許容な中間セグメントが null の場合、NRE にならずターゲット型の default になる。
    // A null intermediate segment yields the destination type's default instead of an NRE.
    [Fact]
    public void MapCtorNestedGuard_UsesDefaultWhenIntermediateNull()
    {
        var source = new CtorNestedGuardSource { Child = null };

        var destination = PrimaryConstructorMappers.MapCtorNestedGuard(source);

        Assert.Null(destination.Value);
    }
}
