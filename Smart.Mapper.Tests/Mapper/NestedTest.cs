namespace Smart.Mapper
{
    using Xunit;

    public class NestedTest
    {
        //--------------------------------------------------------------------------------
        // Nested
        //--------------------------------------------------------------------------------

        [Fact]
        public void Nested()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Inner, opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = 1 } });

            // TODO
            Assert.Equal(1, destination.Inner!.Value);
        }

        // TODO Misc...

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
