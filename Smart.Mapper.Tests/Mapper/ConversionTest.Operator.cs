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

            Assert.Equal(-1, mapper.Map<Int32Holder, ClassValueHolder>(new Int32Holder { Value = -1 }).Value!.RawValue);
            Assert.Equal(Int32.MaxValue, mapper.Map<Int32Holder, ClassValueHolder>(new Int32Holder { Value = Int32.MaxValue }).Value!.RawValue);
        }

        [Fact]
        public void ConvertClassToValue()
        {
            var config = new MapperConfig();
            config.CreateMap<ClassValueHolder, Int32Holder>();
            using var mapper = config.ToMapper();

            Assert.Equal(-1, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = -1 }).Value);
            Assert.Equal(Int32.MaxValue, mapper.Map<ClassValueHolder, Int32Holder>(new ClassValueHolder { Value = Int32.MaxValue }).Value);
        }
    }
}
