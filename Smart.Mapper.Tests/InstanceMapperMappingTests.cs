namespace Smart.Mapper;

using System.Globalization;

// An instance mapper calls the instance methods of its class, which use its fields: the callbacks, a converter, a
// condition, a [MapUsing] method, a nested mapper, and an expression
public sealed partial class InstanceMapperMappingTests
{
    [Fact]
    public void InstanceMethodsUseFieldsOfMapper()
    {
        var mapper = new OrderMapper("No.", 10);
        var source = new Order { Id = 7, Amount = 3, Note = "note", Customer = new Customer { Name = "Sato" } };

        var destination = mapper.Map(source);

        Assert.Equal("No.7", destination.Code);
        Assert.Equal(30, destination.Amount);
        Assert.Equal(1, destination.Sequence);
        Assert.Equal(17, destination.Total);
        Assert.Equal("No.Sato", destination.Customer.Name);
        Assert.Equal(string.Empty, destination.Note);
        Assert.Equal("No.", destination.Prefix);
        Assert.Equal(7, mapper.LastId);
    }

    [Fact]
    public void InstanceMapperKeepsItsStateAcrossCalls()
    {
        var mapper = new OrderMapper("A", 1);

        mapper.Map(new Order());
        var destination = mapper.Map(new Order());

        Assert.Equal(2, destination.Sequence);
    }

    [Fact]
    public void VoidInstanceMapperCallsBeforeMap()
    {
        var mapper = new OrderMapper("B", 1);
        var destination = new OrderDto();

        mapper.Fill(new Order { Id = 3 }, destination);

        Assert.Equal("B-before3", destination.Note);
        Assert.Equal("B3", destination.Code);
    }

    [Fact]
    public void InstanceMapperTakesCultureParameter()
    {
        var mapper = new OrderMapper(string.Empty, 1);

        var german = mapper.Format(new Price { Value = 1234.5m }, CultureInfo.GetCultureInfo("de-DE"));
        var english = mapper.Format(new Price { Value = 1234.5m }, CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal("1234,5", german.Value);
        Assert.Equal("1234.5", english.Value);
    }

    public sealed class Customer
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class Order
    {
        public int Id { get; set; }

        public int Amount { get; set; }

        public string Note { get; set; } = string.Empty;

        public Customer Customer { get; set; } = new();
    }

    public sealed class OrderDto
    {
        public string Code { get; set; } = string.Empty;

        public int Amount { get; set; }

        public int Sequence { get; set; }

        public int Total { get; set; }

        public string Note { get; set; } = string.Empty;

        public string Prefix { get; set; } = string.Empty;

        public Customer Customer { get; set; } = new();
    }

    public sealed class Price
    {
        public decimal Value { get; set; }
    }

    public sealed class PriceText
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed partial class OrderMapper
    {
        private readonly string prefix;

        private readonly int rate;

        private int sequence;

        public int LastId { get; private set; }

        public OrderMapper(string prefix, int rate)
        {
            this.prefix = prefix;
            this.rate = rate;
        }

        [Mapper]
        [MapProperty(nameof(OrderDto.Amount), nameof(Order.Amount), Converter = nameof(ApplyRate))]
        [MapCondition(nameof(OrderDto.Note), nameof(IsLong))]
        [MapUsing(nameof(OrderDto.Code), nameof(ToCode))]
        [MapUsing(nameof(OrderDto.Total), nameof(ToTotal))]
        [MapNested(nameof(OrderDto.Customer), Mapper = nameof(MapCustomerName))]
        [MapExpression(nameof(OrderDto.Prefix), "prefix")]
        [AfterMap(nameof(Count))]
        public partial OrderDto Map(Order source);

        [Mapper]
        [MapIgnore(nameof(OrderDto.Note))]
        [MapUsing(nameof(OrderDto.Code), nameof(ToCode))]
        [BeforeMap(nameof(Before))]
        public partial void Fill(Order source, OrderDto destination);

        [Mapper]
        public partial PriceText Format(Price source, CultureInfo culture);

        private int ApplyRate(int value) => value * rate;

        private bool IsLong(string value) => value.Length > rate;

        private string ToCode(Order source) => prefix + source.Id;

        private int ToTotal(Order source) => source.Id + rate;

        private void Count(Order source, OrderDto destination)
        {
            LastId = source.Id;
            destination.Sequence = ++sequence;
        }

        private void Before(Order source, OrderDto destination) => destination.Note = prefix + "-before" + source.Id;

        [Mapper]
        [MapUsing(nameof(Customer.Name), nameof(ToName))]
        private partial Customer MapCustomerName(Customer source);

        private string ToName(Customer source) => prefix + source.Name;
    }
}
