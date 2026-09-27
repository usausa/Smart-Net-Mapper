namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A dotted source whose intermediate member is null takes NullValue: in an assignment, an else of the null check of
// the intermediate members gives NullValue to the mappings of the group that have one, as a constructor argument and
// an object initializer already did. Without NullValue, and with NullBehavior.Skip or a [MapCondition], which has no
// source value to test, the target is left as it is, as before. The NullValue given this way is checked (SMP0218).
public class NullValueIntermediateTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Lines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Source(string attributes, string mapper = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Leaf { public string? Name { get; set; } public int Value { get; set; } public int? Count { get; set; } }
        public class Mid { public Leaf? Leaf { get; set; } }
        public class Info { public string Name { get; set; } = ""; }
        public class Src { public Leaf? C { get; set; } public Mid? Mid { get; set; } }
        public class Dst
        {
            public string Name { get; set; } = "";
            public int Value { get; set; }
            public int Count { get; set; }
            public string Text { get; set; } = "";
            public string Skipped { get; set; } = "";
            public string Guarded { get; set; } = "";
            public Info? Info { get; set; }
        }
        public record DstRecord(string Name) { public int Value { get; init; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static string ToText(int? value) => "text";
            private static bool IsWanted(string? value) => true;
        }
        """;

    // Of the mappings sharing the null check, only the ones with NullValue take it in the else
    [Fact]
    public void GroupGivesNullValueForNullIntermediate()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Name), \"C.Name\", NullValue = \"none\")] " +
            "[MapProperty(nameof(Dst.Value), \"C.Value\")] " +
            "[MapProperty(nameof(Dst.Count), \"C.Count\", NullValue = -1)] " +
            "[MapProperty(nameof(Dst.Text), \"C.Count\", NullValue = \"none\", Converter = nameof(ToText))] " +
            "[MapProperty(nameof(Dst.Skipped), \"C.Name\", NullValue = \"skip\", NullBehavior = NullBehavior.Skip)] " +
            "[MapProperty(nameof(Dst.Guarded), \"C.Name\", NullValue = \"guard\")] [MapCondition(nameof(Dst.Guarded), nameof(IsWanted))]"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    else
                    {
                        dst.Name = "none";
                        dst.Count = -1;
                        dst.Text = "none";
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains("dst.Text = src.C.Count is not null ? ToText(src.C.Count) : \"none\";", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("\"skip\"", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dst.Guarded = \"guard\";", generated, StringComparison.Ordinal);
    }

    // Without NullValue there is no else, as before
    [Fact]
    public void GroupWithoutNullValueHasNoElse()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Name), \"C.Name\")] [MapProperty(nameof(Dst.Value), \"C.Value\")]"));

        Assert.Empty(problems);
        Assert.DoesNotContain("else", generated, StringComparison.Ordinal);
    }

    // Two nullable intermediate members, and a dotted target created in the else as well
    [Fact]
    public void DottedTargetIsCreatedInElse()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(\"Info.Name\", \"Mid.Leaf.Name\", NullValue = \"none\")] [MapProperty(nameof(Dst.Value), \"Mid.Leaf.Value\", NullValue = -1)]",
            "public static partial Dst Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Mid is not null && src.Mid.Leaf is not null)", generated, StringComparison.Ordinal);
        Assert.Contains(
            Lines("""
                    else
                    {
                        __d.Info ??= new global::Test.Info();
                        __d.Info.Name = "none";
                        __d.Value = -1;
                    }
            """),
            generated,
            StringComparison.Ordinal);
    }

    // A constructor argument and an object initializer take NullValue for a null intermediate member, as before
    [Fact]
    public void ExpressionsTakeNullValue()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(DstRecord.Name), \"Mid.Leaf.Name\", NullValue = \"none\")] [MapProperty(nameof(DstRecord.Value), \"Mid.Leaf.Value\", NullValue = -1)]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.DstRecord(src.Mid is not null && src.Mid.Leaf is not null ? src.Mid.Leaf.Name ?? \"none\" : \"none\")", generated, StringComparison.Ordinal);
        Assert.Contains("Value = src.Mid is not null && src.Mid.Leaf is not null ? src.Mid.Leaf.Value : -1,", generated, StringComparison.Ordinal);
    }

    // The NullValue of a member that cannot be null itself is written for a null intermediate member, so it is checked
    [Fact]
    public void NullValueForNullIntermediateIsChecked()
    {
        var (_, problems) = Build(Source("[MapProperty(nameof(Dst.Value), \"C.Value\", NullValue = \"x\")]"));

        Assert.Equal("SMP0218", Assert.Single(problems));
    }
}
