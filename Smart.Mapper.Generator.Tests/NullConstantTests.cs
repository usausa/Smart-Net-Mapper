namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A null [MapConstant] value, a NullValue of null, and the null elements of an array constant have to go where
// the target takes null. One whose type is a reference annotated as not null is reported (SMP0218) instead of
// warning in the generated code (CS8625 / CS8601 / CS8619); a nullable target, one declared with nullable
// annotations disabled, and one with [AllowNull] keep compiling.
public class NullConstantTests
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

    private static void AssertDiagnostic(string source, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
    }

    private static string Source(string attributes, bool returns = true) =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Src
        {
            public string? Name { get; set; }
            public string[]? Names { get; set; }
        }
        public class Dst
        {
            public string Name { get; set; } = "";
            public string? Label { get; set; }
            public object Value { get; set; } = "";
            public object? Any { get; set; }
            [AllowNull] public string Allowed { get; set; } = "";
            [DisallowNull] public string? Disallowed { get; set; }
            public string[] Names { get; set; } = [];
            public string?[] Labels { get; set; } = [];
            public IEnumerable<string> Sequence { get; set; } = [];
            public IEnumerable<string?> Labelled { get; set; } = [];
            public object[] Values { get; set; } = [];
            public required string Code { get; init; }
        }
        #nullable disable
        public class Legacy
        {
            public string Name { get; set; }
            public string[] Names { get; set; }
        }
        #nullable enable
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            [MapConstant(nameof(Dst.Code), "")]
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Legacy dst);")}}
        }
        """;

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Name), null)]")]
    [InlineData("[MapConstant(nameof(Dst.Value), null)]")]
    [InlineData("[MapConstant(nameof(Dst.Disallowed), null)]")]
    [InlineData("[MapConstant<string>(nameof(Dst.Name), null!)]")]
    [InlineData("[MapProperty(nameof(Dst.Name), NullValue = null)]")]
    public void NullIntoNonNullableReferenceEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute), "SMP0218");
    }

    // A required member assigned in the object initializer is checked the same way
    [Fact]
    public void NullIntoInitializerEntryEmitsDiagnostic()
    {
        AssertDiagnostic(Source(string.Empty).Replace("[MapConstant(nameof(Dst.Code), \"\")]", "[MapConstant(nameof(Dst.Code), null)]", StringComparison.Ordinal), "SMP0218");
    }

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Label), null)]", "__d.Label = null;")]
    [InlineData("[MapConstant(nameof(Dst.Any), null)]", "__d.Any = null;")]
    [InlineData("[MapConstant(nameof(Dst.Allowed), null)]", "__d.Allowed = null;")]
    [InlineData("[MapProperty(nameof(Dst.Label), nameof(Src.Name), NullValue = null)]", "__d.Label = src.Name ?? null;")]
    public void NullIntoTargetTakingNullCompiles(string attribute, string expected)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A member declared with nullable annotations disabled takes null as before
    [Theory]
    [InlineData("[MapConstant(nameof(Legacy.Name), null)]", "dst.Name = null;")]
    [InlineData("[MapConstant(nameof(Legacy.Names), new[] { \"a\", null })]", "dst.Names = new string?[] { \"a\", null };")]
    [InlineData("[MapProperty(nameof(Legacy.Name), nameof(Src.Name), NullValue = null)]", "dst.Name = src.Name ?? null;")]
    public void NullIntoObliviousTargetCompiles(string attribute, string expected)
    {
        var source = Source(attribute, returns: false).Replace("[MapConstant(nameof(Dst.Code), \"\")]", string.Empty, StringComparison.Ordinal);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Names), new[] { \"a\", null })]")]
    [InlineData("[MapConstant(nameof(Dst.Sequence), new[] { \"a\", null })]")]
    [InlineData("[MapConstant(nameof(Dst.Values), new object?[] { 1, null })]")]
    [InlineData("[MapProperty(nameof(Dst.Names), NullValue = new[] { \"a\", null })]")]
    public void NullElementIntoNonNullableElementsEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute), "SMP0218");
    }

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Labels), new[] { \"a\", null })]", "__d.Labels = new string?[] { \"a\", null };")]
    [InlineData("[MapConstant(nameof(Dst.Labelled), new[] { \"a\", null })]", "__d.Labelled = new string?[] { \"a\", null };")]
    [InlineData("[MapConstant(nameof(Dst.Any), new[] { \"a\", null })]", "__d.Any = new string?[] { \"a\", null };")]
    [InlineData("[MapConstant(nameof(Dst.Names), new[] { \"a\", \"b\" })]", "__d.Names = new string[] { \"a\", \"b\" };")]
    public void ArrayIntoElementsTakingItCompiles(string attribute, string expected)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
