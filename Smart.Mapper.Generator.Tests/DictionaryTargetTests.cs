namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A [MapCollection] target of IDictionary<TKey, TValue> or IReadOnlyDictionary<TKey, TValue> gets a
// Dictionary<TKey, TValue>, filled through ICollection<KeyValuePair<TKey, TValue>> with the pairs the element mapper
// returns, as a List<T> is created for IList<T>. The loop used to build a List<KeyValuePair<TKey, TValue>> for it,
// which was reported (SMP0212). InPlace keeps its rules: an IReadOnlyDictionary<TKey, TValue> cannot be refilled
// (SMP0208), and a null IDictionary<TKey, TValue> the mapper can assign gets a Dictionary<TKey, TValue>, as a null
// IList<T> gets a List<T>, where the List<KeyValuePair<TKey, TValue>> it used to try was reported (SMP0212).
public class DictionaryTargetTests
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

    private static string Source(string target, string attributes, string mapper = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class SrcItem { public int V { get; set; } }
        public class DstItem { public int V { get; set; } }
        public class Src { public Dictionary<string, SrcItem> Items { get; set; } = []; public Dictionary<string, SrcItem>? Maybe { get; set; } }
        public class Dst { {{target}} }
        public record DstRecord(IReadOnlyDictionary<string, DstItem> Items);
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static KeyValuePair<string, DstItem> MapPair(KeyValuePair<string, SrcItem> pair) => new(pair.Key, new DstItem { V = pair.Value.V });
        }
        """;

    [Theory]
    [InlineData("public IDictionary<string, DstItem> Items { get; set; } = new Dictionary<string, DstItem>();")]
    [InlineData("public IReadOnlyDictionary<string, DstItem> Items { get; set; } = new Dictionary<string, DstItem>();")]
    [InlineData("public IReadOnlyDictionary<string, DstItem>? Items { get; set; }")]
    public void DictionaryInterfaceGetsDictionary(string target)
    {
        var (generated, problems) = Build(Source(target, "[MapCollection(\"Items\", Mapper = nameof(MapPair))]"));

        Assert.Empty(problems);
        Assert.Contains("var __coll = new global::System.Collections.Generic.Dictionary<string, global::Test.DstItem>();", generated, StringComparison.Ordinal);
        Assert.Contains("var __items = (global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<string, global::Test.DstItem>>)__coll;", generated, StringComparison.Ordinal);
        Assert.Contains("__items.Add(MapPair(__item));", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Items = __coll;", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullSourceGivesDefault()
    {
        var (generated, problems) = Build(Source(
            "public IReadOnlyDictionary<string, DstItem>? Items { get; set; }",
            "[MapCollection(\"Items\", nameof(Src.Maybe), Mapper = nameof(MapPair))]"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Maybe is null)", generated, StringComparison.Ordinal);
        Assert.Contains("new global::System.Collections.Generic.Dictionary<string, global::Test.DstItem>();", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorArgumentGetsDictionary()
    {
        var (generated, problems) = Build(Source(
            string.Empty,
            "[MapCollection(\"Items\", Mapper = nameof(MapPair))]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("global::System.Collections.Generic.IReadOnlyDictionary<string, global::Test.DstItem> __arg0;", generated, StringComparison.Ordinal);
        Assert.Contains("new global::System.Collections.Generic.Dictionary<string, global::Test.DstItem>();", generated, StringComparison.Ordinal);
    }

    // InPlace keeps its rules
    [Theory]
    [InlineData("public IReadOnlyDictionary<string, DstItem> Items { get; set; } = new Dictionary<string, DstItem>();", "SMP0208")]
    public void InPlaceKeepsItsRules(string target, string id)
    {
        var (_, problems) = Build(Source(
            target,
            "[MapCollection(\"Items\", Mapper = nameof(MapPair), Strategy = CollectionStrategy.InPlace)]",
            "public static partial void Map(Src src, Dst dst);"));

        Assert.Equal(id, Assert.Single(problems));
    }

    // A null IDictionary<TKey, TValue> the mapper can assign gets a Dictionary<TKey, TValue>, as a null IList<T> gets a
    // List<T>
    [Fact]
    public void InPlaceCreatesDictionaryForNullTarget()
    {
        var (generated, problems) = Build(Source(
            "public IDictionary<string, DstItem>? Items { get; set; }",
            "[MapCollection(\"Items\", Mapper = nameof(MapPair), Strategy = CollectionStrategy.InPlace)]",
            "public static partial void Map(Src src, Dst dst);"));

        Assert.Empty(problems);
        Assert.Contains("dst.Items = new global::System.Collections.Generic.Dictionary<string, global::Test.DstItem>();", generated, StringComparison.Ordinal);
        Assert.Contains("((global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<string, global::Test.DstItem>>)dst.Items).Clear();", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void InPlaceRefillsDictionary()
    {
        var (generated, problems) = Build(Source(
            "public IDictionary<string, DstItem> Items { get; } = new Dictionary<string, DstItem>();",
            "[MapCollection(\"Items\", Mapper = nameof(MapPair), Strategy = CollectionStrategy.InPlace)]",
            "public static partial void Map(Src src, Dst dst);"));

        Assert.Empty(problems);
        Assert.Contains("((global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<string, global::Test.DstItem>>)dst.Items).Clear();", generated, StringComparison.Ordinal);
    }
}
