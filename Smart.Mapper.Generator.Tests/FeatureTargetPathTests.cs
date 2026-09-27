namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A dotted target of [MapConstant], [MapExpression] and [MapUsing] goes through its intermediate members the way
// one of [MapProperty] does: a member the mapper can create is created when null, one whose instance is written
// into is null-checked, and a struct property is copied and assigned back. It used to be assigned as written,
// which threw on a null member (and warned, CS8602). The method of [MapUsing] is matched against the type of a
// dotted or field target, with the custom parameters and the parameter modifiers, as it is for a property; it
// used to be called without them (CS7036). [MapExpression] gets its local function for such a target too.
public class FeatureTargetPathTests
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

    private static string Body(string source)
    {
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var start = generated.IndexOf(" Map(", StringComparison.Ordinal);
        return String.Join("\n", generated[start..].Split('\n').Skip(1).Select(static l => l.Trim()).Where(static l => l.Length > 0));
    }

    private static string Source(string attributes, string signature = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Inner { public int Value { get; set; } public string? Text { get; set; } }
        public struct Point { public int X { get; set; } }
        public class Src { public int X { get; set; } }
        public class Context { public int Offset { get; set; } }
        public class Dst
        {
            public Inner? Child { get; set; }
            public Inner Held { get; } = new();
            public Point Point { get; set; }
            public int Field;
            public Inner? FieldChild;
        }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{signature}}
            private static int Calc(Src src) => src.X;
            private static int WithContext(Src src, Context context) => src.X + context.Offset;
            private static int ByRef(in Src src, ref Context context) => src.X + context.Offset;
            private static string Text(Src src) => "text";
        }
        """;

    [Theory]
    [InlineData("[MapConstant(\"Child.Value\", 1)]", "dst.Child ??= new global::Test.Inner();\ndst.Child.Value = 1;")]
    [InlineData("[MapExpression(\"Child.Value\", \"src.X + 1\")]", "dst.Child ??= new global::Test.Inner();\ndst.Child.Value = __expression0(src, dst);")]
    [InlineData("[MapUsing(\"Child.Value\", nameof(Calc))]", "dst.Child ??= new global::Test.Inner();\ndst.Child.Value = Calc(src);")]
    [InlineData("[MapConstant(\"FieldChild.Value\", 1)]", "dst.FieldChild ??= new global::Test.Inner();\ndst.FieldChild.Value = 1;")]
    [InlineData("[MapConstant(\"Held.Value\", 1)]", "if (dst.Held is not null)\n{\ndst.Held.Value = 1;\n}")]
    [InlineData("[MapUsing(\"Point.X\", nameof(Calc))]", "{\nvar __copy0 = dst.Point;\n__copy0.X = Calc(src);\ndst.Point = __copy0;\n}")]
    public void DottedTargetGoesThroughIntermediateMembers(string attribute, string expected)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(expected, Body(source), StringComparison.Ordinal);
    }

    // Consecutive assignments share the creation and the checks
    [Fact]
    public void ConsecutiveTargetsShareIntermediateMembers()
    {
        var source = Source("""
            [MapConstant("Held.Value", 1)]
            [MapConstant("Held.Text", "a")]
            """);

        AssertCompiles(source);
        Assert.Contains("if (dst.Held is not null)\n{\ndst.Held.Value = 1;\ndst.Held.Text = \"a\";\n}", Body(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapUsing(\"Child.Value\", nameof(WithContext))]", "public static partial void Map(Src src, Dst dst, Context context);", "dst.Child.Value = WithContext(src, context);")]
    [InlineData("[MapUsing(nameof(Dst.Field), nameof(WithContext))]", "public static partial void Map(Src src, Dst dst, Context context);", "dst.Field = WithContext(src, context);")]
    [InlineData("[MapUsing(\"Child.Value\", nameof(ByRef))]", "public static partial void Map(Src src, Dst dst, ref Context context);", "dst.Child.Value = ByRef(in src, ref context);")]
    public void MapUsingMethodIsMatchedForDottedAndFieldTargets(string attribute, string signature, string expected)
    {
        var source = Source(attribute, signature);

        AssertCompiles(source);
        Assert.Contains(expected, Body(source), StringComparison.Ordinal);
    }

    // A method returning another type is reported for such a target as for a property
    [Theory]
    [InlineData("[MapUsing(\"Child.Value\", nameof(Text))]")]
    [InlineData("[MapUsing(nameof(Dst.Field), nameof(Text))]")]
    public void MapUsingReturnTypeMismatchEmitsDiagnostic(string attribute)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(attribute));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0202", diagnostic.Id);
    }

    // The expression of a dotted target is compiled as a local function of the target's type
    [Fact]
    public void MapExpressionGetsLocalFunctionForDottedTarget()
    {
        var source = Source("[MapExpression(\"Child.Text\", \"src.X.ToString()\")]");

        AssertCompiles(source);
        Assert.Contains("static string? __expression0(global::Test.Src src, global::Test.Dst dst) => src.X.ToString();", Body(source), StringComparison.Ordinal);
    }
}
