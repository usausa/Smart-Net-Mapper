namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// An init-only member at the end of a dotted target path cannot be assigned after construction (CS8852). A
// return mapper assigns it in its object initializer, creating the members it goes through there (Child = new
// Inner() { Value = ... }), when each of them can be assigned there and created; the init-only members under
// the same member share its creation. A void mapper cannot (SMP0302), and a path the initializer cannot create
// either, through a get-only member or a type the mapper cannot create, is reported (SMP0102). This holds for
// the targets of [MapProperty], [MapConstant], [MapExpression] and [MapUsing].
public class InitOnlyTargetPathTests
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

    private static void AssertDiagnostic(string source, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private static string Body(string source)
    {
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var start = generated.IndexOf(" Map(", StringComparison.Ordinal);
        return String.Join("\n", generated[start..].Split('\n').Skip(1).Select(static l => l.Trim()).Where(static l => l.Length > 0));
    }

    private static string Source(string attributes, bool returns = true) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Leaf { public int Value { get; init; } public int Plain { get; set; } }
        public class Inner { public int Value { get; init; } public int Other { get; init; } public int Plain { get; set; } public Leaf Leaf { get; set; } = new(); }
        public abstract class Abstract { public int Value { get; init; } }
        public class Src { public int X { get; set; } public int Y { get; set; } }
        public class Dst
        {
            public Inner Child { get; set; } = new();
            public Inner InitChild { get; init; } = new();
            public Inner GetOnly { get; } = new();
            public Abstract? Abstract { get; set; }
            public int Top { get; set; }
        }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
            private static int Calc(Src src) => src.Y;
        }
        """;

    [Fact]
    public void InitOnlyMembersAreSetInInitializer()
    {
        var source = Source("""
            [MapProperty("Child.Value", nameof(Src.X))]
            [MapProperty("Child.Other", nameof(Src.Y))]
            [MapProperty("Child.Plain", nameof(Src.X))]
            [MapProperty(nameof(Dst.Top), nameof(Src.Y))]
            """);

        AssertCompiles(source);
        Assert.Contains(
            "var __d = new global::Test.Dst()\n{\nChild = new global::Test.Inner()\n{\nValue = src.X,\nOther = src.Y,\n},\n};\n" +
            "__d.Child ??= new global::Test.Inner();\n__d.Child.Plain = src.X;\n__d.Top = src.Y;",
            Body(source),
            StringComparison.Ordinal);
    }

    // Deeper paths nest the creations, and the other attributes join them in their order
    [Fact]
    public void NestedPathsAndOtherAttributesShareCreation()
    {
        var source = Source("""
            [MapProperty("Child.Leaf.Value", nameof(Src.X))]
            [MapConstant("Child.Value", 5)]
            [MapExpression("Child.Other", "src.X * 2")]
            [MapUsing("InitChild.Value", nameof(Calc))]
            """);

        AssertCompiles(source);
        Assert.Contains(
            "{\nChild = new global::Test.Inner()\n{\nLeaf = new global::Test.Leaf()\n{\nValue = src.X,\n},\nValue = 5,\nOther = __expression0(src),\n},\n" +
            "InitChild = new global::Test.Inner()\n{\nValue = Calc(src),\n},\n};",
            Body(source),
            StringComparison.Ordinal);
    }

    // A settable member under an init-only one is written into the instance the initializer created
    [Fact]
    public void SettableMemberUnderInitOnlyMemberIsWrittenInto()
    {
        var source = Source("""
            [MapProperty("InitChild.Value", nameof(Src.X))]
            [MapProperty("InitChild.Plain", nameof(Src.Y))]
            """);

        AssertCompiles(source);
        Assert.Contains(
            "InitChild = new global::Test.Inner()\n{\nValue = src.X,\n},\n};\nif (__d.InitChild is not null)\n{\n__d.InitChild.Plain = src.Y;\n}",
            Body(source),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapProperty(\"Child.Value\", nameof(Src.X))]")]
    [InlineData("[MapConstant(\"Child.Value\", 1)]")]
    [InlineData("[MapExpression(\"Child.Leaf.Value\", \"src.X\")]")]
    [InlineData("[MapUsing(\"InitChild.Value\", nameof(Calc))]")]
    public void InitOnlyMemberInVoidMapperEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute, returns: false), "SMP0302");
    }

    // An initializer cannot assign a get-only member, nor create an abstract one
    [Theory]
    [InlineData("[MapProperty(\"GetOnly.Value\", nameof(Src.X))]", true)]
    [InlineData("[MapConstant(\"Abstract.Value\", 1)]", true)]
    [InlineData("[MapProperty(\"GetOnly.Value\", nameof(Src.X))]", false)]
    public void InitOnlyMemberInitializerCannotReachEmitsDiagnostic(string attribute, bool returns)
    {
        AssertDiagnostic(Source(attribute, returns), "SMP0102");
    }
}
