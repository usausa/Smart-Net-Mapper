namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A value of an enum no member of another enum has the name of gives null to a nullable enum target, where it gave
// the default value, and still gives the default value to a target that is not nullable. A string no member has the
// name of goes to a nullable enum target through Enum.TryParse, which gives null for one it cannot parse where
// Enum.Parse threw, and still through Enum.Parse to a target that is not nullable.
public class UnmatchedEnumMemberTests
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
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Source(string mapper, string attributes = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public enum Color { Red, Green, Blue }
        public enum Shade { Red, Green }
        public class Src { public Color Color { get; set; } public Color? Maybe { get; set; } public string Text { get; set; } = ""; public string? TextOrNull { get; set; } }
        public class Dst { public Shade? Color { get; set; } public Shade? Maybe { get; set; } public Color? Text { get; set; } public Color? TextOrNull { get; set; } }
        public class PlainDst { public Shade Color { get; set; } public Color Text { get; set; } }
        public record DstRecord(Shade? Color, Color? Text);
        public class InitDst { public Shade? Color { get; init; } public Color? TextOrNull { get; init; } }
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{mapper}}
        }
        """;

    [Fact]
    public void NullableTargetGetsNull()
    {
        var (generated, problems) = Build(Source("public static partial Dst Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("global::Test.Color.Green => global::Test.Shade.Green,\n            _ => null\n        };", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Maybe = src.Maybe is not null ? src.Maybe.GetValueOrDefault() switch", generated, StringComparison.Ordinal);
        Assert.Contains("_ => global::System.Enum.TryParse<global::Test.Color>(src.Text, out var __enum) ? __enum : null", generated, StringComparison.Ordinal);
        Assert.Contains("_ => global::System.Enum.TryParse<global::Test.Color>(src.TextOrNull, out var __enum) ? __enum : null", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("_ => default", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void PlainTargetGetsDefault()
    {
        var (generated, problems) = Build(Source("public static partial PlainDst Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("_ => default", generated, StringComparison.Ordinal);
        Assert.Contains("_ => global::System.Enum.Parse<global::Test.Color>(src.Text)", generated, StringComparison.Ordinal);
    }

    // A constructor argument and an object initializer entry, several in one statement
    [Theory]
    [InlineData("public static partial DstRecord Map(Src src);")]
    [InlineData("public static partial InitDst Map(Src src);")]
    public void ExpressionsTakeNull(string mapper)
    {
        var (generated, problems) = Build(Source(mapper));

        Assert.Empty(problems);
        Assert.Contains("_ => null", generated, StringComparison.Ordinal);
        Assert.Contains("? __enum : null", generated, StringComparison.Ordinal);
    }
}
