namespace Smart.Mapper.Struct;

using System;

using Xunit;

public class StructConstTest
{
    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    [Fact]
    public void ConstByForMember()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .ForMember(x => x.StringValue, opt => opt.Const("x"))
            .ForMember(x => x.IntValue, opt => opt.Const(1))
            .ForMember(x => x.EnumValue, opt => opt.Const(MyEnum.One))
            .ForMember(x => x.DateTimeValue, opt => opt.Const(new DateTime(2000, 1, 1)))
            .ForMember(x => x.NullableIntValue, opt => opt.Const(2))
            .ForMember(x => x.NullableEnumValue, opt => opt.Const(MyEnum.Two))
            .ForMember(x => x.NullableDateTimeValue, opt => opt.Const(new DateTime(2000, 12, 31)));
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(default);

        Assert.Equal("x", destination.StringValue);
        Assert.Equal(1, destination.IntValue);
        Assert.Equal(MyEnum.One, destination.EnumValue);
        Assert.Equal(new DateTime(2000, 1, 1), destination.DateTimeValue);
        Assert.Equal(2, destination.NullableIntValue);
        Assert.Equal(MyEnum.Two, destination.NullableEnumValue);
        Assert.Equal(new DateTime(2000, 12, 31), destination.NullableDateTimeValue);
    }

    [Fact]
    public void ConstByMappingDefault()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, Destination>()
            .Default(opt =>
            {
                opt.Const("x");
                opt.Const(1);
                opt.Const(MyEnum.One);
                opt.Const(new DateTime(2000, 1, 1));
                opt.Const<int?>(2);
                opt.Const<MyEnum?>(MyEnum.Two);
                opt.Const<DateTime?>(new DateTime(2000, 12, 31));
            });
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(default);

        Assert.Equal("x", destination.StringValue);
        Assert.Equal(1, destination.IntValue);
        Assert.Equal(MyEnum.One, destination.EnumValue);
        Assert.Equal(new DateTime(2000, 1, 1), destination.DateTimeValue);
        Assert.Equal(2, destination.NullableIntValue);
        Assert.Equal(MyEnum.Two, destination.NullableEnumValue);
        Assert.Equal(new DateTime(2000, 12, 31), destination.NullableDateTimeValue);
    }

    [Fact]
    public void ConstByDefault()
    {
        var config = new MapperConfig();
        config.Default(opt =>
        {
            opt.Const("x");
            opt.Const(1);
            opt.Const(MyEnum.One);
            opt.Const(new DateTime(2000, 1, 1));
            opt.Const<int?>(2);
            opt.Const<MyEnum?>(MyEnum.Two);
            opt.Const<DateTime?>(new DateTime(2000, 12, 31));
        });
        config.CreateMap<Source, Destination>();
        using var mapper = config.ToMapper();

        var destination = mapper.Map<Source, Destination>(default);

        Assert.Equal("x", destination.StringValue);
        Assert.Equal(1, destination.IntValue);
        Assert.Equal(MyEnum.One, destination.EnumValue);
        Assert.Equal(new DateTime(2000, 1, 1), destination.DateTimeValue);
        Assert.Equal(2, destination.NullableIntValue);
        Assert.Equal(MyEnum.Two, destination.NullableEnumValue);
        Assert.Equal(new DateTime(2000, 12, 31), destination.NullableDateTimeValue);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public struct Source
    {
    }

    public struct Destination
    {
        public string? StringValue { get; set; }

        public int IntValue { get; set; }

        public MyEnum EnumValue { get; set; }

        public DateTime DateTimeValue { get; set; }

        public int? NullableIntValue { get; set; }

        public MyEnum? NullableEnumValue { get; set; }

        public DateTime? NullableDateTimeValue { get; set; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Ignore")]
    public enum MyEnum
    {
        Zero,
        One,
        Two
    }
}
