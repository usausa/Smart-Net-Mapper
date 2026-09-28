namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A method returning a nullable reference returns one that is not null for a first argument that is not null when
// [return: NotNullIfNotNull] names its first parameter, or when it is a mapper the generated code declares so (a
// return-type mapper whose source may be null returning a nullable type). Given such a value, its result goes to a
// target not annotated as nullable without !, and Strict mode does not report it (SMP0502), as the compiler takes it so:
// the converter of a source that is not nullable, or of one it is called for a value only (with NullValue, or for a
// parameter not taking null), the method of [MapUsing], which gets the source past its null check, the mapper of
// [MapNested] for a source that is not nullable or that it is called for a value only, and the element mapper of
// [MapCollection] for elements that are not nullable. A value that may be null keeps the ! and the warning.
public class NotNullResultCallTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    // The warnings of the generated code, the errors, and SMP0502; the unmapped properties of strict mode aside
    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || (d.Id == "SMP0502"))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string attributes) =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        public class ChildDto { public int V { get; set; } }
        public class Src
        {
            public Child Child { get; set; } = new();
            public Child? MaybeChild { get; set; }
            public List<Child> Items { get; set; } = [];
            public List<Child?> MaybeItems { get; set; } = [];
            public string Name { get; set; } = "";
            public string? Nick { get; set; }
        }
        public class Dst
        {
            public ChildDto Child { get; set; } = new();
            public List<ChildDto> Items { get; set; } = [];
            public string Name { get; set; } = "";
            public string Using { get; set; } = "";
        }
        public static partial class M
        {
            [Mapper]
            public static partial ChildDto? MapChild(Child? source);

            [Mapper(AutoMap = false, Strict = true)]
            {{attributes}}
            public static partial Dst Map(Src src);

            [return: NotNullIfNotNull(nameof(value))]
            private static string? Trim(string? value) => value?.Trim();

            [return: NotNullIfNotNull(nameof(value))]
            private static string? TrimValue(string value) => value.Trim();

            private static string? TrimPlain(string? value) => value?.Trim();

            [return: NotNullIfNotNull(nameof(source))]
            private static string? Describe(Src? source) => source?.Name;

            private static string? DescribePlain(Src source) => source.Name;

            [return: NotNullIfNotNull(nameof(source))]
            private static ChildDto? MapOwn(Child? source) => source is null ? null : new ChildDto { V = source.V };
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = nameof(Trim))]", "__d.Name = Trim(src.Name);")]
    [InlineData("[MapProperty(nameof(Dst.Name), nameof(Src.Nick), Converter = nameof(Trim), NullValue = \"none\")]", "__d.Name = src.Nick is not null ? Trim(src.Nick) : \"none\";")]
    [InlineData("[MapUsing(nameof(Dst.Using), nameof(Describe))]", "__d.Using = Describe(src);")]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = nameof(MapChild))]", "__d.Child = MapChild(src.Child);")]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = nameof(MapOwn))]", "__d.Child = MapOwn(src.Child);")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = nameof(MapChild))]", "__dst[__i] = MapChild(__src[__i]);")]
    public void ResultForValueIsTakenAsNotNull(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A value that may be null, and a method without the attribute
    [Theory]
    [InlineData("[MapNested(nameof(Dst.Child), nameof(Src.MaybeChild), Mapper = nameof(MapChild))]", "__d.Child = MapChild(src.MaybeChild)!;")]
    [InlineData("[MapCollection(nameof(Dst.Items), nameof(Src.MaybeItems), Mapper = nameof(MapChild))]", "__dst[__i] = MapChild(__src[__i])!;")]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = nameof(TrimPlain))]", "__d.Name = TrimPlain(src.Name)!;")]
    [InlineData("[MapUsing(nameof(Dst.Using), nameof(DescribePlain))]", "__d.Using = DescribePlain(src)!;")]
    public void ResultThatMayBeNullIsForgivenAndReported(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Equal("SMP0502", Assert.Single(problems)[..7]);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A converter whose parameter does not take null gets a nullable source after its check, a value, and a null one
    // leaves the target as it is
    [Fact]
    public void ConverterNotTakingNullGetsValue()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Name), nameof(Src.Nick), Converter = nameof(TrimValue))]"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Nick is not null)", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Name = TrimValue(src.Nick);", generated, StringComparison.Ordinal);
    }
}
