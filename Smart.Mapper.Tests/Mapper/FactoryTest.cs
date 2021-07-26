namespace Smart.Mapper
{
    using System;

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

            var source = new Source { Value = 1 };
            var destination = mapper.Map<Source, Destination>(source);

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void FactoryUsingServiceProviderByDefault()
        {
            var config = new MapperConfig();
            config.Default(opt => opt.FactoryUsingServiceProvider());
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var source = new Source { Value = 1 };
            var destination = mapper.Map<Source, Destination>(source);

            Assert.Equal(1, destination.Value);
        }

        [Fact]
        public void FactoryUsingCustomServiceProvider()
        {
            var config = new MapperConfig();
            config.UseServiceProvider<CustomServiceProvider>();
            config.CreateMap<Source, Destination>().FactoryUsingServiceProvider();
            using var mapper = config.ToMapper();

            var source = new Source { Value = 1 };
            var destination = mapper.Map<Source, Destination>(source);

            Assert.Equal(1, destination.Value);
            Assert.Equal(2, destination.Value2);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int Value { get; set; }
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
    }
}
