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

        //--------------------------------------------------------------------------------
        // Nullable
        //--------------------------------------------------------------------------------

        [Fact]
        public void NestedNullable()
        {
            var config = new MapperConfig();
            config.CreateMap<StructSourceInner, StructDestinationInner>();
            config.CreateMap<Source2, Destination2>()
                .ForMember(x => x.Inner, opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source2, Destination2>(new Source2 { Inner = new StructSourceInner { Value = 1 } });

            Assert.Equal(1, destination.Inner!.Value.Value);
        }

        // TODO

        //--------------------------------------------------------------------------------
        // Manual
        //--------------------------------------------------------------------------------

        [Fact]
        public void ManualNestByMap()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .AfterMap((s, d, c) =>
                {
                    d.Inner = new DestinationInner();
                    c.Mapper.Map(s.Inner, d.Inner);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = 1 } });

            Assert.Equal(1, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .AfterMap((s, d, c) => d.Inner = c.Mapper.Map<SourceInner, DestinationInner>(s.Inner!));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = 1 } });

            Assert.Equal(1, destination.Inner!.Value);
        }

        // TODO manual

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        // Class

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

        // Struct

        public struct StructSourceInner
        {
            public int Value { get; set; }
        }

        public struct StructDestinationInner
        {
            public int Value { get; set; }
        }

        public class Source2
        {
            public StructSourceInner? Inner { get; set; }
        }

        public class Destination2
        {
            public StructDestinationInner? Inner { get; set; }
        }
    }
}
