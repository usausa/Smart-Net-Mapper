namespace Smart.Mapper;

using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0060
#pragma warning disable SA1503
// Default type converter for property mappings.
// Provides both specialized methods for optimal performance and generic fallback.
public static class DefaultValueConverter
{
    // ============================================================
    // Specialized methods: string -> numeric types
    // ============================================================

    // Converts string to Int32.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ConvertToInt32(string source) => int.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to Int32 using specified culture and optional format.
    // The format parameter is part of the unified 3-argument lookup signature used by the generator; numeric Parse does not support format strings.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ConvertToInt32(string source, IFormatProvider culture, string? format) => int.Parse(source, culture);

    // Converts string to Int64.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ConvertToInt64(string source) => long.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to Int64 using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ConvertToInt64(string source, IFormatProvider culture, string? format) => long.Parse(source, culture);

    // Converts string to Int16.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ConvertToInt16(string source) => short.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to Int16 using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ConvertToInt16(string source, IFormatProvider culture, string? format) => short.Parse(source, culture);

    // Converts string to Byte.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ConvertToByte(string source) => byte.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to Byte using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ConvertToByte(string source, IFormatProvider culture, string? format) => byte.Parse(source, culture);

    // Converts string to SByte.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ConvertToSByte(string source) => sbyte.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to SByte using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ConvertToSByte(string source, IFormatProvider culture, string? format) => sbyte.Parse(source, culture);

    // Converts string to UInt32.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ConvertToUInt32(string source) => uint.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to UInt32 using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ConvertToUInt32(string source, IFormatProvider culture, string? format) => uint.Parse(source, culture);

    // Converts string to UInt64.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ConvertToUInt64(string source) => ulong.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to UInt64 using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ConvertToUInt64(string source, IFormatProvider culture, string? format) => ulong.Parse(source, culture);

    // Converts string to UInt16.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ConvertToUInt16(string source) => ushort.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to UInt16 using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ConvertToUInt16(string source, IFormatProvider culture, string? format) => ushort.Parse(source, culture);

    // Converts string to Single.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ConvertToSingle(string source) => float.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to Single using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ConvertToSingle(string source, IFormatProvider culture, string? format) => float.Parse(source, culture);

    // Converts string to Double.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ConvertToDouble(string source) => double.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to Double using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ConvertToDouble(string source, IFormatProvider culture, string? format) => double.Parse(source, culture);

    // Converts string to Decimal.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ConvertToDecimal(string source) => decimal.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to Decimal using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ConvertToDecimal(string source, IFormatProvider culture, string? format) => decimal.Parse(source, culture);

    // Converts string to Boolean.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConvertToBoolean(string source) => bool.Parse(source);

    // Converts string to DateTime.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ConvertToDateTime(string source) => DateTime.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to DateTime using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ConvertToDateTime(string source, IFormatProvider culture, string? format) =>
        format is null ? DateTime.Parse(source, culture) : DateTime.ParseExact(source, format, culture);

    // Converts string to Guid.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid ConvertToGuid(string source) => Guid.Parse(source);

    // ============================================================
    // Specialized methods: numeric types -> string
    // ============================================================

    // Converts Int32 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(int source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Int32 to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(int source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Int64 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(long source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Int64 to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(long source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Int16 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(short source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Int16 to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(short source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Byte to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(byte source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Byte to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(byte source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts SByte to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(sbyte source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts SByte to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(sbyte source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts UInt32 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(uint source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts UInt32 to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(uint source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts UInt64 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(ulong source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts UInt64 to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(ulong source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts UInt16 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(ushort source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts UInt16 to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(ushort source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Single to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(float source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Single to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(float source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Double to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(double source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Double to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(double source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Decimal to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(decimal source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Decimal to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(decimal source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Boolean to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(bool source) => source.ToString();

    // Converts DateTime to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(DateTime source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts DateTime to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(DateTime source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts Guid to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(Guid source) => source.ToString();

    // ============================================================
    // Specialized methods: string <-> date/time types (.NET 6+)
    // ============================================================

    // Converts string to DateOnly.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateOnly ConvertToDateOnly(string source) => DateOnly.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to DateOnly using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateOnly ConvertToDateOnly(string source, IFormatProvider culture, string? format) =>
        format is null ? DateOnly.Parse(source, culture) : DateOnly.ParseExact(source, format, culture);

    // Converts string to TimeOnly.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeOnly ConvertToTimeOnly(string source) => TimeOnly.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to TimeOnly using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeOnly ConvertToTimeOnly(string source, IFormatProvider culture, string? format) =>
        format is null ? TimeOnly.Parse(source, culture) : TimeOnly.ParseExact(source, format, culture);

    // Converts string to DateTimeOffset.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ConvertToDateTimeOffset(string source) => DateTimeOffset.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to DateTimeOffset using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset ConvertToDateTimeOffset(string source, IFormatProvider culture, string? format) =>
        format is null ? DateTimeOffset.Parse(source, culture) : DateTimeOffset.ParseExact(source, format, culture);

