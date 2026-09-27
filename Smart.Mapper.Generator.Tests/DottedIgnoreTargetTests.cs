namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// [MapIgnore] keeps a member of the destination from the automatic mapping, which assigns the members as a whole,
// so a dotted target (Child.Value) did nothing without a word: the member was still copied as a whole. Leaving out
// a part of what a member is assigned cannot be done, so it is reported (SMP0223). A dotted target another
// attribute maps as well is still reported as naming the same target (SMP0101), and one that is not found as not
// found (SMP0214).
public class DottedIgnoreTargetTests
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
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains($"target=[{target}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private static string Source(string attributes, string mapper = "[Mapper]", string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Inner { public int Value { get; set; } }
        public class Holder { public Inner? Inner { get; set; } }
        public class Src { public Inner? Child { get; set; } public Holder? Holder { get; set; } public int Number { get; set; } }
        public class Dst { public Inner? Child { get; set; } public Holder? Holder { get; set; } public int Number { get; set; } }
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            {{signature}}
        }
        """;

    [Theory]
    [InlineData("[MapIgnore(\"Child.Value\")]", "Child.Value")]
    [InlineData("[MapIgnore(\"Holder.Inner.Value\")]", "Holder.Inner.Value")]
    [InlineData("[MapIgnore(\"Holder.Inner\")]", "Holder.Inner")]
    public void DottedTargetEmitsDiagnostic(string attribute, string target)
    {
        AssertDiagnostic(Source(attribute), "SMP0223", target);
    }

    [Fact]
    public void DottedTargetInVoidMapperEmitsDiagnostic()
    {
        AssertDiagnostic(Source("[MapIgnore(\"Child.Value\")]", signature: "public static partial void Map(Src src, Dst dst);"), "SMP0223", "Child.Value");
    }

    // Matched as declared under the mapper's name comparison
    [Fact]
    public void DottedTargetNamedIgnoringCaseEmitsDiagnostic()
    {
        AssertDiagnostic(Source("[MapIgnore(\"child.value\")]", "[Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]"), "SMP0223", "Child.Value");
    }

    [Fact]
    public void DottedTargetMappedAsWellEmitsDuplicate()
    {
        AssertDiagnostic(Source("[MapIgnore(\"Child.Value\")] [MapProperty(\"Child.Value\", nameof(Src.Number))]"), "SMP0101", "Child.Value");
    }

    [Fact]
    public void MissingDottedTargetEmitsNotFound()
    {
        AssertDiagnostic(Source("[MapIgnore(\"Child.Missing\")]"), "SMP0214", "Child.Missing");
    }

    // A member as a whole is left out as before
    [Fact]
    public void WholeMemberIsLeftOut()
    {
        var source = Source("[MapIgnore(nameof(Dst.Child))]");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.DoesNotContain("__d.Child", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Holder = src.Holder;", generated, StringComparison.Ordinal);
    }
}
