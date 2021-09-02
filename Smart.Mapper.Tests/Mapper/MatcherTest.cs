namespace Smart.Mapper
{
    using System;

    using Xunit;

    public class MatcherTest
    {
        //--------------------------------------------------------------------------------
        // Match
        //--------------------------------------------------------------------------------

        [Fact]
        public void MatchPropertyName()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .MatchMember(d => d.EndsWith("Ignore", StringComparison.Ordinal) ? null : "Source" + d[11..]);
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source { SourceValue1 = -1, SourceValue2 = -2, SourceValueIgnore = -3 });

            Assert.Equal(-1, destination.DestinationValue1);
            Assert.Equal(-2, destination.DestinationValue2);
            Assert.Equal(0, destination.DestinationValueIgnore);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
            public int SourceValue1 { get; set; }

            public int SourceValue2 { get; set; }

            public int SourceValueIgnore { get; set; }
        }

        public class Destination
        {
            public int DestinationValue1 { get; set; }

            public int DestinationValue2 { get; set; }

            public int DestinationValueIgnore { get; set; }
        }
    }
}
