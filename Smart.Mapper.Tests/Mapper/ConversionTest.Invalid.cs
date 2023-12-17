namespace Smart.Mapper;

public sealed partial class ConversionTest
{
    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [Fact]
    public void InvalidConvertClassToValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NoConverterClassValueHolder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<NoConverterClassValueHolder, Int32Holder>(new NoConverterClassValueHolder()));
    }

    [Fact]
    public void InvalidConvertClassToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NoConverterClassValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<NoConverterClassValueHolder, NullableInt32Holder>(new NoConverterClassValueHolder()));
    }

    [Fact]
    public void InvalidConvertStructToValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NoConverterStructValueHolder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<NoConverterStructValueHolder, Int32Holder>(new NoConverterStructValueHolder()));
    }

    [Fact]
    public void InvalidConvertStructToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NoConverterStructValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<NoConverterStructValueHolder, NullableInt32Holder>(new NoConverterStructValueHolder()));
    }

    [Fact]
    public void InvalidConvertNullableStructToValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NoConverterNullableStructValueHolder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<NoConverterNullableStructValueHolder, Int32Holder>(new NoConverterNullableStructValueHolder()));
    }

    [Fact]
    public void InvalidConvertNullableStructToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NoConverterNullableStructValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<NoConverterNullableStructValueHolder, NullableInt32Holder>(new NoConverterNullableStructValueHolder()));
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class NoConverterClassValue
    {
    }

    public sealed class NoConverterClassValueHolder
    {
        public NoConverterClassValue? Value { get; set; }
    }

    public struct NoConverterStructValue
    {
    }

    public sealed class NoConverterStructValueHolder
    {
        public NoConverterStructValue Value { get; set; }
    }

    public sealed class NoConverterNullableStructValueHolder
    {
        public NoConverterStructValue? Value { get; set; }
    }
}
