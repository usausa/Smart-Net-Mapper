namespace Smart.Mapper.Generator.Tests;

using System.Globalization;

using Microsoft.CodeAnalysis;

// A source parameter of a nullable struct type has none of the members of the struct it holds (only HasValue and
// Value), so it is reported (SMP0006) at the parameter. It used to be taken without a null check, and nothing was
// mapped from it.
public class NullableStructSourceTests
{
    private static string Source(string mapper) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public struct Point { public int X { get; set; } }
        public class Dst { public int X { get; set; } }
        public static partial class M
        {
            {{mapper}}
        }
        """;

    [Theory]
    [InlineData("[Mapper] public static partial Dst Map(Point? src);")]
    [InlineData("[Mapper] public static partial void Map(Point? src, Dst dst);")]
    [InlineData("[Mapper] public static partial Dst ToDst(this Point? src);")]
    [InlineData("[Mapper] public static partial Dst Map(in Point? src);")]
    public void NullableStructSourceEmitsDiagnostic(string mapper)
    {
        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(Source(mapper)), static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));

        Assert.Equal("SMP0006", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("parameter=[src], type=[Point?]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public void StructSourceIsMapped()
    {
        var source = Source("[Mapper] public static partial Dst Map(Point src);");

        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnostics(source), static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Contains("__d.X = src.X;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
