namespace Smart.Mapper
{
    using System;

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

        //--------------------------------------------------------------------------------
        // Enum
        //--------------------------------------------------------------------------------

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

        public class NullableEnum16Holder
        {
            public Enum16? Value { get; set; }
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

        public class NullableEnum32Holder
        {
            public Enum32? Value { get; set; }
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

        public class NullableEnum64Holder
        {
            public Enum64? Value { get; set; }
        }

        //--------------------------------------------------------------------------------
        // Operator
        //--------------------------------------------------------------------------------

        public struct StructValue
        {
            public int RawValue { get; set; }

            public static implicit operator StructValue(int value) => new() { RawValue = value };
            public static explicit operator int(StructValue value) => value.RawValue;
        }

        public class StructValueHolder
        {
            public StructValue Value { get; set; }
        }

        public class ClassValue
        {
            public int RawValue { get; set; }

            public static implicit operator ClassValue(int value) => new() { RawValue = value };
            public static explicit operator int(ClassValue value) => value.RawValue;
        }

        public class ClassValueHolder
        {
            public ClassValue? Value { get; set; }
        }

        public struct StructNullableValue
        {
            public int? RawValue { get; set; }

            public static implicit operator StructNullableValue(int? value) => new() { RawValue = value };
            public static explicit operator int?(StructNullableValue value) => value.RawValue;
        }

        public class StructNullableValueHolder
        {
            public StructNullableValue Value { get; set; }
        }

        public class ClassNullableValue
        {
            public int? RawValue { get; set; }

            public static implicit operator ClassNullableValue(int? value) => new() { RawValue = value };
            public static explicit operator int?(ClassNullableValue value) => value.RawValue;
        }

        public class ClassNullableValueHolder
        {
            public ClassNullableValue? Value { get; set; }
        }

        public class ClassPair1Value
        {
            public int RawValue { get; set; }

            public static implicit operator ClassPair1Value(ClassPair2Value value) => new() { RawValue = value.RawValue };
            public static explicit operator ClassPair2Value(ClassPair1Value value) => new() { RawValue = value.RawValue };
        }

        public class ClassPair1ValueHolder
        {
            public ClassPair1Value? Value { get; set; }
        }

        public class ClassPair2Value
        {
            public int RawValue { get; set; }
        }

        public class ClassPair2ValueHolder
        {
            public ClassPair2Value? Value { get; set; }
        }

        public struct StructPair1Value
        {
            public int RawValue { get; set; }

            public static implicit operator StructPair1Value(StructPair2Value value) => new() { RawValue = value.RawValue };
            public static explicit operator StructPair2Value(StructPair1Value value) => new() { RawValue = value.RawValue };
        }

        public class StructPair1ValueHolder
        {
            public StructPair1Value Value { get; set; }
        }

        public struct StructPair2Value
        {
            public int RawValue { get; set; }
        }

        public class StructPair2ValueHolder
        {
            public StructPair2Value Value { get; set; }
        }

        public class CrossPairClassValue
        {
            public int RawValue { get; set; }

            public static implicit operator CrossPairClassValue(CrossPairStructValue value) => new() { RawValue = value.RawValue };
            public static explicit operator CrossPairStructValue(CrossPairClassValue value) => new() { RawValue = value.RawValue };
        }

        public class CrossPairClassValueHolder
        {
            public CrossPairClassValue? Value { get; set; }
        }

        public struct CrossPairStructValue
        {
            public int RawValue { get; set; }
        }

        public class CrossPairStructValueHolder
        {
            public CrossPairStructValue Value { get; set; }
        }
    }
}
