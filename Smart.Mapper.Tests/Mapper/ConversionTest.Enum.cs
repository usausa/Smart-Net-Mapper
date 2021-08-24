namespace Smart.Mapper
{
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

        // TODO

        // ...
    }
}
