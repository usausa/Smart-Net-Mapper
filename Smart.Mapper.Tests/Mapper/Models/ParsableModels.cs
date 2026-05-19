#pragma warning disable CA1815
#pragma warning disable CA2231
namespace Smart.Mapper.Models;

using System;
using System.Globalization;

// IParsable<T> のみ実装するユーザー定義型
public readonly struct TestParsableId : IParsable<TestParsableId>, IEquatable<TestParsableId>
{
    public int Value { get; }

    public TestParsableId(int value) => Value = value;

    public static TestParsableId Parse(string s, IFormatProvider? provider)
        => new(int.Parse(s, provider ?? CultureInfo.InvariantCulture));

    public static bool TryParse(string? s, IFormatProvider? provider, out TestParsableId result)
    {
        if (int.TryParse(s, NumberStyles.Any, provider ?? CultureInfo.InvariantCulture, out var v))
        {
            result = new(v);
            return true;
        }
        result = default;
        return false;
    }

    public bool Equals(TestParsableId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is TestParsableId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

// ISpanParsable<T> を実装するユーザー定義型
public readonly struct TestSpanParsableId : ISpanParsable<TestSpanParsableId>, IEquatable<TestSpanParsableId>
{
    public int Value { get; }

    public TestSpanParsableId(int value) => Value = value;

    public static TestSpanParsableId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => new(int.Parse(s, NumberStyles.Any, provider ?? CultureInfo.InvariantCulture));

    public static TestSpanParsableId Parse(string s, IFormatProvider? provider)
        => Parse(s.AsSpan(), provider);

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TestSpanParsableId result)
    {
        if (int.TryParse(s, NumberStyles.Any, provider ?? CultureInfo.InvariantCulture, out var v))
        {
            result = new(v);
            return true;
        }
        result = default;
        return false;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out TestSpanParsableId result)
        => TryParse(s.AsSpan(), provider, out result);

    public bool Equals(TestSpanParsableId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is TestSpanParsableId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

// IParsable のみ実装する参照型
public class TestParsableCode : IParsable<TestParsableCode>
{
    public string Code { get; }

    public TestParsableCode(string code) => Code = code;

    public static TestParsableCode Parse(string s, IFormatProvider? provider) => new(s.ToUpperInvariant());

    public static bool TryParse(string? s, IFormatProvider? provider, out TestParsableCode result)
    {
        result = new(s?.ToUpperInvariant() ?? string.Empty);
        return s is not null;
    }
}

// ---- マッピング用モデル ----

// T1: string → TestParsableId (IParsable のみ)
public class ParsableSource
{
    public string IdText { get; set; } = string.Empty;
}

public class ParsableDestination
{
    public TestParsableId IdText { get; set; }
}

// T2: string → TestSpanParsableId (ISpanParsable)
public class SpanParsableSource
{
    public string IdText { get; set; } = string.Empty;
}

public class SpanParsableDestination
{
    public TestSpanParsableId IdText { get; set; }
}

// T3/T4: Culture 指定あり
public class ParsableCultureSource
{
    public string IdText { get; set; } = string.Empty;
}

public class ParsableCultureDestination
{
    public TestParsableId IdText { get; set; }
}

public class SpanParsableCultureSource
{
    public string IdText { get; set; } = string.Empty;
}

public class SpanParsableCultureDestination
{
    public TestSpanParsableId IdText { get; set; }
}

// T7: string? → TestSpanParsableId (nullable source)
public class NullableSpanParsableSource
{
    public string? IdText { get; set; }
}

public class NullableSpanParsableDestination
{
    public TestSpanParsableId IdText { get; set; }
}
