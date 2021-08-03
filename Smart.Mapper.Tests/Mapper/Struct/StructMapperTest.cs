namespace Smart.Mapper.Struct
{
    using Xunit;

    public class StructMapperTest
    {
        //--------------------------------------------------------------------------------
        // Mapper
        //--------------------------------------------------------------------------------

        private static ObjectMapper CreateMapper()
        {
            var config = new MapperConfig();
            config.CreateMap<StructSource, StructDestination>();
            return config.ToMapper();
        }

        [Fact]
        public void MapByActionNotWork()
        {
            using var mapper = CreateMapper();

            var destination = default(StructDestination);
            mapper.Map(new StructSource { Value = 1 }, destination);

            // Copy not work
            Assert.Equal(0, destination.Value);
        }

        [Fact]
        public void MapByFunc()
        {
            using var mapper = CreateMapper();

            var destination = mapper.Map<StructSource, StructDestination>(new StructSource { Value = 1 });

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void MapByParameterActionNotWork()
        {
            using var mapper = CreateMapper();

            var destination = default(StructDestination);
            mapper.Map(new StructSource { Value = 1 }, destination, 0);

            // Copy not work
            Assert.Equal(0, destination.Value);
        }

        [Fact]
        public void MapByParameterFunc()
        {
            using var mapper = CreateMapper();

            var destination = mapper.Map<StructSource, StructDestination>(new StructSource { Value = 1 }, 0);

            Assert.Equal(1, destination.Value);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public struct StructSource
        {
            public int Value { get; set; }
        }

        public struct StructDestination
        {
            public int Value { get; set; }
        }
    }
}
