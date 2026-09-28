namespace Smart.Mapper.Generator.Tests;

using Microsoft.CodeAnalysis;

// A warning reported at the mapper method starts at its first attribute, and one reported at an attribute at that
// attribute, so a #pragma warning disable suppresses it only when it comes before that line: before the attributes of
// the method, not between them and the method. [SuppressMessage] on the method suppresses it wherever it is written.
public class PragmaSuppressionTests
{
    private static string Source(string declaration) =>
        $$"""
        #nullable enable
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Id { get; set; } public string? Name { get; set; } }
        public class Dst { public int Id { get; set; } public string Name { get; set; } = ""; public int Extra { get; set; } }
        public static partial class M
        {
        {{declaration}}
        }
        """;

    private static Diagnostic Warning(string source, string id) =>
        Assert.Single(GeneratorTestHelper.GetDiagnostics(source), d => d.Id == id);

    [Theory]
    [InlineData("#pragma warning disable SMP0501, SMP0502\n[Mapper(Strict = true)]\n[MapProperty(nameof(Dst.Name), nameof(Src.Name))]\npublic static partial Dst Map(Src src);\n#pragma warning restore SMP0501, SMP0502", true, true)]
    [InlineData("[Mapper(Strict = true)]\n[MapProperty(nameof(Dst.Name), nameof(Src.Name))]\n#pragma warning disable SMP0501, SMP0502\npublic static partial Dst Map(Src src);\n#pragma warning restore SMP0501, SMP0502", false, false)]
    [InlineData("[Mapper(Strict = true)]\n#pragma warning disable SMP0501, SMP0502\n[MapProperty(nameof(Dst.Name), nameof(Src.Name))]\npublic static partial Dst Map(Src src);\n#pragma warning restore SMP0501, SMP0502", false, true)]
    [InlineData("[SuppressMessage(\"Usage\", \"SMP0501\")]\n[SuppressMessage(\"Usage\", \"SMP0502\")]\n[Mapper(Strict = true)]\n[MapProperty(nameof(Dst.Name), nameof(Src.Name))]\npublic static partial Dst Map(Src src);", true, true)]
    public void PragmaSuppressesOnlyBeforeLocation(string declaration, bool methodWarningSuppressed, bool attributeWarningSuppressed)
    {
        var source = Source(declaration);

        Assert.Equal(methodWarningSuppressed, Warning(source, "SMP0501").IsSuppressed);
        Assert.Equal(attributeWarningSuppressed, Warning(source, "SMP0502").IsSuppressed);
    }

    // The diagnostics of the test runner come with their suppression, as the compiler reports them
    [Fact]
    public void WarningWithoutPragmaIsNotSuppressed()
    {
        var source = Source("[Mapper(Strict = true)]\n[MapProperty(nameof(Dst.Name), nameof(Src.Name))]\npublic static partial Dst Map(Src src);");

        Assert.False(Warning(source, "SMP0501").IsSuppressed);
        Assert.False(Warning(source, "SMP0502").IsSuppressed);
        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnostics(source), static d => d.Severity == DiagnosticSeverity.Error);
    }
}
