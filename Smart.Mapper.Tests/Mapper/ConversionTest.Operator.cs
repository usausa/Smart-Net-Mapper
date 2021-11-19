namespace Smart.Mapper;

using System;

using Xunit;

public partial class ConversionTest
{
    //--------------------------------------------------------------------------------
    // Value/Class
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertValueToClass()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, ClassValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<Int32Holder, ClassValueHolder>(new Int32Holder { Value = 0 }).Value!.RawValue);
        Assert.Equal(-1, mapper.Map<Int32Holder, ClassValueHolder>(new Int32Holder { Value = -1 }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, ClassValueHolder>(new Int32Holder { Value = Int32.MaxValue }).Value!.RawValue);
    }

    [Fact]
    public void ConvertClassToValue()
    {
        var config = new MapperConfig();
        config.CreateMap<ClassValueHolder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = new ClassValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = new ClassValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // Value/Struct
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertValueToStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, StructValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<Int32Holder, StructValueHolder>(new Int32Holder { Value = 0 }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<Int32Holder, StructValueHolder>(new Int32Holder { Value = -1 }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, StructValueHolder>(new Int32Holder { Value = Int32.MaxValue }).Value.RawValue);
    }

    [Fact]
    public void ConvertStructToValue()
    {
        var config = new MapperConfig();
        config.CreateMap<StructValueHolder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<StructValueHolder, Int32Holder>(new StructValueHolder { Value = new StructValue { RawValue = 0 } }).Value);
        Assert.Equal(-1, mapper.Map<StructValueHolder, Int32Holder>(new StructValueHolder { Value = new StructValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<StructValueHolder, Int32Holder>(new StructValueHolder { Value = new StructValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // Value/Class
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertValueToClassNullable()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, ClassNullableValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<Int32Holder, ClassNullableValueHolder>(new Int32Holder { Value = 0 }).Value!.RawValue);
        Assert.Equal(-1, mapper.Map<Int32Holder, ClassNullableValueHolder>(new Int32Holder { Value = -1 }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, ClassNullableValueHolder>(new Int32Holder { Value = Int32.MaxValue }).Value!.RawValue);
    }

    // ClassNullableValue to int is not supported

    //--------------------------------------------------------------------------------
    // Value/StructNullable
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertValueToStructNullable()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, StructNullableValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<Int32Holder, StructNullableValueHolder>(new Int32Holder { Value = 0 }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<Int32Holder, StructNullableValueHolder>(new Int32Holder { Value = -1 }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, StructNullableValueHolder>(new Int32Holder { Value = Int32.MaxValue }).Value.RawValue);
    }

    // StructNullableValue to int is not supported

    //--------------------------------------------------------------------------------
    // NullableValue/Class
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableValueToClass()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, ClassValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableInt32Holder, ClassValueHolder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableInt32Holder, ClassValueHolder>(new NullableInt32Holder { Value = -1 }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, ClassValueHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value!.RawValue);
    }

    [Fact]
    public void ConvertClassToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<ClassValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<ClassValueHolder, NullableInt32Holder>(new ClassValueHolder { Value = new ClassValue { RawValue = 0 } }).Value);
        Assert.Equal(-1, mapper.Map<ClassValueHolder, NullableInt32Holder>(new ClassValueHolder { Value = new ClassValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<ClassValueHolder, NullableInt32Holder>(new ClassValueHolder { Value = new ClassValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // NullableValue/Struct
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableValueToStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, StructValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<NullableInt32Holder, StructValueHolder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableInt32Holder, StructValueHolder>(new NullableInt32Holder { Value = -1 }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, StructValueHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value.RawValue);
    }

    [Fact]
    public void ConvertStructToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<StructValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<StructValueHolder, NullableInt32Holder>(new StructValueHolder { Value = new StructValue { RawValue = 0 } }).Value);
        Assert.Equal(-1, mapper.Map<StructValueHolder, NullableInt32Holder>(new StructValueHolder { Value = new StructValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<StructValueHolder, NullableInt32Holder>(new StructValueHolder { Value = new StructValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // NullableValue/ClassNullable
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableValueToClassNullable()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, ClassNullableValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableInt32Holder, ClassNullableValueHolder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableInt32Holder, ClassNullableValueHolder>(new NullableInt32Holder { Value = -1 }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, ClassNullableValueHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value!.RawValue);
    }

    [Fact]
    public void ConvertClassNullableToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<ClassNullableValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<ClassNullableValueHolder, NullableInt32Holder>(new ClassNullableValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<ClassNullableValueHolder, NullableInt32Holder>(new ClassNullableValueHolder { Value = new ClassNullableValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<ClassNullableValueHolder, NullableInt32Holder>(new ClassNullableValueHolder { Value = new ClassNullableValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // NullableValue/StructNullable
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableValueToStructNullable()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, StructNullableValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableInt32Holder, StructNullableValueHolder>(new NullableInt32Holder { Value = null }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<NullableInt32Holder, StructNullableValueHolder>(new NullableInt32Holder { Value = -1 }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, StructNullableValueHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value.RawValue);
    }

    [Fact]
    public void ConvertStructNullableToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<StructNullableValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<StructNullableValueHolder, NullableInt32Holder>(new StructNullableValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<StructNullableValueHolder, NullableInt32Holder>(new StructNullableValueHolder { Value = new StructNullableValue { RawValue = -1 } }).Value!.Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<StructNullableValueHolder, NullableInt32Holder>(new StructNullableValueHolder { Value = new StructNullableValue { RawValue = Int32.MaxValue } }).Value!.Value);
    }

    //--------------------------------------------------------------------------------
    // Value/NullableStruct
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertValueToNullableStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, NullableStructValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<Int32Holder, NullableStructValueHolder>(new Int32Holder { Value = 0 }).Value!.Value.RawValue);
        Assert.Equal(-1, mapper.Map<Int32Holder, NullableStructValueHolder>(new Int32Holder { Value = -1 }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, NullableStructValueHolder>(new Int32Holder { Value = Int32.MaxValue }).Value!.Value.RawValue);
    }

    [Fact]
    public void ConvertNullableStructToValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableStructValueHolder, Int32Holder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<NullableStructValueHolder, Int32Holder>(new NullableStructValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableStructValueHolder, Int32Holder>(new NullableStructValueHolder { Value = new StructValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableStructValueHolder, Int32Holder>(new NullableStructValueHolder { Value = new StructValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // Value/NullableStructNullable
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertValueToNullableStructNullable()
    {
        var config = new MapperConfig();
        config.CreateMap<Int32Holder, NullableStructNullableValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<Int32Holder, NullableStructNullableValueHolder>(new Int32Holder { Value = 0 }).Value!.Value.RawValue);
        Assert.Equal(-1, mapper.Map<Int32Holder, NullableStructNullableValueHolder>(new Int32Holder { Value = -1 }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, NullableStructNullableValueHolder>(new Int32Holder { Value = Int32.MaxValue }).Value!.Value.RawValue);
    }

    // StructNullableValue to int is not supported

    //--------------------------------------------------------------------------------
    // NullableValue/NullableStruct
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableValueToNullableStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, NullableStructValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableInt32Holder, NullableStructValueHolder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableInt32Holder, NullableStructValueHolder>(new NullableInt32Holder { Value = -1 }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, NullableStructValueHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value!.Value.RawValue);
    }

    [Fact]
    public void ConvertNullableStructToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableStructValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableStructValueHolder, NullableInt32Holder>(new NullableStructValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableStructValueHolder, NullableInt32Holder>(new NullableStructValueHolder { Value = new StructValue { RawValue = -1 } }).Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableStructValueHolder, NullableInt32Holder>(new NullableStructValueHolder { Value = new StructValue { RawValue = Int32.MaxValue } }).Value);
    }

    //--------------------------------------------------------------------------------
    // Value/NullableStructNullable
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableValueToNullableStructNullable()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableInt32Holder, NullableStructNullableValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableInt32Holder, NullableStructNullableValueHolder>(new NullableInt32Holder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableInt32Holder, NullableStructNullableValueHolder>(new NullableInt32Holder { Value = -1 }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, NullableStructNullableValueHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value!.Value.RawValue);
    }

    [Fact]
    public void ConvertNullableStructNullableToNullableValue()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableStructNullableValueHolder, NullableInt32Holder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableStructNullableValueHolder, NullableInt32Holder>(new NullableStructNullableValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableStructNullableValueHolder, NullableInt32Holder>(new NullableStructNullableValueHolder { Value = new StructNullableValue { RawValue = -1 } }).Value!.Value);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableStructNullableValueHolder, NullableInt32Holder>(new NullableStructNullableValueHolder { Value = new StructNullableValue { RawValue = Int32.MaxValue } }).Value!.Value);
    }

    //--------------------------------------------------------------------------------
    // ClassPair1/ClassPair2
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertClassPair1ToClassPair2()
    {
        var config = new MapperConfig();
        config.CreateMap<ClassPair1ValueHolder, ClassPair2ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<ClassPair1ValueHolder, ClassPair2ValueHolder>(new ClassPair1ValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<ClassPair1ValueHolder, ClassPair2ValueHolder>(new ClassPair1ValueHolder { Value = new ClassPair1Value { RawValue = -1 } }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<ClassPair1ValueHolder, ClassPair2ValueHolder>(new ClassPair1ValueHolder { Value = new ClassPair1Value { RawValue = Int32.MaxValue } }).Value!.RawValue);
    }

    [Fact]
    public void ConvertClassPair2ToClassPair1()
    {
        var config = new MapperConfig();
        config.CreateMap<ClassPair2ValueHolder, ClassPair1ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<ClassPair2ValueHolder, ClassPair1ValueHolder>(new ClassPair2ValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<ClassPair2ValueHolder, ClassPair1ValueHolder>(new ClassPair2ValueHolder { Value = new ClassPair2Value { RawValue = -1 } }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<ClassPair2ValueHolder, ClassPair1ValueHolder>(new ClassPair2ValueHolder { Value = new ClassPair2Value { RawValue = Int32.MaxValue } }).Value!.RawValue);
    }

    //--------------------------------------------------------------------------------
    // StructPair1/StructPair2
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertStructPair1ToStructPair2()
    {
        var config = new MapperConfig();
        config.CreateMap<StructPair1ValueHolder, StructPair2ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<StructPair1ValueHolder, StructPair2ValueHolder>(new StructPair1ValueHolder { Value = new StructPair1Value { RawValue = 0 } }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<StructPair1ValueHolder, StructPair2ValueHolder>(new StructPair1ValueHolder { Value = new StructPair1Value { RawValue = -1 } }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<StructPair1ValueHolder, StructPair2ValueHolder>(new StructPair1ValueHolder { Value = new StructPair1Value { RawValue = Int32.MaxValue } }).Value.RawValue);
    }

    [Fact]
    public void ConvertStructPair2ToStructPair1()
    {
        var config = new MapperConfig();
        config.CreateMap<StructPair2ValueHolder, StructPair1ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<StructPair2ValueHolder, StructPair1ValueHolder>(new StructPair2ValueHolder { Value = new StructPair2Value { RawValue = 0 } }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<StructPair2ValueHolder, StructPair1ValueHolder>(new StructPair2ValueHolder { Value = new StructPair2Value { RawValue = -1 } }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<StructPair2ValueHolder, StructPair1ValueHolder>(new StructPair2ValueHolder { Value = new StructPair2Value { RawValue = Int32.MaxValue } }).Value.RawValue);
    }

    //--------------------------------------------------------------------------------
    // NullableStructPair1/StructPair2
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableStructPair1ToStructPair2()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableStructPair1ValueHolder, StructPair2ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<NullableStructPair1ValueHolder, StructPair2ValueHolder>(new NullableStructPair1ValueHolder { Value = null }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<NullableStructPair1ValueHolder, StructPair2ValueHolder>(new NullableStructPair1ValueHolder { Value = new StructPair1Value { RawValue = -1 } }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableStructPair1ValueHolder, StructPair2ValueHolder>(new NullableStructPair1ValueHolder { Value = new StructPair1Value { RawValue = Int32.MaxValue } }).Value.RawValue);
    }

    [Fact]
    public void ConvertStructPair2ToNullableStructPair1()
    {
        var config = new MapperConfig();
        config.CreateMap<StructPair2ValueHolder, NullableStructPair1ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<StructPair2ValueHolder, NullableStructPair1ValueHolder>(new StructPair2ValueHolder { Value = new StructPair2Value { RawValue = 0 } }).Value!.Value.RawValue);
        Assert.Equal(-1, mapper.Map<StructPair2ValueHolder, NullableStructPair1ValueHolder>(new StructPair2ValueHolder { Value = new StructPair2Value { RawValue = -1 } }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<StructPair2ValueHolder, NullableStructPair1ValueHolder>(new StructPair2ValueHolder { Value = new StructPair2Value { RawValue = Int32.MaxValue } }).Value!.Value.RawValue);
    }

    //--------------------------------------------------------------------------------
    // StructPair1/NullableStructPair2
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertStructPair1ToNullableStructPair2()
    {
        var config = new MapperConfig();
        config.CreateMap<StructPair1ValueHolder, NullableStructPair2ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<StructPair1ValueHolder, NullableStructPair2ValueHolder>(new StructPair1ValueHolder { Value = new StructPair1Value { RawValue = 0 } }).Value!.Value.RawValue);
        Assert.Equal(-1, mapper.Map<StructPair1ValueHolder, NullableStructPair2ValueHolder>(new StructPair1ValueHolder { Value = new StructPair1Value { RawValue = -1 } }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<StructPair1ValueHolder, NullableStructPair2ValueHolder>(new StructPair1ValueHolder { Value = new StructPair1Value { RawValue = Int32.MaxValue } }).Value!.Value.RawValue);
    }

    [Fact]
    public void ConvertNullableStructPair2ToStructPair1()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableStructPair2ValueHolder, StructPair1ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<NullableStructPair2ValueHolder, StructPair1ValueHolder>(new NullableStructPair2ValueHolder { Value = null }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<NullableStructPair2ValueHolder, StructPair1ValueHolder>(new NullableStructPair2ValueHolder { Value = new StructPair2Value { RawValue = -1 } }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableStructPair2ValueHolder, StructPair1ValueHolder>(new NullableStructPair2ValueHolder { Value = new StructPair2Value { RawValue = Int32.MaxValue } }).Value.RawValue);
    }

    //--------------------------------------------------------------------------------
    // NullableStructPair1/NullableStructPair2
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertNullableStructPair1ToNullableStructPair2()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableStructPair1ValueHolder, NullableStructPair2ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableStructPair1ValueHolder, NullableStructPair2ValueHolder>(new NullableStructPair1ValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableStructPair1ValueHolder, NullableStructPair2ValueHolder>(new NullableStructPair1ValueHolder { Value = new StructPair1Value { RawValue = -1 } }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableStructPair1ValueHolder, NullableStructPair2ValueHolder>(new NullableStructPair1ValueHolder { Value = new StructPair1Value { RawValue = Int32.MaxValue } }).Value!.Value.RawValue);
    }

    [Fact]
    public void ConvertNullableStructPair2ToNullableStructPair1()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableStructPair2ValueHolder, NullableStructPair1ValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableStructPair2ValueHolder, NullableStructPair1ValueHolder>(new NullableStructPair2ValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<NullableStructPair2ValueHolder, NullableStructPair1ValueHolder>(new NullableStructPair2ValueHolder { Value = new StructPair2Value { RawValue = -1 } }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableStructPair2ValueHolder, NullableStructPair1ValueHolder>(new NullableStructPair2ValueHolder { Value = new StructPair2Value { RawValue = Int32.MaxValue } }).Value!.Value.RawValue);
    }

    //--------------------------------------------------------------------------------
    // CrossPairClass/CrossPairStruct
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertCrossPairClassToCrossPairStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<CrossPairClassValueHolder, CrossPairStructValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<CrossPairClassValueHolder, CrossPairStructValueHolder>(new CrossPairClassValueHolder { Value = null }).Value.RawValue);
        Assert.Equal(-1, mapper.Map<CrossPairClassValueHolder, CrossPairStructValueHolder>(new CrossPairClassValueHolder { Value = new CrossPairClassValue { RawValue = -1 } }).Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<CrossPairClassValueHolder, CrossPairStructValueHolder>(new CrossPairClassValueHolder { Value = new CrossPairClassValue { RawValue = Int32.MaxValue } }).Value.RawValue);
    }

    [Fact]
    public void ConvertCrossPairStructToCrossPairClass()
    {
        var config = new MapperConfig();
        config.CreateMap<CrossPairStructValueHolder, CrossPairClassValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<CrossPairStructValueHolder, CrossPairClassValueHolder>(new CrossPairStructValueHolder { Value = new CrossPairStructValue { RawValue = 0 } }).Value!.RawValue);
        Assert.Equal(-1, mapper.Map<CrossPairStructValueHolder, CrossPairClassValueHolder>(new CrossPairStructValueHolder { Value = new CrossPairStructValue { RawValue = -1 } }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<CrossPairStructValueHolder, CrossPairClassValueHolder>(new CrossPairStructValueHolder { Value = new CrossPairStructValue { RawValue = Int32.MaxValue } }).Value!.RawValue);
    }

    //--------------------------------------------------------------------------------
    // CrossPairClass/NullableCrossPairStruct
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConvertCrossPairClassToNullableCrossPairStruct()
    {
        var config = new MapperConfig();
        config.CreateMap<CrossPairClassValueHolder, NullableCrossPairStructValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<CrossPairClassValueHolder, NullableCrossPairStructValueHolder>(new CrossPairClassValueHolder { Value = null }).Value);
        Assert.Equal(-1, mapper.Map<CrossPairClassValueHolder, NullableCrossPairStructValueHolder>(new CrossPairClassValueHolder { Value = new CrossPairClassValue { RawValue = -1 } }).Value!.Value.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<CrossPairClassValueHolder, NullableCrossPairStructValueHolder>(new CrossPairClassValueHolder { Value = new CrossPairClassValue { RawValue = Int32.MaxValue } }).Value!.Value.RawValue);
    }

    [Fact]
    public void ConvertNullableCrossPairStructToCrossPairClass()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableCrossPairStructValueHolder, CrossPairClassValueHolder>();
        using var mapper = config.ToMapper();

        Assert.Equal(0, mapper.Map<NullableCrossPairStructValueHolder, CrossPairClassValueHolder>(new NullableCrossPairStructValueHolder { Value = new CrossPairStructValue { RawValue = 0 } }).Value!.RawValue);
        Assert.Equal(-1, mapper.Map<NullableCrossPairStructValueHolder, CrossPairClassValueHolder>(new NullableCrossPairStructValueHolder { Value = new CrossPairStructValue { RawValue = -1 } }).Value!.RawValue);
        Assert.Equal(Int32.MaxValue, mapper.Map<NullableCrossPairStructValueHolder, CrossPairClassValueHolder>(new NullableCrossPairStructValueHolder { Value = new CrossPairStructValue { RawValue = Int32.MaxValue } }).Value!.RawValue);
    }

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

    public struct StructValue
    {
        public int RawValue { get; set; }

        public static implicit operator StructValue(int value) => new() { RawValue = value };
        public static explicit operator int(StructValue value) => value.RawValue;
    }

    public class StructValueHolder
    {
        public StructValue Value { get; set; }
    }

    public class NullableStructValueHolder
    {
        public StructValue? Value { get; set; }
    }

    public class ClassValue
    {
        public int RawValue { get; set; }

        public static implicit operator ClassValue(int value) => new() { RawValue = value };
        public static explicit operator int(ClassValue value) => value.RawValue;
    }

    public class ClassValueHolder
    {
        public ClassValue? Value { get; set; }
    }

    public struct StructNullableValue
    {
        public int? RawValue { get; set; }

        public static implicit operator StructNullableValue(int? value) => new() { RawValue = value };
        public static explicit operator int?(StructNullableValue value) => value.RawValue;
    }

    public class StructNullableValueHolder
    {
        public StructNullableValue Value { get; set; }
    }

    public class NullableStructNullableValueHolder
    {
        public StructNullableValue? Value { get; set; }
    }

    public class ClassNullableValue
    {
        public int? RawValue { get; set; }

        public static implicit operator ClassNullableValue(int? value) => new() { RawValue = value };
        public static explicit operator int?(ClassNullableValue value) => value.RawValue;
    }

    public class ClassNullableValueHolder
    {
        public ClassNullableValue? Value { get; set; }
    }

    public class ClassPair1Value
    {
        public int RawValue { get; set; }

        public static implicit operator ClassPair1Value(ClassPair2Value value) => new() { RawValue = value.RawValue };
        public static explicit operator ClassPair2Value(ClassPair1Value value) => new() { RawValue = value.RawValue };
    }

    public class ClassPair1ValueHolder
    {
        public ClassPair1Value? Value { get; set; }
    }

    public class ClassPair2Value
    {
        public int RawValue { get; set; }
    }

    public class ClassPair2ValueHolder
    {
        public ClassPair2Value? Value { get; set; }
    }

    public struct StructPair1Value
    {
        public int RawValue { get; set; }

        public static implicit operator StructPair1Value(StructPair2Value value) => new() { RawValue = value.RawValue };
        public static explicit operator StructPair2Value(StructPair1Value value) => new() { RawValue = value.RawValue };
    }

    public class StructPair1ValueHolder
    {
        public StructPair1Value Value { get; set; }
    }

    public class NullableStructPair1ValueHolder
    {
        public StructPair1Value? Value { get; set; }
    }

    public struct StructPair2Value
    {
        public int RawValue { get; set; }
    }

    public class StructPair2ValueHolder
    {
        public StructPair2Value Value { get; set; }
    }

    public class NullableStructPair2ValueHolder
    {
        public StructPair2Value? Value { get; set; }
    }

    public class CrossPairClassValue
    {
        public int RawValue { get; set; }

        public static implicit operator CrossPairClassValue(CrossPairStructValue value) => new() { RawValue = value.RawValue };
        public static implicit operator CrossPairStructValue(CrossPairClassValue value) => new() { RawValue = value.RawValue };
    }

    public class CrossPairClassValueHolder
    {
        public CrossPairClassValue? Value { get; set; }
    }

    public struct CrossPairStructValue
    {
        public int RawValue { get; set; }
    }

    public class CrossPairStructValueHolder
    {
        public CrossPairStructValue Value { get; set; }
    }

    public class NullableCrossPairStructValueHolder
    {
        public CrossPairStructValue? Value { get; set; }
    }
}
