namespace Smart.Mapper
{
    using System;

    using Smart.Mapper.Functions;

    using Xunit;

    public class FactoryTest
    {
        //--------------------------------------------------------------------------------
        // ServiceProvider
        //--------------------------------------------------------------------------------

        [Fact]
        public void FactoryUsingDefaultServiceProvider()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsingServiceProvider();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

            Assert.Equal(-1, destination.Value);
        }

        [Fact]
        public void FactoryUsingServiceProviderByDefault()
        {
            var config = new MapperConfig();
            config.Default(opt => opt.FactoryUsingServiceProvider());
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

            Assert.Equal(-1, destination.Value);
        }

        [Fact]
        public void FactoryUsingCustomServiceProvider()
        {
            var config = new MapperConfig();
            config.UseServiceProvider<CustomServiceProvider>();
            config.CreateMap<Source, Destination>().FactoryUsingServiceProvider();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void FactoryUsingCustomServiceProvider2()
        {
            var config = new MapperConfig();
            config.UseServiceProvider(new CustomServiceProvider());
            config.CreateMap<Source, Destination>().FactoryUsingServiceProvider();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-2, destination.ValueDestinationOnly);
        }

        //--------------------------------------------------------------------------------
        // Factory
        //--------------------------------------------------------------------------------

        [Fact]
        public void FactoryUsingDefaultFunc()
        {
            var config = new MapperConfig()
                .Default(opt => opt.FactoryUsing(() => new Destination { ValueDestinationOnly = -2 }));
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void FactoryUsingFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing(() => new Destination { ValueDestinationOnly = -2 });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 });

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void FactoryUsingFuncWithSource()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing(s => new Destination { ValueDestinationOnly = s.ValueSourceOnly });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void FactoryUsingFuncWithContext()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing((_, c) => new Destination { ValueDestinationOnly = (int)c.Parameter! });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1 }, -1);

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-1, destination.ValueDestinationOnly);
        }

        [Fact]
        public void FactoryUsingObjectFactory()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing<CustomObjectFactory>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 });

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-2, destination.ValueDestinationOnly);
        }

        [Fact]
        public void FactoryUsingObjectFactory2()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing(new CustomObjectFactory());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = -1, ValueSourceOnly = -2 }, -1);

            Assert.Equal(-1, destination.Value);
            Assert.Equal(-1, destination.ValueDestinationOnly);
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

        public sealed class CustomServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => new Destination { ValueDestinationOnly = -2 };
        }

        public sealed class CustomObjectFactory : IObjectFactory<Source, Destination>
        {
#pragma warning disable CA1508
            public Destination Create(Source source, ResolutionContext context) =>
                new() { ValueDestinationOnly = (int?)context.Parameter ?? source.ValueSourceOnly };
#pragma warning restore CA1508 // 使用されない条件付きコードを回避する
        }
    }
}
