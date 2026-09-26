namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The converter class of [ValueConverter] / [CollectionConverter] is the symbol its typeof names. It used to
// be looked up again by its displayed name, which misses nested and generic classes: their specialized
// methods were not used, and the checks of their methods did not run. The declared types of the members
// are taken from their symbols in the same way, which a dotted path to a parsable type needs.
public class ConverterTypeResolutionTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static string Source(string converter, string attributes, string classAttributes = "") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Linq;
        using Smart.Mapper;
        namespace Test
        {
            public class Src { public int Value { get; set; } public List<int> Items { get; set; } = []; }
            public class Dst { public string Value { get; set; } = ""; public List<string> Items { get; set; } = []; }
            {{converter}}
            {{classAttributes}}
            public static partial class M
            {
                [Mapper]
                {{attributes}}
                public static partial Dst Map(Src src);
                static string MapItem(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        namespace Other
        {
            public static class Conv
            {
                public static string ConvertToString(int value) => "other";
                public static TDest Convert<TSrc, TDest>(TSrc source) => throw new System.NotSupportedException();
            }
        }
        """;

    private const string ConverterBody =
        "public static string ConvertToString(int value) => \"\"; " +
        "public static TDest Convert<TSrc, TDest>(TSrc source) => throw new System.NotSupportedException();";

    // Nested, generic, nested in a generic, and in another namespace: the specialized method is used
    [Theory]
    [InlineData("public static class Outer { public static class Conv { " + ConverterBody + " } }", "[ValueConverter(typeof(Outer.Conv))]", "global::Test.Outer.Conv.ConvertToString(src.Value)")]
    [InlineData("public static class Conv<TMarker> { " + ConverterBody + " }", "[ValueConverter(typeof(Conv<string>))]", "global::Test.Conv<string>.ConvertToString(src.Value)")]
    [InlineData("public static class Outer<TMarker> { public static class Conv { " + ConverterBody + " } }", "[ValueConverter(typeof(Outer<int>.Conv))]", "global::Test.Outer<int>.Conv.ConvertToString(src.Value)")]
    [InlineData("public static class Outer { public static class Conv<TMarker> { " + ConverterBody + " } }", "[ValueConverter(typeof(Outer.Conv<long>))]", "global::Test.Outer.Conv<long>.ConvertToString(src.Value)")]
    [InlineData("", "[ValueConverter(typeof(Other.Conv))]", "global::Other.Conv.ConvertToString(src.Value)")]
    public void ValueConverterClassUsesSpecializedMethod(string converter, string attributes, string call)
    {
        var source = Source(converter, attributes);

        AssertCompiles(source);
        Assert.Contains("__d.Value = " + call + ";", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // On the containing class as well
    [Fact]
    public void ClassLevelNestedValueConverterUsesSpecializedMethod()
    {
        var source = Source(
            "public static class Outer { public static class Conv { " + ConverterBody + " } }",
            string.Empty,
            "[ValueConverter(typeof(Outer.Conv))]");

        AssertCompiles(source);
        Assert.Contains("__d.Value = global::Test.Outer.Conv.ConvertToString(src.Value);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A nested collection converter is called, and its methods are checked
    [Fact]
    public void NestedCollectionConverterIsCalled()
    {
        var source = Source(
            "public static class Outer { public static class Conv { public static List<TDest> ToList<TSource, TDest>(IEnumerable<TSource> source, System.Func<TSource, TDest> mapper) => source.Select(mapper).ToList(); } }",
            "[CollectionConverter(typeof(Outer.Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))] [MapIgnore(nameof(Dst.Value))]");

        AssertCompiles(source);
        Assert.Contains("__d.Items = global::Test.Outer.Conv.ToList<int, string>(src.Items, MapItem)!;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // The checks of the methods run for a nested class: a parameter that cannot take the value
    [Theory]
    [InlineData("public static class Outer { public static class Conv { public static string ConvertToString(ref int value) => \"\"; public static TDest Convert<TSrc, TDest>(TSrc source) => throw new System.NotSupportedException(); } }", "[ValueConverter(typeof(Outer.Conv))]")]
    [InlineData("public static class Outer { public static class Conv { public static List<TDest> ToList<TSource, TDest>(ref IEnumerable<TSource> source, System.Func<TSource, TDest> mapper) => []; } }", "[CollectionConverter(typeof(Outer.Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem))] [MapIgnore(nameof(Dst.Value))]")]
    public void NestedConverterMethodIsChecked(string converter, string attributes)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(converter, attributes));

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0104", diagnostic.Id);
    }

    // A dotted source path to a parsable type, nested or not, is parsed. Its types used to be compared and
    // looked up by name, which never matched, so it fell back to the converter (SMP0402).
    [Theory]
    [InlineData("TopId", "global::Test.TopId.Parse(src.Child!.Text, global::System.Globalization.CultureInfo.InvariantCulture)")]
    [InlineData("Outer.NestedId", "global::Test.Outer.NestedId.Parse(src.Child!.Text, global::System.Globalization.CultureInfo.InvariantCulture)")]
    public void DottedPathToParsableTypeIsParsed(string targetType, string value)
    {
        var source = $$"""
            #nullable enable
            using System;
            using System.Diagnostics.CodeAnalysis;
            using Smart.Mapper;
            namespace Test;
            public readonly record struct TopId(int Value) : IParsable<TopId>
            {
                public static TopId Parse(string s, IFormatProvider? provider) => new(int.Parse(s, provider));
                public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TopId result) { result = default; return false; }
            }
            public static class Outer
            {
                public readonly record struct NestedId(int Value) : IParsable<NestedId>
                {
                    public static NestedId Parse(string s, IFormatProvider? provider) => new(int.Parse(s, provider));
                    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NestedId result) { result = default; return false; }
                }
            }
            public class Child { public string Text { get; set; } = ""; }
            public class Src { public Child Child { get; set; } = new(); }
            public class Dst { public {{targetType}} Id { get; set; } }
            public static partial class M
            {
                [Mapper]
                [MapProperty(nameof(Dst.Id), "Child.Text")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        Assert.Contains("__d.Id = " + value + ";", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
