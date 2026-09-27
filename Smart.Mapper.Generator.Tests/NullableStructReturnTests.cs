namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A return-type mapper returning a nullable struct creates and fills the struct it holds, which converts to the
// return type, and returns default (null) for a null source. It used to create the nullable struct itself with
// new T?(), which is null, and returned that without mapping anything.
public class NullableStructReturnTests
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

    private static string Source(string mapper) =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public struct Point { public int X { get; set; } public int Y { get; set; } }
        public record struct PointRecord(int X, int Y);
        public struct PointRequired { public required int X { get; init; } public int Y { get; set; } }
        public class Src { public int X { get; set; } public int Y { get; set; } }
        public static partial class M
        {
            {{mapper}}
        }
        """;

    [Theory]
    [InlineData("public static partial Point? Map(Src src);", "global::Test.Point? Map(global::Test.Src src)", "var __d = new global::Test.Point();", "__d.Y = src.Y;")]
    [InlineData("public static partial PointRecord? Map(Src src);", "global::Test.PointRecord? Map(global::Test.Src src)", "var __d = new global::Test.PointRecord(src.X, src.Y);", "return __d;")]
    [InlineData("public static partial PointRequired? Map(Src src);", "global::Test.PointRequired? Map(global::Test.Src src)", "X = src.X,", "__d.Y = src.Y;")]
    public void NullableStructIsCreatedAsItsStruct(string mapper, string signature, string creation, string mapping)
    {
        var (generated, problems) = Build(Source("[Mapper] " + mapper));

        Assert.Empty(problems);
        Assert.Contains(signature, generated, StringComparison.Ordinal);
        Assert.Contains(creation, generated, StringComparison.Ordinal);
        Assert.Contains(mapping, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullSourceReturnsNull()
    {
        var (generated, problems) = Build(Source("[Mapper] public static partial Point? Map(Src? src);"));

        Assert.Empty(problems);
        Assert.Contains("return default;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.X = src.X;", generated, StringComparison.Ordinal);
    }
}
