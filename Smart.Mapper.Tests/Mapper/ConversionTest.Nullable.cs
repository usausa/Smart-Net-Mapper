namespace Smart.Mapper
{
    using System;

    using Xunit;

    public partial class ConversionTest
    {
        //--------------------------------------------------------------------------------
        // Same
        //--------------------------------------------------------------------------------

        [Fact]
        public void ConvertNullableInt32ToInt32()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableInt32Holder, Int32Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0, mapper.Map<NullableInt32Holder, Int32Holder>(new NullableInt32Holder { Value = null }).Value);
            Assert.Equal(-1, mapper.Map<NullableInt32Holder, Int32Holder>(new NullableInt32Holder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, Int32Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertInt32ToNullableInt32()
        {
            var config = new MapperConfig();
            config.CreateMap<Int32Holder, NullableInt32Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0, mapper.Map<Int32Holder, NullableInt32Holder>(new Int32Holder { Value = 0 }).Value);
            Assert.Equal(-1, mapper.Map<Int32Holder, NullableInt32Holder>(new Int32Holder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, NullableInt32Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertNullableInt32NullableToInt32()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableInt32Holder, NullableInt32Holder>();
            using var mapper = config.ToMapper();

            Assert.Null(mapper.Map<NullableInt32Holder, NullableInt32Holder>(new NullableInt32Holder { Value = null }).Value);
            Assert.Equal(-1, mapper.Map<NullableInt32Holder, NullableInt32Holder>(new NullableInt32Holder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, NullableInt32Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // To Large
        //--------------------------------------------------------------------------------

        [Fact]
        public void ConvertNullableInt32ToInt64()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableInt32Holder, Int64Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0L, mapper.Map<NullableInt32Holder, Int64Holder>(new NullableInt32Holder { Value = null }).Value);
            Assert.Equal(-1L, mapper.Map<NullableInt32Holder, Int64Holder>(new NullableInt32Holder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, Int64Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertInt32ToNullableInt64()
        {
            var config = new MapperConfig();
            config.CreateMap<Int32Holder, NullableInt64Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0L, mapper.Map<Int32Holder, NullableInt64Holder>(new Int32Holder { Value = 0 }).Value);
            Assert.Equal(-1L, mapper.Map<Int32Holder, NullableInt64Holder>(new Int32Holder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, NullableInt64Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertNullableInt32ToNullableInt64()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableInt32Holder, NullableInt64Holder>();
            using var mapper = config.ToMapper();

            Assert.Null(mapper.Map<NullableInt32Holder, NullableInt64Holder>(new NullableInt32Holder { Value = null }).Value);
            Assert.Equal(-1L, mapper.Map<NullableInt32Holder, NullableInt64Holder>(new NullableInt32Holder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, NullableInt64Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // To Small
        //--------------------------------------------------------------------------------

        [Fact]
        public void ConvertNullableInt32ToInt16()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableInt32Holder, Int16Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0, mapper.Map<NullableInt32Holder, Int16Holder>(new NullableInt32Holder { Value = null }).Value);
            Assert.Equal(-1, mapper.Map<NullableInt32Holder, Int16Holder>(new NullableInt32Holder { Value = -1 }).Value);
            Assert.Equal(unchecked((short)Int32.MaxValue), mapper.Map<NullableInt32Holder, Int16Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertInt32ToNullableInt16()
        {
            var config = new MapperConfig();
            config.CreateMap<Int32Holder, NullableInt16Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal((short)0, mapper.Map<Int32Holder, NullableInt16Holder>(new Int32Holder { Value = 0 }).Value);
            Assert.Equal((short)-1, mapper.Map<Int32Holder, NullableInt16Holder>(new Int32Holder { Value = -1 }).Value);
            Assert.Equal(unchecked((short)Int32.MaxValue), mapper.Map<Int32Holder, NullableInt16Holder>(new Int32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertNullableInt32ToNullableInt16()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableInt32Holder, NullableInt16Holder>();
            using var mapper = config.ToMapper();

            Assert.Null(mapper.Map<NullableInt32Holder, NullableInt16Holder>(new NullableInt32Holder { Value = null }).Value);
            Assert.Equal((short)-1, mapper.Map<NullableInt32Holder, NullableInt16Holder>(new NullableInt32Holder { Value = -1 }).Value);
            Assert.Equal(unchecked((short)Int32.MaxValue), mapper.Map<NullableInt32Holder, NullableInt16Holder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
        }

        //--------------------------------------------------------------------------------
        // Extra
        //--------------------------------------------------------------------------------

        [Fact]
        public void ConvertNullableInt32ToDecimal()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableInt32Holder, DecimalHolder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0m, mapper.Map<NullableInt32Holder, DecimalHolder>(new NullableInt32Holder { Value = null }).Value);
            Assert.Equal(1m, mapper.Map<NullableInt32Holder, DecimalHolder>(new NullableInt32Holder { Value = 1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<NullableInt32Holder, DecimalHolder>(new NullableInt32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertInt32ToNullableDecimal()
        {
            var config = new MapperConfig();
            config.CreateMap<Int32Holder, NullableDecimalHolder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0m, mapper.Map<Int32Holder, NullableDecimalHolder>(new Int32Holder { Value = 0 }).Value);
            Assert.Equal(1m, mapper.Map<Int32Holder, NullableDecimalHolder>(new Int32Holder { Value = 1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, NullableDecimalHolder>(new Int32Holder { Value = Int32.MaxValue }).Value);
        }

        [Fact]
        public void ConvertDecimalToNullableInt32()
        {
            var config = new MapperConfig();
            config.CreateMap<DecimalHolder, NullableInt32Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0, mapper.Map<DecimalHolder, NullableInt32Holder>(new DecimalHolder { Value = 0m }).Value);
            Assert.Equal(1, mapper.Map<DecimalHolder, NullableInt32Holder>(new DecimalHolder { Value = 1m }).Value);
            Assert.Equal(-1, mapper.Map<DecimalHolder, NullableInt32Holder>(new DecimalHolder { Value = -1m }).Value);
        }

        [Fact]
        public void ConvertNullableDecimalToInt32()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableDecimalHolder, Int32Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(0, mapper.Map<NullableDecimalHolder, Int32Holder>(new NullableDecimalHolder { Value = null }).Value);
            Assert.Equal(1, mapper.Map<NullableDecimalHolder, Int32Holder>(new NullableDecimalHolder { Value = 1m }).Value);
            Assert.Equal(-1, mapper.Map<NullableDecimalHolder, Int32Holder>(new NullableDecimalHolder { Value = -1m }).Value);
        }
    }
}
