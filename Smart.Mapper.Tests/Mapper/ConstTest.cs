namespace Smart.Mapper
{
    using System;

    using Xunit;

    public class ConstTest
    {
        //--------------------------------------------------------------------------------
        // Order
        //--------------------------------------------------------------------------------

        [Fact]
        public void ConstByForMember()
        {
            var config = new MapperConfig();
            config.CreateMap<Source, Destination>()
                .ForMember(x => x.StringValue, opt => opt.Const("x"));
            using var mapper = config.ToMapper();

            var destination = mapper.Map<Source, Destination>(new Source());

            Assert.Equal("x", destination.StringValue);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        public class Source
        {
        }

        public class Destination
        {
            public string? StringValue { get; set; }

            //public bool BoolValue { get; set; }
            //public byte ByteValue { get; set; }
            //public sbyte SByteValue { get; set; }
            //public char CharValue { get; set; }
            //public short ShortValue { get; set; }
            //public ushort UShortValue { get; set; }
            //public int IntValue { get; set; }
            //public uint UIntValue { get; set; }
            //public long LongValue { get; set; }
            //public ulong ULongValue { get; set; }
            //public float FloatValue { get; set; }
            //public double DoubleValue { get; set; }
            //public decimal DecimalValue { get; set; }
            //public IntPtr IntPtrValue { get; set; }
            //public UIntPtr UIntPtrValue { get; set; }

            //public MyEnum EnumValue { get; set; }
            //public MyEnumShort EnumShortValue { get; set; }
            //public MyEnumByte EnumByteValue { get; set; }

            //public DateTime DateTimeValue { get; set; }
            //public DateTimeOffset DateTimeOffsetValue { get; set; }

            //public bool? NullableBoolValue { get; set; }
            //public byte? NullableByteValue { get; set; }
            //public sbyte? NullableSByteValue { get; set; }
            //public char? NullableCharValue { get; set; }
            //public short? NullableShortValue { get; set; }
            //public ushort? NullableUShortValue { get; set; }
            //public int? NullableIntValue { get; set; }
            //public uint? NullableUIntValue { get; set; }
            //public long? NullableLongValue { get; set; }
            //public ulong? NullableULongValue { get; set; }
            //public float? NullableFloatValue { get; set; }
            //public double? NullableDoubleValue { get; set; }
            //public decimal? NullableDecimalValue { get; set; }
            //public IntPtr? NullableIntPtrValue { get; set; }
            //public UIntPtr? NullableUIntPtrValue { get; set; }

            //public MyEnum? NullableEnumValue { get; set; }
            //public MyEnumShort? NullableEnumShortValue { get; set; }
            //public MyEnumByte? NullableEnumByteValue { get; set; }

            //public DateTime? NullableDateTimeValue { get; set; }
            //public DateTimeOffset? NullableDateTimeOffsetValue { get; set; }
        }

        public enum MyEnum
        {
            Zero,
            One
        }

        public enum MyEnumShort : short
        {
            Zero,
            One
        }

        public enum MyEnumByte : byte
        {
            Zero,
            One
        }
    }
}
