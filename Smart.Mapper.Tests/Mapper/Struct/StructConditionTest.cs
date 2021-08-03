namespace Smart.Mapper.Struct
{
    using Smart.Mapper.Functions;

    using Xunit;

    public class StructConditionTest
    {
        //--------------------------------------------------------------------------------
        // Condition
        //--------------------------------------------------------------------------------

        [Fact]
        public void ConditionByFunc()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Value, opt => opt.Condition(s => s.Value > 0));
            using var mapper = config.ToMapper();

            var destination1 = mapper.Map<Source, Destination>(new Source { Value = 1 });
            var destination2 = mapper.Map<Source, Destination>(new Source { Value = -1 });

            Assert.Equal(1, destination1.Value);
            Assert.Equal(0, destination2.Value);
        }

        [Fact]
        public void ConditionByFuncWithContext()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Value, opt => opt.Condition((s, c) => (s.Value > 0) && (bool)c.Parameter!));
            using var mapper = config.ToMapper();

            var destination1 = mapper.Map<Source, Destination>(new Source { Value = 1 }, true);
            var destination2 = mapper.Map<Source, Destination>(new Source { Value = 1 }, false);
            var destination3 = mapper.Map<Source, Destination>(new Source { Value = -1 }, true);
            var destination4 = mapper.Map<Source, Destination>(new Source { Value = -1 }, false);

            Assert.Equal(1, destination1.Value);
            Assert.Equal(0, destination2.Value);
            Assert.Equal(0, destination3.Value);
            Assert.Equal(0, destination4.Value);
        }

        [Fact]
        public void ConditionByFuncWithDestinationAndContext()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Value, opt => opt.Condition((s, _, c) => (s.Value > 0) && (bool)c.Parameter!));
            using var mapper = config.ToMapper();

            var destination1 = mapper.Map<Source, Destination>(new Source { Value = 1 }, true);
            var destination2 = mapper.Map<Source, Destination>(new Source { Value = 1 }, false);
            var destination3 = mapper.Map<Source, Destination>(new Source { Value = -1 }, true);
            var destination4 = mapper.Map<Source, Destination>(new Source { Value = -1 }, false);

            Assert.Equal(1, destination1.Value);
            Assert.Equal(0, destination2.Value);
            Assert.Equal(0, destination3.Value);
            Assert.Equal(0, destination4.Value);
        }

        [Fact]
        public void ConditionByMemberCondition()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Value, opt => opt.Condition<CustomMemberCondition>());
            using var mapper = config.ToMapper();

            var destination1 = mapper.Map<Source, Destination>(new Source { Value = 1 }, true);
            var destination2 = mapper.Map<Source, Destination>(new Source { Value = 1 }, false);
            var destination3 = mapper.Map<Source, Destination>(new Source { Value = -1 }, true);
            var destination4 = mapper.Map<Source, Destination>(new Source { Value = -1 }, false);

            Assert.Equal(1, destination1.Value);
            Assert.Equal(0, destination2.Value);
            Assert.Equal(0, destination3.Value);
            Assert.Equal(0, destination4.Value);
        }

        [Fact]
        public void ConditionByMemberCondition2()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Value, opt => opt.Condition(new CustomMemberCondition()));
            using var mapper = config.ToMapper();

            var destination1 = mapper.Map<Source, Destination>(new Source { Value = 1 }, true);
            var destination2 = mapper.Map<Source, Destination>(new Source { Value = 1 }, false);
            var destination3 = mapper.Map<Source, Destination>(new Source { Value = -1 }, true);
            var destination4 = mapper.Map<Source, Destination>(new Source { Value = -1 }, false);

            Assert.Equal(1, destination1.Value);
            Assert.Equal(0, destination2.Value);
            Assert.Equal(0, destination3.Value);
            Assert.Equal(0, destination4.Value);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public struct Source
        {
            public int Value { get; set; }
        }

        public struct Destination
        {
            public int Value { get; set; }
        }

        public sealed class CustomMemberCondition : IMemberCondition<Source, Destination>
        {
            public bool Eval(Source source, Destination destination, ResolutionContext context)
            {
                return (source.Value > 0) && (bool)context.Parameter!;
            }
        }
    }
}
