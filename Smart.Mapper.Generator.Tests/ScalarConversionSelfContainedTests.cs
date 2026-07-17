namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// Generated scalar-conversion code must be self-contained (no consumer usings) and must compile.
// Regressions found by the scalar conversion matrix sweep:
//   - char -> string  : was routed to a bogus IFormattable path (char.ToString(format, culture) does not exist).
//   - string -> char  : char implements IParsable/ISpanParsable explicitly, so char.Parse(span, provider) is not callable.
//   - int/long/float/double -> Half with Culture : specialized ConvertToHalf(numeric) had no 3-arg culture overload.
//   - string -> <custom ISpanParsable> : emitted a bare .AsSpan() that needs `using System`.
public class ScalarConversionSelfContainedTests
{
    private static string Source(string fqSourceType, string fqTargetType, string? culture, string extraTypes = "")
    {
        var attr = culture is null
            ? "[global::Smart.Mapper.Mapper]"
            : $"[global::Smart.Mapper.Mapper(Culture = \"{culture}\")]";
        return
            "#nullable enable\n" +
            "internal static partial class M\n{\n" +
            $"    {attr}\n    public static partial Dst Map(Src src);\n}}\n" +
            $"public class Src {{ public {fqSourceType} Value {{ get; set; }} }}\n" +
            $"public class Dst {{ public {fqTargetType} Value {{ get; set; }} }}\n" +
            extraTypes;
    }

    // No usings are supplied, so any non-fully-qualified BCL call in the generated code fails to compile,
    // and any accepted-but-broken conversion surfaces as a CS error.
    private static void AssertCompiles(string fqSourceType, string fqTargetType, string? culture = null, string extraTypes = "")
    {
        var errors = GeneratorTestHelper.GetDiagnosticsAll(Source(fqSourceType, fqTargetType, culture, extraTypes))
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void CharToString() => AssertCompiles("char", "string");

    [Fact]
    public void CharToString_WithCulture() => AssertCompiles("char", "string", "de-DE");

    [Fact]
    public void StringToChar() => AssertCompiles("string", "char");

    [Fact]
    public void StringToChar_WithCulture() => AssertCompiles("string", "char", "de-DE");

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("float")]
    [InlineData("double")]
    public void NumericToHalf_WithCulture(string numeric) =>
        AssertCompiles(numeric, "global::System.Half", "de-DE");

    [Theory]
    [InlineData("global::System.Half")]
    [InlineData("global::System.Int128")]
    [InlineData("global::System.UInt128")]
    [InlineData("global::System.Numerics.BigInteger")]
    [InlineData("bool")]
    [InlineData("global::System.Guid")]
    public void StringToSpecialized_WithCulture(string target) =>
        AssertCompiles("string", target, "de-DE");

    [Theory]
    [InlineData("global::System.Half")]
    [InlineData("global::System.Int128")]
    [InlineData("bool")]
    [InlineData("global::System.Guid")]
    public void SpecializedToString_WithCulture(string source) =>
        AssertCompiles(source, "string", "de-DE");

    // A custom type implementing ISpanParsable<T> publicly exercises the string-parse path that
    // emits .AsSpan(); the generated call must be fully qualified (global::System.MemoryExtensions.AsSpan).
    [Fact]
    public void StringToCustomSpanParsable_IsSelfContained()
    {
        const string sp =
            "public readonly struct Sp : global::System.ISpanParsable<Sp>\n" +
            "{\n" +
            "    public int V { get; init; }\n" +
            "    public static Sp Parse(string s, global::System.IFormatProvider? p) => new Sp { V = int.Parse(s) };\n" +
            "    public static Sp Parse(global::System.ReadOnlySpan<char> s, global::System.IFormatProvider? p) => new Sp { V = int.Parse(s) };\n" +
            "    public static bool TryParse(string? s, global::System.IFormatProvider? p, out Sp r) { r = default; return false; }\n" +
            "    public static bool TryParse(global::System.ReadOnlySpan<char> s, global::System.IFormatProvider? p, out Sp r) { r = default; return false; }\n" +
            "}\n";
        AssertCompiles("string", "Sp", culture: null, extraTypes: sp);
    }
}
