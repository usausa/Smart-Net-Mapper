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

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void FactoryUsingServiceProviderByDefault()
        {
            var config = new MapperConfig();
            config.Default(opt => opt.FactoryUsingServiceProvider());
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void FactoryUsingCustomServiceProvider()
        {
            var config = new MapperConfig();
            config.UseServiceProvider<CustomServiceProvider>();
            config.CreateMap<Source, Destination>().FactoryUsingServiceProvider();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.Value2);
        }

        //--------------------------------------------------------------------------------
        // Factory
        //--------------------------------------------------------------------------------

        [Fact]
        public void FactoryUsingDefaultFunc()
        {
            var config = new MapperConfig()
                .Default(opt => opt.FactoryUsing(() => new Destination { Value2 = 2 }));
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.Value2);
        }

        [Fact]
        public void FactoryUsingFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing(() => new Destination { Value2 = 2 });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.Value2);
        }

        [Fact]
        public void FactoryUsingFuncWithSource()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing(s => new Destination { Value2 = s.Value3 });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, Value3 = 3 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(3, destination.Value2);
        }

        [Fact]
        public void FactoryUsingFuncWithContext()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing((_, c) => new Destination { Value2 = (int)c.Parameter! });
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, -1);

            Assert.Equal(1, destination.Value);
            Assert.Equal(-1, destination.Value2);
        }

        [Fact]
        public void FactoryUsingObjectFactory()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing<CustomObjectFactory>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, Value3 = 3 });

            Assert.Equal(1, destination.Value);
            Assert.Equal(3, destination.Value2);
        }

        [Fact]
        public void FactoryUsingObjectFactory2()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>().FactoryUsing<CustomObjectFactory>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1, Value3 = 3 }, -1);

            Assert.Equal(1, destination.Value);
            Assert.Equal(-1, destination.Value2);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int Value { get; set; }

            public int Value3 { get; set; }
        }

        public class Destination
        {
            public int Value { get; set; }

            public int Value2 { get; set; }
        }

        public sealed class CustomServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => new Destination { Value2 = 2 };
        }

        public sealed class CustomObjectFactory : IObjectFactory<Source, Destination>
        {
            public Destination Create(Source source, ResolutionContext context) =>
                new() { Value2 = (int?)context.Parameter ?? source.Value3 };
        }
    }
}
