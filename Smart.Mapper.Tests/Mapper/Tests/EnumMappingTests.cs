namespace Smart.Mapper;

/// <summary>
/// A2: Enum マッピングのテスト。
/// enum ↔ enum、enum ↔ int、enum ↔ string の各変換パターンを検証する。
/// </summary>
public class EnumMappingTests
{
    // ---- enum ↔ enum (by name) ----

    [Fact]
    public void MapEnumToEnum_Active_MapsCorrectly()
    {
        var source = new EnumToEnumSource { Status = SourceStatus.Active };
        var destination = new EnumToEnumDestination();

        TestMappers.MapEnumToEnum(source, destination);

        Assert.Equal(DestStatus.Active, destination.Status);
    }

    [Fact]
    public void MapEnumToEnum_Inactive_MapsCorrectly()
    {
        var source = new EnumToEnumSource { Status = SourceStatus.Inactive };
        var destination = new EnumToEnumDestination();

        TestMappers.MapEnumToEnum(source, destination);

        Assert.Equal(DestStatus.Inactive, destination.Status);
    }

    [Fact]
    public void MapEnumToEnum_AllValues_MappedByName()
    {
        foreach (SourceStatus value in Enum.GetValues<SourceStatus>())
        {
            var source = new EnumToEnumSource { Status = value };
            var destination = new EnumToEnumDestination();

            TestMappers.MapEnumToEnum(source, destination);

            Assert.Equal(value.ToString(), destination.Status.ToString());
        }
    }

    // ---- partial enum (Pending → default) ----

    [Fact]
    public void MapPartialEnum_UnmatchedValue_ReturnsDefault()
    {
        var source = new PartialEnumSource { Status = SourceStatus.Pending };
        var destination = new PartialEnumDestination();

        TestMappers.MapPartialEnum(source, destination);

        Assert.Equal(default(PartialDestStatus), destination.Status);
    }

    [Fact]
    public void MapPartialEnum_MatchedValue_MapsCorrectly()
    {
        var source = new PartialEnumSource { Status = SourceStatus.Active };
        var destination = new PartialEnumDestination();

        TestMappers.MapPartialEnum(source, destination);

        Assert.Equal(PartialDestStatus.Active, destination.Status);
    }

    // ---- nullable enum ↔ nullable enum ----

    [Fact]
    public void MapNullableEnum_NonNullValue_MapsCorrectly()
    {
        var source = new NullableEnumSource { Status = SourceStatus.Inactive };
        var destination = new NullableEnumDestination();

        TestMappers.MapNullableEnum(source, destination);

        Assert.Equal(DestStatus.Inactive, destination.Status);
    }

    [Fact]
    public void MapNullableEnum_NullValue_ResultsInDefault()
    {
        var source = new NullableEnumSource { Status = null };
        var destination = new NullableEnumDestination();

        TestMappers.MapNullableEnum(source, destination);

        Assert.Null(destination.Status);
    }

    // ---- enum → int ----

    [Fact]
    public void MapEnumToInt_Active_ReturnsIntValue()
    {
        var source = new EnumToIntSource { Status = SourceStatus.Active };
        var destination = new EnumToIntDestination();

        TestMappers.MapEnumToInt(source, destination);

        Assert.Equal((int)SourceStatus.Active, destination.Status);
    }

    [Fact]
    public void MapEnumToInt_Inactive_ReturnsIntValue()
    {
        var source = new EnumToIntSource { Status = SourceStatus.Inactive };
        var destination = new EnumToIntDestination();

        TestMappers.MapEnumToInt(source, destination);

        Assert.Equal((int)SourceStatus.Inactive, destination.Status);
    }

    // ---- int → enum ----

    [Fact]
    public void MapIntToEnum_ValidValue_MapsCorrectly()
    {
        var source = new IntToEnumSource { Status = (int)DestStatus.Active };
        var destination = new IntToEnumDestination();

        TestMappers.MapIntToEnum(source, destination);

        Assert.Equal(DestStatus.Active, destination.Status);
    }

    // ---- enum → string ----

    [Fact]
    public void MapEnumToString_Active_ReturnsStringName()
    {
        var source = new EnumToStringSource { Status = SourceStatus.Active };
        var destination = new EnumToStringDestination();

        TestMappers.MapEnumToString(source, destination);

        Assert.Equal("Active", destination.Status);
    }

    [Fact]
    public void MapEnumToString_Pending_ReturnsStringName()
    {
        var source = new EnumToStringSource { Status = SourceStatus.Pending };
        var destination = new EnumToStringDestination();

        TestMappers.MapEnumToString(source, destination);

        Assert.Equal("Pending", destination.Status);
    }

    // ---- string → enum ----

    [Fact]
    public void MapStringToEnum_ActiveString_MapsCorrectly()
    {
        var source = new StringToEnumSource { Status = "Active" };
        var destination = new StringToEnumDestination();

        TestMappers.MapStringToEnum(source, destination);

        Assert.Equal(DestStatus.Active, destination.Status);
    }

    [Fact]
    public void MapStringToEnum_InactiveString_MapsCorrectly()
    {
        var source = new StringToEnumSource { Status = "Inactive" };
        var destination = new StringToEnumDestination();

        TestMappers.MapStringToEnum(source, destination);

        Assert.Equal(DestStatus.Inactive, destination.Status);
    }
}
