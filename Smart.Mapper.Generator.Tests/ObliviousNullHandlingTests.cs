namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A reference declared without nullable annotations (nullable disabled) may hold null, so NullValue and
// NullBehavior.Skip apply to it as they do to one annotated as nullable. They used to be left out for it, and the
// value was copied as it is.
public class ObliviousNullHandlingTests
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

    private static string Source(string attributes, string mapper = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        using System;
        using Smart.Mapper;
        namespace Test;
        #nullable disable
        public class Src { public string Name { get; set; } public string Code { get; set; } }
        public class Dst { public string Name { get; set; } public int Code { get; set; } }
        public record DstRecord(string Name, int Code);
        #nullable enable
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name), NullValue = \"Unknown\")]", "dst.Name = src.Name ?? \"Unknown\";")]
    [InlineData("[MapProperty(nameof(Dst.Code), NullValue = 0)]", "dst.Code = src.Code is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(src.Code) : 0;")]
    [InlineData("[MapProperty(nameof(Dst.Name), NullBehavior = NullBehavior.Skip)]", "if (src.Name is not null)")]
    [InlineData("[MapProperty(nameof(Dst.Code), NullBehavior = NullBehavior.Skip)]", "if (src.Code is not null)")]
    public void NullHandlingAppliesToObliviousSource(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullValueAppliesToObliviousConstructorArgument()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(DstRecord.Name), NullValue = \"Unknown\")] [MapProperty(nameof(DstRecord.Code), NullValue = 0)]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.DstRecord(src.Name ?? \"Unknown\", src.Code is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(src.Code) : 0)", generated, StringComparison.Ordinal);
    }

    // Without an attribute asking for null handling, it is copied as before
    [Fact]
    public void ObliviousSourceIsCopiedWithoutNullHandling()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Name))]"));

        Assert.Empty(problems);
        Assert.Contains("dst.Name = src.Name;", generated, StringComparison.Ordinal);
    }
}
