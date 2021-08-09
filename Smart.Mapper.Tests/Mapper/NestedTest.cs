namespace Smart.Mapper
{
    using Xunit;

    public class NestedTest
    {
        //--------------------------------------------------------------------------------
        // Nested
        //--------------------------------------------------------------------------------

        [Fact]
        public void NestedClass()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Inner, opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = 1 } });

            Assert.Equal(1, destination.Inner!.Value);
        }

        [Fact]
        public void NestedClassNull()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Inner, opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = null });

            Assert.Null(destination.Inner);
        }

        // TODO nullable, struct, manual

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class SourceInner
        {
            public int Value { get; set; }
        }

        public class DestinationInner
        {
            public int Value { get; set; }
        }

        public class Source
        {
            public SourceInner? Inner { get; set; }
        }

        public class Destination
        {
            public DestinationInner? Inner { get; set; }
        }
    }
}
