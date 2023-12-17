namespace Smart.Mapper;

public partial class ConversionTest
{
    //--------------------------------------------------------------------------------
    // NullIf
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableToValueWithNullIf()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, Int32Holder>()
            .ForMember(x => x.Value, opt => opt.NullIf(Int32.MinValue));
        using var mapper = config.ToMapper();

        Assert.Equal(Int32.MinValue, mapper.Map<NullableInt32Holder, Int32Holder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal(0, mapper.Map<NullableInt32Holder, Int32Holder>(new NullableInt32Holder { Value = 0 }).Value);
        Assert.Equal(-1, mapper.Map<NullableInt32Holder, Int32Holder>(new NullableInt32Holder { Value = -1 }).Value);
    }

    [Fact]
    public void ConvertClassToValueWithNullIf()
    {
        var config = new MapperConfig();
        config.CreateMap<NullIfClassValueHolder, Int32Holder>()
            .ForMember(x => x.Value, opt => opt.NullIf(Int32.MinValue));
        using var mapper = config.ToMapper();

        Assert.Equal(Int32.MinValue, mapper.Map<NullIfClassValueHolder, Int32Holder>(new NullIfClassValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullIfClassValueHolder, Int32Holder>(new NullIfClassValueHolder { Value = new NullIfClassValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullIfClassValueHolder, Int32Holder>(new NullIfClassValueHolder { Value = new NullIfClassValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public sealed class NullIfClassValue
    {
        public int RawValue { get; set; }

        public static implicit operator NullIfClassValue(int value) => new() { RawValue = value };
        public static explicit operator int(NullIfClassValue value) => value.RawValue;
    }

    public sealed class NullIfClassValueHolder
    {
        public NullIfClassValue? Value { get; set; }
    }
}
