namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A dotted path into a member the constructor of a return mapper assigns from an argument used to write, after
// construction, into the object passed to the constructor, the source's own when the argument copied it
// (new RDst(src.Item) and then __d.Item.B = ...). It is reported (SMP0301), whichever attribute the path is of.
// The member and the parameter are matched as the arguments are bound. A void mapper never constructs, and a
// constructor construction does not call assigns nothing, so they are not concerned.
public class ConstructorAssignedPathTests
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

    private static void AssertDiagnostic(string source, string id, string target, string? parameter = null)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains($"target=[{target}]", message, StringComparison.Ordinal);
        if (parameter is not null)
        {
            Assert.Contains($"parameter=[{parameter}]", message, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private static string Source(string destination, string attributes, string mapper = "[Mapper]", string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Leaf { public int B { get; set; } }
        public class Child { public int B { get; set; } public Leaf? Inner { get; set; } }
        public class Src { public Child Item { get; set; } = new(); public int Y { get; set; } }
        {{destination}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            {{signature}}

            private static int Calc(Src src) => src.Y;
        }
        """;

    private const string Record = "public record Dst(Child Item);";

    [Theory]
    [InlineData("[MapProperty(\"Item.B\", nameof(Src.Y))]")]
    [InlineData("[MapConstant(\"Item.B\", 3)]")]
    [InlineData("[MapExpression(\"Item.B\", \"src.Y\")]")]
    [InlineData("[MapUsing(\"Item.B\", nameof(Calc))]")]
    public void PathIntoRecordParameterEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(Record, attribute), "SMP0301", "Item.B", "Item");
    }

    // A longer path, a path named ignoring case, and a constructor parameter named in camelCase
    [Fact]
    public void LongerPathIntoRecordParameterEmitsDiagnostic()
    {
        AssertDiagnostic(Source(Record, "[MapConstant(\"Item.Inner.B\", 3)]"), "SMP0301", "Item.Inner.B", "Item");
    }

    [Fact]
    public void PathNamedIgnoringCaseEmitsDiagnostic()
    {
        AssertDiagnostic(
            Source(Record, "[MapExpression(\"item.b\", \"1\")]", "[Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]"),
            "SMP0301",
            "Item.B",
            "Item");
    }

    [Theory]
    [InlineData("public class Dst { public Dst(Child item, int y) { Item = item; Y = y; } public Child Item { get; set; } public int Y { get; } }")]
    [InlineData("public class Dst { public Dst(Child item) { Item = item; } public Child Item { get; } }")]
    public void PathIntoParameterOfCalledConstructorEmitsDiagnostic(string destination)
    {
        AssertDiagnostic(Source(destination, "[MapProperty(\"Item.B\", nameof(Src.Y))]"), "SMP0301", "Item.B", "item");
    }

    // A constructor construction does not call, a void mapper, which never constructs, and a member no
    // parameter assigns are not concerned
    [Theory]
    [InlineData("public class Dst { public Dst() { } public Dst(Child item) { Item = item; } public Child? Item { get; set; } }", "Item.B", "__d.Item ??= new global::Test.Child();\n__d.Item.B = src.Y;")]
    [InlineData("public record Dst(int Y) { public Child? Other { get; set; } }", "Other.B", "__d.Other ??= new global::Test.Child();\n__d.Other.B = src.Y;")]
    public void PathIntoMemberConstructorDoesNotAssignCompiles(string destination, string target, string expected)
    {
        var source = Source(destination, $"[MapProperty(\"{target}\", nameof(Src.Y))]");

        AssertCompiles(source);
        var lines = String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));
        Assert.Contains(expected, lines, StringComparison.Ordinal);
    }

    [Fact]
    public void PathInVoidMapperCompiles()
    {
        var source = Source(Record, "[MapConstant(\"Item.B\", 3)]", signature: "public static partial void Map(Src src, Dst dst);");

        AssertCompiles(source);
        Assert.Contains("dst.Item.B = 3;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A parameter without a member of its name leaves the path to be reported as not found
    [Fact]
    public void PathIntoMissingMemberEmitsNotFound()
    {
        AssertDiagnostic(
            Source("public class Dst { public Dst(Child value) { Text = value.B.ToString(); } public string Text { get; } }", "[MapProperty(\"Value.B\", nameof(Src.Y))]"),
            "SMP0102",
            "Value.B");
    }
}
