namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A member along a dotted target path of [MapProperty] that the mapper cannot assign (get-only, init-only, or
// with a setter it cannot call) cannot be created with ??= (CS0200 / CS0272 / CS8852). The instance it holds
// is written into instead, under a null check that leaves the assignments out when it is null; the members
// after it that the mapper can assign are created inside the check.
public class NestedTargetPathTests
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

    // The generated method body with the indentation removed, one statement or brace per line
    private static string Body(string source)
    {
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var start = generated.IndexOf(" Map(", StringComparison.Ordinal);
        return String.Join("\n", generated[start..].Split('\n').Skip(1).Select(static l => l.Trim()).Where(static l => l.Length > 0));
    }

    private static string Source(string attributes, bool returns = false) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Leaf { public int Value { get; set; } }
        public class Inner
        {
            public int Value { get; set; }
            public string? Text { get; set; }
            public Leaf Held { get; } = new();
            public Leaf? Created { get; set; }
        }
        public class Src
        {
            public int Value { get; set; }
            public string? Text { get; set; }
            public SrcInner? Nested { get; set; }
        }
        public class SrcInner { public int Value { get; set; } }
        public class Dst
        {
            public Inner Settable { get; set; } = new();
            public Inner GetOnly { get; } = new();
            public Inner? Missing { get; }
            public Inner Private { get; private set; } = new();
            public Inner Init { get; init; } = new();
        }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
            private static bool IsPositive(int value) => value > 0;
        }
        """;

    [Theory]
    [InlineData("GetOnly", false)]
    [InlineData("GetOnly", true)]
    [InlineData("Missing", false)]
    [InlineData("Private", false)]
    [InlineData("Init", false)]
    [InlineData("Init", true)]
    public void MemberMapperCannotAssignIsWrittenInto(string member, bool returns)
    {
        var source = Source($"[MapProperty(\"{member}.Value\", nameof(Src.Value))]", returns);
        var destination = returns ? "__d" : "dst";

        AssertCompiles(source);
        var body = Body(source);
        Assert.Contains($"if ({destination}.{member} is not null)\n{{\n{destination}.{member}.Value = src.Value;\n}}", body, StringComparison.Ordinal);
        Assert.DoesNotContain("??=", body, StringComparison.Ordinal);
    }

    // A member the mapper can assign is created as before
    [Fact]
    public void SettableMemberIsCreated()
    {
        var source = Source("[MapProperty(\"Settable.Value\", nameof(Src.Value))]");

        AssertCompiles(source);
        Assert.Contains("dst.Settable ??= new global::Test.Inner();\ndst.Settable.Value = src.Value;", Body(source), StringComparison.Ordinal);
    }

    // Consecutive assignments under the same member share the check
    [Fact]
    public void AssignmentsUnderSameMemberShareCheck()
    {
        var source = Source(
            """
            [MapProperty("GetOnly.Value", nameof(Src.Value))]
            [MapProperty("GetOnly.Text", nameof(Src.Text))]
            """);

        AssertCompiles(source);
        Assert.Contains("if (dst.GetOnly is not null)\n{\ndst.GetOnly.Value = src.Value;\ndst.GetOnly.Text = src.Text;\n}", Body(source), StringComparison.Ordinal);
    }

    [Theory]
    // Held in held: both checked
    [InlineData("GetOnly.Held.Value", "if (dst.GetOnly is not null)\n{\nif (dst.GetOnly.Held is not null)\n{\ndst.GetOnly.Held.Value = src.Value;\n}\n}")]
    // Created in held: created inside the check
    [InlineData("GetOnly.Created.Value", "if (dst.GetOnly is not null)\n{\ndst.GetOnly.Created ??= new global::Test.Leaf();\ndst.GetOnly.Created.Value = src.Value;\n}")]
    // Held in created: created before the check
    [InlineData("Settable.Held.Value", "dst.Settable ??= new global::Test.Inner();\nif (dst.Settable.Held is not null)\n{\ndst.Settable.Held.Value = src.Value;\n}")]
    public void DeeperPathMixesChecksAndCreation(string path, string expected)
    {
        var source = Source($"[MapProperty(\"{path}\", nameof(Src.Value))]");

        AssertCompiles(source);
        Assert.Contains(expected, Body(source), StringComparison.Ordinal);
    }

    // The check goes inside the null check of a dotted source path, and around a condition or a skipped null
    [Theory]
    [InlineData("[MapProperty(\"GetOnly.Value\", \"Nested.Value\")]", "if (src.Nested is not null)\n{\nif (dst.GetOnly is not null)\n{\ndst.GetOnly.Value = src.Nested.Value;\n}\n}")]
    [InlineData("[MapProperty(\"GetOnly.Value\", nameof(Src.Value))][MapCondition(\"GetOnly.Value\", nameof(IsPositive))]", "if (dst.GetOnly is not null)\n{\nif (IsPositive(src.Value))\n{\ndst.GetOnly.Value = src.Value;\n}\n}")]
    [InlineData("[MapProperty(\"GetOnly.Text\", nameof(Src.Text), NullBehavior = NullBehavior.Skip)]", "if (dst.GetOnly is not null)\n{\nif (src.Text is not null)\n{\ndst.GetOnly.Text = src.Text;\n}\n}")]
    public void CheckCombinesWithOtherGuards(string attributes, string expected)
    {
        var source = Source(attributes);

        AssertCompiles(source);
        Assert.Contains(expected, Body(source), StringComparison.Ordinal);
    }
}
