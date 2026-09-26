namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The rest of the methods the generated code calls follow the rule of the hooks: the element mappers of
// [MapCollection] / [MapNested], the methods of a [CollectionConverter] class, and the overload of a
// [ValueConverter] method that takes a culture and a format. Each argument is passed the way the parameter
// takes it, and a modifier that cannot take its argument makes the method a mismatch, reported with the
// diagnostic of that feature. The generator used to ignore the modifiers of these methods, so ref failed
// with CS1620 and ref readonly warned.
public class ElementMapperModifierTests
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

    // The source and destination members are given per case; E1 / E2 are classes, S1 / S2 structs.
    private static string Source(string sourceMember, string targetMember, string attributes, string members, string types = "", string mapper = "[Mapper]") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Linq;
        using Smart.Mapper;
        namespace Test;
        public class E1 { public int V { get; set; } }
        public class E2 { public int V { get; set; } }
        public struct S1 { public int V { get; set; } }
        public struct S2 { public int V { get; set; } }
        public class Src { public int Value { get; set; } {{sourceMember}} }
        public class Dst { public string Text { get; set; } = ""; {{targetMember}} }
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);
            {{members}}
        }
        """;

    private const string Items = "[MapCollection(\"Items\", Mapper = nameof(MapElem))]";
    private const string InPlaceItems = "[MapCollection(\"Items\", Mapper = nameof(MapElem), Strategy = CollectionStrategy.InPlace)]";
    private const string Child = "[MapNested(\"Child\", Mapper = nameof(MapChild))]";

    [Theory]
    // Element mapper: a span or array element is a writable variable, a read-only span element and a
    // foreach variable are read-only ones, and an IList / IReadOnlyList indexer gives a value
    [InlineData("public E1[] Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(in E1 s) => new() { V = s.V };", "MapElem(in __src[__i])")]
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(ref E1 s) => new() { V = s.V };", "MapElem(ref __src[__i])")]
    [InlineData("public ImmutableArray<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(ref readonly E1 s) => new() { V = s.V };", "MapElem(in __src[__i])")]
    [InlineData("public IEnumerable<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(in E1 s) => new() { V = s.V };", "MapElem(in __item)")]
    [InlineData("public IReadOnlyList<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(in E1 s) => new() { V = s.V };", "MapElem(__srcList[__i])")]
    // A void element mapper gets the instance the loop creates by ref
    [InlineData("public E1[] Items { get; set; } = [];", "public List<S2> Items { get; set; } = [];", Items, "static void MapElem(in E1 s, ref S2 d) => d.V = s.V;", "MapElem(in __src[__i], ref __dest);")]
    // InPlace loops pass array elements and foreach variables the same way
    [InlineData("public E1[] Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", InPlaceItems, "static E2 MapElem(in E1 s) => new() { V = s.V };", "MapElem(in __srcArr[__i])")]
    [InlineData("public IEnumerable<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", InPlaceItems, "static E2 MapElem(ref readonly E1 s) => new() { V = s.V };", "MapElem(in __item)")]
    // The first mapper decides between returning the element and filling it; a void overload taking
    // everything by value does not replace it
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(in E1 s) => new() { V = s.V }; static void MapElem(E1 s, E2 d) => d.V = s.V;", "__dst[__i] = MapElem(in __src[__i]);")]
    // Nested mapper: the property value goes as is, the instance of a void mapper by ref
    [InlineData("public E1? Child { get; set; }", "public E2? Child { get; set; }", Child, "static E2 MapChild(in E1 s) => new() { V = s.V };", "MapChild(src.Child!)")]
    [InlineData("public S1 Child { get; set; }", "public S2 Child { get; set; }", Child, "static void MapChild(in S1 s, ref S2 d) => d.V = s.V;", "MapChild(src.Child, ref __nested_Child);")]
    public void ElementArgumentsFollowMapperModifiers(string sourceMember, string targetMember, string attributes, string members, string call)
    {
        var source = Source(sourceMember, targetMember, attributes, members);

        AssertCompiles(source);
        Assert.Contains(call, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A converter class method taking the collection or the element mapper by in gets them as they are.
    [Fact]
    public void CollectionConverterMethodTakesArgumentsByIn()
    {
        var source = Source(
            "public List<E1> Items { get; set; } = [];",
            "public List<E2> Items { get; set; } = [];",
            "[CollectionConverter(typeof(Conv))] " + Items,
            "static E2 MapElem(E1 s) => new() { V = s.V };",
            "public static class Conv { public static List<TDest>? ToList<TSource, TDest>(in IEnumerable<TSource>? source, in System.Func<TSource, TDest> mapper) => source?.Select(mapper).ToList(); }");

        AssertCompiles(source);
        Assert.Contains("__d.Items = global::Test.Conv.ToList<global::Test.E1, global::Test.E2>(src.Items, MapElem)!;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A delegate of the converter taking its parameter by in takes an element mapper doing the same.
    [Fact]
    public void ElementMapperMatchingConverterDelegateIsAccepted()
    {
        var source = Source(
            "public List<E1> Items { get; set; } = [];",
            "public List<E2> Items { get; set; } = [];",
            "[CollectionConverter(typeof(Conv))] " + Items,
            "static E2 MapElem(in E1 s) => new() { V = s.V };",
            "public delegate TDest InMapper<TSource, TDest>(in TSource source); " +
            "public static class Conv { public static List<TDest>? ToList<TSource, TDest>(IEnumerable<TSource>? source, InMapper<TSource, TDest> mapper) => source?.Select(x => mapper(in x)).ToList(); }");

        AssertCompiles(source);
        Assert.Contains("__d.Items = global::Test.Conv.ToList<global::Test.E1, global::Test.E2>(src.Items, MapElem)!;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // InPlace always loops, so the element mapper gets the element even when a collection converter is set.
    [Fact]
    public void InPlaceLoopPassesElementDespiteCollectionConverter()
    {
        var source = Source(
            "public List<E1> Items { get; set; } = [];",
            "public List<E2> Items { get; set; } = [];",
            "[CollectionConverter(typeof(Conv))] " + InPlaceItems,
            "static E2 MapElem(in E1 s) => new() { V = s.V };",
            "public static class Conv { public static List<TDest>? ToList<TSource, TDest>(IEnumerable<TSource>? source, System.Func<TSource, TDest> mapper) => null; }");

        AssertCompiles(source);
        Assert.Contains("__dstColl.Add(MapElem(in __srcSpan[__i]));", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A converter with the one-parameter method, which makes the conversion use it, and the given overloads.
    private static string CultureConverter(string overloads) =>
        "public static class Conv { public static string ConvertToString(int value) => \"\"; " + overloads +
        " public static TDest Convert<TSrc, TDest>(TSrc source) => throw new System.NotSupportedException(); }";

    // The overload a culture calls gets the value and the format as they are. The culture field goes by in
    // to an in / ref readonly CultureInfo, and as is to a type it converts to, as the converted value.
    [Theory]
    [InlineData("", "public static string ConvertToString(in int value, ref readonly System.Globalization.CultureInfo provider, in string? format) => \"\";", "ConvertToString(src.Value, in __culture_en_US, null)")]
    [InlineData("", "public static string ConvertToString(int value, in System.Globalization.CultureInfo provider, string? format) => \"\";", "ConvertToString(src.Value, in __culture_en_US, null)")]
    [InlineData("", "public static string ConvertToString(int value, in System.IFormatProvider provider, string? format) => \"\";", "ConvertToString(src.Value, __culture_en_US, null)")]
    [InlineData("public int? Count { get; set; }", "public static string ConvertToString(int value, ref readonly System.Globalization.CultureInfo provider, string? format) => \"\";", "ConvertToString(src.Count.GetValueOrDefault(), in __culture_en_US, null)")]
    // An overload taking every argument by value wins
    [InlineData("", "public static string ConvertToString(int value, in System.Globalization.CultureInfo provider, string? format) => \"\"; public static string ConvertToString(int value, System.IFormatProvider provider, string? format) => \"\";", "ConvertToString(src.Value, __culture_en_US, null)")]
    public void CultureOverloadArgumentsFollowModifiers(string sourceMember, string overloads, string call)
    {
        var targetMember = sourceMember.Length > 0 ? "public string Count { get; set; } = \"\";" : "public string Value { get; set; } = \"\";";
        var source = Source(sourceMember, targetMember, "[ValueConverter(typeof(Conv))]", string.Empty, CultureConverter(overloads), "[Mapper(Culture = \"en-US\")]");

        AssertCompiles(source);
        Assert.Contains(call, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // Methods without modifiers keep their calls, and one taking every argument by value wins over an
    // overload taking them by reference.
    [Theory]
    [InlineData("static E2 MapElem(E1 s) => new() { V = s.V };")]
    [InlineData("static E2 MapElem(ref E1 s) => new() { V = 0 }; static E2 MapElem(E1 s) => new() { V = s.V };")]
    public void ByValueElementMapperKeepsCall(string members)
    {
        var source = Source("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, members);

        AssertCompiles(source);
        Assert.Contains("__dst[__i] = MapElem(__src[__i]);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    // Element mappers: out, ref for a read-only element or a value, ref readonly for a value
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(out E1 s) { s = new E1(); return new E2(); }", "", "SMP0210")]
    [InlineData("public IReadOnlyList<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(ref E1 s) => new();", "", "SMP0210")]
    [InlineData("public IReadOnlyList<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(ref readonly E1 s) => new();", "", "SMP0210")]
    [InlineData("public IEnumerable<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(ref E1 s) => new();", "", "SMP0210")]
    [InlineData("public ImmutableArray<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", Items, "static E2 MapElem(ref E1 s) => new();", "", "SMP0210")]
    [InlineData("public E1? Child { get; set; }", "public E2? Child { get; set; }", Child, "static E2 MapChild(ref E1 s) => new();", "", "SMP0211")]
    [InlineData("public E1? Child { get; set; }", "public E2? Child { get; set; }", Child, "static E2 MapChild(ref readonly E1 s) => new();", "", "SMP0211")]
    // The instance a void mapper fills cannot go to out
    [InlineData("public E1[] Items { get; set; } = [];", "public List<S2> Items { get; set; } = [];", Items, "static void MapElem(E1 s, out S2 d) => d = new S2();", "", "SMP0210")]
    // Passed to a collection converter as a Func / Action, the element mapper has to take its parameters by value
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", "[CollectionConverter(typeof(Conv))] " + Items, "static E2 MapElem(in E1 s) => new();", "public static class Conv { public static List<TDest>? ToList<TSource, TDest>(IEnumerable<TSource>? source, System.Func<TSource, TDest> mapper) => null; }", "SMP0210")]
    // Converter of [MapCollection] without [CollectionConverter] calls DefaultCollectionConverter, which takes a Func
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", "[MapCollection(\"Items\", Mapper = nameof(MapElem), Converter = \"ToList\")]", "static E2 MapElem(in E1 s) => new();", "", "SMP0210")]
    // Collection converter methods get the property value and the element mapper
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", "[CollectionConverter(typeof(Conv))] " + Items, "static E2 MapElem(E1 s) => new();", "public static class Conv { public static List<TDest>? ToList<TSource, TDest>(ref List<TSource>? source, System.Func<TSource, TDest> mapper) => null; }", "SMP0104")]
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", "[CollectionConverter(typeof(Conv))] " + Items, "static E2 MapElem(E1 s) => new();", "public static class Conv { public static List<TDest>? ToList<TSource, TDest>(IEnumerable<TSource>? source, out System.Func<TSource, TDest> mapper) { mapper = null!; return null; } }", "SMP0104")]
    [InlineData("public List<E1> Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", "[CollectionConverter(typeof(Conv))] " + Items, "static E2 MapElem(E1 s) => new();", "public static class Conv { public static List<TDest>? ToList<TSource, TDest>(ref readonly IEnumerable<TSource>? source, System.Func<TSource, TDest> mapper) => null; }", "SMP0104")]
    public void ModifierThatCannotTakeArgumentEmitsDiagnostic(string sourceMember, string targetMember, string attributes, string members, string types, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(sourceMember, targetMember, attributes, members, types));

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
    }

    // The culture overload: the value and the format cannot go by ref, the culture field is read-only, and
    // converted to IFormatProvider it is no longer a variable for ref readonly.
    [Theory]
    [InlineData("public static string ConvertToString(ref int value, System.IFormatProvider? provider, string? format) => \"\";")]
    [InlineData("public static string ConvertToString(int value, ref System.Globalization.CultureInfo provider, string? format) => \"\";")]
    [InlineData("public static string ConvertToString(int value, ref readonly System.IFormatProvider provider, string? format) => \"\";")]
    [InlineData("public static string ConvertToString(int value, System.IFormatProvider? provider, ref string? format) => \"\";")]
    [InlineData("public static string ConvertToString(int value, System.IFormatProvider? provider, out string? format) { format = null; return \"\"; }")]
    public void CultureOverloadThatCannotTakeArgumentsEmitsDiagnostic(string overload)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            string.Empty,
            "public string Value { get; set; } = \"\";",
            "[ValueConverter(typeof(Conv))]",
            string.Empty,
            CultureConverter(overload),
            "[Mapper(Culture = \"en-US\")]"));

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0104", diagnostic.Id);
    }

    // Without a culture the one-parameter method is called, so the overload is not looked at.
    [Fact]
    public void CultureOverloadIsNotCheckedWithoutCulture()
    {
        var source = Source(
            string.Empty,
            "public string Value { get; set; } = \"\";",
            "[ValueConverter(typeof(Conv))]",
            string.Empty,
            CultureConverter("public static string ConvertToString(int value, ref System.Globalization.CultureInfo provider, string? format) => \"\";"));

        AssertCompiles(source);
        Assert.Contains("__d.Value = global::Test.Conv.ConvertToString(src.Value);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
