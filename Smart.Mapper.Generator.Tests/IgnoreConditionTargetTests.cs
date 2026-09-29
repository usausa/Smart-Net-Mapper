namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// [MapIgnore] and [MapCondition] name a target that has to exist, or they would do nothing without a word, as
// they used to. A name that is not found (misspelled, differently cased under the ordinal comparison, a missing
// segment of a dotted path) is reported as a target that is not found (SMP0102). A property or field of the
// destination, a dotted path of them, and a parameter of the constructor a return mapper calls are found, the name
// matched under the mapper's comparison (a [MapCondition] on a member no property mapping assigns is SMP0109, and
// a dotted target of [MapIgnore] is SMP0103).
public class IgnoreConditionTargetTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static void AssertDiagnostic(string source, string id, string target)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains($"target=[{target}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static string Source(string attributes, string mapper = "[Mapper]", string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Inner { public int Value { get; set; } }
        public class Src { public int Number { get; set; } public string Name { get; set; } = ""; }
        public class Dst
        {
            public int Number { get; set; }
            public string Name { get; set; } = "";
            public Inner Child { get; set; } = new();
            public int Field;
        }
        public record Positional(int Number, string Name);
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            {{signature}}
            private static bool Always(int value) => true;
        }
        """;

    [Theory]
    [InlineData("[MapIgnore(\"Missing\")]", "Missing")]
    [InlineData("[MapIgnore(\"name\")]", "name")]
    [InlineData("[MapIgnore(\"Child.Missing\")]", "Child.Missing")]
    [InlineData("[MapIgnore(\"Missing.Value\")]", "Missing.Value")]
    [InlineData("[MapCondition(\"Missing\", nameof(Always))]", "Missing")]
    [InlineData("[MapCondition(\"number\", nameof(Always))]", "number")]
    public void TargetThatIsNotFoundEmitsDiagnostic(string attribute, string target)
    {
        AssertDiagnostic(Source(attribute), "SMP0102", target);
    }

    // In a void mapper as well
    [Fact]
    public void TargetThatIsNotFoundInVoidMapperEmitsDiagnostic()
    {
        AssertDiagnostic(Source("[MapIgnore(\"Missing\")]", signature: "public static partial void Map(Src src, Dst dst);"), "SMP0102", "Missing");
    }

    [Theory]
    [InlineData("[MapIgnore(nameof(Dst.Name))]")]
    [InlineData("[MapIgnore(nameof(Dst.Field))]")]
    [InlineData("[MapCondition(nameof(Dst.Number), nameof(Always))]")]
    public void TargetThatExistsCompiles(string attribute)
    {
        AssertCompiles(Source(attribute));
    }

    // Matched under the mapper's comparison
    [Theory]
    [InlineData("[MapIgnore(\"name\")]")]
    [InlineData("[MapCondition(\"number\", nameof(Always))]")]
    public void TargetMatchedIgnoringCaseCompiles(string attribute)
    {
        AssertCompiles(Source(attribute, mapper: "[Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]"));
    }

    // A parameter of the constructor a return mapper calls is found; ignoring it is reported as before (SMP0304),
    // and so is a condition on it (SMP0306)
    [Theory]
    [InlineData("[MapIgnore(nameof(Positional.Name))]", "SMP0304")]
    [InlineData("[MapCondition(nameof(Positional.Number), nameof(Always))]", "SMP0306")]
    public void ConstructorParameterIsFound(string attribute, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(attribute, signature: "public static partial Positional Map(Src src);"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
    }
}
