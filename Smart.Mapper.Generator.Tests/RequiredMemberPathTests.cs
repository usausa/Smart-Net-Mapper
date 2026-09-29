namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A required member of the destination a return mapper creates is set when the object initializer creates it. The
// dotted paths into one used to leave it reported as unmapped (SMP0308), even where the initializer created it with
// the member at the end of a path (Item = new Child() { Value = ... }). A member of a type that can be created is
// now created in the initializer, and the paths write into it after construction as into any member, a
// [MapCondition] still guarding them; one of a type that cannot be created is still reported.
public class RequiredMemberPathTests
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

    private static string Lines(string source) =>
        String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string destination, string attributes, string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Child { public int B { get; set; } public int C { get; set; } }
        public class InitChild { public int B { get; init; } public int C { get; set; } }
        public struct Point { public int B { get; set; } }
        public abstract class Shape { public int B { get; set; } }
        public class Owned { public required int B { get; set; } public int C { get; set; } }
        public class Created { public Created(int b) { B = b; } public int B { get; set; } }
        public class Src { public int Y { get; set; } }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{signature}}

            private static int Calc(Src src) => src.Y;
            private static bool IsPositive(int value) => value > 0;
        }
        """;

    // A path only the object initializer reaches creates the member there, which is then set
    [Fact]
    public void PathOnlyInitializerReachesSetsMember()
    {
        var source = Source("public class Dst { public required InitChild Item { get; set; } }", "[MapProperty(\"Item.B\", nameof(Src.Y))]");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst()\n{\nItem = new global::Test.InitChild()\n{\nB = src.Y,\n},\n};", Lines(source), StringComparison.Ordinal);
    }

    // The paths written after construction write into the member the initializer creates
    [Theory]
    [InlineData("[MapProperty(\"Item.B\", nameof(Src.Y))]", "__d.Item.B = src.Y;")]
    [InlineData("[MapConstant(\"Item.B\", 3)]", "__d.Item.B = 3;")]
    [InlineData("[MapExpression(\"Item.B\", \"src.Y + 1\")]", "__d.Item.B = __expression0(src);")]
    [InlineData("[MapUsing(\"Item.B\", nameof(Calc))]", "__d.Item.B = Calc(src);")]
    public void MemberWrittenAfterConstructionIsCreatedInInitializer(string attribute, string expected)
    {
        var source = Source("public class Dst { public required Child Item { get; set; } public int Y { get; set; } }", attribute);

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("var __d = new global::Test.Dst()\n{\nItem = new global::Test.Child(),\n};", lines, StringComparison.Ordinal);
        Assert.Contains(expected, lines, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Item ??=", lines, StringComparison.Ordinal);
    }

    // A condition guards the path as before, and an init-only member is written into when it holds the instance
    [Fact]
    public void ConditionGuardsPath()
    {
        var source = Source(
            "public class Dst { public required Child Item { get; set; } }",
            "[MapProperty(\"Item.B\", nameof(Src.Y))] [MapCondition(\"Item.B\", nameof(IsPositive))]");

        AssertCompiles(source);
        Assert.Contains("Item = new global::Test.Child(),\n};\nif (IsPositive(src.Y))\n{\n__d.Item.B = src.Y;\n}", Lines(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public class Dst { public required Child Item { get; init; } }", "Item = new global::Test.Child(),\n};\nif (__d.Item is not null)\n{\n__d.Item.B = src.Y;\n}")]
    [InlineData("public class Dst { public required Point Item { get; set; } }", "Item = new global::Test.Point(),\n};\n{\nvar __copy0 = __d.Item;\n__copy0.B = src.Y;\n__d.Item = __copy0;\n}")]
    public void MemberIsCreatedWhateverItsAccess(string destination, string expected)
    {
        var source = Source(destination, "[MapProperty(\"Item.B\", nameof(Src.Y))]");

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // A type the mapper cannot create leaves the member unset, as before
    [Theory]
    [InlineData("public class Dst { public required Shape Item { get; set; } }", "Item.B")]
    [InlineData("public class Dst { public required Owned Item { get; set; } }", "Item.C")]
    [InlineData("public class Dst { public required Created Item { get; set; } }", "Item.B")]
    public void UncreatableMemberEmitsDiagnostic(string destination, string path)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(destination, $"[MapProperty(\"{path}\", nameof(Src.Y))]"));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0308", diagnostic.Id);
        Assert.Contains("member=[Item]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // A void mapper fills an instance that exists
    [Fact]
    public void VoidMapperIsNotConcerned()
    {
        var source = Source("public class Dst { public required Child Item { get; set; } }", "[MapProperty(\"Item.B\", nameof(Src.Y))]", "public static partial void Map(Src src, Dst dst);");

        AssertCompiles(source);
        Assert.Contains("dst.Item ??= new global::Test.Child();\ndst.Item.B = src.Y;", Lines(source), StringComparison.Ordinal);
    }
}