    // Converts string to TimeSpan.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan ConvertToTimeSpan(string source) => TimeSpan.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to TimeSpan using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan ConvertToTimeSpan(string source, IFormatProvider culture, string? format) =>
        format is null ? TimeSpan.Parse(source, culture) : TimeSpan.ParseExact(source, format, culture);

    // Converts DateOnly to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(DateOnly source) => source.ToString("O", CultureInfo.InvariantCulture);

    // Converts DateOnly to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(DateOnly source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts TimeOnly to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(TimeOnly source) => source.ToString("O", CultureInfo.InvariantCulture);

    // Converts TimeOnly to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(TimeOnly source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts DateTimeOffset to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(DateTimeOffset source) => source.ToString("O", CultureInfo.InvariantCulture);

    // Converts DateTimeOffset to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(DateTimeOffset source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(culture) : source.ToString(format, culture);

    // Converts TimeSpan to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(TimeSpan source) => source.ToString("c", CultureInfo.InvariantCulture);

    // Converts TimeSpan to string using specified culture and optional format.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(TimeSpan source, IFormatProvider culture, string? format) =>
        format is null ? source.ToString(null, culture) : source.ToString(format, culture);

    // ============================================================
    // Specialized methods: string <-> .NET 7+ numeric types
    // ============================================================

    // Converts string to Half.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Half ConvertToHalf(string source) => Half.Parse(source, CultureInfo.InvariantCulture);

    // Converts int to Half.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Half ConvertToHalf(int source) => (Half)source;

    // Converts long to Half.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Half ConvertToHalf(long source) => (Half)source;

    // Converts double to Half.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Half ConvertToHalf(double source) => (Half)source;

    // Converts float to Half.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Half ConvertToHalf(float source) => (Half)source;

    // Converts string to Int128.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int128 ConvertToInt128(string source) => Int128.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to UInt128.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UInt128 ConvertToUInt128(string source) => UInt128.Parse(source, CultureInfo.InvariantCulture);

    // Converts string to BigInteger.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BigInteger ConvertToBigInteger(string source) => BigInteger.Parse(source, CultureInfo.InvariantCulture);

    // Converts Half to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(Half source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts Int128 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(Int128 source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts UInt128 to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(UInt128 source) => source.ToString(CultureInfo.InvariantCulture);

    // Converts BigInteger to string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(BigInteger source) => source.ToString(CultureInfo.InvariantCulture);

    // ============================================================
    // Generic fallback method
    // ============================================================

    // Converts a value from source type to destination type (generic fallback).
    // This method is called only when:
    // - No specialized method exists (e.g., ConvertToInt32)
    // - Actual type conversion is required (not same type, not nullable wrapping/unwrapping)
    // - Nullable handling has already been done by the generated code.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        // Numeric conversions - JIT will optimize away unused branches
        return ConvertNumeric<TSource, TDestination>(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TDestination ConvertNumeric<TSource, TDestination>(TSource source)
    {
        // int -> other numeric types
        if (typeof(TSource) == typeof(int))
        {
            var value = (int)(object)source!;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // long -> other numeric types
        if (typeof(TSource) == typeof(long))
        {
            var value = (long)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // short -> other numeric types
        if (typeof(TSource) == typeof(short))
        {
            var value = (short)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // byte -> other numeric types
        if (typeof(TSource) == typeof(byte))
        {
            var value = (byte)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // float -> other numeric types
        if (typeof(TSource) == typeof(float))
        {
            var value = (float)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // double -> other numeric types
        if (typeof(TSource) == typeof(double))
        {
            var value = (double)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // decimal -> other numeric types
        if (typeof(TSource) == typeof(decimal))
        {
            var value = (decimal)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // string -> numeric types (parsing) - fallback for generic usage
        if (typeof(TSource) == typeof(string))
        {
            var value = (string)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)int.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)long.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)short.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)byte.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)sbyte.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)uint.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)ulong.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)ushort.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)float.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)double.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)decimal.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(bool)) return (TDestination)(object)bool.Parse(value);
            if (typeof(TDestination) == typeof(DateTime)) return (TDestination)(object)DateTime.Parse(value, CultureInfo.InvariantCulture);
            if (typeof(TDestination) == typeof(Guid)) return (TDestination)(object)Guid.Parse(value);
        }

        // bool -> string
        if (typeof(TSource) == typeof(bool) && typeof(TDestination) == typeof(string))
        {
            var value = (bool)(object)source!;
            return (TDestination)(object)value.ToString();
        }

        // DateTime -> string
        if (typeof(TSource) == typeof(DateTime) && typeof(TDestination) == typeof(string))
        {
            var value = (DateTime)(object)source!;
            return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture);
        }

        // Guid -> string
        if (typeof(TSource) == typeof(Guid) && typeof(TDestination) == typeof(string))
        {
            var value = (Guid)(object)source!;
            return (TDestination)(object)value.ToString();
        }

        // Fallback: try direct cast (for enums and compatible types)
        return (TDestination)(object)source!;
    }
}
