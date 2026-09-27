namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The method of [MapUsing] and the member of [MapFrom] give a value the generated code assigns as it is, so a type
// C# converts to the target implicitly is taken: a numeric widening (int to long or double), a value to its nullable
// type (decimal to decimal?), a reference conversion through variance (List<Derived> to IEnumerable<Base>) and a
// user-defined implicit operator, as well as the same type, a base class and an interface as before. They used to be
// reported as not matching (SMP0202 / SMP0205). A type only an explicit conversion takes is still reported.
public class ImplicitReturnConversionTests
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

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Base { }
        public class Derived : Base { }
        public readonly struct Amount
        {
            public Amount(decimal value) { Value = value; }
            public decimal Value { get; }
            public static implicit operator Amount(decimal value) => new(value);
        }
        public class Src
        {
            public List<int> Items { get; set; } = [];
            public int Count => Items.Count;
            public int GetCount() => Items.Count;
            public long Big { get; set; }
            public long GetBig() => Big;
        }
        public class Dst
        {
            public long Long { get; set; }
            public double Double { get; set; }
            public decimal? Total { get; set; }
            public int? MaybeCount { get; set; }
            public IEnumerable<Base> Bases { get; set; } = [];
            public Amount Amount { get; set; }
            public int Int { get; set; }
        }
        public record DstRecord(long Long, decimal? Total);
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static int ComputeCount(Src source) => source.Items.Count;
            private static decimal ComputeTotal(Src source) => source.Items.Count;
            private static List<Derived> ComputeBases(Src source) => [];
            private static long ComputeBig(Src source) => source.Big;
        }
        """;

    [Theory]
    [InlineData("[MapUsing(nameof(Dst.Long), nameof(ComputeCount))]", "__d.Long = ComputeCount(src);")]
    [InlineData("[MapUsing(nameof(Dst.Double), nameof(ComputeCount))]", "__d.Double = ComputeCount(src);")]
    [InlineData("[MapUsing(nameof(Dst.Total), nameof(ComputeTotal))]", "__d.Total = ComputeTotal(src);")]
    [InlineData("[MapUsing(nameof(Dst.Bases), nameof(ComputeBases))]", "__d.Bases = ComputeBases(src);")]
    [InlineData("[MapUsing(nameof(Dst.Amount), nameof(ComputeTotal))]", "__d.Amount = ComputeTotal(src);")]
    [InlineData("[MapFrom(nameof(Dst.Long), \"Items.Count\")]", "__d.Long = src.Items.Count;")]
    [InlineData("[MapFrom(nameof(Dst.MaybeCount), nameof(Src.GetCount))]", "__d.MaybeCount = src.GetCount();")]
    [InlineData("[MapFrom(nameof(Dst.Double), nameof(Src.Count))]", "__d.Double = src.Count;")]
    public void ImplicitConversionIsTaken(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The same for a constructor argument, of the type of the parameter
    [Fact]
    public void ConstructorArgumentTakesImplicitConversion()
    {
        var (generated, problems) = Build(Source(
            "[MapUsing(nameof(DstRecord.Long), nameof(ComputeCount))] [MapFrom(nameof(DstRecord.Total), nameof(Src.Count))]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.DstRecord(ComputeCount(src), src.Count)", generated, StringComparison.Ordinal);
    }

    // A type only an explicit conversion takes is still reported
    [Theory]
    [InlineData("[MapUsing(nameof(Dst.Int), nameof(ComputeBig))]", "SMP0202")]
    [InlineData("[MapFrom(nameof(Dst.Int), nameof(Src.GetBig))]", "SMP0205")]
    [InlineData("[MapFrom(nameof(Dst.Int), nameof(Src.Big))]", "SMP0205")]
    public void ExplicitConversionIsReported(string attributes, string id)
    {
        var (_, problems) = Build(Source(attributes));

        Assert.Equal(id, Assert.Single(problems));
    }
}
