namespace Smart.Mapper;

public sealed partial class ConversionTest
{
    //--------------------------------------------------------------------------------
    // Basic
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertInt16ToInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Int16Holder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<Int16Holder, Int32Holder>(new Int16Holder { Value = -1 }).Value);
        Assert.Equal(Int16.MaxValue, mapper.Map<Int16Holder, Int32Holder>(new Int16Holder { Value = Int16.MaxValue }).Value);
    }

    [Fact]
    public void ConvertInt64ToInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Int64Holder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<Int64Holder, Int32Holder>(new Int64Holder { Value = -1L }).Value);
        Assert.Equal(unchecked((int)Int64.MaxValue), mapper.Map<Int64Holder, Int32Holder>(new Int64Holder { Value = Int64.MaxValue }).Value);
    }

    [Fact]
    public void ConvertInt32ToDecimal()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, DecimalHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<Int32Holder, DecimalHolder>(new Int32Holder { Value = -1 }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, DecimalHolder>(new Int32Holder { Value = Int32.MaxValue }).Value);
    }

    [Fact]
    public void ConvertDecimalToInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<DecimalHolder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(-1, mapper.Map<DecimalHolder, Int32Holder>(new DecimalHolder { Value = -1m }).Value);
    }
}
