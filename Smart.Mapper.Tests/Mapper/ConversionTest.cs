namespace Smart.Mapper
{
    using System;

    using Xunit;

    public partial class ConversionTest
    {
        //--------------------------------------------------------------------------------
        // Data
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

        public enum Enum16 : short
        {
            Zero = 0,
            One = 1,
            Max = Int16.MaxValue
        }

        public class Enum16Holder
        {
            public Enum16 Value { get; set; }
        }

        public enum Enum32
        {
            Zero = 0,
            One = 1,
            Max = Int32.MaxValue
        }

        public class Enum32Holder
        {
            public Enum32 Value { get; set; }
        }

        public enum Enum64 : long
        {
            Zero = 0,
            One = 1,
            Max = Int64.MaxValue
        }

        public class Enum64Holder
        {
            public Enum64 Value { get; set; }
        }
    }
}
