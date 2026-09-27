namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

// A diagnostic whose cause is an attribute is reported at that attribute: the second of two attributes that
// contradict each other (SMP0101), and the [MapperProfile] of the class for a value it gives. One about the method,
// the automatic mapping, the construction of the destination, or strict mode is reported at the mapper method, as
// before. They used to be reported at the method all alike. The locations stay out of the models the source is
// generated from, so moving the code does not generate the source again.
public class DiagnosticLocationTests
{
    // The line the marker comment is on, where the diagnostic is expected to start
    private static int MarkedLine(string source) =>
        Array.FindIndex(source.Split('\n'), static line => line.Contains("/*here*/", StringComparison.Ordinal));

    private static void AssertReportedAtMarker(string source, string id)
    {
        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(source), d => d.Id == id);

        Assert.True(diagnostic.Location.IsInSource);
        Assert.Equal(MarkedLine(source), diagnostic.Location.GetLineSpan().StartLinePosition.Line);
    }

    private static string Source(string attributes, string members = "", string classAttributes = "", string signature = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        public class Src { public int X { get; set; } public int Y { get; set; } public Child Item { get; set; } = new(); public string Text { get; set; } = ""; }
        public class Dst { public int X { get; set; } public int Y { get; set; } public Child Item { get; set; } = new(); public string Text { get; set; } = ""; }
        public record Rec(int X, int Z);
        {{classAttributes}}
        public static partial class M
        {
        {{attributes}}
            {{signature}}
            {{members}}
        }
        """;

    // At the attribute that is the cause
    [Theory]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapProperty(nameof(Dst.Y), \"Missing\")] /*here*/", "", "SMP0213")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapProperty(\"Missing\", nameof(Src.X))] /*here*/", "", "SMP0214")]
    [InlineData("    [Mapper]\n    [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = \"Missing\")] /*here*/", "", "SMP0104")]
    [InlineData("    [Mapper]\n    [MapCondition(nameof(Dst.Y), \"Missing\")] /*here*/", "", "SMP0106")]
    [InlineData("    [Mapper]\n    [BeforeMap(\"Missing\")] /*here*/", "", "SMP0102")]
    [InlineData("    [Mapper]\n    [AfterMap(\"Missing\")] /*here*/", "", "SMP0103")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapUsing(nameof(Dst.Y), \"Missing\")] /*here*/", "", "SMP0201")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapFrom(nameof(Dst.Y), \"Missing\")] /*here*/", "", "SMP0204")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapNested(nameof(Dst.Item), \"Missing\", Mapper = nameof(MapChild))] /*here*/", "public static partial Child MapChild(Child source);", "SMP0206")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapNested(nameof(Dst.Item), Mapper = \"Missing\")] /*here*/", "", "SMP0211")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapConstant(nameof(Dst.Y), \"text\")] /*here*/", "", "SMP0218")]
    [InlineData("    [Mapper]\n    [MapIgnore(\"Item.V\")] /*here*/", "", "SMP0223")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapCondition(nameof(Dst.Y), nameof(Can))] /*here*/", "private static bool Can(Src src) => true;", "SMP0221")]
    [InlineData("    [Mapper(Culture = \"not a culture\")] /*here*/", "", "SMP0404")]
    [InlineData("    [Mapper]\n    [MapProperty(nameof(Dst.Text), nameof(Src.X), NumberFormat = \"N2\")] /*here*/", "", "SMP0401")]
    public void DiagnosticOfAttributeIsReportedAtAttribute(string attributes, string members, string id)
    {
        AssertReportedAtMarker(Source(attributes, members), id);
    }

    // At the second of the attributes contradicting each other, in the order they are declared
    [Theory]
    [InlineData("    [Mapper]\n    [MapProperty(nameof(Dst.Y), nameof(Src.X))]\n    [MapConstant(nameof(Dst.Y), 1)] /*here*/")]
    [InlineData("    [Mapper]\n    [MapIgnore(nameof(Dst.Y))]\n    [MapConstant(nameof(Dst.Y), 1)] /*here*/")]
    [InlineData("    [Mapper]\n    [MapConstant(nameof(Dst.Y), 1)]\n    [MapIgnore(nameof(Dst.Y))] /*here*/")]
    [InlineData("    [Mapper]\n    [MapProperty(\"Item.V\", nameof(Src.X))]\n    [MapProperty(nameof(Dst.Item), nameof(Src.Item))] /*here*/")]
    public void DuplicateIsReportedAtSecondAttribute(string attributes)
    {
        AssertReportedAtMarker(Source(attributes), "SMP0101");
    }

    // The [MapperProfile] of the class gives the value
    [Fact]
    public void ProfileValueIsReportedAtProfile()
    {
        AssertReportedAtMarker(Source("    [Mapper]", classAttributes: "[MapperProfile(Culture = \"not a culture\")] /*here*/"), "SMP0404");
    }

    // The converter class the [ValueConverter] of the class names lacks the overload taking the culture
    [Fact]
    public void ConverterClassIsReportedAtValueConverter()
    {
        var source = Source(
            "    [Mapper(Culture = \"en-US\")]\n    [MapProperty(nameof(Dst.Text), nameof(Src.X))]",
            classAttributes: "public static class MyConverter { public static string ConvertToString(int value) => \"\"; public static TD Convert<TS, TD>(TS value) => default!; }\n[ValueConverter(typeof(MyConverter))] /*here*/");

        AssertReportedAtMarker(source, "SMP0104");
    }

    // The automatic mapping, the construction and the method itself are reported at the method
    [Theory]
    [InlineData("    [Mapper] /*here*/", "public static partial Rec Map(Src src);", "SMP0301")]
    [InlineData("    [Mapper] /*here*/", "public static partial IDisposable Map(Src src);", "SMP0305")]
    [InlineData("    [Mapper] /*here*/", "public static partial Dst Map(Src src, int a, int b);", "SMP0003")]
    public void DiagnosticOfMethodIsReportedAtMethod(string attributes, string signature, string id)
    {
        AssertReportedAtMarker(Source(attributes, signature: signature), id);
    }

    // The automatic mapping of a property falls back to a conversion that is not AOT-safe, reported at the method,
    // while one a [MapProperty] names is reported at the attribute
    [Theory]
    [InlineData("    [Mapper] /*here*/", "SMP0402")]
    [InlineData("    [Mapper(AutoMap = false)]\n    [MapProperty(nameof(Dst.X), nameof(Src.Item))] /*here*/", "SMP0402")]
    public void FallbackIsReportedAtCause(string attributes, string id)
    {
        var source = $$"""
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Child { public int V { get; set; } }
            public class Src { public Child Item { get; set; } = new(); }
            public class Dst { public int X { get; set; } public decimal Item { get; set; } }
            public static partial class M
            {
            {{attributes}}
                public static partial Dst Map(Src src);
            }
            """;

        AssertReportedAtMarker(source, id);
    }

    // Moving the code moves the diagnostics, while the source generated is not generated again
    [Fact]
    public void MovedCodeKeepsSourceCached()
    {
        var source = Source("    [Mapper]\n    [MapProperty(nameof(Dst.Y), nameof(Src.X))]");
        var (driver, compilation) = GeneratorTestHelper.CreateTrackingDriver(source);
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        var tree = compilation.SyntaxTrees.First();
        var moved = compilation.ReplaceSyntaxTree(tree, tree.WithChangedText(SourceText.From("// moved\n// down\n" + source)));
        driver = driver.RunGenerators(moved, TestContext.Current.CancellationToken);

        var reasons = driver.GetRunResult().Results[0].TrackedOutputSteps[WellKnownGeneratorOutputs.ImplementationSourceOutput]
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .ToList();
        Assert.NotEmpty(reasons);
        Assert.All(reasons, static reason => Assert.True(reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged, reason.ToString()));
    }
}
