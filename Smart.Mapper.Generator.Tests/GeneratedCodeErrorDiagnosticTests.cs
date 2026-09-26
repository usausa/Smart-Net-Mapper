namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Cases the generated code used to fail to compile on are reported as diagnostics instead: a converter
// class without the method the generated code calls (the overload taking the culture and the format, the
// method for the target collection, the generic fallback), a collection target that cannot take the
// collection the generated code creates or cannot be assigned, a void mapper whose instance the generated
// code cannot create, and a value formatted to a string by a type without ToString(format, provider).
public class GeneratedCodeErrorDiagnosticTests
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

    private static void AssertDiagnostic(string source, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
    }

    // E1 / E2 are classes, S2 a struct, NoCtor a class without a constructor taking no arguments.
    private static string Source(string members, string declaration, string types) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Collections.ObjectModel;
        using System.Linq;
        using Smart.Mapper;
        namespace Test;
        public class E1 { public int V { get; set; } }
        public class E2 { public int V { get; set; } }
        public struct S2 { public int V { get; set; } }
        public class NoCtor { public NoCtor(int v) { V = v; } public int V { get; set; } }
        {{types}}
        public static partial class M
        {
            [Mapper]
            {{declaration}}
            {{members}}
        }
        """;

    // ------------------------------------------------------------------
    // Value converter: the overload taking the culture and the format, and the generic fallback
    // ------------------------------------------------------------------

    private static string ValueConverterSource(string converterMembers, string mapper = "[Mapper(Culture = \"en-US\")]") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Value { get; set; } public string Text { get; set; } = ""; }
        public class Dst { public string Value { get; set; } = ""; public System.Uri? Text { get; set; } }
        public static class Conv
        {
            public static string ConvertToString(int value) => "";
            {{converterMembers}}
        }
        public static partial class M
        {
            {{mapper}}
            [ValueConverter(typeof(Conv))]
            public static partial Dst Map(Src src);
        }
        """;

    private const string GenericConvert = "public static TDest Convert<TSrc, TDest>(TSrc source) => throw new System.NotSupportedException();";

    [Theory]
    // With a culture, the overload taking the culture and the format is called: missing, or with a format
    // parameter a string does not convert to
    [InlineData(GenericConvert, "[Mapper(Culture = \"en-US\")]")]
    [InlineData(GenericConvert + " public static string ConvertToString(int value, System.IFormatProvider provider, int format) => \"\";", "[Mapper(Culture = \"en-US\")]")]
    // The generic fallback a user converter calls for a conversion nothing else claims (string to Uri)
    [InlineData("public static string ConvertToString(int value, System.IFormatProvider provider, string? format) => \"\";", "[Mapper]")]
    [InlineData("public static TDest Convert<TSrc, TDest>(TSrc source, TDest other) => other;", "[Mapper]")]
    public void MissingValueConverterMethodEmitsDiagnostic(string converterMembers, string mapper)
    {
        AssertDiagnostic(ValueConverterSource(converterMembers, mapper), "SMP0104");
    }

    [Theory]
    [InlineData(GenericConvert + " public static string ConvertToString(int value, System.IFormatProvider provider, string? format) => \"\";", "[Mapper(Culture = \"en-US\")]")]
    [InlineData(GenericConvert, "[Mapper]")]
    // Optional parameters after the arguments are taken as the compiler takes them
    [InlineData("public static TDest Convert<TSrc, TDest>(TSrc source, int depth = 0) => throw new System.NotSupportedException(); public static string ConvertToString(int value, System.IFormatProvider provider, string? format, bool trim = false) => \"\";", "[Mapper(Culture = \"en-US\")]")]
    public void PresentValueConverterMethodCompiles(string converterMembers, string mapper)
    {
        AssertCompiles(ValueConverterSource(converterMembers, mapper));
    }

    // ------------------------------------------------------------------
    // Collection converter: the method for the target collection
    // ------------------------------------------------------------------

    private const string CollectionSourceTypes =
        "public class Src { public List<E1> Items { get; set; } = []; } " +
        "public class Dst { public HashSet<E2> Items { get; set; } = []; } " +
        "public class ListDst { public List<S2> Items { get; set; } = []; } ";

    [Theory]
    // No ToHashSet for a HashSet target, or one returning what the target cannot take
    [InlineData("public static class Conv { public static List<TDest> ToList<TSource, TDest>(IEnumerable<TSource> source, Func<TSource, TDest> mapper) => []; }", "[CollectionConverter(typeof(Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))] public static partial Dst Map(Src src);")]
    [InlineData("public static class Conv { public static List<TDest> ToHashSet<TSource, TDest>(IEnumerable<TSource> source, Func<TSource, TDest> mapper) => []; }", "[CollectionConverter(typeof(Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))] public static partial Dst Map(Src src);")]
    // A first parameter the source collection does not convert to
    [InlineData("public static class Conv { public static HashSet<TDest> ToHashSet<TSource, TDest>(TSource[] source, Func<TSource, TDest> mapper) => []; }", "[CollectionConverter(typeof(Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))] public static partial Dst Map(Src src);")]
    // Converter of [MapCollection] naming a method DefaultCollectionConverter does not have
    [InlineData("", "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem), Converter = \"ToBag\")] public static partial Dst Map(Src src);")]
    public void MissingCollectionConverterMethodEmitsDiagnostic(string types, string declaration)
    {
        AssertDiagnostic(Source("static E2 MapElem(E1 s) => new();", declaration, CollectionSourceTypes + types), "SMP0104");
    }

    [Theory]
    [InlineData("public static class Conv { public static HashSet<TDest> ToHashSet<TSource, TDest>(IEnumerable<TSource> source, Func<TSource, TDest> mapper) => source.Select(mapper).ToHashSet(); }", "[CollectionConverter(typeof(Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))] public static partial Dst Map(Src src);")]
    [InlineData("public static class Conv { public static HashSet<TDest> ToHashSet<TSource, TDest>(IEnumerable<TSource> source, Func<TSource, TDest> mapper, int capacity = 0) => source.Select(mapper).ToHashSet(); }", "[CollectionConverter(typeof(Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))] public static partial Dst Map(Src src);")]
    [InlineData("", "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem), Converter = \"ToHashSet\")] public static partial Dst Map(Src src);")]
    public void PresentCollectionConverterMethodCompiles(string types, string declaration)
    {
        AssertCompiles(Source("static E2 MapElem(E1 s) => new();", declaration, CollectionSourceTypes + types));
    }

    // The Action overload of DefaultCollectionConverter needs new() on the target element; for one without
    // a constructor taking no arguments, the void element mapper matches no overload.
    [Fact]
    public void ConstraintOfCollectionConverterMethodIsChecked()
    {
        var types = "public class Src { public List<E1> Items { get; set; } = []; } public class NoCtorDst { public List<NoCtor> Items { get; set; } = []; }";

        AssertDiagnostic(
            Source("static void Fill(E1 s, NoCtor d) { }", "[MapCollection(nameof(NoCtorDst.Items), Mapper = nameof(Fill), Converter = \"ToList\")] public static partial NoCtorDst Map(Src src);", types),
            "SMP0210");
    }

    // ------------------------------------------------------------------
    // Collection targets: the collection the generated code creates, and the setter it assigns with
    // ------------------------------------------------------------------

    [Theory]
    // Replace builds a List<T> for a collection class of its own
    [InlineData("public ObservableCollection<E2> Items { get; set; } = [];", "", "SMP0217")]
    [InlineData("public Queue<E2> Items { get; set; } = new();", "", "SMP0217")]
    // InPlace creates a List<T> (HashSet<T> for a set) when the target is null
    [InlineData("public E2[] Items { get; set; } = [];", ", Strategy = CollectionStrategy.InPlace", "SMP0217")]
    [InlineData("public ImmutableArray<E2> Items { get; set; }", ", Strategy = CollectionStrategy.InPlace", "SMP0217")]
    [InlineData("public ObservableCollection<E2> Items { get; set; } = [];", ", Strategy = CollectionStrategy.InPlace", "SMP0217")]
    // A target without a setter the mapper class can call
    [InlineData("public List<E2> Items { get; } = [];", "", "SMP0212")]
    [InlineData("public List<E2> Items { get; private set; } = [];", "", "SMP0212")]
    [InlineData("public List<E2> Items { get; } = [];", ", Strategy = CollectionStrategy.InPlace", "SMP0212")]
    public void UnsupportedCollectionTargetEmitsDiagnostic(string targetMember, string options, string id)
    {
        var types = "public class Src { public List<E1> Items { get; set; } = []; } public class Dst { " + targetMember + " }";

        AssertDiagnostic(Source("static E2 MapElem(E1 s) => new();", "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem)" + options + ")] public static partial Dst Map(Src src);", types), id);
    }

    [Theory]
    [InlineData("public IReadOnlyList<E2> Items { get; set; } = [];", "")]
    [InlineData("public IReadOnlySet<E2> Items { get; set; } = new HashSet<E2>();", "")]
    [InlineData("public ImmutableArray<E2> Items { get; set; }", "")]
    [InlineData("public List<E2> Items { get; internal set; } = [];", "")]
    [InlineData("public IList<E2> Items { get; set; } = [];", ", Strategy = CollectionStrategy.InPlace")]
    [InlineData("public ISet<E2> Items { get; set; } = new HashSet<E2>();", ", Strategy = CollectionStrategy.InPlace")]
    [InlineData("public IReadOnlyList<E2> Items { get; set; } = new List<E2>();", ", Strategy = CollectionStrategy.InPlace")]
    public void SupportedCollectionTargetCompiles(string targetMember, string options)
    {
        var types = "public class Src { public List<E1> Items { get; set; } = []; } public class Dst { " + targetMember + " }";

        AssertCompiles(Source("static E2 MapElem(E1 s) => new();", "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem)" + options + ")] public static partial Dst Map(Src src);", types));
    }

    // A collection converter builds the target itself, so a collection class of its own is fine with one.
    [Fact]
    public void CollectionConverterBuildsTargetOfItsOwn()
    {
        var types =
            "public class Src { public List<E1> Items { get; set; } = []; } public class Dst { public ObservableCollection<E2> Items { get; set; } = []; } " +
            "public static class Conv { public static ObservableCollection<TDest> ToList<TSource, TDest>(IEnumerable<TSource> source, Func<TSource, TDest> mapper) => new(source.Select(mapper)); }";

        AssertCompiles(Source("static E2 MapElem(E1 s) => new();", "[CollectionConverter(typeof(Conv))] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))] public static partial Dst Map(Src src);", types));
    }

    [Fact]
    public void NestedTargetWithoutSetterEmitsDiagnostic()
    {
        var types = "public class Src { public E1? Child { get; set; } } public class Dst { public E2? Child { get; } }";

        AssertDiagnostic(Source("static E2 MapChild(E1 s) => new();", "[MapNested(nameof(Dst.Child), Mapper = nameof(MapChild))] public static partial Dst Map(Src src);", types), "SMP0212");
    }

    // ------------------------------------------------------------------
    // Void mappers: the instance the generated code creates with new T()
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("public List<NoCtor> Items { get; set; } = [];", "[MapCollection(nameof(Dst.Items), Mapper = nameof(Fill))]", "static void Fill(E1 s, NoCtor d) { }", "SMP0210")]
    [InlineData("public NoCtor? Child { get; set; }", "[MapNested(nameof(Dst.Child), Mapper = nameof(Fill))]", "static void Fill(E1 s, NoCtor d) { }", "SMP0211")]
    [InlineData("public List<Req> Items { get; set; } = [];", "[MapCollection(nameof(Dst.Items), Mapper = nameof(Fill))]", "static void Fill(E1 s, Req d) { }", "SMP0210")]
    [InlineData("public List<Abs> Items { get; set; } = [];", "[MapCollection(nameof(Dst.Items), Mapper = nameof(Fill))]", "static void Fill(E1 s, Abs d) { }", "SMP0210")]
    public void VoidMapperForInstanceThatCannotBeCreatedEmitsDiagnostic(string targetMember, string attribute, string members, string id)
    {
        var types =
            "public class Req { public required int V { get; set; } } public abstract class Abs { } " +
            "public class Src { public List<E1> Items { get; set; } = []; public E1? Child { get; set; } } public class Dst { " + targetMember + " }";

        AssertDiagnostic(Source(members, attribute + " public static partial Dst Map(Src src);", types), id);
    }

    [Theory]
    // A struct, a constructor the mapper class can call (internal, or with optional parameters), and
    // required members set by the constructor
    [InlineData("public List<S2> Items { get; set; } = [];", "static void Fill(E1 s, ref S2 d) => d.V = s.V;")]
    [InlineData("public List<Internal> Items { get; set; } = [];", "static void Fill(E1 s, Internal d) => d.V = s.V;")]
    [InlineData("public List<Optional> Items { get; set; } = [];", "static void Fill(E1 s, Optional d) => d.V = s.V;")]
    [InlineData("public List<SetsReq> Items { get; set; } = [];", "static void Fill(E1 s, SetsReq d) => d.V = s.V;")]
    // A returning overload is used when a void one cannot be
    [InlineData("public List<NoCtor> Items { get; set; } = [];", "static void Fill(E1 s, NoCtor d) { } static NoCtor Fill(E1 s) => new(s.V);")]
    public void VoidMapperForInstanceThatCanBeCreatedCompiles(string targetMember, string members)
    {
        var types =
            "public class Internal { internal Internal() { } public int V { get; set; } } " +
            "public class Optional { public Optional(int v = 0) { V = v; } public int V { get; set; } } " +
            "public class SetsReq { [System.Diagnostics.CodeAnalysis.SetsRequiredMembers] public SetsReq() { V = 0; } public required int V { get; set; } } " +
            "public class Src { public List<E1> Items { get; set; } = []; } public class Dst { " + targetMember + " }";

        AssertCompiles(Source(members, "[MapCollection(nameof(Dst.Items), Mapper = nameof(Fill))] public static partial Dst Map(Src src);", types));
    }

    // ------------------------------------------------------------------
    // To a string: ToString(format, provider)
    // ------------------------------------------------------------------

    [Theory]
    // No such method, and IFormattable implemented explicitly
    [InlineData("public class Value { }")]
    [InlineData("public class Value : System.IFormattable { string System.IFormattable.ToString(string? format, System.IFormatProvider? provider) => \"\"; }")]
    public void ValueWithoutFormatToStringEmitsDiagnostic(string type)
    {
        AssertDiagnostic(
            Source(string.Empty, "public static partial Dst Map(Src src);", type + " public class Src { public Value Item { get; set; } = new(); } public class Dst { public string Item { get; set; } = \"\"; }"),
            "SMP0402");
    }

    [Fact]
    public void ValueWithFormatToStringIsFormatted()
    {
        var source = Source(
            string.Empty,
            "public static partial Dst Map(Src src);",
            "public class Value : System.IFormattable { public string ToString(string? format, System.IFormatProvider? provider) => \"\"; } " +
            "public class Src { public Value Item { get; set; } = new(); } public class Dst { public string Item { get; set; } = \"\"; }");

        AssertCompiles(source);
        Assert.Contains("__d.Item = src.Item.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
