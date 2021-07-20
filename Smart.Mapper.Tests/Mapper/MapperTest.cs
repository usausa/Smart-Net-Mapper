namespace Smart.Mapper
{
    using Xunit;

    public class MapperTest
    {
        private static ObjectMapper CreateMapper()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>();
            return config.ToMapper();
        }

        [Fact]
        public void MapByAction()
        {
            using var mapper = CreateMapper();

            var source = new Source { Value = 1 };
            var destination = new Destination();
            mapper.Map(source, destination);

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void MapByFunc()
        {
            using var mapper = CreateMapper();

            var source = new Source { Value = 1 };
            var destination = mapper.Map<Source, Destination>(source);

            Assert.Equal(1, destination.Value);
        }

        public class Source
        {
            public int Value { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }
        }
    }
}
