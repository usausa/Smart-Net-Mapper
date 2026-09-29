namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// An interface extending two interfaces that each declare a member of a name, neither hiding the other, makes the
// name ambiguous: x.Name binds to neither (CS0229). The automatic mapping used to take the first and fail there; the
// name reaches no member, so it is not mapped, and an attribute naming it reports a target that is not found. One
// hidden by a member of an interface extending its interface is not ambiguous.
public class AmbiguousMemberTests
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

    private static string Source(string types, string signature, string attributes = "", string mapper = "[Mapper]") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public interface IA { int X { get; set; } int Y { get; set; } }
        public interface IB { int X { get; set; } }
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            {{signature}}
        }
        """;

    [Fact]
    public void AmbiguousSourceNameIsNotMapped()
    {
        var source = Source(
            "public interface ISrc : IA, IB { } public class Dst { public int X { get; set; } public int Y { get; set; } }",
            "public static partial Dst Map(ISrc src);");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Y = src.Y;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.X", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void AmbiguousDestinationNameIsNotMapped()
    {
        var source = Source(
            "public interface IDst : IA, IB { } public class Src { public int X { get; set; } public int Y { get; set; } }",
            "public static partial void Map(Src src, IDst dst);");

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("dst.Y = src.Y;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dst.X", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void AttributeNamingAmbiguousMemberEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            "public interface IDst : IA, IB { } public class Src { public int X { get; set; } }",
            "public static partial void Map(Src src, IDst dst);",
            "[MapConstant(\"X\", 1)]",
            "[Mapper(AutoMap = false)]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0102", diagnostic.Id);
        Assert.Contains("target=[X]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // A member of an interface extending both hides theirs
    [Fact]
    public void HidingMemberIsNotAmbiguous()
    {
        var source = Source(
            "public interface IDst : IA, IB { new int X { get; set; } } public class Src { public int X { get; set; } }",
            "public static partial void Map(Src src, IDst dst);");

        AssertCompiles(source);
        Assert.Contains("dst.X = src.X;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
