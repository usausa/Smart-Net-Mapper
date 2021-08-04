namespace Smart.Mapper.Helpers
{
    using System;
    using System.Collections.Generic;

    using Smart.Mapper.Mappers;

    internal static class DefaultConverters
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:SpecifyIFormatProvider", Justification = "Ignore")]
        // ReSharper disable MergeConditionalExpression
        // ReSharper disable SpecifyACultureInStringConversionExplicitly
        public static Dictionary<Tuple<Type, Type>, TypeEntry<ConverterType>> Entries => new()
        {
            // To Bool
            { new(typeof(byte), typeof(bool)), new(ConverterType.FuncSource, (Func<byte, bool>)(x => x != 0)) },
            { new(typeof(sbyte), typeof(bool)), new(ConverterType.FuncSource, (Func<sbyte, bool>)(x => x != 0)) },
            { new(typeof(short), typeof(bool)), new(ConverterType.FuncSource, (Func<short, bool>)(x => x != 0)) },
            { new(typeof(ushort), typeof(bool)), new(ConverterType.FuncSource, (Func<ushort, bool>)(x => x != 0)) },
            { new(typeof(int), typeof(bool)), new(ConverterType.FuncSource, (Func<int, bool>)(x => x != 0)) },
            { new(typeof(uint), typeof(bool)), new(ConverterType.FuncSource, (Func<uint, bool>)(x => x != 0)) },
            { new(typeof(long), typeof(bool)), new(ConverterType.FuncSource, (Func<long, bool>)(x => x != 0)) },
            { new(typeof(ulong), typeof(bool)), new(ConverterType.FuncSource, (Func<ulong, bool>)(x => x != 0)) },
            { new(typeof(float), typeof(bool)), new(ConverterType.FuncSource, (Func<float, bool>)(x => x != 0)) },
            { new(typeof(double), typeof(bool)), new(ConverterType.FuncSource, (Func<double, bool>)(x => x != 0)) },
            { new(typeof(decimal), typeof(bool)), new(ConverterType.FuncSource, (Func<decimal, bool>)(x => x != 0)) },
            // Bool to
            { new(typeof(bool), typeof(byte)), new(ConverterType.FuncSource, (Func<bool, byte>)(x => x ? (byte)1 : (byte)0)) },
            { new(typeof(bool), typeof(sbyte)), new(ConverterType.FuncSource, (Func<bool, sbyte>)(x => x ? (sbyte)1 : (sbyte)0)) },
            { new(typeof(bool), typeof(short)), new(ConverterType.FuncSource, (Func<bool, short>)(x => x ? (short)1 : (short)0)) },
            { new(typeof(bool), typeof(ushort)), new(ConverterType.FuncSource, (Func<bool, ushort>)(x => x ? (ushort)1 : (ushort)0)) },
            { new(typeof(bool), typeof(int)), new(ConverterType.FuncSource, (Func<bool, int>)(x => x ? 1 : 0)) },
            { new(typeof(bool), typeof(uint)), new(ConverterType.FuncSource, (Func<bool, uint>)(x => x ? 1u : 0u)) },
            { new(typeof(bool), typeof(long)), new(ConverterType.FuncSource, (Func<bool, long>)(x => x ? 1L : 0L)) },
            { new(typeof(bool), typeof(ulong)), new(ConverterType.FuncSource, (Func<bool, ulong>)(x => x ? 1ul : 0ul)) },
            { new(typeof(bool), typeof(float)), new(ConverterType.FuncSource, (Func<bool, float>)(x => x ? 1f : 0f)) },
            { new(typeof(bool), typeof(double)), new(ConverterType.FuncSource, (Func<bool, double>)(x => x ? 1d : 0d)) },
            { new(typeof(bool), typeof(decimal)), new(ConverterType.FuncSource, (Func<bool, decimal>)(x => x ? 1m : 0m)) },
            // To String
            { new(typeof(byte), typeof(string)), new(ConverterType.FuncSource, (Func<byte, string>)(x => x.ToString())) },
            { new(typeof(sbyte), typeof(string)), new(ConverterType.FuncSource, (Func<sbyte, string>)(x => x.ToString())) },
            { new(typeof(char), typeof(string)), new(ConverterType.FuncSource, (Func<char, string>)(x => x.ToString())) },
            { new(typeof(short), typeof(string)), new(ConverterType.FuncSource, (Func<short, string>)(x => x.ToString())) },
            { new(typeof(ushort), typeof(string)), new(ConverterType.FuncSource, (Func<ushort, string>)(x => x.ToString())) },
            { new(typeof(int), typeof(string)), new(ConverterType.FuncSource, (Func<int, string>)(x => x.ToString())) },
            { new(typeof(uint), typeof(string)), new(ConverterType.FuncSource, (Func<uint, string>)(x => x.ToString())) },
            { new(typeof(long), typeof(string)), new(ConverterType.FuncSource, (Func<long, string>)(x => x.ToString())) },
            { new(typeof(ulong), typeof(string)), new(ConverterType.FuncSource, (Func<ulong, string>)(x => x.ToString())) },
            { new(typeof(float), typeof(string)), new(ConverterType.FuncSource, (Func<float, string>)(x => x.ToString())) },
            { new(typeof(double), typeof(string)), new(ConverterType.FuncSource, (Func<double, string>)(x => x.ToString())) },
            { new(typeof(decimal), typeof(string)), new(ConverterType.FuncSource, (Func<decimal, string>)(x => x.ToString())) },
            { new(typeof(bool), typeof(string)), new(ConverterType.FuncSource, (Func<bool, string>)(x => x.ToString())) },
            // String To
            { new(typeof(string), typeof(byte)), new(ConverterType.FuncSource, (Func<string?, byte>)(x => x is null ? default : Byte.Parse(x))) },
            { new(typeof(string), typeof(sbyte)), new(ConverterType.FuncSource, (Func<string?, sbyte>)(x => x is null ? default : SByte.Parse(x))) },
            { new(typeof(string), typeof(char)), new(ConverterType.FuncSource, (Func<string?, char>)(x => x is null ? default : x[0])) },
            { new(typeof(string), typeof(short)), new(ConverterType.FuncSource, (Func<string?, short>)(x => x is null ? default : Int16.Parse(x))) },
            { new(typeof(string), typeof(ushort)), new(ConverterType.FuncSource, (Func<string?, ushort>)(x => x is null ? default : UInt16.Parse(x))) },
            { new(typeof(string), typeof(int)), new(ConverterType.FuncSource, (Func<string?, int>)(x => x is null ? default : Int32.Parse(x))) },
            { new(typeof(string), typeof(uint)), new(ConverterType.FuncSource, (Func<string?, uint>)(x => x is null ? default : UInt32.Parse(x))) },
            { new(typeof(string), typeof(long)), new(ConverterType.FuncSource, (Func<string?, long>)(x => x is null ? default : Int64.Parse(x))) },
            { new(typeof(string), typeof(ulong)), new(ConverterType.FuncSource, (Func<string?, ulong>)(x => x is null ? default : UInt64.Parse(x))) },
            { new(typeof(string), typeof(float)), new(ConverterType.FuncSource, (Func<string?, float>)(x => x is null ? default : Single.Parse(x))) },
            { new(typeof(string), typeof(double)), new(ConverterType.FuncSource, (Func<string?, double>)(x => x is null ? default : Double.Parse(x))) },
            { new(typeof(string), typeof(decimal)), new(ConverterType.FuncSource, (Func<string?, decimal>)(x => x is null ? default : Decimal.Parse(x))) },
            { new(typeof(string), typeof(bool)), new(ConverterType.FuncSource, (Func<string?, bool>)(x => x is null ? default : Boolean.Parse(x))) },
            // DateTime
            { new(typeof(long), typeof(DateTime)), new(ConverterType.FuncSource, (Func<long, DateTime>)(x => new DateTime(x))) },
            { new(typeof(long), typeof(DateTimeOffset)), new(ConverterType.FuncSource, (Func<long, DateTimeOffset>)(x => new DateTimeOffset(new DateTime(x)))) },
            { new(typeof(DateTime), typeof(long)), new(ConverterType.FuncSource, (Func<DateTime, long>)(x => x.Ticks)) },
            { new(typeof(DateTimeOffset), typeof(long)), new(ConverterType.FuncSource, (Func<DateTimeOffset, long>)(x => x.Ticks)) },
            { new(typeof(string), typeof(DateTime)), new(ConverterType.FuncSource, (Func<string?, DateTime>)(x => x is null ? default : DateTime.Parse(x))) },
            { new(typeof(string), typeof(DateTimeOffset)), new(ConverterType.FuncSource, (Func<string?, DateTimeOffset>)(x => x is null ? default : DateTime.Parse(x))) },
            { new(typeof(DateTime), typeof(string)), new(ConverterType.FuncSource, (Func<DateTime, string>)(x => x.ToString())) },
            { new(typeof(DateTimeOffset), typeof(string)), new(ConverterType.FuncSource, (Func<DateTimeOffset, string>)(x => x.ToString())) },
            // Guid
            { new(typeof(string), typeof(Guid)), new(ConverterType.FuncSource, (Func<string?, Guid>)(x => x is null ? default : Guid.Parse(x))) },
            { new(typeof(Guid), typeof(string)), new(ConverterType.FuncSource, (Func<Guid, string>)(x => x.ToString())) },
        };
    }
}
