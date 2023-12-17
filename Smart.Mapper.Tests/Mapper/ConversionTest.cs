namespace Smart.Mapper;

public partial class ConversionTest
{
    //--------------------------------------------------------------------------------
    // Basic
    //--------------------------------------------------------------------------------

    public sealed class Int16Holder
    {
        public short Value { get; set; }
    }

    public sealed class Int32Holder
    {
        public int Value { get; set; }
    }

    public sealed class Int64Holder
    {
        public long Value { get; set; }
    }

    public sealed class DecimalHolder
    {
        public decimal Value { get; set; }
    }

    public sealed class NullableInt16Holder
    {
        public short? Value { get; set; }
    }

    public sealed class NullableInt32Holder
    {
        public int? Value { get; set; }
    }

    public sealed class NullableInt64Holder
    {
        public long? Value { get; set; }
    }

    public sealed class NullableDecimalHolder
    {
        public decimal? Value { get; set; }
    }
}
