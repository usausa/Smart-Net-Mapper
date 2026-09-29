namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A required member that a parameter of the constructor a return mapper calls assigns has to be set in the object
// initializer as well, unless the constructor has [SetsRequiredMembers]. The generated code used to pass the
// argument only (CS9035), and setting the member again in the initializer would replace what the constructor made
// of the argument, so it is reported (SMP0307).
public class RequiredConstructorArgumentTests
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

    private static string Source(string destination, bool returns = true) =>
        $$"""
        #nullable enable
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Src { public int X { get; set; } public int Y { get; set; } }
        {{destination}}
        internal static partial class M
        {
            [Mapper]
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
        }
        """;

    [Theory]
    [InlineData("public class Dst { public Dst(int x) { X = x; } public required int X { get; set; } public int Y { get; } }", "x")]
    [InlineData("public class Dst { public Dst(int x, int y) { X = x; Y = y; } public required int X { get; set; } public int Y { get; } }", "x")]
    [InlineData("public record Dst(int X) { public required int X { get; init; } = X; }", "X")]
    [InlineData("internal class Dst { public Dst(int x) { X = x; } internal required int X; public int Y { get; } }", "x")]
    public void RequiredMemberAssignedByArgumentEmitsDiagnostic(string destination, string parameter)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(destination));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0307", diagnostic.Id);
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains("member=[X]", message, StringComparison.Ordinal);
        Assert.Contains($"parameter=[{parameter}]", message, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // [SetsRequiredMembers] lets the argument stand
    [Fact]
    public void SetsRequiredMembersConstructorCompiles()
    {
        var source = Source("public class Dst { [SetsRequiredMembers] public Dst(int x) { X = x; } public required int X { get; set; } public int Y { get; } }");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst(src.X);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A constructor construction does not call assigns nothing, and a void mapper does not construct
    [Fact]
    public void ConstructorNotCalledIsNotConcerned()
    {
        var source = Source("public class Dst { public Dst() { } public Dst(int x) { X = x; } public required int X { get; set; } }");

        AssertCompiles(source);
        Assert.Contains("X = src.X,", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Fact]
    public void VoidMapperIsNotConcerned()
    {
        AssertCompiles(Source("public class Dst { public Dst(int x) { X = x; } public required int X { get; set; } public int Y { get; } }", returns: false));
    }
}
