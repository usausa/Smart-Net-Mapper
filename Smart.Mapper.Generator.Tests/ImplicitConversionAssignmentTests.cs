namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The converter of [MapProperty] returns the target type or a type C# converts to it implicitly, as [MapUsing] and
// [MapFrom] do (decimal to decimal?, int to long), where it used to have to return the target type itself (SMP0111);
// one only an explicit conversion takes is still reported. The automatic mapping and [MapProperty] assign a value the
// target takes by an implicit reference conversion as it is, through variance as well (IReadOnlyList<Circle> to
// IReadOnlyList<Shape>, Circle[] to Shape[]), where it used to be reported as having no conversion (SMP0402) or to go
// through the generic conversion of a [ValueConverter] class.
public class ImplicitConversionAssignmentTests
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

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);", string mapperAttribute = "[Mapper(AutoMap = false)]") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public interface IShape { int Size { get; } }
        public class Shape : IShape { public int Size { get; set; } }
        public class Circle : Shape { }
        public class Src
        {
            public string Price { get; set; } = "1";
            public int Count { get; set; }
            public long Big { get; set; }
            public IReadOnlyList<Circle> Circles { get; set; } = [];
            public List<Circle> More { get; set; } = [];
            public Circle[] Array { get; set; } = [];
            public IShape Shape { get; set; } = new Shape();
        }
        public class Dst
        {
            public decimal? Price { get; set; }
            public long Count { get; set; }
            public int Big { get; set; }
            public IReadOnlyList<Shape> Circles { get; set; } = [];
            public IEnumerable<Shape> More { get; set; } = [];
            public Shape[] Array { get; set; } = [];
            public object Shape { get; set; } = new();
        }
        public record DstRecord(decimal? Price, IReadOnlyList<Shape> Circles);
        public static class Conv
        {
            public static TDestination Convert<TSource, TDestination>(TSource source) => default!;
        }
        public static partial class M
        {
            {{mapperAttribute}}
            {{attributes}}
            {{mapper}}

            private static decimal ParsePrice(string value) => 1m;
            private static int Twice(int value) => value * 2;
            private static long Widen(long value) => value;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Price), Converter = nameof(ParsePrice))]", "__d.Price = ParsePrice(src.Price);")]
    [InlineData("[MapProperty(nameof(Dst.Count), Converter = nameof(Twice))]", "__d.Count = Twice(src.Count);")]
    public void ConverterReturningImplicitlyConvertibleTypeIsTaken(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The same for a constructor argument, of the type of the parameter
    [Fact]
    public void ConstructorArgumentConverterTakesImplicitConversion()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(DstRecord.Price), Converter = nameof(ParsePrice))] [MapProperty(nameof(DstRecord.Circles))]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.DstRecord(ParsePrice(src.Price), src.Circles)", generated, StringComparison.Ordinal);
    }

    // A converter returning a type only an explicit conversion takes is still reported
    [Fact]
    public void ConverterReturningExplicitlyConvertibleTypeIsReported()
    {
        var (_, problems) = Build(Source("[MapProperty(nameof(Dst.Big), Converter = nameof(Widen))]"));

        Assert.Equal("SMP0111", Assert.Single(problems));
    }

    [Theory]
    [InlineData("__d.Circles = src.Circles;")]
    [InlineData("__d.More = src.More;")]
    [InlineData("__d.Array = src.Array;")]
    [InlineData("__d.Shape = src.Shape;")]
    public void ImplicitReferenceConversionIsAssigned(string expected)
    {
        var (generated, problems) = Build(Source("[MapIgnore(nameof(Dst.Price))] [MapIgnore(nameof(Dst.Big))]", mapperAttribute: "[Mapper]"));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The generic conversion of a [ValueConverter] class is not called for it any longer
    [Fact]
    public void ImplicitReferenceConversionDoesNotUseValueConverter()
    {
        var (generated, problems) = Build(Source("[ValueConverter(typeof(Conv))] [MapProperty(nameof(Dst.Circles))]"));

        Assert.Empty(problems);
        Assert.Contains("__d.Circles = src.Circles;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Conv.Convert", generated, StringComparison.Ordinal);
    }
}
