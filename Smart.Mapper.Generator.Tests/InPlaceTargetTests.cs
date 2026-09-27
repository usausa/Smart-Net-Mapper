namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// InPlace clears and refills the target through ICollection<T>. A target the mapper cannot assign (get-only,
// a private setter, init-only) is filled when it holds an instance and left null otherwise, and a
// collection class it can create, such as ObservableCollection<T>, is created with its own constructor when
// the target is null. A declared type without ICollection<T>, or one that is read-only by design, cannot be
// refilled and is reported (SMP0219) instead of throwing at run time.
public class InPlaceTargetTests
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

    private static string Source(string targetMember, string types = "") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Collections.ObjectModel;
        using Smart.Mapper;
        namespace Test;
        public class E1 { public int V { get; set; } }
        public class E2 { public int V { get; set; } }
        public class Src { public List<E1> Items { get; set; } = []; }
        public class Dst { {{targetMember}} }
        {{types}}
        public static partial class M
        {
            [Mapper]
            [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem), Strategy = CollectionStrategy.InPlace)]
            public static partial void Map(Src src, Dst dst);
            static E2 MapElem(E1 s) => new() { V = s.V };
        }
        """;

    // Without a setter the mapper can call, the target is filled only when it holds an instance.
    [Theory]
    [InlineData("public List<E2> Items { get; } = [];", "dst.Items.Clear();")]
    [InlineData("public List<E2> Items { get; private set; } = [];", "dst.Items.Clear();")]
    [InlineData("public List<E2> Items { get; init; } = [];", "dst.Items.Clear();")]
    [InlineData("public ObservableCollection<E2> Items { get; } = [];", "((global::System.Collections.Generic.ICollection<global::Test.E2>)dst.Items).Clear();")]
    public void TargetWithoutSetterIsFilledWhenItHoldsInstance(string targetMember, string clear)
    {
        var source = Source(targetMember);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("if (dst.Items is not null)", generated, StringComparison.Ordinal);
        Assert.Contains(clear, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dst.Items = new", generated, StringComparison.Ordinal);
    }

    // A null target the mapper can assign gets an instance of its own type, or a List<T> / HashSet<T> for
    // an interface, which take the capacity.
    [Theory]
    [InlineData("public ObservableCollection<E2> Items { get; set; } = [];", "dst.Items = new global::System.Collections.ObjectModel.ObservableCollection<global::Test.E2>();")]
    [InlineData("public SortedSet<E2> Items { get; set; } = new();", "dst.Items = new global::System.Collections.Generic.SortedSet<global::Test.E2>();")]
    [InlineData("public ItemCollection Items { get; set; } = new();", "dst.Items = new global::Test.ItemCollection();")]
    [InlineData("public List<E2> Items { get; set; } = [];", "dst.Items = new global::System.Collections.Generic.List<global::Test.E2>(src.Items.Count);")]
    [InlineData("public IList<E2> Items { get; set; } = [];", "dst.Items = new global::System.Collections.Generic.List<global::Test.E2>(src.Items.Count);")]
    [InlineData("public ISet<E2> Items { get; set; } = new HashSet<E2>();", "dst.Items = new global::System.Collections.Generic.HashSet<global::Test.E2>(src.Items.Count);")]
    public void NullTargetGetsInstance(string targetMember, string creation)
    {
        var source = Source(targetMember, "public class ItemCollection : Collection<E2> { }");

        AssertCompiles(source);
        Assert.Contains(creation, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A declared type without ICollection<T>, or read-only by design, cannot be refilled.
    [Theory]
    [InlineData("public IReadOnlyList<E2> Items { get; set; } = [];")]
    [InlineData("public IReadOnlyCollection<E2> Items { get; set; } = [];")]
    [InlineData("public IEnumerable<E2> Items { get; set; } = [];")]
    [InlineData("public E2[] Items { get; set; } = [];")]
    [InlineData("public ImmutableArray<E2> Items { get; set; }")]
    [InlineData("public ImmutableList<E2> Items { get; set; } = [];")]
    [InlineData("public ReadOnlyCollection<E2> Items { get; set; } = new([]);")]
    public void TargetThatCannotBeRefilledEmitsDiagnostic(string targetMember)
    {
        AssertDiagnostic(Source(targetMember), "SMP0219");
    }

    // A settable target whose type the mapper cannot create, nor take a List<T> into, is reported as before.
    [Theory]
    [InlineData("public ItemsBase Items { get; set; } = new ItemsImpl();")]
    [InlineData("public IItems Items { get; set; } = new ItemsImpl();")]
    public void TargetThatCannotBeCreatedEmitsDiagnostic(string targetMember)
    {
        AssertDiagnostic(
            Source(targetMember, "public abstract class ItemsBase : Collection<E2> { } public interface IItems : IList<E2> { } public class ItemsImpl : ItemsBase, IItems { }"),
            "SMP0217");
    }
}
