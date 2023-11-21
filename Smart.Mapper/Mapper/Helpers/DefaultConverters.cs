namespace Smart.Mapper.Helpers;

using Smart.Mapper.Mappers;

internal static class DefaultConverters
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:SpecifyIFormatProvider", Justification = "Ignore")]
    // ReSharper disable MergeConditionalExpression
    // ReSharper disable SpecifyACultureInStringConversionExplicitly
    public static Dictionary<Tuple<Type, Type>, ConverterEntry> Entries => new()
    {
        // To Bool
        { new(typeof(byte), typeof(bool)), new(ConverterType.FuncSource, typeof(byte), typeof(bool), (Func<byte, bool>)(static x => x != 0)) },
        { new(typeof(sbyte), typeof(bool)), new(ConverterType.FuncSource, typeof(sbyte), typeof(bool), (Func<sbyte, bool>)(static x => x != 0)) },
        { new(typeof(short), typeof(bool)), new(ConverterType.FuncSource, typeof(short), typeof(bool), (Func<short, bool>)(static x => x != 0)) },
        { new(typeof(ushort), typeof(bool)), new(ConverterType.FuncSource, typeof(ushort), typeof(bool), (Func<ushort, bool>)(static x => x != 0)) },
        { new(typeof(int), typeof(bool)), new(ConverterType.FuncSource, typeof(int), typeof(bool), (Func<int, bool>)(static x => x != 0)) },
        { new(typeof(uint), typeof(bool)), new(ConverterType.FuncSource, typeof(uint), typeof(bool), (Func<uint, bool>)(static x => x != 0)) },
        { new(typeof(long), typeof(bool)), new(ConverterType.FuncSource, typeof(long), typeof(bool), (Func<long, bool>)(static x => x != 0)) },
        { new(typeof(ulong), typeof(bool)), new(ConverterType.FuncSource, typeof(ulong), typeof(bool), (Func<ulong, bool>)(static x => x != 0)) },
        { new(typeof(float), typeof(bool)), new(ConverterType.FuncSource, typeof(float), typeof(bool), (Func<float, bool>)(static x => x != 0)) },
        { new(typeof(double), typeof(bool)), new(ConverterType.FuncSource, typeof(double), typeof(bool), (Func<double, bool>)(static x => x != 0)) },
        { new(typeof(decimal), typeof(bool)), new(ConverterType.FuncSource, typeof(decimal), typeof(bool), (Func<decimal, bool>)(static x => x != 0)) },
        // Bool to
        { new(typeof(bool), typeof(byte)), new(ConverterType.FuncSource, typeof(bool), typeof(byte), (Func<bool, byte>)(static x => x ? (byte)1 : (byte)0)) },
        { new(typeof(bool), typeof(sbyte)), new(ConverterType.FuncSource, typeof(bool), typeof(sbyte), (Func<bool, sbyte>)(static x => x ? (sbyte)1 : (sbyte)0)) },
        { new(typeof(bool), typeof(short)), new(ConverterType.FuncSource, typeof(bool), typeof(short), (Func<bool, short>)(static x => x ? (short)1 : (short)0)) },
        { new(typeof(bool), typeof(ushort)), new(ConverterType.FuncSource, typeof(bool), typeof(ushort), (Func<bool, ushort>)(static x => x ? (ushort)1 : (ushort)0)) },
        { new(typeof(bool), typeof(int)), new(ConverterType.FuncSource, typeof(bool), typeof(int), (Func<bool, int>)(static x => x ? 1 : 0)) },
        { new(typeof(bool), typeof(uint)), new(ConverterType.FuncSource, typeof(bool), typeof(uint), (Func<bool, uint>)(static x => x ? 1u : 0u)) },
        { new(typeof(bool), typeof(long)), new(ConverterType.FuncSource, typeof(bool), typeof(long), (Func<bool, long>)(static x => x ? 1L : 0L)) },
        { new(typeof(bool), typeof(ulong)), new(ConverterType.FuncSource, typeof(bool), typeof(ulong), (Func<bool, ulong>)(static x => x ? 1ul : 0ul)) },
        { new(typeof(bool), typeof(float)), new(ConverterType.FuncSource, typeof(bool), typeof(float), (Func<bool, float>)(static x => x ? 1f : 0f)) },
        { new(typeof(bool), typeof(double)), new(ConverterType.FuncSource, typeof(bool), typeof(double), (Func<bool, double>)(static x => x ? 1d : 0d)) },
        { new(typeof(bool), typeof(decimal)), new(ConverterType.FuncSource, typeof(bool), typeof(decimal), (Func<bool, decimal>)(static x => x ? 1m : 0m)) },
        // To String
        { new(typeof(byte), typeof(string)), new(ConverterType.FuncSource, typeof(byte), typeof(string), (Func<byte, string>)(static x => x.ToString())) },
        { new(typeof(sbyte), typeof(string)), new(ConverterType.FuncSource, typeof(sbyte), typeof(string), (Func<sbyte, string>)(static x => x.ToString())) },
        { new(typeof(char), typeof(string)), new(ConverterType.FuncSource, typeof(char), typeof(string), (Func<char, string>)(static x => x.ToString())) },
        { new(typeof(short), typeof(string)), new(ConverterType.FuncSource, typeof(short), typeof(string), (Func<short, string>)(static x => x.ToString())) },
        { new(typeof(ushort), typeof(string)), new(ConverterType.FuncSource, typeof(ushort), typeof(string), (Func<ushort, string>)(static x => x.ToString())) },
        { new(typeof(int), typeof(string)), new(ConverterType.FuncSource, typeof(int), typeof(string), (Func<int, string>)(static x => x.ToString())) },
        { new(typeof(uint), typeof(string)), new(ConverterType.FuncSource, typeof(uint), typeof(string), (Func<uint, string>)(static x => x.ToString())) },
        { new(typeof(long), typeof(string)), new(ConverterType.FuncSource, typeof(long), typeof(string), (Func<long, string>)(static x => x.ToString())) },
        { new(typeof(ulong), typeof(string)), new(ConverterType.FuncSource, typeof(ulong), typeof(string), (Func<ulong, string>)(static x => x.ToString())) },
        { new(typeof(float), typeof(string)), new(ConverterType.FuncSource, typeof(float), typeof(string), (Func<float, string>)(static x => x.ToString())) },
        { new(typeof(double), typeof(string)), new(ConverterType.FuncSource, typeof(double), typeof(string), (Func<double, string>)(static x => x.ToString())) },
        { new(typeof(decimal), typeof(string)), new(ConverterType.FuncSource, typeof(decimal), typeof(string), (Func<decimal, string>)(static x => x.ToString())) },
        { new(typeof(bool), typeof(string)), new(ConverterType.FuncSource, typeof(bool), typeof(string), (Func<bool, string>)(static x => x.ToString())) },
        // String To
        { new(typeof(string), typeof(byte)), new(ConverterType.FuncSource, typeof(string), typeof(byte), (Func<string, byte>)Byte.Parse) },
        { new(typeof(string), typeof(sbyte)), new(ConverterType.FuncSource, typeof(string), typeof(sbyte), (Func<string, sbyte>)SByte.Parse) },
        { new(typeof(string), typeof(char)), new(ConverterType.FuncSource, typeof(string), typeof(char), (Func<string, char>)(static x => x[0])) },
        { new(typeof(string), typeof(short)), new(ConverterType.FuncSource, typeof(string), typeof(short), (Func<string, short>)Int16.Parse) },
        { new(typeof(string), typeof(ushort)), new(ConverterType.FuncSource, typeof(string), typeof(ushort), (Func<string, ushort>)UInt16.Parse) },
        { new(typeof(string), typeof(int)), new(ConverterType.FuncSource, typeof(string), typeof(int), (Func<string, int>)Int32.Parse) },
        { new(typeof(string), typeof(uint)), new(ConverterType.FuncSource, typeof(string), typeof(uint), (Func<string, uint>)UInt32.Parse) },
        { new(typeof(string), typeof(long)), new(ConverterType.FuncSource, typeof(string), typeof(long), (Func<string, long>)Int64.Parse) },
        { new(typeof(string), typeof(ulong)), new(ConverterType.FuncSource, typeof(string), typeof(ulong), (Func<string, ulong>)UInt64.Parse) },
        { new(typeof(string), typeof(float)), new(ConverterType.FuncSource, typeof(string), typeof(float), (Func<string, float>)Single.Parse) },
        { new(typeof(string), typeof(double)), new(ConverterType.FuncSource, typeof(string), typeof(double), (Func<string, double>)Double.Parse) },
        { new(typeof(string), typeof(decimal)), new(ConverterType.FuncSource, typeof(string), typeof(decimal), (Func<string, decimal>)Decimal.Parse) },
        { new(typeof(string), typeof(bool)), new(ConverterType.FuncSource, typeof(string), typeof(bool), (Func<string, bool>)Boolean.Parse) },
        // String
        { new(typeof(char[]), typeof(string)), new(ConverterType.FuncSource, typeof(char[]), typeof(string), (Func<char[], string>)(static x => new string(x))) },
        { new(typeof(string), typeof(char[])), new(ConverterType.FuncSource, typeof(string), typeof(char[]), (Func<string, char[]>)(static x => x.ToCharArray())) },
        // DateTime
        { new(typeof(long), typeof(DateTime)), new(ConverterType.FuncSource, typeof(long), typeof(DateTime), (Func<long, DateTime>)(static x => new DateTime(x))) },
        { new(typeof(long), typeof(DateTimeOffset)), new(ConverterType.FuncSource, typeof(long), typeof(DateTimeOffset), (Func<long, DateTimeOffset>)(static x => new DateTimeOffset(new DateTime(x)))) },
        { new(typeof(DateTime), typeof(long)), new(ConverterType.FuncSource, typeof(DateTime), typeof(long), (Func<DateTime, long>)(static x => x.Ticks)) },
        { new(typeof(DateTimeOffset), typeof(long)), new(ConverterType.FuncSource, typeof(DateTimeOffset), typeof(long), (Func<DateTimeOffset, long>)(static x => x.Ticks)) },
        { new(typeof(string), typeof(DateTime)), new(ConverterType.FuncSource, typeof(string), typeof(DateTime), (Func<string, DateTime>)DateTime.Parse) },
        { new(typeof(string), typeof(DateTimeOffset)), new(ConverterType.FuncSource, typeof(string), typeof(DateTimeOffset), (Func<string, DateTimeOffset>)(static x => DateTime.Parse(x))) },
        { new(typeof(DateTime), typeof(string)), new(ConverterType.FuncSource, typeof(DateTime), typeof(string), (Func<DateTime, string>)(static x => x.ToString())) },
        { new(typeof(DateTimeOffset), typeof(string)), new(ConverterType.FuncSource, typeof(DateTimeOffset), typeof(string), (Func<DateTimeOffset, string>)(static x => x.ToString())) },
        // Guid
        { new(typeof(string), typeof(Guid)), new(ConverterType.FuncSource, typeof(string), typeof(Guid), (Func<string, Guid>)Guid.Parse) },
        { new(typeof(Guid), typeof(string)), new(ConverterType.FuncSource, typeof(Guid), typeof(string), (Func<Guid, string>)(static x => x.ToString())) }
    };
}
