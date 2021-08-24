namespace Smart.Mapper
{
    using System;

    using Xunit;

    public partial class ConversionTest
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

        // TODO

        // ...
    }
}
