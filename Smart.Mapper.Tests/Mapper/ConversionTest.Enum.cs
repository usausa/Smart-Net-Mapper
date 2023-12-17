namespace Smart.Mapper;

public sealed partial class ConversionTest
{
    //--------------------------------------------------------------------------------
    // Enum to Value
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertEnum16ToInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum16Holder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((int)Enum16.Zero, mapper.Map<Enum16Holder, Int32Holder>(new Enum16Holder { Value = Enum16.Zero }).Value);
        Assert.Equal((int)Enum16.One, mapper.Map<Enum16Holder, Int32Holder>(new Enum16Holder { Value = Enum16.One }).Value);
        Assert.Equal((int)Enum16.Max, mapper.Map<Enum16Holder, Int32Holder>(new Enum16Holder { Value = Enum16.Max }).Value);
    }

    [Fact]
    public void ConvertEnum32ToInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((int)Enum32.Zero, mapper.Map<Enum32Holder, Int32Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal((int)Enum32.One, mapper.Map<Enum32Holder, Int32Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal((int)Enum32.Max, mapper.Map<Enum32Holder, Int32Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertEnum64ToInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum64Holder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((int)Enum64.Zero, mapper.Map<Enum64Holder, Int32Holder>(new Enum64Holder { Value = Enum64.Zero }).Value);
        Assert.Equal((int)Enum64.One, mapper.Map<Enum64Holder, Int32Holder>(new Enum64Holder { Value = Enum64.One }).Value);
        Assert.Equal(unchecked((int)Enum64.Max), mapper.Map<Enum64Holder, Int32Holder>(new Enum64Holder { Value = Enum64.Max }).Value);
    }

    //--------------------------------------------------------------------------------
    // Value to Enum
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertInt32ToEnum16()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, Enum16Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum16)0, mapper.Map<Int32Holder, Enum16Holder>(new Int32Holder { Value = 0 }).Value);
        Assert.Equal((Enum16)(-1), mapper.Map<Int32Holder, Enum16Holder>(new Int32Holder { Value = -1 }).Value);
        Assert.Equal(unchecked((Enum16)Int32.MaxValue), mapper.Map<Int32Holder, Enum16Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
    }

    [Fact]
    public void ConvertInt32ToEnum32()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, Enum32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum32)0, mapper.Map<Int32Holder, Enum32Holder>(new Int32Holder { Value = 0 }).Value);
        Assert.Equal((Enum32)(-1), mapper.Map<Int32Holder, Enum32Holder>(new Int32Holder { Value = -1 }).Value);
        Assert.Equal((Enum32)Int32.MaxValue, mapper.Map<Int32Holder, Enum32Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
    }

    [Fact]
    public void ConvertInt32ToEnum64()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, Enum64Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum64)0, mapper.Map<Int32Holder, Enum64Holder>(new Int32Holder { Value = 0 }).Value);
        Assert.Equal((Enum64)(-1), mapper.Map<Int32Holder, Enum64Holder>(new Int32Holder { Value = -1 }).Value);
        Assert.Equal((Enum64)Int32.MaxValue, mapper.Map<Int32Holder, Enum64Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
    }

    //--------------------------------------------------------------------------------
    // Enum to Enum
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertEnum32ToEnum16()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, Enum16Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum16.Zero, mapper.Map<Enum32Holder, Enum16Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal(Enum16.One, mapper.Map<Enum32Holder, Enum16Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(unchecked((Enum16)Enum32.Max), mapper.Map<Enum32Holder, Enum16Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertEnum32ToEnum32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, Enum32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum32.Zero, mapper.Map<Enum32Holder, Enum32Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal(Enum32.One, mapper.Map<Enum32Holder, Enum32Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(Enum32.Max, mapper.Map<Enum32Holder, Enum32Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertEnum32ToEnum64()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, Enum64Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum64.Zero, mapper.Map<Enum32Holder, Enum64Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal(Enum64.One, mapper.Map<Enum32Holder, Enum64Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal((Enum64)Enum32.Max, mapper.Map<Enum32Holder, Enum64Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    //--------------------------------------------------------------------------------
    // Enum to NullableValue
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertEnum16ToNullableInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum16Holder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((int)Enum16.Zero, mapper.Map<Enum16Holder, NullableInt32Holder>(new Enum16Holder { Value = Enum16.Zero }).Value);
        Assert.Equal((int)Enum16.One, mapper.Map<Enum16Holder, NullableInt32Holder>(new Enum16Holder { Value = Enum16.One }).Value);
        Assert.Equal((int)Enum16.Max, mapper.Map<Enum16Holder, NullableInt32Holder>(new Enum16Holder { Value = Enum16.Max }).Value);
    }

    [Fact]
    public void ConvertEnum32ToNullableInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((int)Enum32.Zero, mapper.Map<Enum32Holder, NullableInt32Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal((int)Enum32.One, mapper.Map<Enum32Holder, NullableInt32Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal((int)Enum32.Max, mapper.Map<Enum32Holder, NullableInt32Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertEnum64ToNullableInt32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum64Holder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((int)Enum64.Zero, mapper.Map<Enum64Holder, NullableInt32Holder>(new Enum64Holder { Value = Enum64.Zero }).Value);
        Assert.Equal((int)Enum64.One, mapper.Map<Enum64Holder, NullableInt32Holder>(new Enum64Holder { Value = Enum64.One }).Value);
        Assert.Equal(unchecked((int)Enum64.Max), mapper.Map<Enum64Holder, NullableInt32Holder>(new Enum64Holder { Value = Enum64.Max }).Value);
    }

    //--------------------------------------------------------------------------------
    // NullableValue to Enum
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableInt32ToEnum16()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, Enum16Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum16)0, mapper.Map<NullableInt32Holder, Enum16Holder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal((Enum16)(-1), mapper.Map<NullableInt32Holder, Enum16Holder>(new NullableInt32Holder { Value = -1 }).Value);
        Assert.Equal(unchecked((Enum16)Int32.MaxValue), mapper.Map<NullableInt32Holder, Enum16Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
    }

    [Fact]
    public void ConvertNullableInt32ToEnum32()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, Enum32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum32)0, mapper.Map<NullableInt32Holder, Enum32Holder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal((Enum32)(-1), mapper.Map<NullableInt32Holder, Enum32Holder>(new NullableInt32Holder { Value = -1 }).Value);
        Assert.Equal((Enum32)Int32.MaxValue, mapper.Map<NullableInt32Holder, Enum32Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
    }

    [Fact]
    public void ConvertNullableInt32ToEnum64()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, Enum64Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum64)0, mapper.Map<NullableInt32Holder, Enum64Holder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal((Enum64)(-1), mapper.Map<NullableInt32Holder, Enum64Holder>(new NullableInt32Holder { Value = -1 }).Value);
        Assert.Equal((Enum64)Int32.MaxValue, mapper.Map<NullableInt32Holder, Enum64Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
    }

    //--------------------------------------------------------------------------------
    // Value to NullableEnum
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertInt32ToNullableEnum16()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, NullableEnum16Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum16)0, mapper.Map<Int32Holder, NullableEnum16Holder>(new Int32Holder { Value = 0 }).Value);
        Assert.Equal((Enum16)(-1), mapper.Map<Int32Holder, NullableEnum16Holder>(new Int32Holder { Value = -1 }).Value);
        Assert.Equal(unchecked((Enum16)Int32.MaxValue), mapper.Map<Int32Holder, NullableEnum16Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
    }

    [Fact]
    public void ConvertInt32ToNullableEnum32()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, NullableEnum32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum32)0, mapper.Map<Int32Holder, NullableEnum32Holder>(new Int32Holder { Value = 0 }).Value);
        Assert.Equal((Enum32)(-1), mapper.Map<Int32Holder, NullableEnum32Holder>(new Int32Holder { Value = -1 }).Value);
        Assert.Equal((Enum32)Int32.MaxValue, mapper.Map<Int32Holder, NullableEnum32Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
    }

    [Fact]
    public void ConvertInt32ToNullableEnum64()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, NullableEnum64Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal((Enum64)0, mapper.Map<Int32Holder, NullableEnum64Holder>(new Int32Holder { Value = 0 }).Value);
        Assert.Equal((Enum64)(-1), mapper.Map<Int32Holder, NullableEnum64Holder>(new Int32Holder { Value = -1 }).Value);
        Assert.Equal((Enum64)Int32.MaxValue, mapper.Map<Int32Holder, NullableEnum64Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
    }

    //--------------------------------------------------------------------------------
    // NullableEnum to Enum
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableEnum32ToEnum16()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableEnum32Holder, Enum16Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum16.Zero, mapper.Map<NullableEnum32Holder, Enum16Holder>(new NullableEnum32Holder { Value = null }).Value);
        Assert.Equal(Enum16.One, mapper.Map<NullableEnum32Holder, Enum16Holder>(new NullableEnum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(unchecked((Enum16)Enum32.Max), mapper.Map<NullableEnum32Holder, Enum16Holder>(new NullableEnum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertNullableEnum32ToEnum32()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableEnum32Holder, Enum32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum32.Zero, mapper.Map<NullableEnum32Holder, Enum32Holder>(new NullableEnum32Holder { Value = null }).Value);
        Assert.Equal(Enum32.One, mapper.Map<NullableEnum32Holder, Enum32Holder>(new NullableEnum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(Enum32.Max, mapper.Map<NullableEnum32Holder, Enum32Holder>(new NullableEnum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertNullableEnum32ToEnum64()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableEnum32Holder, Enum64Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum64.Zero, mapper.Map<NullableEnum32Holder, Enum64Holder>(new NullableEnum32Holder { Value = null }).Value);
        Assert.Equal(Enum64.One, mapper.Map<NullableEnum32Holder, Enum64Holder>(new NullableEnum32Holder { Value = Enum32.One }).Value);
        Assert.Equal((Enum64)Enum32.Max, mapper.Map<NullableEnum32Holder, Enum64Holder>(new NullableEnum32Holder { Value = Enum32.Max }).Value);
    }

    //--------------------------------------------------------------------------------
    // Enum to NullableEnum
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertEnum32ToNullableEnum16()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, NullableEnum16Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum16.Zero, mapper.Map<Enum32Holder, NullableEnum16Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal(Enum16.One, mapper.Map<Enum32Holder, NullableEnum16Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(unchecked((Enum16)Enum32.Max), mapper.Map<Enum32Holder, NullableEnum16Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertEnum32ToNullableEnum32()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, NullableEnum32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum32.Zero, mapper.Map<Enum32Holder, NullableEnum32Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal(Enum32.One, mapper.Map<Enum32Holder, NullableEnum32Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(Enum32.Max, mapper.Map<Enum32Holder, NullableEnum32Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertEnum32ToNullableEnum64()
    {
        var config = new MapperConfig();
        config.CreateMap<Enum32Holder, NullableEnum64Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(Enum64.Zero, mapper.Map<Enum32Holder, NullableEnum64Holder>(new Enum32Holder { Value = Enum32.Zero }).Value);
        Assert.Equal(Enum64.One, mapper.Map<Enum32Holder, NullableEnum64Holder>(new Enum32Holder { Value = Enum32.One }).Value);
        Assert.Equal((Enum64)Enum32.Max, mapper.Map<Enum32Holder, NullableEnum64Holder>(new Enum32Holder { Value = Enum32.Max }).Value);
    }

    //--------------------------------------------------------------------------------
    // NullableEnum to NullableEnum
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableEnum32ToNullableEnum16()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableEnum32Holder, NullableEnum16Holder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableEnum32Holder, NullableEnum16Holder>(new NullableEnum32Holder { Value = null }).Value);
        Assert.Equal(Enum16.One, mapper.Map<NullableEnum32Holder, NullableEnum16Holder>(new NullableEnum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(unchecked((Enum16)Enum32.Max), mapper.Map<NullableEnum32Holder, NullableEnum16Holder>(new NullableEnum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertNullableEnum32ToNullableEnum32()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableEnum32Holder, NullableEnum32Holder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableEnum32Holder, NullableEnum32Holder>(new NullableEnum32Holder { Value = null }).Value);
        Assert.Equal(Enum32.One, mapper.Map<NullableEnum32Holder, NullableEnum32Holder>(new NullableEnum32Holder { Value = Enum32.One }).Value);
        Assert.Equal(Enum32.Max, mapper.Map<NullableEnum32Holder, NullableEnum32Holder>(new NullableEnum32Holder { Value = Enum32.Max }).Value);
    }

    [Fact]
    public void ConvertNullableEnum32ToNullableEnum64()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableEnum32Holder, NullableEnum64Holder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableEnum32Holder, NullableEnum64Holder>(new NullableEnum32Holder { Value = null }).Value);
        Assert.Equal(Enum64.One, mapper.Map<NullableEnum32Holder, NullableEnum64Holder>(new NullableEnum32Holder { Value = Enum32.One }).Value);
        Assert.Equal((Enum64)Enum32.Max, mapper.Map<NullableEnum32Holder, NullableEnum64Holder>(new NullableEnum32Holder { Value = Enum32.Max }).Value);
    }

    //--------------------------------------------------------------------------------
    // Enum
    //--------------------------------------------------------------------------------

    public enum Enum16 : short
    {
        Zero = 0,
        One = 1,
        Max = Int16.MaxValue
    }

    public sealed class Enum16Holder
    {
        public Enum16 Value { get; set; }
    }

    public sealed class NullableEnum16Holder
    {
        public Enum16? Value { get; set; }
    }

    public enum Enum32
    {
        Zero = 0,
        One = 1,
        Max = Int32.MaxValue
    }

    public sealed class Enum32Holder
    {
        public Enum32 Value { get; set; }
    }

    public sealed class NullableEnum32Holder
    {
        public Enum32? Value { get; set; }
    }

    public enum Enum64 : long
    {
        Zero = 0,
        One = 1,
        Max = Int64.MaxValue
    }

    public sealed class Enum64Holder
    {
        public Enum64 Value { get; set; }
    }

    public sealed class NullableEnum64Holder
    {
        public Enum64? Value { get; set; }
    }
}
