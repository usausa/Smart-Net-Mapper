namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A byte, sbyte, short or ushort constant, which has no literal of its own, is written as a cast, so that it
// keeps its type: an object target and the elements of an object array box it as it is, not as an int. It
// converts to the target the way a value of its type does, so a short does not go to a byte.
public class SmallIntegerConstantTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static string Source(string attributes) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public byte? Small { get; set; } }
        public class Dst
        {
            public object? Value { get; set; }
            public object?[]? Values { get; set; }
            public byte[]? Bytes { get; set; }
            public short[]? Shorts { get; set; }
            public byte Byte { get; set; }
            public sbyte SByte { get; set; }
            public short Short { get; set; }
            public int Int { get; set; }
            public long Long { get; set; }
            public byte Small { get; set; }
        }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Value), (byte)1)]", "__d.Value = (byte)1;")]
    [InlineData("[MapConstant(nameof(Dst.Value), (sbyte)-2)]", "__d.Value = (sbyte)-2;")]
    [InlineData("[MapConstant(nameof(Dst.Value), (short)-3)]", "__d.Value = (short)-3;")]
    [InlineData("[MapConstant(nameof(Dst.Value), (ushort)4)]", "__d.Value = (ushort)4;")]
    [InlineData("[MapConstant(nameof(Dst.Values), new object[] { (byte)1, (short)-2, 3 })]", "__d.Values = new object[] { (byte)1, (short)-2, 3 };")]
    [InlineData("[MapConstant(nameof(Dst.Bytes), new byte[] { 1, 2 })]", "__d.Bytes = new byte[] { 1, 2 };")]
    [InlineData("[MapConstant(nameof(Dst.Shorts), new short[] { -1 })]", "__d.Shorts = new short[] { -1 };")]
    [InlineData("[MapConstant<byte>(nameof(Dst.Byte), 5)]", "__d.Byte = (byte)5;")]
    [InlineData("[MapConstant<sbyte>(nameof(Dst.SByte), -5)]", "__d.SByte = (sbyte)-5;")]
    [InlineData("[MapConstant<short>(nameof(Dst.Int), 6)]", "__d.Int = (short)6;")]
    [InlineData("[MapConstant<byte>(nameof(Dst.Long), 7)]", "__d.Long = (byte)7;")]
    [InlineData("[MapProperty<byte>(nameof(Dst.Small), NullValue = 8)]", "__d.Small = src.Small ?? (byte)8;")]
    public void SmallIntegerIsWrittenWithItsType(string attribute, string expected)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A short converts to a byte only explicitly, so it is reported where the int literal used to pass
    [Theory]
    [InlineData("[MapConstant<short>(nameof(Dst.Byte), 3)]")]
    [InlineData("[MapConstant(nameof(Dst.SByte), (byte)1)]")]
    public void SmallIntegerThatDoesNotConvertEmitsDiagnostic(string attribute)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(attribute));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0216", diagnostic.Id);
    }
}
