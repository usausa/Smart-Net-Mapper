namespace Smart.Mapper
{
    using Xunit;

    public class StructTest
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
        public void MapByAction()
        {
            using var mapper = CreateMapper();

            var destination = new StructDestination();
            mapper.Map(new StructSource { Value = 1 }, destination);

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void MapByFunc()
        {
            using var mapper = CreateMapper();

            var destination = mapper.Map<StructSource, StructDestination>(new StructSource { Value = 1 });

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void MapByParameterAction()
        {
            using var mapper = CreateMapper();

            var destination = new StructDestination();
            mapper.Map(new StructSource { Value = 1 }, destination, 0);

            Assert.Equal(1, destination.Value);
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

        public class StructSource
        {
            public int Value { get; set; }
        }

        public class StructDestination
        {
            public int Value { get; set; }
        }
    }
}
