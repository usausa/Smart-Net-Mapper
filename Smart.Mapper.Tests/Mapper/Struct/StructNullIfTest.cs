namespace Smart.Mapper.Struct
{
    using Xunit;

    public class StructNullIfTest
    {
        //--------------------------------------------------------------------------------
        // MapFrom
        //--------------------------------------------------------------------------------

        [Fact]
        public void NullIfForClass()
        {
            var config = new MapperConfig();
            config.CreateMap<ClassSource, ClassDestination>()
                .ForMember(d => d.Value, opt => opt.NullIf("-"));
            using var mapper = config.ToMapper();

            var destination1 = mapper.Map<ClassSource, ClassDestination>(new ClassSource { Value = "x" });
            Assert.Equal("x", destination1.Value);

            var destination2 = mapper.Map<ClassSource, ClassDestination>(default);
            Assert.Equal("-", destination2.Value);
        }

        [Fact]
        public void NullIfForNullable()
        {
            var config = new MapperConfig();
            config.CreateMap<NullableSource, NullableDestination>()
                .ForMember(d => d.Value, opt => opt.NullIf(-1));
            using var mapper = config.ToMapper();

            var destination1 = mapper.Map<NullableSource, NullableDestination>(new NullableSource { Value = 1 });
            Assert.Equal(1, destination1.Value);

            var destination2 = mapper.Map<NullableSource, NullableDestination>(default);
            Assert.Equal(-1, destination2.Value);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public struct ClassSource
        {
            public string? Value { get; set; }
        }

        public struct ClassDestination
        {
            public string? Value { get; set; }
        }

        public struct NullableSource
        {
            public int? Value { get; set; }

            public string? StringValue { get; set; }
        }

        public struct NullableDestination
        {
            public int? Value { get; set; }
        }
    }
}
