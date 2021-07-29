namespace Smart.Mapper
{
    using Smart.Mapper.Functions;

    using Xunit;

    public class MappingActionTest
    {
        //--------------------------------------------------------------------------------
        // BeforeMap
        //--------------------------------------------------------------------------------

        [Fact]
        public void BeforeMapFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap((s, d) => { d.ValueDestinationOnly = s.ValueSourceOnly; });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, ValueSourceOnly = 2 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void BeforeMapFuncWithContext()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap((_, d, c) => { d.ValueDestinationOnly = (int)c.Parameter!; });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, -1);

            Assert.Equal(1, destination.Value);
            Assert.Equal(-1, destination.ValueDestinationOnly);
        }

        [Fact]
        public void BeforeMapMappingAction()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap<CustomMappingAction>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, ValueSourceOnly = 2 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void BeforeMapMappingAction2()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap(new CustomMappingAction());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, ValueSourceOnly = 2 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.ValueDestinationOnly);
        }

        //--------------------------------------------------------------------------------
        // AfterMap
        //--------------------------------------------------------------------------------

        [Fact]
        public void AfterMapFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap((s, d) => { d.ValueDestinationOnly = s.ValueSourceOnly; });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, ValueSourceOnly = 2 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void AfterMapFuncWithContext()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap((_, d, c) => { d.ValueDestinationOnly = (int)c.Parameter!; });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, -1);

            Assert.Equal(1, destination.Value);
            Assert.Equal(-1, destination.ValueDestinationOnly);
        }

        [Fact]
        public void AfterMapMappingAction()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap<CustomMappingAction>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, ValueSourceOnly = 2 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void AfterMapMappingAction2()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap(new CustomMappingAction());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, ValueSourceOnly = 2 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.ValueDestinationOnly);
        }

        //--------------------------------------------------------------------------------
        // Chain
        //--------------------------------------------------------------------------------

        [Fact]
        public void MappingActionChain()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap((_, d) => { d.ValueDestinationOnly += 1; })
                .BeforeMap((_, d, _) => { d.ValueDestinationOnly += 10; })
                .AfterMap((_, d) => { d.ValueDestinationOnly += 100; })
                .AfterMap((_, d, _) => { d.ValueDestinationOnly += 1000; });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source());

            Assert.Equal(1111, destination.ValueDestinationOnly);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int Value { get; set; }

            public int ValueSourceOnly { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }

            public int ValueDestinationOnly { get; set; }
        }

        public sealed class CustomMappingAction : IMappingAction<Source, Destination>
        {
#pragma warning disable CA1508
            public void Process(Source source, Destination destination, ResolutionContext context)
            {
                destination.ValueDestinationOnly = (int?)context.Parameter ?? source.ValueSourceOnly;
            }
#pragma warning restore CA1508
        }
    }
}
