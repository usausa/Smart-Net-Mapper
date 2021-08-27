namespace Smart.Mapper
{
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

        // TODO
        //[Fact]
        //public void ConvertClassToValue()
        //{
        //    var config = new MapperConfig();
        //    config.CreateMap<ClassValueHolder, Int32Holder>();
        //    using var mapper = config.ToMapper();

        //    Assert.Equal(0, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = null }).Value);
        //    Assert.Equal(-1, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = -1 }).Value);
        //    Assert.Equal(Int32.MaxValue, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = Int32.MaxValue }).Value);
        //}

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

            Assert.Equal(0, mapper.Map<StructValueHolder, Int32Holder>(new StructValueHolder { Value = 0 }).Value);
            Assert.Equal(-1, mapper.Map<StructValueHolder, Int32Holder>(new StructValueHolder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<StructValueHolder, Int32Holder>(new StructValueHolder { Value = Int32.MaxValue }).Value);
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

        // TODO
        //[Fact]
        //public void ConvertClassToNullableValue()
        //{
        //    var config = new MapperConfig();
        //    config.CreateMap<ClassValueHolder, NullableInt32Holder>();
        //    using var mapper = config.ToMapper();

        //    Assert.Equal(0, mapper.Map<ClassValueHolder, NullableInt32Holder>(new ClassValueHolder { Value = 0 }).Value);
        //    Assert.Equal(-1, mapper.Map<ClassValueHolder, NullableInt32Holder>(new ClassValueHolder { Value = -1 }).Value);
        //    Assert.Equal(Int32.MaxValue, mapper.Map<ClassValueHolder, NullableInt32Holder>(new ClassValueHolder { Value = Int32.MaxValue }).Value);
        //}

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

            Assert.Equal(0, mapper.Map<StructValueHolder, NullableInt32Holder>(new StructValueHolder { Value = 0 }).Value);
            Assert.Equal(-1, mapper.Map<StructValueHolder, NullableInt32Holder>(new StructValueHolder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<StructValueHolder, NullableInt32Holder>(new StructValueHolder { Value = Int32.MaxValue }).Value);
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

        // TODO
        //[Fact]
        //public void ConvertClassNullableToNullableValue()
        //{
        //    var config = new MapperConfig();
        //    config.CreateMap<ClassNullableValueHolder, NullableInt32Holder>();
        //    using var mapper = config.ToMapper();

        //    Assert.Null(mapper.Map<ClassNullableValueHolder, NullableInt32Holder>(new ClassNullableValueHolder { Value = null }).Value.RawValue);
        //    Assert.Equal(-1, mapper.Map<ClassNullableValueHolder, NullableInt32Holder>(new ClassNullableValueHolder { Value = -1 }).Value.RawValue);
        //    Assert.Equal(Int32.MaxValue, mapper.Map<ClassNullableValueHolder, NullableInt32Holder>(new ClassNullableValueHolder { Value = Int32.MaxValue }).Value);
        //}

        //--------------------------------------------------------------------------------
        // NullableValue/StructNullable
        //--------------------------------------------------------------------------------

        //[Fact]
        //public void ConvertNullableValueToStructNullable()
        //{
        //    var config = new MapperConfig();
        //    config.CreateMap<NullableInt32Holder, StructNullableValueHolder>();
        //    using var mapper = config.ToMapper();

        //    // TODO 4
        //    Assert.Equal(0, mapper.Map<NullableInt32Holder, StructNullableValueHolder>(new NullableInt32Holder { Value = null }).Value.RawValue);
        //    Assert.Equal(-1, mapper.Map<NullableInt32Holder, StructNullableValueHolder>(new NullableInt32Holder { Value = -1 }).Value.RawValue);
        //    Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, StructNullableValueHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value.RawValue);
        //}

        [Fact]
        public void ConvertStructNullableToNullableValue()
        {
            var config = new MapperConfig();
            config.CreateMap<StructNullableValueHolder, NullableInt32Holder>();
            using var mapper = config.ToMapper();

            Assert.Null(mapper.Map<StructNullableValueHolder, NullableInt32Holder>(new StructNullableValueHolder { Value = null }).Value);
            Assert.Equal(-1, mapper.Map<StructNullableValueHolder, NullableInt32Holder>(new StructNullableValueHolder { Value = -1 }).Value!.Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<StructNullableValueHolder, NullableInt32Holder>(new StructNullableValueHolder { Value = Int32.MaxValue }).Value!.Value);
        }

        // TODO NullableValue/NStruct
        // TODO Value/NullableStruct
        // TODO Value/NullableNStruct
        // TODO NullableValue/NullableStruct
        // TODO NullableValue/NullableNStruct

        //--------------------------------------------------------------------------------
        // ClassPair1/ClassPair2
        //--------------------------------------------------------------------------------

        //[Fact]
        //public void ConvertClassPair1ToClassPair2()
        //{
        //    var config = new MapperConfig();
        //    config.CreateMap<ClassPair1ValueHolder, ClassPair2ValueHolder>();
        //    using var mapper = config.ToMapper();

        //    // TODO
        //    Assert.Null(mapper.Map<ClassPair1ValueHolder, ClassPair2ValueHolder>(new ClassPair1ValueHolder { Value = null }).Value);
        //    Assert.Equal(-1, mapper.Map<ClassPair1ValueHolder, ClassPair2ValueHolder>(new ClassPair1ValueHolder { Value = new ClassPair1Value { RawValue = -1 } }).Value!.RawValue);
        //    Assert.Equal(Int32.MaxValue, mapper.Map<ClassPair1ValueHolder, ClassPair2ValueHolder>(new ClassPair1ValueHolder { Value = new ClassPair1Value { RawValue = Int32.MaxValue } }).Value!.RawValue);
        //}

        //[Fact]
        //public void ConvertClassPair2ToClassPair1()
        //{
        //    var config = new MapperConfig();
        //    config.CreateMap<ClassPair2ValueHolder, ClassPair1ValueHolder>();
        //    using var mapper = config.ToMapper();

        //    // TODO
        //    Assert.Null(mapper.Map<ClassPair2ValueHolder, ClassPair1ValueHolder>(new ClassPair2ValueHolder { Value = null }).Value);
        //    Assert.Equal(-1, mapper.Map<ClassPair2ValueHolder, ClassPair1ValueHolder>(new ClassPair2ValueHolder { Value = new ClassPair2Value { RawValue = -1 } }).Value!.RawValue);
        //    Assert.Equal(Int32.MaxValue, mapper.Map<ClassPair2ValueHolder, ClassPair1ValueHolder>(new ClassPair2ValueHolder { Value = new ClassPair2Value { RawValue = Int32.MaxValue } }).Value!.RawValue);
        //}

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

        // TODO NullableStructPair1Value/StructPair2Value
        // TODO StructPair1Value/NullableStructPair2Value
        // TODO NullableStructPair1Value/NullableStructPair2Value
        // TODO CrossPairClassValue/CrossPairStructValue
        // TODO CrossPairClassValue/NullableCrossPairStructValue
    }
}
