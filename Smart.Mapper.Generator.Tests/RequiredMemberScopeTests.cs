namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Every required member of the destination a return mapper creates is set in its object initializer: the check
// that each one is mapped (SMP0303), and not ignored (SMP0216), covers properties and fields of any accessibility
// and those of the base types, where it used to leave out the properties that are not public, which then failed
// in the generated code (CS9035). A constructor with [SetsRequiredMembers] sets them all itself.
public class RequiredMemberScopeTests
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

    private static void AssertDiagnostic(string source, string id, string member)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains($"=[{member}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private static string Lines(string source) =>
        String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string destination, string attributes = "", bool returns = true) =>
        $$"""
        #nullable enable
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Y { get; set; } }
        {{destination}}
        internal static partial class M
        {
            [Mapper]
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
        }
        """;

    [Theory]
    [InlineData("internal class Dst { internal required int X { get; set; } public int Y { get; set; } }")]
    [InlineData("internal class Dst { protected internal required int X { get; set; } public int Y { get; set; } }")]
    [InlineData("internal class Dst { internal required int X; public int Y { get; set; } }")]
    [InlineData("internal class Base { internal required int X { get; set; } } internal class Dst : Base { public int Y { get; set; } }")]
    [InlineData("internal class Base { internal required int X; } internal class Dst : Base { public int Y { get; set; } }")]
    [InlineData("public class Base { public required int X { get; set; } } public class Dst : Base { public int Y { get; set; } }")]
    public void UnmappedRequiredMemberEmitsDiagnostic(string destination)
    {
        AssertDiagnostic(Source(destination), "SMP0303", "X");
    }

    // A property overriding a required one is required as well, and is reported once
    [Fact]
    public void UnmappedOverriddenRequiredPropertyEmitsDiagnostic()
    {
        AssertDiagnostic(
            Source("public class Base { public virtual required int X { get; set; } } public class Dst : Base { public override required int X { get; set; } public int Y { get; set; } }"),
            "SMP0303",
            "X");
    }

    [Theory]
    [InlineData("internal class Dst { internal required int X { get; set; } public int Y { get; set; } }")]
    [InlineData("internal class Base { internal required int X; } internal class Dst : Base { public int Y { get; set; } }")]
    public void IgnoredRequiredMemberEmitsDiagnostic(string destination)
    {
        AssertDiagnostic(Source(destination, "[MapIgnore(\"X\")]"), "SMP0216", "X");
    }

    // Mapped, the member is set in the object initializer
    [Theory]
    [InlineData("[MapConstant(\"X\", 5)]", "X = 5,")]
    [InlineData("[MapExpression(\"X\", \"src.Y * 2\")]", "X = __expression0(src),")]
    public void MappedRequiredMemberIsSetInInitializer(string attribute, string expected)
    {
        var source = Source("internal class Dst { internal required int X { get; set; } public int Y { get; set; } }", attribute);

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst()\n{\n" + expected + "\n};", Lines(source), StringComparison.Ordinal);
    }

    // A constructor with [SetsRequiredMembers] sets them, so they are neither required nor refused to be ignored
    [Theory]
    [InlineData("")]
    [InlineData("[MapIgnore(\"X\")]")]
    public void SetsRequiredMembersCoversThem(string attributes)
    {
        var source = Source("internal class Dst { [SetsRequiredMembers] public Dst() { } internal required int X { get; set; } public int Y { get; set; } }", attributes);

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst();\n__d.Y = src.Y;", Lines(source), StringComparison.Ordinal);
    }

    // A void mapper fills an instance that exists
    [Fact]
    public void VoidMapperIsNotConcerned()
    {
        var source = Source("internal class Dst { internal required int X { get; set; } public int Y { get; set; } }", returns: false);

        AssertCompiles(source);
        Assert.Contains("dst.Y = src.Y;", Lines(source), StringComparison.Ordinal);
    }
}
