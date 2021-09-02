namespace Smart.Mapper
{
    using Xunit;

    public class FieldTest
    {
        //--------------------------------------------------------------------------------
        // Mapper
        //--------------------------------------------------------------------------------

        [Fact]
        public void MapPropertyToField()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, FieldDestination>();
            using var mapper = config.ToMapper();

            Assert.Equal(-1, mapper.Map<Source, FieldDestination>(new Source { Value = -1 }).Value);
        }

        [Fact]
        public void MapFieldToProperty()
        {
            var config = new MapperConfig();
            config.CreateMap<FieldSource, Destination>();
            using var mapper = config.ToMapper();

            Assert.Equal(-1, mapper.Map<FieldSource, Destination>(new FieldSource { Value = -1 }).Value);
        }

        [Fact]
        public void MapFieldToField()
        {
            var config = new MapperConfig();
            config.CreateMap<FieldSource, FieldDestination>();
            using var mapper = config.ToMapper();

            Assert.Equal(-1, mapper.Map<FieldSource, FieldDestination>(new FieldSource { Value = -1 }).Value);
        }

        [Fact]
        public void MapFieldToFieldManual()
        {
            var config = new MapperConfig();
            config.CreateMap<FieldSource, FieldDestination>()
                .ForMember(x => x.Value, opt => opt.MapFrom(x => x.Value));
            using var mapper = config.ToMapper();

            Assert.Equal(-1, mapper.Map<FieldSource, FieldDestination>(new FieldSource { Value = -1 }).Value);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int Value { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore")]
        public class FieldSource
        {
            public int Value;
        }

        public class Destination
        {
            public int Value { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Ignore")]
        public class FieldDestination
        {
            public int Value;
        }
    }
}
