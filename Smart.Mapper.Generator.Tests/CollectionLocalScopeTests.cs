namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// The locals emitted for a collection mapping (__src, __srcList, __count, __list, __dst, ...) have
// fixed names. A mapping whose source cannot be null used to declare them directly in the method body,
// so a second such [MapCollection] in the same mapper redeclared them (CS0128), and a mapping with a
// nullable source, which declares them in the else block of its null guard, collided with them (CS0136).
// The other tests put several collections in one mapper only when every source is nullable.
public class CollectionLocalScopeTests
{
    private static void AssertCompiles(string source)
    {
        var errors = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(errors.Count == 0, String.Join("\n", errors));
    }

    // Locals declared directly in the body of a generated method, outside any nested block.
    private static string[] GetMethodScopeLocals(string generated, string methodName) =>
        CSharpSyntaxTree.ParseText(generated).GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(x => x.Identifier.Text == methodName)
            .Body!.Statements
            .OfType<LocalDeclarationStatementSyntax>()
            .SelectMany(static x => x.Declaration.Variables)
            .Select(static x => x.Identifier.Text)
            .ToArray();

    // Each source shape declares its own set of locals, and the target shape adds more.
    [Theory]
    [InlineData("global::System.Collections.Generic.IReadOnlyList<E1>", "global::System.Collections.Generic.IReadOnlyList<E2>")]
    [InlineData("global::System.Collections.Generic.List<E1>", "global::System.Collections.Generic.List<E2>")]
    [InlineData("E1[]", "E2[]")]
    [InlineData("global::System.Collections.Generic.IEnumerable<E1>", "global::System.Collections.Generic.List<E2>")]
    [InlineData("global::System.Collections.Generic.IEnumerable<E1>", "global::System.Collections.Immutable.ImmutableHashSet<E2>")]
    [InlineData("global::System.Collections.Generic.IReadOnlyCollection<E1>", "E2[]")]
    [InlineData("global::System.Collections.Generic.ICollection<E1>", "global::System.Collections.Immutable.ImmutableArray<E2>")]
    [InlineData("global::System.Collections.Immutable.ImmutableArray<E1>", "global::System.Collections.Generic.HashSet<E2>")]
    [InlineData("global::System.Memory<E1>", "global::System.Collections.Frozen.FrozenSet<E2>")]
    [InlineData("global::System.ReadOnlyMemory<E1>", "global::System.Collections.Immutable.ImmutableList<E2>")]
    public void NonNullableCollectionsInOneMapperCompile(string sourceType, string targetType)
    {
        var source = $$"""
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class E1 { public int V { get; set; } }
            public class E2 { public int V { get; set; } }
            public class Src
            {
                public {{sourceType}} First { get; set; } = default!;
                public {{sourceType}} Second { get; set; } = default!;
            }
            public class Dst
            {
                public {{targetType}} First { get; set; } = default!;
                public {{targetType}} Second { get; set; } = default!;
            }
            public static partial class M
            {
                [Mapper]
                [MapCollection(nameof(Dst.First), Mapper = nameof(MapElem))]
                [MapCollection(nameof(Dst.Second), Mapper = nameof(MapElem))]
                public static partial Dst Map(Src src);
                public static E2 MapElem(E1 s) => new() { V = s.V };
            }
            """;

        AssertCompiles(source);
    }

