namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A member the constructor a return mapper calls assigns from an argument can be assigned by [MapConstant],
// [MapExpression], [MapUsing], [MapFrom], [MapNested] or [MapCollection]: the value goes to the argument
// (new Dst(Build(src))). It used to be reported as a member [MapIgnore] leaves out (SMP0216) without one. The value
// is checked as when it is assigned to the member, and [MapNested] and [MapCollection] make theirs before
// construction into locals the call passes; InPlace, which has no instance to refill there, is reported (SMP0219).
// SMP0216 is left to [MapIgnore].
public class ConstructorArgumentAttributeTests
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

    private static string Source(string attributes, string destination = "public record Dst(Child Item, int Y);") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int B { get; set; } }
        public class Src
        {
            public Child Item { get; set; } = new();
            public Child? Other { get; set; }
            public int Y { get; set; }
            public List<Child> Items { get; set; } = [];
            public int Count() => 7;
        }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            public static partial Child MapChild(Child source);

            [Mapper]
            public static partial void FillChild(Child source, Child destination);

            [Mapper]
            {{attributes}}
            public static partial Dst Map(Src src);

            private static Child Build(Src src) => new() { B = src.Y };
            private static string Text(Src src) => "";
            private static bool IsPositive(int value) => value > 0;
        }
        """;

    [Theory]
    [InlineData("[MapUsing(\"Item\", nameof(Build))]", "var __d = new global::Test.Dst(Build(src), src.Y);")]
    [InlineData("[MapConstant(\"Y\", 5)]", "var __d = new global::Test.Dst(src.Item, 5);")]
    [InlineData("[MapExpression(\"Y\", \"src.Y + 1\")]", "var __d = new global::Test.Dst(src.Item, __expression0(src));")]
    [InlineData("[MapFrom(\"Y\", nameof(Src.Count))]", "var __d = new global::Test.Dst(src.Item, src.Count());")]
    [InlineData("[MapNested(\"Item\", Mapper = nameof(MapChild))]", "global::Test.Child __arg0;\n__arg0 = MapChild(src.Item);\nvar __d = new global::Test.Dst(__arg0, src.Y);")]
    [InlineData("[MapNested(\"Item\", nameof(Src.Other), Mapper = nameof(MapChild))]", "global::Test.Child __arg0;\n__arg0 = src.Other is not null ? MapChild(src.Other!) : default!;\nvar __d = new global::Test.Dst(__arg0, src.Y);")]
    [InlineData("[MapNested(\"Item\", Mapper = nameof(FillChild))]", "global::Test.Child __arg0;\nvar __nested_Item = new global::Test.Child();\nFillChild(src.Item, __nested_Item);\n__arg0 = __nested_Item;\nvar __d = new global::Test.Dst(__arg0, src.Y);")]
    public void AttributeValueGoesToArgument(string attribute, string expected)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    [Fact]
    public void CollectionIsMadeBeforeConstruction()
    {
        var source = Source("[MapCollection(\"Items\", Mapper = nameof(MapChild))]", "public record Dst(List<Child> Items);");

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("global::System.Collections.Generic.List<global::Test.Child> __arg0;\n{", lines, StringComparison.Ordinal);
        Assert.Contains("__arg0 = __list;\n}\nvar __d = new global::Test.Dst(__arg0);", lines, StringComparison.Ordinal);
    }

    // A parameter of a class constructor, assigning a get-only property
    [Fact]
    public void ClassConstructorParameterTakesValue()
    {
        var source = Source(
            "[MapUsing(nameof(Dst.Item), nameof(Build))]",
            "public class Dst { public Dst(Child item, int y) { Item = item; Y = y; } public Child Item { get; } public int Y { get; } }");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst(Build(src), src.Y);", Lines(source), StringComparison.Ordinal);
    }

    // The value is checked as when it is assigned to the member
    [Theory]
    [InlineData("[MapConstant(\"Y\", \"text\")]", "SMP0218", "target=[Y]")]
    [InlineData("[MapConstant(\"Item\", null)]", "SMP0218", "target=[Item]")]
    [InlineData("[MapUsing(\"Y\", nameof(Text))]", "SMP0202", "using=[Text]")]
    public void ValueThatDoesNotFitEmitsDiagnostic(string attribute, string id, string text)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(attribute));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Contains(text, diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    [Fact]
    public void InPlaceCollectionEmitsDiagnostic()
    {
        AssertDiagnostic(
            Source("[MapCollection(\"Items\", Mapper = nameof(MapChild), Strategy = CollectionStrategy.InPlace)]", "public record Dst(List<Child> Items);"),
            "SMP0219",
            "Items");
    }

    // [MapIgnore] of such a member is still reported, and a condition has no property mapping to guard
    [Theory]
    [InlineData("[MapIgnore(\"Item\")]", "SMP0216", "Item")]
    [InlineData("[MapExpression(\"Y\", \"1\")] [MapCondition(\"Y\", nameof(IsPositive))]", "SMP0221", "Y")]
    public void IgnoreAndConditionEmitDiagnostic(string attributes, string id, string target)
    {
        AssertDiagnostic(Source(attributes), id, target);
    }
}
