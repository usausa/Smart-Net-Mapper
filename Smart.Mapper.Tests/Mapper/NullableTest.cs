namespace Smart.Mapper
{
    using Xunit;

    public class NullableTest
    {
        //--------------------------------------------------------------------------------
        // Nested
        //--------------------------------------------------------------------------------

        [Fact]
        public void StructToNullableByStructToStruct()
        {
            var config = new MapperConfig();
            config.CreateMap<StructSource, StructDestination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<StructSource, StructDestination?>(new StructSource { IntValue = 1 });

            Assert.Equal(1, destination!.Value.IntValue);
        }

        //[Fact]
        //public void NullableToNullableByStructToStruct()
        //{
        //    var config = new MapperConfig();
        //    config.CreateMap<StructSource, StructDestination>();
        //    using var mapper = config.ToMapper();

        //    var destination = mapper.Map<StructSource?, StructDestination?>(new StructSource { IntValue = 1 });

        //    Assert.Equal(1, destination!.Value.IntValue);
        //}

        // TODO pattern * pattern
        // TODO null guard

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public struct StructSource
        {
            public int IntValue { get; set; }
        }

        public struct StructDestination
        {
            public int IntValue { get; set; }
        }
    }
}