    // A void element mapper adds __dest inside the loop body.
    [Fact]
    public void NonNullableCollectionsWithVoidElementMapperCompile()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            public class E1 { public int V { get; set; } }
            public class E2 { public int V { get; set; } }
            public class Src
            {
                public E1[] First { get; set; } = [];
                public IEnumerable<E1> Second { get; set; } = [];
            }
            public class Dst
            {
                public List<E2> First { get; set; } = [];
                public List<E2> Second { get; set; } = [];
            }
            public static partial class M
            {
                [Mapper]
                [MapCollection(nameof(Dst.First), Mapper = nameof(FillElem))]
                [MapCollection(nameof(Dst.Second), Mapper = nameof(FillElem))]
                public static partial void Map(Src src, Dst dst);
                public static void FillElem(E1 s, E2 d) => d.V = s.V;
            }
            """;

        AssertCompiles(source);
    }

    // One nullable and one non-nullable source of the same shape were already enough to fail.
    [Fact]
    public void NonNullableAndNullableCollectionsInOneMapperCompile()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            public class E1 { public int V { get; set; } }
            public class E2 { public int V { get; set; } }
            public class Src
            {
                public List<E1>? Optional { get; set; }
                public List<E1> Items { get; set; } = [];
            }
            public class Dst
            {
                public List<E2>? Optional { get; set; }
                public List<E2> Items { get; set; } = [];
            }
            public static partial class M
            {
                [Mapper]
                [MapCollection(nameof(Dst.Optional), Mapper = nameof(MapElem))]
                [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem))]
                public static partial void Map(Src src, Dst dst);
                public static E2 MapElem(E1 s) => new() { V = s.V };
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("if (src.Optional is null)", generated, StringComparison.Ordinal);
        Assert.Contains("dst.Optional = __list;", generated, StringComparison.Ordinal);
        Assert.Contains("dst.Items = __list;", generated, StringComparison.Ordinal);
        Assert.Empty(GetMethodScopeLocals(generated, "Map"));
    }

    // InPlace mappings declare their own locals (__srcSpan, __dstColl, ...) and share __srcList and
    // __count with the inline IReadOnlyList path.
    [Fact]
    public void InPlaceAndInlineCollectionsInOneMapperCompile()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            public class E1 { public int V { get; set; } }
            public class E2 { public int V { get; set; } }
            public class Src
            {
                public List<E1> Items { get; set; } = [];
                public E1[] Values { get; set; } = [];
                public IReadOnlyList<E1> Lines { get; set; } = [];
                public IEnumerable<E1>? Optional { get; set; }
                public IReadOnlyList<E1> Others { get; set; } = [];
            }
            public class Dst
            {
                public List<E2> Items { get; set; } = [];
                public List<E2> Values { get; set; } = [];
                public ICollection<E2> Lines { get; set; } = [];
                public HashSet<E2>? Optional { get; set; }
                public List<E2> Others { get; set; } = [];
            }
            public static partial class M
            {
                [Mapper]
                [MapCollection(nameof(Dst.Items), Mapper = nameof(MapElem), Strategy = CollectionStrategy.InPlace)]
                [MapCollection(nameof(Dst.Values), Mapper = nameof(MapElem), Strategy = CollectionStrategy.InPlace)]
                [MapCollection(nameof(Dst.Lines), Mapper = nameof(MapElem), Strategy = CollectionStrategy.InPlace)]
                [MapCollection(nameof(Dst.Optional), Mapper = nameof(MapElem), Strategy = CollectionStrategy.InPlace)]
                [MapCollection(nameof(Dst.Others), Mapper = nameof(MapElem))]
                public static partial void Map(Src src, Dst dst);
                public static E2 MapElem(E1 s) => new() { V = s.V };
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("if (src.Optional is not null)", generated, StringComparison.Ordinal);
        Assert.Empty(GetMethodScopeLocals(generated, "Map"));
    }

    // The shape that hit the bug: lists that are never null next to a nullable one, nested objects and
    // a computed member, all in one return-type mapper.
    [Fact]
    public void ReturnMapperWithListsAndNestedObjectsCompiles()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            public class E1 { public int V { get; set; } }
            public class E2 { public int V { get; set; } }
            public class N1 { public int V { get; set; } }
            public class N2 { public int V { get; set; } }
            public class Src
            {
                public IReadOnlyList<E1> Lines { get; set; } = default!;
                public IReadOnlyList<E1> Discounts { get; set; } = [];
                public IReadOnlyList<E1> TaxSummaries { get; set; } = [];
                public IReadOnlyList<E1> Payments { get; set; } = default!;
                public List<E1>? Notes { get; set; }
                public N1? Delivery { get; set; }
                public N1 Owner { get; set; } = new();
            }
            public class Dst
            {
                public IReadOnlyList<E2> Lines { get; set; } = default!;
                public IReadOnlyList<E2> Discounts { get; set; } = default!;
                public IReadOnlyList<E2> TaxSummaries { get; set; } = default!;
                public IReadOnlyList<E2> Payments { get; set; } = default!;
                public List<E2>? Notes { get; set; }
                public N2? Delivery { get; set; }
                public N2 Owner { get; set; } = new();
                public int LineCount { get; set; }
            }
            public static partial class M
            {
                [Mapper]
                [MapCollection(nameof(Dst.Lines), Mapper = nameof(MapElem))]
                [MapCollection(nameof(Dst.Discounts), Mapper = nameof(MapElem))]
                [MapCollection(nameof(Dst.TaxSummaries), Mapper = nameof(MapElem))]
                [MapCollection(nameof(Dst.Payments), Mapper = nameof(MapElem))]
                [MapCollection(nameof(Dst.Notes), Mapper = nameof(MapElem))]
                [MapNested(nameof(Dst.Delivery), Mapper = nameof(MapNestedValue))]
                [MapNested(nameof(Dst.Owner), Mapper = nameof(FillNestedValue))]
                [MapUsing(nameof(Dst.LineCount), nameof(CountLines))]
                public static partial Dst Map(Src src);
                public static E2 MapElem(E1 s) => new() { V = s.V };
                public static N2 MapNestedValue(N1 s) => new() { V = s.V };
                public static void FillNestedValue(N1 s, N2 d) => d.V = s.V;
                public static int CountLines(Src s) => s.Lines.Count;
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Lines = __list;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Discounts = __list;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.TaxSummaries = __list;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Payments = __list;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Notes = __list;", generated, StringComparison.Ordinal);
        // The nested instance of a void mapper keeps a per-target name and stays at method scope
        Assert.Equal(["__d", "__nested_Owner"], GetMethodScopeLocals(generated, "Map"));
    }
}
