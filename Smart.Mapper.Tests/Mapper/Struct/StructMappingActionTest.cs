namespace Smart.Mapper.Struct
{
    using Smart.Mapper.Functions;

    using Xunit;

    public class StructMappingActionTest
    {
        //--------------------------------------------------------------------------------
        // BeforeMap
        //--------------------------------------------------------------------------------

        [Fact]
        public void BeforeMapFuncNotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap((s, d) => d.ValueDestinationOnly = s.ValueSourceOnly);
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        [Fact]
        public void BeforeMapFuncWithContextNotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap((_, d, c) => d.ValueDestinationOnly = (int)c.Parameter!);
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, -1);

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        [Fact]
        public void BeforeMapMappingActionNotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap<CustomMappingAction>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        [Fact]
        public void BeforeMapMappingAction2NotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap(new CustomMappingAction());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        //--------------------------------------------------------------------------------
        // AfterMap
        //--------------------------------------------------------------------------------

        [Fact]
        public void AfterMapFuncNotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap((s, d) => d.ValueDestinationOnly = s.ValueSourceOnly);
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        [Fact]
        public void AfterMapFuncWithContextNotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap((_, d, c) => d.ValueDestinationOnly = (int)c.Parameter!);
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, -1);

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        [Fact]
        public void AfterMapMappingActionNotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap<CustomMappingAction>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        [Fact]
        public void AfterMapMappingAction2NotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .AfterMap(new CustomMappingAction());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        //--------------------------------------------------------------------------------
        // Chain
        //--------------------------------------------------------------------------------

        [Fact]
        public void MappingActionChainNotWork()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .BeforeMap((_, d) => d.ValueDestinationOnly -= 1)
                .BeforeMap((_, d, _) => d.ValueDestinationOnly -= 10)
                .AfterMap((_, d) => d.ValueDestinationOnly -= 100)
                .AfterMap((_, d, _) => d.ValueDestinationOnly -= 1000);
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(default);

            // Copy not work
            Assert.Equal(0, destination.ValueDestinationOnly);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public struct Source
        {
            public int Value { get; set; }

            public int ValueSourceOnly { get; set; }
        }

        public struct Destination
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
