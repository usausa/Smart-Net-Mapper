namespace Smart.Mapper
{
    using Xunit;

    public class NestedTest
    {
        private const string Profile = "sub";

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

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value);
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

            var destination = mapper.Map<Source2, Destination2>(new Source2 { Inner = new StructSourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value.Value);
        }

        [Fact]
        public void NestedNullableNull()
        {
            var config = new MapperConfig();
            config.CreateMap<StructSourceInner, StructDestinationInner>();
            config.CreateMap<Source2, Destination2>()
                .ForMember(x => x.Inner, opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source2, Destination2>(new Source2 { Inner = null });

            Assert.Null(destination.Inner);
        }

        //--------------------------------------------------------------------------------
        // Profile
        //--------------------------------------------------------------------------------

        [Fact]
        public void ProfileNested()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>(Profile);
            config.CreateMap<Source, Destination>(Profile)
                .ForMember(x => x.Inner, opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(Profile, new Source { Inner = new SourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value);
        }

        [Fact]
        public void ProfileNestedNull()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>(Profile);
            config.CreateMap<Source, Destination>(Profile)
                .ForMember(x => x.Inner, opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(Profile, new Source { Inner = null });

            Assert.Null(destination.Inner);
        }

        //--------------------------------------------------------------------------------
        // All
        //--------------------------------------------------------------------------------

        [Fact]
        public void NestedAll()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>(Profile);
            config.CreateMap<Source, Destination>(Profile)
                .ForAllMember(opt => opt.Nested());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(Profile, new Source { Inner = new SourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value);
        }

        //--------------------------------------------------------------------------------
        // Manual
        //--------------------------------------------------------------------------------

        [Fact]
        public void ManualNestByMap()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) =>
                {
                    d.Inner = new DestinationInner();
                    c.Mapper.Map(s.Inner, d.Inner);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) => d.Inner = c.Mapper.Map<SourceInner?, DestinationInner>(s.Inner));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByParameterMap()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>()
                .ForMember(d => d.Value, opt => opt.MapFrom((s, _, c) => s.Value + (int)c.Parameter!));
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) =>
                {
                    d.Inner = new DestinationInner();
                    c.Mapper.Map(s.Inner, d.Inner, c.Parameter);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } }, -1);

            Assert.Equal(-2, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByParameterFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>()
                .ForMember(d => d.Value, opt => opt.MapFrom((s, _, c) => s.Value + (int)c.Parameter!));
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) =>
                {
                    d.Inner = c.Mapper.Map<SourceInner?, DestinationInner>(s.Inner, c.Parameter);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } }, -1);

            Assert.Equal(-2, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByCacheMap()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) =>
                {
                    var map = c.Mapper.GetMapperAction<SourceInner?, DestinationInner>();
                    d.Inner = new DestinationInner();
                    map(s.Inner, d.Inner);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByCacheFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>();
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) =>
                {
                    var map = c.Mapper.GetMapperFunc<SourceInner?, DestinationInner>();
                    d.Inner = map(s.Inner);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } });

            Assert.Equal(-1, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByCacheParameterMap()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>()
                .ForMember(d => d.Value, opt => opt.MapFrom((s, _, c) => s.Value + (int)c.Parameter!));
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) =>
                {
                    var map = c.Mapper.GetParameterMapperAction<SourceInner?, DestinationInner>();
                    d.Inner = new DestinationInner();
                    map(s.Inner, d.Inner, c.Parameter);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } }, -1);

            Assert.Equal(-2, destination.Inner!.Value);
        }

        [Fact]
        public void ManualNestByCacheParameterFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<SourceInner, DestinationInner>()
                .ForMember(d => d.Value, opt => opt.MapFrom((s, _, c) => s.Value + (int)c.Parameter!));
            config.CreateMap<Source, Destination>()
                .ForMember(d => d.Inner, opt => opt.Ignore())
                .AfterMap((s, d, c) =>
                {
                    var map = c.Mapper.GetParameterMapperFunc<SourceInner?, DestinationInner>();
                    d.Inner = map(s.Inner, c.Parameter);
                });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Inner = new SourceInner { Value = -1 } }, -1);

            Assert.Equal(-2, destination.Inner!.Value);
        }

        // TODO manual profile

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
