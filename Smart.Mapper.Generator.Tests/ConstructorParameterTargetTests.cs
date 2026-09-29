namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// As with [MapProperty], the target of [MapConstant], [MapExpression], [MapUsing], [MapFrom], [MapNested] and
// [MapCollection] can be the name of a parameter of the constructor a return mapper calls, when no member of the
// destination has that name, matched under the mapper's name comparison. It used to be reported as a parameter
// without a source (SMP0305), or a target that is not found (SMP0102). [MapIgnore] and [MapCondition] on such a
// parameter of the only constructor are reported as before (SMP0304 / SMP0306).
public class ConstructorParameterTargetTests
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

    private static void AssertDiagnostic(string source, string id, string target)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains($"target=[{target}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private static string Lines(string source) =>
        String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string attributes, string mapper = "[Mapper]", string destination = Destination) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Globalization;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int B { get; set; } }
        public class Src
        {
            public int Number { get; set; }
            public string Text { get; set; } = "";
            public Child Item { get; set; } = new();
            public List<Child> Items { get; set; } = [];
            public int Count() => 3;
        }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            public static partial Child MapChild(Child source);

            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);

            private static int Calc(Src src) => src.Number;
            private static bool IsPositive(int value) => value > 0;
        }
        """;

    // The parameters have no member of their names
    private const string Destination = "public class Dst { public Dst(int number) { Label = number.ToString(CultureInfo.InvariantCulture); } public string Label { get; } }";

    [Theory]
    [InlineData("[MapConstant(\"number\", 3)]", "var __d = new global::Test.Dst(3);")]
    [InlineData("[MapExpression(\"number\", \"src.Number + 1\")]", "var __d = new global::Test.Dst(__expression0(src));")]
    [InlineData("[MapUsing(\"number\", nameof(Calc))]", "var __d = new global::Test.Dst(Calc(src));")]
    [InlineData("[MapFrom(\"number\", nameof(Src.Count))]", "var __d = new global::Test.Dst(src.Count());")]
    public void AttributeTargetsParameter(string attribute, string expected)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    [Fact]
    public void NestedAndCollectionTargetParameters()
    {
        var source = Source(
            "[MapNested(\"copy\", nameof(Src.Item), Mapper = nameof(MapChild))] [MapCollection(\"list\", nameof(Src.Items), Mapper = nameof(MapChild))]",
            destination: "public class Dst { public Dst(Child copy, List<Child> list) { Holder = copy; All = list; } public Child Holder { get; } public List<Child> All { get; } }");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("global::Test.Child __arg0;\n__arg0 = MapChild(src.Item);", lines, StringComparison.Ordinal);
        Assert.Contains("var __d = new global::Test.Dst(__arg0, __arg1);", lines, StringComparison.Ordinal);
    }

    // Matched under the mapper's name comparison
    [Fact]
    public void ParameterNamedIgnoringCase()
    {
        var source = Source("[MapUsing(\"NUMBER\", nameof(Calc))]", "[Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst(Calc(src));", Lines(source), StringComparison.Ordinal);
    }

    // The value is checked for the parameter's type
    [Fact]
    public void ValueThatDoesNotFitEmitsDiagnostic()
    {
        AssertDiagnostic(Source("[MapConstant(\"number\", \"text\")]"), "SMP0216", "number");
    }

    // A parameter with a member of its name takes an attribute naming the member, not the parameter
    [Fact]
    public void ParameterWithMemberIsNotTarget()
    {
        AssertDiagnostic(Source("[MapConstant(\"number\", 3)]", destination: "public record Dst(int Number);"), "SMP0102", "number");
    }

    [Theory]
    [InlineData("[MapIgnore(\"number\")]", "SMP0304")]
    [InlineData("[MapCondition(\"number\", nameof(IsPositive))]", "SMP0306")]
    public void IgnoreAndConditionOnParameterEmitDiagnostic(string attribute, string id)
    {
        AssertDiagnostic(Source(attribute), id, "number");
    }
}
