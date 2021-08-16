namespace Smart.Mapper
{
    using Smart.Mapper.Functions;

    using Xunit;

    public class ConverterTest
    {
        //--------------------------------------------------------------------------------
        // Default
        //--------------------------------------------------------------------------------

        [Fact]
        public void UseConverter()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal("1", destination.Value);
        }

        //--------------------------------------------------------------------------------
        // Default option
        //--------------------------------------------------------------------------------

        [Fact]
        public void UseConverterByDefaultFunc()
        {
            var config = new MapperConfig();
            config.Default(opt => opt.ConvertUsing<int, string>(x => $"#{x}"));
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal("#1", destination.Value);
        }

        [Fact]
        public void UseConverterByDefaultFuncContext()
        {
            var config = new MapperConfig();
            config.Default(opt => opt.ConvertUsing<int, string>((x, c) => $"{c.Parameter}{x}"));
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, "#");

            Assert.Equal("#1", destination.Value);
        }

        [Fact]
        public void UseConverterByDefaultConverter()
        {
            var config = new MapperConfig();
            config.Default(opt => opt.ConvertUsing<int, string, CustomValueConverter>());
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, "#");

            Assert.Equal("#1", destination.Value);
        }

        [Fact]
        public void UseConverterByDefaultConverter2()
        {
            var config = new MapperConfig();
            config.Default(opt => opt.ConvertUsing(new CustomValueConverter()));
            config.CreateMap<Source, Destination>();
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, "#");

            Assert.Equal("#1", destination.Value);
        }

        // TODO

        //--------------------------------------------------------------------------------
        // Mapping option
        //--------------------------------------------------------------------------------

        [Fact]
        public void UseConverterByMappingFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .Default(opt => opt.ConvertUsing<int, string>(x => $"#{x}"));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 });

            Assert.Equal("#1", destination.Value);
        }

        [Fact]
        public void UseConverterByMappingFuncContext()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .Default(opt => opt.ConvertUsing<int, string>((x, c) => $"{c.Parameter}{x}"));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, "#");

            Assert.Equal("#1", destination.Value);
        }

        [Fact]
        public void UseConverterByMappingConverter()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .Default(opt => opt.ConvertUsing<int, string, CustomValueConverter>());
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, "#");

            Assert.Equal("#1", destination.Value);
        }

        [Fact]
        public void UseConverterByMappingConverter2()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .Default(opt => opt.ConvertUsing(new CustomValueConverter()));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { Value = 1 }, "#");

            Assert.Equal("#1", destination.Value);
        }

        // TODO

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int Value { get; set; }
        }

        public class Destination
        {
            public string? Value { get; set; }
        }

        private sealed class CustomValueConverter : IValueConverter<int, string>
        {
            public string Convert(int value, ResolutionContext context) => $"{context.Parameter}{value}";
        }
    }
}
