namespace Smart.Mapper;

using System.Globalization;

// The custom parameters go to the methods the attributes name and to the nested and element mappers that declare
// them, in any order, by type, or by name for several of a type, so that a CultureInfo parameter formats the nested
// objects as well
public sealed partial class CustomParameterMappingTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void CultureGoesToNestedAndElementMappers()
    {
        var source = new Order
        {
            Total = 1234.5m,
            Buyer = new Customer { Balance = 10.25m },
            Lines = [new Line { Price = 1.5m }, new Line { Price = 2.75m }]
        };

        var german = CustomParameterMappers.Map(source, German);
        var english = CustomParameterMappers.Map(source, English);

        Assert.Equal("1234,5", german.Total);
        Assert.Equal("10,25", german.Buyer.Balance);
        Assert.Equal(["1,5", "2,75"], german.Lines.Select(static x => x.Price));
        Assert.Equal("1234.5", english.Total);
        Assert.Equal("10.25", english.Buyer.Balance);
        Assert.Equal(["1.5", "2.75"], english.Lines.Select(static x => x.Price));
    }

    [Fact]
    public void CallbacksTakeCustomParametersInAnyOrder()
    {
        var destination = CustomParameterMappers.MapWithCallback(new Order(), new Counter { Value = 3 }, "x");

        Assert.Equal("x3", destination.Note);
    }

    [Fact]
    public void CustomParametersOfSameTypeGoByName()
    {
        var destination = CustomParameterMappers.MapWrapped(new Order { Note = "note" }, "[", "]");

        Assert.Equal("note]", destination.Note);
        Assert.Equal("[note]", destination.Title);
    }

    public sealed class Customer
    {
        public decimal Balance { get; set; }
    }

    public sealed class Line
    {
        public decimal Price { get; set; }
    }

    public sealed class Order
    {
        public decimal Total { get; set; }

        public string Note { get; set; } = string.Empty;

        public Customer Buyer { get; set; } = new();

        public IReadOnlyList<Line> Lines { get; set; } = [];
    }

    public sealed class CustomerDto
    {
        public string Balance { get; set; } = string.Empty;
    }

    public sealed class LineDto
    {
        public string Price { get; set; } = string.Empty;
    }

    public sealed class OrderDto
    {
        public string Total { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public CustomerDto Buyer { get; set; } = new();

        public IReadOnlyList<LineDto> Lines { get; set; } = [];
    }

    public sealed class Counter
    {
        public int Value { get; set; }
    }

    internal static partial class CustomParameterMappers
    {
        [Mapper]
        [MapNested(nameof(OrderDto.Buyer), Mapper = nameof(MapCustomer))]
        [MapCollection(nameof(OrderDto.Lines), Mapper = nameof(MapLine))]
        public static partial OrderDto Map(Order source, CultureInfo culture);

        [Mapper]
        public static partial CustomerDto MapCustomer(Customer source, CultureInfo culture);

        [Mapper]
        public static partial LineDto MapLine(Line source, CultureInfo culture);

        [Mapper]
        [MapIgnore(nameof(OrderDto.Buyer))]
        [MapIgnore(nameof(OrderDto.Lines))]
        [AfterMap(nameof(Complete))]
        public static partial OrderDto MapWithCallback(Order source, Counter counter, string prefix);

        [Mapper]
        [MapIgnore(nameof(OrderDto.Buyer))]
        [MapIgnore(nameof(OrderDto.Lines))]
        [MapProperty(nameof(OrderDto.Note), Converter = nameof(Append))]
        [MapUsing(nameof(OrderDto.Title), nameof(Enclose))]
        public static partial OrderDto MapWrapped(Order source, string open, string close);

        private static void Complete(Order source, OrderDto destination, string prefix, Counter counter) =>
            destination.Note = prefix + counter.Value + source.Note;

        private static string Append(string value, string close) => value + close;

        private static string Enclose(Order source, string close, string open) => open + source.Note + close;
    }
}
