namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The mapper of [MapNested] / [MapCollection] matches through conversions as well as by the types themselves: the
// source (element) goes to a parameter taken by value through an implicit reference conversion, a nullable struct as
// the value it holds after a null check (a null one giving default), the result goes to the target through an implicit
// reference conversion or into a nullable struct, and the instance a void mapper fills goes to a parameter taken by
// value through an implicit reference conversion. Such mappers used to be reported as not matching (SMP0214 /
// SMP0213). An overload of the types themselves is taken over one through conversions, overloads matching through
// conversions alike are ambiguous and reported, and a mapper that no conversion reaches is reported as before.
public class MapperTypeConversionTests
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

    private static string Source(string attributes, string members, string mapper = "public static partial Dst Map(Src src);", string classAttributes = "") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public interface IChild { int V { get; } }
        public class Child : IChild { public int V { get; set; } }
        public interface ISrc { }
        public class SrcBase { public int V { get; set; } }
        public class SrcDerived : SrcBase, ISrc { }
        public struct SP { public int V { get; set; } }
        public struct DP { public int V { get; set; } }
        public class Src
        {
            public SrcDerived A { get; set; } = new();
            public SP? B { get; set; }
            public SP C { get; set; }
            public List<SrcDerived> L { get; set; } = [];
            public List<SP?> N { get; set; } = [];
            public SP?[] R { get; set; } = [];
        }
        public class Dst
        {
            public IChild? A { get; set; }
            public Child? AC { get; set; }
            public DP B { get; set; }
            public DP? BN { get; set; }
            public DP? C { get; set; }
            public List<IChild> L { get; set; } = [];
            public List<DP> N { get; set; } = [];
            public DP?[] R { get; set; } = [];
        }
        {{classAttributes}}
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            {{members}}
        }
        """;

    [Theory]
    // The result into an interface it implements, and the source into a base class
    [InlineData("[MapNested(nameof(Dst.A), Mapper = nameof(ToChild))]", "static Child ToChild(SrcBase src) => new();", "__d.A = ToChild(src.A);")]
    // A nullable struct as the value it holds, into the struct and into a nullable struct
    [InlineData("[MapNested(nameof(Dst.B), Mapper = nameof(MapP))]", "static DP MapP(SP src) => new();", "__d.B = src.B is not null ? MapP(src.B.Value) : default!;")]
    [InlineData("[MapNested(nameof(Dst.BN), nameof(Src.B), Mapper = nameof(MapP))]", "static DP MapP(SP src) => new();", "__d.BN = src.B is not null ? MapP(src.B.Value) : default(global::Test.DP?);")]
    [InlineData("[MapNested(nameof(Dst.C), Mapper = nameof(MapP))]", "static DP MapP(SP src) => new();", "__d.C = MapP(src.C);")]
    // A void mapper filling a base class of the instance, taking a nullable struct as its value
    [InlineData("[MapNested(nameof(Dst.AC), nameof(Src.A), Mapper = nameof(Fill))]", "static void Fill(SrcBase src, IChild dst) { }", "Fill(src.A, __nested_AC);")]
    [InlineData("[MapNested(nameof(Dst.B), Mapper = nameof(FillP))]", "static void FillP(SP src, ref DP dst) { }", "FillP(src.B.Value, ref __nested_B);")]
    public void NestedMapperMatchesThroughConversions(string attributes, string members, string expected)
    {
        var (generated, problems) = Build(Source(attributes, members));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapCollection(nameof(Dst.L), Mapper = nameof(ToChild))]", "static Child ToChild(SrcBase src) => new();", "__dst[__i] = ToChild(__src[__i]);")]
    [InlineData("[MapCollection(nameof(Dst.N), Mapper = nameof(MapP))]", "static DP MapP(SP src) => new();", "__dst[__i] = __src[__i] is { } __value ? MapP(__value) : default!;")]
    [InlineData("[MapCollection(nameof(Dst.R), Mapper = nameof(MapP))]", "static DP MapP(SP src) => new();", "__arr[__i] = __src[__i] is { } __value ? MapP(__value) : default(global::Test.DP?);")]
    [InlineData("[MapCollection(nameof(Dst.N), Mapper = nameof(MapIn))]", "static DP MapIn(in SP src) => new();", "__dst[__i] = __src[__i] is { } __value ? MapIn(__value) : default!;")]
    [InlineData("[MapCollection(nameof(Dst.N), Mapper = nameof(FillP))]", "static void FillP(SP src, ref DP dst) { }", "if (__src[__i] is { } __value)")]
    public void ElementMapperMatchesThroughConversions(string attributes, string members, string expected)
    {
        var (generated, problems) = Build(Source(attributes, members));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A collection converter takes the mapper as a delegate, which a reference conversion reaches, but not the value
    // of a nullable struct
    [Theory]
    [InlineData("[MapCollection(nameof(Dst.L), Mapper = nameof(ToChild))]", "static Child ToChild(SrcBase src) => new();", "")]
    [InlineData("[MapCollection(nameof(Dst.N), Mapper = nameof(MapP))]", "static DP MapP(SP src) => new();", "SMP0213")]
    public void ConverterTakesReferenceConversionsOnly(string attributes, string members, string id)
    {
        var (generated, problems) = Build(Source(attributes, members, classAttributes: "[CollectionConverter(typeof(DefaultCollectionConverter))]"));

        if (id.Length == 0)
        {
            Assert.Empty(problems);
            Assert.Contains("ToList<global::Test.SrcDerived, global::Test.IChild>(src.L, ToChild)!", generated, StringComparison.Ordinal);
        }
        else
        {
            Assert.Equal(id, Assert.Single(problems));
        }
    }

    // The overload of the types themselves is taken, and of those through conversions, the one of the more
    // specific types, as the call binds
    [Theory]
    [InlineData("static Child ToChild(SrcBase src) => new(); static Child ToChild(SrcDerived src) => new();")]
    [InlineData("static Child ToChild(SrcBase src) => new(); static Child ToChild(object src) => new();")]
    public void OverloadTheCallBindsToIsTaken(string members)
    {
        var (generated, problems) = Build(Source("[MapNested(nameof(Dst.AC), nameof(Src.A), Mapper = nameof(ToChild))]", members));

        Assert.Empty(problems);
        Assert.Contains("__d.AC = ToChild(src.A);", generated, StringComparison.Ordinal);
    }

    [Theory]
    // Two overloads neither of which is more specific would make the call ambiguous (CS0121)
    [InlineData("static Child ToChild(SrcBase src) => new(); static Child ToChild(ISrc src) => new();")]
    // No conversion from the source, to the target, or by ref
    [InlineData("static Child ToChild(SP src) => new();")]
    [InlineData("static SrcBase ToChild(SrcDerived src) => new();")]
    [InlineData("static void ToChild(SrcDerived src, ref IChild dst) { }")]
    public void MapperWithoutConversionEmitsDiagnostic(string members)
    {
        var (_, problems) = Build(Source("[MapNested(nameof(Dst.AC), nameof(Src.A), Mapper = nameof(ToChild))]", members));

        Assert.Equal("SMP0214", Assert.Single(problems));
    }
}
