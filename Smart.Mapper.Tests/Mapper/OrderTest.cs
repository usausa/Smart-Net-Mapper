namespace Smart.Mapper
{
    using Xunit;

    public class OrderTest
    {
        //--------------------------------------------------------------------------------
        // Order
        //--------------------------------------------------------------------------------

        [Fact]
        public void OrderByForMember()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.Value1, opt => opt.Order(1))
                .ForMember(x => x.Value2, opt => opt.Order(0));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source());

            Assert.Equal(1, destination.Value1Order);
            Assert.Equal(0, destination.Value2Order);
        }

        [Fact]
        public void OrderByForAllMember()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForAllMember(opt => opt.Order(opt.DestinationMember.Name == "Value1" ? 1 : 0));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source());

            Assert.Equal(1, destination.Value1Order);
            Assert.Equal(0, destination.Value2Order);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int Value1 { get; set; }

            public int Value2 { get; set; }
        }

        public class Destination
        {
            private int order;

            private int value1;

            private int value2;

            public int Value1Order { get; private set; }

            public int Value2Order { get; private set; }

            public int Value1
            {
                get => value1;
                set
                {
                    Value1Order = order++;
                    value1 = value;
                }
            }

            public int Value2
            {
                get => value2;
                set
                {
                    Value2Order = order++;
                    value2 = value;
                }
            }
        }
    }
}
