namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A collection class of its own, such as class ItemList : List<Item>, is a collection by the IEnumerable<T>
// it implements through its base type or interfaces, on the source and on the target. It used to be
// reported as not a collection (SMP0210 / SMP0211). A target of such a class, generic or not, that cannot
// take the List<T> the loop builds is created with its own constructor and filled through ICollection<T>;
// one the mapper cannot create is reported (SMP0212).
public class CollectionClassTests
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

    private static string Source(string sourceMember, string targetMember) =>
        $$"""
        #nullable enable
        using System.Collections;
        using System.Collections.Generic;
        using System.Collections.ObjectModel;
        using Smart.Mapper;
        namespace Test;
        public class E1 { public int V { get; set; } }
        public class E2 { public int V { get; set; } }
        public class E1List : List<E1> { }
        public class E2List : List<E2> { }
        public class E2Collection : Collection<E2> { }
        public class E2NoCtorList : List<E2> { public E2NoCtorList(int capacity) : base(capacity) { } }
        public class E1Sequence : IEnumerable<E1>
        {
            public IEnumerator<E1> GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public class E2Sequence : IEnumerable<E2>
        {
            public IEnumerator<E2> GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public class Src { {{sourceMember}} }
        public class Dst { {{targetMember}} }
        public static partial class M
        {
            [Mapper]
            [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))]
            public static partial Dst Map(Src src);
            static E2 MapElem(E1 s) => new() { V = s.V };
        }
        """;

    [Theory]
    // Source classes of their own, iterated with foreach
    [InlineData("public E1List Items { get; set; } = [];", "public List<E2> Items { get; set; } = [];", "foreach (var __item in __srcColl)")]
    [InlineData("public E1Sequence Items { get; set; } = new();", "public List<E2> Items { get; set; } = [];", "foreach (var __item in src.Items)")]
    // Target classes of their own, created with their constructor
    [InlineData("public List<E1> Items { get; set; } = [];", "public E2List Items { get; set; } = [];", "var __coll = new global::Test.E2List();")]
    [InlineData("public List<E1> Items { get; set; } = [];", "public E2Collection Items { get; set; } = new();", "var __coll = new global::Test.E2Collection();")]
    [InlineData("public E1List Items { get; set; } = [];", "public ObservableCollection<E2> Items { get; set; } = [];", "var __coll = new global::System.Collections.ObjectModel.ObservableCollection<global::Test.E2>();")]
    public void CollectionClassIsMapped(string sourceMember, string targetMember, string code)
    {
        var source = Source(sourceMember, targetMember);

        AssertCompiles(source);
        Assert.Contains(code, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // The instance is filled through ICollection<T> and assigned to the target
    [Fact]
    public void CollectionClassTargetIsFilledThroughCollection()
    {
        var source = Source("public List<E1> Items { get; set; } = [];", "public E2List Items { get; set; } = [];");

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("var __items = (global::System.Collections.Generic.ICollection<global::Test.E2>)__coll;", generated, StringComparison.Ordinal);
        Assert.Contains("__items.Add(MapElem(__src[__i]));", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Items = __coll;", generated, StringComparison.Ordinal);
    }

    [Theory]
    // A class the mapper cannot create, or one that is only a sequence
    [InlineData("public List<E1> Items { get; set; } = [];", "public E2NoCtorList Items { get; set; } = new(0);", "SMP0212")]
    [InlineData("public List<E1> Items { get; set; } = [];", "public E2Sequence Items { get; set; } = new();", "SMP0212")]
    // A string is not a collection of chars
    [InlineData("public string Items { get; set; } = \"\";", "public List<E2> Items { get; set; } = [];", "SMP0210")]
    public void CollectionClassThatCannotBeMappedEmitsDiagnostic(string sourceMember, string targetMember, string id)
    {
        AssertDiagnostic(Source(sourceMember, targetMember), id);
    }
}
