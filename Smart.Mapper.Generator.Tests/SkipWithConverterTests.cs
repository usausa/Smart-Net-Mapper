namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// NullBehavior.Skip leaves the destination member as it is for a null source when a converter method maps the
// member too: the converter, which takes the source as it is, is called only for a value. It used to be called for
// null as well, and the destination got its result, as if Skip had not been given.
public class SkipWithConverterTests
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

    private static string Source(string attributes) =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Child { public string? Name { get; set; } }
        public class Src { public int? A { get; set; } public string? B { get; set; } public Child? Child { get; set; } }
        public class Dst { public string A { get; set; } = ""; public int B { get; set; } public int C { get; set; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            public static partial void Map(Src src, Dst dst);

            private static string ToText(int? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
            private static int ToLength(string? value) => value?.Length ?? -1;
            private static bool IsWanted(int? value) => true;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.A), NullBehavior = NullBehavior.Skip, Converter = nameof(ToText))]", "if (src.A is not null)", "dst.A = ToText(src.A);")]
    [InlineData("[MapProperty(nameof(Dst.B), NullBehavior = NullBehavior.Skip, Converter = nameof(ToLength))]", "if (src.B is not null)", "dst.B = ToLength(src.B);")]
    [InlineData("[MapProperty(nameof(Dst.C), \"Child.Name\", NullBehavior = NullBehavior.Skip, Converter = nameof(ToLength))]", "if (src.Child.Name is not null)", "dst.C = ToLength(src.Child.Name);")]
    [InlineData("[MapProperty(nameof(Dst.A), NullBehavior = NullBehavior.Skip, Converter = nameof(ToText))] [MapCondition(nameof(Dst.A), nameof(IsWanted))]", "if (src.A is not null)", "dst.A = ToText(src.A);")]
    public void ConverterIsCalledForValueOnly(string attributes, string guard, string assignment)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        var guardIndex = generated.IndexOf(guard, StringComparison.Ordinal);
        Assert.True(guardIndex >= 0, generated);
        Assert.True(generated.IndexOf(assignment, StringComparison.Ordinal) > guardIndex, generated);
    }

    // Without Skip, the converter takes null as before
    [Fact]
    public void ConverterTakesNullWithoutSkip()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.A), Converter = nameof(ToText))]"));

        Assert.Empty(problems);
        Assert.Contains("dst.A = ToText(src.A);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("if (src.A is not null)", generated, StringComparison.Ordinal);
    }
}
