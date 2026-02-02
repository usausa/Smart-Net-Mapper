namespace Smart.Mapper;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

#pragma warning disable SA1503
/// <summary>
/// Default type converter for property mappings.
/// Provides both specialized methods for optimal performance and generic fallback.
/// </summary>
public static class DefaultValueConverter
{
    // ============================================================
    // Specialized methods: string -> numeric types
    // ============================================================

    /// <summary>Converts string to Int32.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ConvertToInt32(string source) => int.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Int64.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ConvertToInt64(string source) => long.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Int16.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ConvertToInt16(string source) => short.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ConvertToByte(string source) => byte.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to SByte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ConvertToSByte(string source) => sbyte.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to UInt32.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ConvertToUInt32(string source) => uint.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to UInt64.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ConvertToUInt64(string source) => ulong.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to UInt16.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ConvertToUInt16(string source) => ushort.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Single.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ConvertToSingle(string source) => float.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Double.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ConvertToDouble(string source) => double.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Decimal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ConvertToDecimal(string source) => decimal.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Boolean.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConvertToBoolean(string source) => bool.Parse(source);

    /// <summary>Converts string to DateTime.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ConvertToDateTime(string source) => DateTime.Parse(source, CultureInfo.InvariantCulture);

    /// <summary>Converts string to Guid.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid ConvertToGuid(string source) => Guid.Parse(source);

    // ============================================================
    // Specialized methods: numeric types -> string
    // ============================================================

    /// <summary>Converts Int32 to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(int source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Int64 to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(long source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Int16 to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(short source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Byte to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(byte source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts SByte to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(sbyte source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts UInt32 to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(uint source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts UInt64 to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(ulong source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts UInt16 to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(ushort source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Single to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(float source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Double to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(double source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Decimal to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(decimal source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Boolean to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(bool source) => source.ToString();


    /// <summary>Converts DateTime to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(DateTime source) => source.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converts Guid to string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToString(Guid source) => source.ToString();

    // ============================================================
    // Generic fallback method
    // ============================================================

    /// <summary>
    /// Converts a value from source type to destination type (generic fallback).
    /// This method is called only when:
    /// - No specialized method exists (e.g., ConvertToInt32)
    /// - Actual type conversion is required (not same type, not nullable wrapping/unwrapping)
    /// - Nullable handling has already been done by the generated code
    /// </summary>
    /// <typeparam name="TSource">The source type (non-nullable, after generator's null handling).</typeparam>
    /// <typeparam name="TDestination">The destination type (non-nullable target type).</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The converted value.</returns>
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
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
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
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
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
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
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
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
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
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
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
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
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
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
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
            return (TDestination)(object)value.ToString()!;
        }

        // DateTime -> string
        if (typeof(TSource) == typeof(DateTime) && typeof(TDestination) == typeof(string))
        {
            var value = (DateTime)(object)source!;
            return (TDestination)(object)value.ToString(CultureInfo.InvariantCulture)!;
        }

        // Guid -> string
        if (typeof(TSource) == typeof(Guid) && typeof(TDestination) == typeof(string))
        {
            var value = (Guid)(object)source!;
            return (TDestination)(object)value.ToString()!;
        }

        // Fallback: try direct cast (for enums and compatible types)
        return (TDestination)(object)source!;
    }
}
