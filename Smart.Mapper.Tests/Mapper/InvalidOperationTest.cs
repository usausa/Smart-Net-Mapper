namespace Smart.Mapper;

using System;

using Xunit;

public class InvalidOperationTest
{
    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    [Fact]
    public void NoDefaultConstructorDestinationIsNotSupported()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, NoDefaultConstructorDestination>();
        using var mapper = config.ToMapper();

        Assert.Throws<InvalidOperationException>(() =>
            mapper.Map<Source, NoDefaultConstructorDestination>(new Source()));
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public class Source
    {
    }

    public class NoDefaultConstructorDestination
    {
        public int Value { get; set; }

        public NoDefaultConstructorDestination(int value)
        {
            Value = value;
        }
    }
}
