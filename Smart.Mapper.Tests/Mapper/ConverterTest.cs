namespace Smart.Mapper
{
    using Xunit;

    public class ConverterTest
    {
        //--------------------------------------------------------------------------------
        // Order
        //--------------------------------------------------------------------------------

        [Fact]
        public void UseConverter()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal("1", destination.Value);
        }

        // TODO

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int Value { get; set; }
        }

        public class Destination
        {
            public string? Value { get; set; }
        }
    }
}
