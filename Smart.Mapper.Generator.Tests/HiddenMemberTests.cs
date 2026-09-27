namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The properties of a type and its base types are taken as x.Name binds to them: of the properties of a name, the
// most derived one. A property overriding one of a base type, or hiding it (new), used to be taken along with it,
// so the automatic mapping assigned the member twice: a second assignment, the value of a hiding property of
// another type converted to the hidden type (CS0029), or a required one set twice in the object initializer
// (CS1912). The names of the attributes and the Strict check go by the same properties.
public class HiddenMemberTests
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

    private static List<string> Lines(string source) =>
        GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0).ToList();

    private static string Source(string types, string mapper = "[Mapper]", string attributes = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    [Theory]
    [InlineData("public class Src { public string X { get; set; } = \"\"; } public class Base { public int X { get; set; } } public class Dst : Base { public new string X { get; set; } = \"\"; }", "__d.X = src.X;")]
    [InlineData("public class Src { public int X { get; set; } } public class Base { public virtual int X { get; set; } } public class Dst : Base { public override int X { get; set; } }", "__d.X = src.X;")]
    [InlineData("public class Src { public int X { get; set; } } public class Base { public virtual required int X { get; set; } } public class Dst : Base { public override required int X { get; set; } }", "X = src.X,")]
    public void PropertyOfBaseTypeIsMappedOnce(string types, string expected)
    {
        var source = Source(types);

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Single(lines, l => l == expected);
        Assert.DoesNotContain(lines, static l => l.Contains("ConvertToInt32", StringComparison.Ordinal));
    }

    // An attribute names the most derived property, of the type it declares
    [Fact]
    public void AttributeTargetsHidingProperty()
    {
        var source = Source(
            "public class Src { public string Text { get; set; } = \"\"; } public class Base { public int X { get; set; } } public class Dst : Base { public new string X { get; set; } = \"\"; }",
            "[Mapper(AutoMap = false)]",
            "[MapProperty(nameof(Dst.X), nameof(Src.Text))]");

        AssertCompiles(source);
        Assert.Contains("__d.X = src.Text;", Lines(source));
    }

    // A hiding property of the source is read as the one of its type
    [Fact]
    public void HidingSourcePropertyIsRead()
    {
        var source = Source("public class SrcBase { public int X { get; set; } } public class Src : SrcBase { public new string X { get; set; } = \"\"; } public class Dst { public string X { get; set; } = \"\"; }");

        AssertCompiles(source);
        Assert.Single(Lines(source), static l => l == "__d.X = src.X;");
    }

    // Strict reports a member once
    [Fact]
    public void StrictReportsHidingPropertyOnce()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            "public class Src { public int Y { get; set; } } public class Base { public int X { get; set; } } public class Dst : Base { public new string X { get; set; } = \"\"; }",
            "[Mapper(Strict = true)]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0501", diagnostic.Id);
        Assert.Contains("property=[X]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}
