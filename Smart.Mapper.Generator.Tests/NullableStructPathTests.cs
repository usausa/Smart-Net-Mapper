namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A dotted source path of [MapProperty] or [MapFrom] through a nullable struct (Location.Lat for a GeoPoint? Location)
// reads the struct it holds through its Value, under the null check of the nullable struct, as it reads through a nullable
// reference: a statement leaves the target as it is when it is null, NullValue applies, and an expression (a constructor
// argument) gives null or default, which Strict mode reports for a target not taking null. It used to be reported as a
// source that is not found (SMP0213, SMP0204). A path naming Value itself, or a member of the nullable struct (HasValue),
// is read as it is written.
public class NullableStructPathTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);", string mapperAttribute = "[Mapper(AutoMap = false)]") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public struct GeoPoint { public double Lat { get; set; } public string? Label { get; set; } }
        public class Src { public GeoPoint? Location { get; set; } }
        public class Dst { public double Lat { get; set; } public string Label { get; set; } = ""; public double? MaybeLat { get; set; } public bool Known { get; set; } }
        public record Rec(double Lat, double? MaybeLat);
        public static partial class M
        {
            {{mapperAttribute}}
            {{attributes}}
            {{mapper}}

            private static bool IsNorth(double value) => value > 0;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Lat), \"Location.Lat\")]", "__d.Lat = src.Location.Value.Lat;")]
    [InlineData("[MapProperty(nameof(Dst.Lat), \"Location.Value.Lat\")]", "__d.Lat = src.Location.Value.Lat;")]
    [InlineData("[MapProperty(nameof(Dst.MaybeLat), \"Location.Lat\")]", "__d.MaybeLat = src.Location.Value.Lat;")]
    [InlineData("[MapFrom(nameof(Dst.Lat), \"Location.Lat\")]", "__d.Lat = src.Location.Value.Lat;")]
    [InlineData("[MapProperty(nameof(Dst.Lat), \"Location.Lat\", NullBehavior = NullBehavior.Skip)]", "__d.Lat = src.Location.Value.Lat;")]
    [InlineData("[MapProperty(nameof(Dst.Lat), \"Location.Lat\")] [MapCondition(nameof(Dst.Lat), nameof(IsNorth))]", "if (IsNorth(src.Location.Value.Lat))")]
    public void PathIsReadUnderNullCheck(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains("if (src.Location is not null)", generated, StringComparison.Ordinal);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.Location!", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void NullValueAppliesForNullStruct()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Label), \"Location.Label\", NullValue = \"none\")]"));

        Assert.Empty(problems);
        Assert.Contains("__d.Label = src.Location.Value.Label ?? \"none\";", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Label = \"none\";", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorArgumentGetsDefaultForNullStruct()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Rec.Lat), \"Location.Lat\")] [MapProperty(nameof(Rec.MaybeLat), \"Location.Lat\")]",
            "public static partial Rec Map(Src src);",
            "[Mapper]"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.Rec(src.Location is not null ? src.Location.Value.Lat : default!, src.Location is not null ? src.Location.Value.Lat : null)", generated, StringComparison.Ordinal);
    }

    // Strict mode reports the constructor argument not taking null, as for a path through a nullable reference
    [Fact]
    public void StrictReportsConstructorArgumentNotTakingNull()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(
            "[MapProperty(nameof(Rec.Lat), \"Location.Lat\")] [MapProperty(nameof(Rec.MaybeLat), \"Location.Lat\")]",
            "public static partial Rec Map(Src src);",
            "[Mapper(Strict = true)]"));

        var warning = Assert.Single(diagnostics, static d => d.Id == "SMP0502");
        Assert.Contains("target=[Lat]", warning.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // The names are matched under the comparison of the mapper, and a member of the nullable struct is read as it is
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Lat), \"location.lat\")]", "[Mapper(AutoMap = false, NameComparison = System.StringComparison.OrdinalIgnoreCase)]", "__d.Lat = src.Location.Value.Lat;")]
    [InlineData("[MapProperty(nameof(Dst.Known), \"Location.HasValue\")]", "[Mapper(AutoMap = false)]", "__d.Known = src.Location.HasValue;")]
    public void PathIsResolvedAsWritten(string attributes, string mapperAttribute, string expected)
    {
        var (generated, problems) = Build(Source(attributes, mapperAttribute: mapperAttribute));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }
}
