namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A name is resolved the way the generated code binds x.Name: to the most derived member the mapper class can
// access, of any kind. The public properties used to be taken first, so a member hiding one of them (an internal
// property, a field, a method, a static property) was passed over and the hidden property mapped, while x.Name
// reached the hiding one (CS0029 / CS1656 / CS0176). A name whose member is not a public instance property is not
// mapped automatically, nor is the one it hides; an attribute that takes other members targets the hiding one. A
// member the mapper class cannot access (private) hides nothing from it. The source and Strict go the same way.
public class HiddenMemberAccessTests
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

    private static string Source(string types, string attributes = "", string mapper = "[Mapper]") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Base { public int X { get; set; } }
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    private const string PlainSource = "public class Src { public int X { get; set; } public int Y { get; set; } }";

    [Theory]
    [InlineData("public class Dst : Base { internal new string X { get; set; } = \"\"; public int Y { get; set; } }")]
    [InlineData("public class Dst : Base { public new string X = \"\"; public int Y { get; set; } }")]
    [InlineData("public class Dst : Base { public new int X() => 1; public int Y { get; set; } }")]
    [InlineData("public class Dst : Base { public static new int X { get; set; } public int Y { get; set; } }")]
    public void NameHiddenByOtherMemberIsNotMapped(string destination)
    {
        var source = Source(PlainSource + " " + destination);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Y = src.Y;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.X", generated, StringComparison.Ordinal);
    }

    // A private member of the destination is not what the mapper class reaches
    [Fact]
    public void InaccessibleMemberHidesNothing()
    {
        var source = Source(PlainSource + " public class Dst : Base { private new string X { get; set; } = \"\"; public string Read() => X; }");

        AssertCompiles(source);
        Assert.Contains("__d.X = src.X;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // An attribute that takes an internal member targets the hiding one; [MapProperty] takes public properties only
    [Fact]
    public void ConstantTargetsHidingMember()
    {
        var source = Source(PlainSource + " public class Dst : Base { internal new string X { get; set; } = \"\"; }", "[MapConstant(\"X\", \"text\")]", "[Mapper(AutoMap = false)]");

        AssertCompiles(source);
        Assert.Contains("__d.X = \"text\";", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Fact]
    public void PropertyTargetingHidingMemberEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            PlainSource + " public class Dst : Base { internal new string X { get; set; } = \"\"; }",
            "[MapProperty(\"X\", nameof(Src.Y))]",
            "[Mapper(AutoMap = false)]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0214", diagnostic.Id);
        Assert.Contains("target=[X]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // The source is read the same way, and Strict does not report a name the mapping cannot take
    [Fact]
    public void SourceNameHiddenByInternalMemberIsNotRead()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class SrcBase { public int X { get; set; } }
            public class Src : SrcBase { internal new string X { get; set; } = ""; public int Y { get; set; } }
            public class Dst { public int X { get; set; } public int Y { get; set; } }
            public static partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Y = src.Y;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.X", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void StrictDoesNotReportHiddenName()
    {
        AssertCompiles(Source(PlainSource + " public class Dst : Base { internal new string X { get; set; } = \"\"; public int Y { get; set; } }", mapper: "[Mapper(Strict = true)]"));
    }
}
