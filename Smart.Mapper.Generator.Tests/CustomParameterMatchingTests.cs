namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A method an attribute names takes the custom parameters of the mapper it declares after its usual parameters, in any
// order and any of them: each parameter takes the custom parameter of its type, or, when the mapper or the method has
// several parameters of that type, the one of its name, and a method with a parameter taking none of them does not
// match. One taking more of them goes before one taking fewer. The mappers of [MapNested] / [MapCollection] take them
// the same way, and a CultureInfo parameter so gives its culture to the nested mapping as well. A mapper may have
// several custom parameters of the same type; of several CultureInfo parameters, the one named culture gives the
// culture of its conversions (SMP0008 without one).
public class CustomParameterMatchingTests
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
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string members) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Globalization;
        using Smart.Mapper;
        namespace Test;
        public sealed class Ctx { public int Value { get; set; } }
        public sealed class Other { public int Value { get; set; } }
        public class Child { public decimal Amount { get; set; } }
        public class ChildDto { public string Amount { get; set; } = ""; }
        public class Src { public int X { get; set; } public string Text { get; set; } = ""; public Child Part { get; set; } = new(); public List<Child> Parts { get; set; } = []; }
        public class Dst { public int X { get; set; } public string Text { get; set; } = ""; public ChildDto Item { get; set; } = new(); public List<ChildDto> Items { get; set; } = []; public int Y { get; set; } }
        public static class ListConverter
        {
            public static List<TDst> ToList<TSrc, TDst>(IEnumerable<TSrc> source, Func<TSrc, TDst> map) => new(System.Linq.Enumerable.Select(source, map));
        }
        public static partial class M
        {
            {{members}}
        }
        """;

    [Theory]
    // In any order
    [InlineData("[Mapper] [AfterMap(nameof(After))] public static partial Dst Map(Src src, Ctx ctx, Other other); private static void After(Src src, Dst dst, Other other, Ctx ctx) { }", "After(src, __d, other, ctx);")]
    // Any of them
    [InlineData("[Mapper] [MapUsing(nameof(Dst.Y), nameof(Calc))] public static partial Dst Map(Src src, Ctx ctx, Other other); private static int Calc(Src src, Other other) => other.Value;", "Calc(src, other)")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(Twice))] public static partial Dst Map(Src src, Ctx ctx); private static int Twice(int value) => value * 2;", "Twice(src.X)")]
    // Several of a type by name
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Wrap))] public static partial Dst Map(Src src, string prefix, string suffix); private static string Wrap(string value, string suffix) => value + suffix;", "Wrap(src.Text, suffix)")]
    [InlineData("[Mapper] [MapCondition(nameof(Dst.Text), nameof(Has))] public static partial Dst Map(Src src, string prefix, string suffix); private static bool Has(string value, string suffix, string prefix) => true;", "Has(src.Text, suffix, prefix)")]
    // One taking more of them over one taking fewer
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(Twice))] public static partial Dst Map(Src src, Ctx ctx); private static int Twice(int value) => value; private static int Twice(int value, Ctx ctx) => value + ctx.Value;", "Twice(src.X, ctx)")]
    public void MethodTakesCustomParametersItDeclares(string members, string call)
    {
        var (generated, problems) = Build(Source(members));

        Assert.Empty(problems);
        Assert.Contains(call, generated, StringComparison.Ordinal);
    }

    [Theory]
    // Of the same type as C# converts between them by identity: whatever the names of the tuple elements, and dynamic as
    // object, nint as IntPtr
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Conv))] public static partial Dst Map(Src src, (int X, string Y) pair); private static string Conv(string value, (int, string) pair) => value;", "Conv(src.Text, pair)")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Conv))] public static partial Dst Map(Src src, dynamic tag); private static string Conv(string value, object tag) => value;", "Conv(src.Text, tag)")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Conv))] public static partial Dst Map(Src src, IntPtr handle); private static string Conv(string value, nint handle) => value;", "Conv(src.Text, handle)")]
    [InlineData("[Mapper] [AfterMap(nameof(After))] public static partial Dst Map(Src src, (int X, string Y) pair); private static void After(Src src, Dst dst, (int A, string B) pair) { }", "After(src, __d, pair);")]
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, (int X, string Y) pair); [Mapper] public static partial ChildDto MapChild(Child child, (int, string) pair);", "MapChild(src.Part, pair)")]
    public void ParameterTakesCustomParameterOfIdenticalType(string members, string call)
    {
        var (generated, problems) = Build(Source(members));

        Assert.Empty(problems);
        Assert.Contains(call, generated, StringComparison.Ordinal);
    }

    [Theory]
    // The CultureInfo parameter that may be null goes to a parameter not taking null as the culture the conversions go
    // with, which the culture of the method gives for null, and to one taking null as it is
    [InlineData("[Mapper(Culture = \"de-DE\")] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo? culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", "MapChild(src.Part, (culture ?? __culture_de_DE))")]
    [InlineData("[Mapper(Culture = \"de-DE\")] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo? culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", "__culture_de_DE = global::System.Globalization.CultureInfo.GetCultureInfo(\"de-DE\");")]
    [InlineData("[Mapper] [MapCollection(nameof(Dst.Items), nameof(Src.Parts), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo? culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", ", (culture ?? global::System.Globalization.CultureInfo.InvariantCulture))")]
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo? culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo? culture);", "MapChild(src.Part, culture)")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Conv))] public static partial Dst Map(Src src, CultureInfo? culture); private static string Conv(string value, CultureInfo culture) => value;", "Conv(src.Text, (culture ?? global::System.Globalization.CultureInfo.InvariantCulture))")]
    [InlineData("[Mapper] [MapCondition(nameof(Dst.Text), nameof(Has))] public static partial Dst Map(Src src, CultureInfo? culture); private static bool Has(string value, in CultureInfo culture) => true;", "Has(src.Text, (culture ?? global::System.Globalization.CultureInfo.InvariantCulture))")]
    [InlineData("[Mapper] [MapUsing(nameof(Dst.Y), nameof(Calc))] public static partial Dst Map(Src src, CultureInfo? culture); private static int Calc(Src src, CultureInfo culture) => 0;", "Calc(src, (culture ?? global::System.Globalization.CultureInfo.InvariantCulture))")]
    [InlineData("[Mapper] [AfterMap(nameof(After))] public static partial Dst Map(Src src, CultureInfo? culture); private static void After(Src src, Dst dst, CultureInfo culture) { }", "After(src, __d, (culture ?? global::System.Globalization.CultureInfo.InvariantCulture));")]
    public void NullableCultureGoesToNonNullParameterAsCulture(string members, string call)
    {
        var (generated, problems) = Build(Source(members));

        Assert.Empty(problems);
        Assert.Contains(call, generated, StringComparison.Ordinal);
    }

    // A parameter of the mapper of the name of a method an attribute names would take the call the generated code makes
    // by the name
    [Theory]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = \"format\")] /*here*/\n    public static partial Dst Map(Src src, Func<string, string> format); private static string format(string value) => value;")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = \"suffix\")] /*here*/\n    public static partial Dst Map(Src src, string suffix); private static string suffix(string value, string text) => value + text;")]
    [InlineData("[Mapper] [AfterMap(\"ctx\")] /*here*/\n    public static partial Dst Map(Src src, Ctx ctx); private static void ctx(Src src, Dst dst) { }")]
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = \"part\")] /*here*/\n    public static partial Dst Map(Src src, Ctx part); private static ChildDto part(Child child) => new();")]
    public void ParameterHidingMethodIsReported(string members)
    {
        var source = Source(members);

        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(source), static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0104", diagnostic.Id);
        var markedLine = Array.FindIndex(source.Split('\n'), static line => line.Contains("/*here*/", StringComparison.Ordinal));
        Assert.Equal(markedLine, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
    }

    [Theory]
    // A parameter of a type the mapper has several of takes the one of its name only
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Wrap))] public static partial Dst Map(Src src, string prefix, string suffix); private static string Wrap(string value, string other) => value;", "SMP0110")]
    [InlineData("[Mapper] [AfterMap(nameof(After))] public static partial Dst Map(Src src, string prefix, string suffix); private static void After(Src src, Dst dst, string other) { }", "SMP0107")]
    [InlineData("[Mapper] [MapUsing(nameof(Dst.Y), nameof(Calc))] public static partial Dst Map(Src src, string prefix, string suffix); private static int Calc(Src src, string other) => 0;", "SMP0201")]
    // Several parameters of a type the mapper has one of take the one of their names only
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Wrap))] public static partial Dst Map(Src src, string prefix); private static string Wrap(string value, string prefix, string other) => value;", "SMP0110")]
    // A parameter the mapper has no custom parameter for
    [InlineData("[Mapper] [MapCondition(nameof(Dst.Text), nameof(Has))] public static partial Dst Map(Src src); private static bool Has(string value, Ctx ctx) => true;", "SMP0112")]
    // Methods taking as many of them but other ones, of which no call chooses one, whatever the order they are declared in
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Conv))] public static partial Dst Map(Src src, Ctx ctx, Other other); private static string Conv(string value, Other other) => value; private static string Conv(string value, Ctx ctx) => value;", "SMP0110")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Text), Converter = nameof(Conv))] public static partial Dst Map(Src src, Ctx ctx, Other other); private static string Conv(string value, Ctx ctx) => value; private static string Conv(string value, Other other) => value; private static string Conv(string value) => value;", "SMP0110")]
    [InlineData("[Mapper] [AfterMap(nameof(After))] public static partial Dst Map(Src src, Ctx ctx, Other other); private static void After(Src src, Dst dst, Other other) { } private static void After(Src src, Dst dst, Ctx ctx) { }", "SMP0107")]
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, Ctx ctx, Other other); private static ChildDto MapChild(Child child, Other other) => new(); private static ChildDto MapChild(Child child, Ctx ctx) => new();", "SMP0214")]
    public void ParameterWithoutCustomParameterDoesNotMatch(string members, string id)
    {
        var (_, problems) = Build(Source(members));

        Assert.Equal([id], problems);
    }

    // Several custom parameters of the same type, which an expression names
    [Fact]
    public void MapperTakesSeveralCustomParametersOfType()
    {
        var (generated, problems) = Build(Source("[Mapper] [MapExpression(nameof(Dst.Text), \"prefix + src.Text + suffix\")] public static partial Dst Map(Src src, string prefix, string suffix);"));

        Assert.Empty(problems);
        Assert.Contains("__expression0(src, prefix, suffix)", generated, StringComparison.Ordinal);
    }

    [Theory]
    // The nested mapper takes the culture, which gives the culture of its conversions
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", "MapChild(src.Part, culture)")]
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", "ConvertToString(child.Amount, culture, null)")]
    // A void one after the instance it fills
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(Fill))] public static partial Dst Map(Src src, Ctx ctx); [Mapper] public static partial void Fill(Child child, ChildDto dto, Ctx ctx);", "Fill(src.Part, __nested_Item, ctx);")]
    // The element mapper in the loop
    [InlineData("[Mapper] [MapCollection(nameof(Dst.Items), nameof(Src.Parts), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", ", culture)")]
    // One taking none of them
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src, CultureInfo culture); [Mapper] public static partial ChildDto MapChild(Child child);", "MapChild(src.Part)")]
    public void NestedMapperTakesCustomParameters(string members, string expected)
    {
        var (generated, problems) = Build(Source(members));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    [Theory]
    // A nested mapper taking a custom parameter the mapper does not have
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] public static partial Dst Map(Src src); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", "SMP0214")]
    // A collection converter takes the element mapper as a delegate, which cannot pass them
    [InlineData("[Mapper] [MapCollection(nameof(Dst.Items), nameof(Src.Parts), Mapper = nameof(MapChild))] [CollectionConverter(typeof(ListConverter))] public static partial Dst Map(Src src, CultureInfo culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo culture);", "SMP0213")]
    public void NestedMapperWithoutCustomParameterDoesNotMatch(string members, string id)
    {
        var (_, problems) = Build(Source(members));

        Assert.Contains(id, problems);
    }

    // Of several CultureInfo parameters, the one named culture gives the culture of the conversions, and the others go
    // by name
    [Fact]
    public void CultureParameterNamedCultureGivesCulture()
    {
        var (generated, problems) = Build(Source("[Mapper] [MapNested(nameof(Dst.Item), nameof(Src.Part), Mapper = nameof(MapChild))] [MapProperty(nameof(Dst.Text), nameof(Src.X))] public static partial Dst Map(Src src, CultureInfo display, CultureInfo culture); [Mapper] public static partial ChildDto MapChild(Child child, CultureInfo display);"));

        Assert.Empty(problems);
        Assert.Contains("ConvertToString(src.X, culture, null)", generated, StringComparison.Ordinal);
        Assert.Contains("MapChild(src.Part, display)", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void SeveralCultureParametersWithoutCultureAreReported()
    {
        var (_, problems) = Build(Source("[Mapper] public static partial Dst Map(Src src, CultureInfo display, CultureInfo other);"));

        Assert.Equal(["SMP0008"], problems);
    }
}
