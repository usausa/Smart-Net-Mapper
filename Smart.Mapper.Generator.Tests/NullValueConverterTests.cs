namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// NullValue with a converter method: a null source takes NullValue, and the converter, which takes the source as it
// is, is called for a value only, in an assignment, a constructor argument and an object initializer alike. The
// converter used to be called with null, and NullValue was left out without being checked; it is now checked as well
// (SMP0218). NullBehavior.Skip still leaves the target as it is, NullValue aside.
public class NullValueConverterTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Src { public int? A { get; set; } public string? B { get; set; } }
        public class Dst { public string A { get; set; } = ""; public int B { get; set; } public string Init { get; init; } = ""; }
        public record DstRecord(string A);
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static string ToText(int? value) => "text";
            private static int ToLength(string? value) => 1;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.A), NullValue = \"none\", Converter = nameof(ToText))]", "__d.A = src.A is not null ? ToText(src.A) : \"none\";")]
    [InlineData("[MapProperty(nameof(Dst.B), NullValue = -1, Converter = nameof(ToLength))]", "__d.B = src.B is not null ? ToLength(src.B) : -1;")]
    [InlineData("[MapProperty(nameof(Dst.Init), nameof(Src.A), NullValue = \"none\", Converter = nameof(ToText))]", "Init = src.A is not null ? ToText(src.A) : \"none\",")]
    public void NullSourceTakesNullValue(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorArgumentTakesNullValue()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(DstRecord.A), NullValue = \"none\", Converter = nameof(ToText))]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.DstRecord(src.A is not null ? ToText(src.A) : \"none\")", generated, StringComparison.Ordinal);
    }

    // NullBehavior.Skip leaves the target as it is for a null source, NullValue aside
    [Fact]
    public void SkipLeavesNullValueOut()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.A), NullValue = \"none\", Converter = nameof(ToText), NullBehavior = NullBehavior.Skip)]",
            "public static partial void Map(Src src, Dst dst);"));

        Assert.Empty(problems);
        Assert.Contains("dst.A = ToText(src.A);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("\"none\"", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullValueThatCannotBeAssignedEmitsDiagnostic()
    {
        var (_, problems) = Build(Source("[MapProperty(nameof(Dst.B), NullValue = \"x\", Converter = nameof(ToLength))]"));

        Assert.Equal("SMP0218", Assert.Single(problems));
    }
}
