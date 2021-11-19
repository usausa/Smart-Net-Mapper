namespace Smart.Mapper;

public partial class ConversionTest
{
    //--------------------------------------------------------------------------------
    // Basic
    //--------------------------------------------------------------------------------

    public class Int16Holder
    {
        public short Value { get; set; }
    }

    public class Int32Holder
    {
        public int Value { get; set; }
    }

    public class Int64Holder
    {
        public long Value { get; set; }
    }

    public class DecimalHolder
    {
        public decimal Value { get; set; }
    }

    public class NullableInt16Holder
    {
        public short? Value { get; set; }
    }

    public class NullableInt32Holder
    {
        public int? Value { get; set; }
    }

    public class NullableInt64Holder
    {
        public long? Value { get; set; }
    }

    public class NullableDecimalHolder
    {
        public decimal? Value { get; set; }
    }
}
