namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A [BeforeMap] / [AfterMap] callback takes the source and the destination as base classes or interfaces they convert
// to by an implicit reference conversion, by value (Audit(IEntity, IDto)), where only their own types matched (SMP0102
// / SMP0103). A struct goes to its own type only, as boxing it would give the callback a copy to write in vain. The
// custom parameters match as before. Of overloads, the one the call binds to is used, as C# binds it, and an ambiguous
// call is reported.
public class CallbackConversionTests
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
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Source(string attributes, string mapper, string methods) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public interface IEntity { int Id { get; } }
        public interface IDto { int Id { get; set; } }
        public class Src : IEntity { public int Id { get; set; } }
        public class Dst : IDto { public int Id { get; set; } }
        public struct PointSrc : IEntity { public int Id { get; set; } }
        public struct PointDst : IDto { public int Id { get; set; } }
        public sealed class Context { }
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{mapper}}

            {{methods}}
        }
        """;

    // A class source and destination go to base classes or interfaces, with the custom parameters as before
    [Theory]
    [InlineData("[AfterMap(nameof(Audit))]", "public static partial void Map(Src src, Dst dst);", "private static void Audit(IEntity source, IDto destination) { }", "Audit(src, dst);")]
    [InlineData("[BeforeMap(nameof(Audit))]", "public static partial void Map(Src src, Dst dst);", "private static void Audit(object source, object destination) { }", "Audit(src, dst);")]
    [InlineData("[AfterMap(nameof(Audit))]", "public static partial Dst Map(Src src);", "private static void Audit(IEntity source, IDto destination) { }", "Audit(src, __d);")]
    [InlineData(
        "[AfterMap(nameof(Audit))]",
        "public static partial void Map(Src src, Dst dst, Context context);",
        "private static void Audit(IEntity source, IDto destination, Context context) { }",
        "Audit(src, dst, context);")]
    [InlineData("[AfterMap(nameof(Audit))]", "public static partial void Map(Src src, ref PointDst dst);", "private static void Audit(IEntity source, ref PointDst destination) { }", "Audit(src, ref dst);")]
    public void SourceAndDestinationGoAsBaseOrInterface(string attributes, string mapper, string methods, string expected)
    {
        var (generated, problems) = Build(Source(attributes, mapper, methods));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A struct goes to its own type only, and a conversion to a parameter by value only
    [Theory]
    [InlineData("[AfterMap(nameof(Audit))]", "public static partial PointDst Map(Src src);", "private static void Audit(IEntity source, IDto destination) { }", "SMP0103")]
    [InlineData("[BeforeMap(nameof(Audit))]", "public static partial void Map(PointSrc src, Dst dst);", "private static void Audit(IEntity source, IDto destination) { }", "SMP0102")]
    [InlineData("[AfterMap(nameof(Audit))]", "public static partial void Map(Src src, Dst dst);", "private static void Audit(IEntity source, ref IDto destination) { }", "SMP0103")]
    public void ConversionIsReported(string attributes, string mapper, string methods, string expected)
    {
        var (_, problems) = Build(Source(attributes, mapper, methods));

        Assert.Equal([expected], problems);
    }

    // The call binds to the most specific overload, here passing the source with in, and an ambiguous one is reported
    [Fact]
    public void CallbackTheCallBindsToIsUsed()
    {
        var (generated, problems) = Build(Source(
            "[AfterMap(nameof(Audit))]",
            "public static partial void Map(Src src, Dst dst);",
            "private static void Audit(IEntity source, IDto destination) { } private static void Audit(in Src source, IDto destination) { }"));

        Assert.Empty(problems);
        Assert.Contains("Audit(in src, dst);", generated, StringComparison.Ordinal);

        var (_, ambiguousProblems) = Build(Source(
            "[AfterMap(nameof(Audit))]",
            "public static partial void Map(Src src, Dst dst);",
            "private static void Audit(IEntity source, Dst destination) { } private static void Audit(Src source, IDto destination) { }"));

        Assert.Equal(["SMP0103"], ambiguousProblems);
    }
}
