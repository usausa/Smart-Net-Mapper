namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A nullable source is not passed to the converter or the condition whose parameter does not take null (a reference
// annotated as not null, or [DisallowNull]). The converter is called for a value only: in an assignment, a null source
// takes NullValue, or leaves the target as it is without one, and a constructor argument or an object initializer
// entry takes NullValue, null for a target taking it, or default. The condition is not met by a null source. A method
// whose parameter takes null (annotated as nullable, with [AllowNull], or declared with nullable annotations disabled)
// is called as before. A nullable reference that [MapUsing] or the converter returns into a target not annotated as
// nullable is taken with !, as [MapFrom] takes it. The null used to be passed on, which warned in the generated code
// (CS8604 / CS8601) and threw in the method.
public class NonNullParameterTests
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

    private static string Lines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Source(string attributes, string mapper = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Child { public string? Name { get; set; } }
        public class Src { public string? Name { get; set; } public string? Code { get; set; } public Child? Child { get; set; } public string Plain { get; set; } = ""; }
        public class Dst { public string Name { get; set; } = ""; public string? Note { get; set; } public string Code { get; set; } = ""; public string ChildName { get; set; } = ""; }
        public record DstRecord(string Name, string? Note);
        public class InitDst { public required string Name { get; init; } public string? Note { get; init; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static string Trim(string value) => value.Trim();
            private static string? TrimOrNull(string? value) => value?.Trim();
            private static string TrimAllowed([AllowNull] string value) => value ?? "";
            private static bool IsShort(string value) => value.Length < 5;
            private static bool IsWanted(string? value) => value is not null;
            private static string? Find(Src source) => source.Code;
            private static string? Label(string value) => value;
        #nullable disable
            private static string TrimOblivious(string value) => value;
        #nullable enable
        }
        """;

    // In an assignment, a null source leaves the target as it is, or takes NullValue
    [Fact]
    public void ConverterIsCalledForValueOnly()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Name), Converter = nameof(Trim))] " +
            "[MapProperty(nameof(Dst.Note), nameof(Src.Name), Converter = nameof(Trim), NullValue = \"none\")] " +
            "[MapProperty(nameof(Dst.ChildName), \"Child.Name\", Converter = nameof(Trim))]"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    if (src.Name is not null)
                    {
                        dst.Name = Trim(src.Name);
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains("dst.Note = src.Name is not null ? Trim(src.Name) : \"none\";", generated, StringComparison.Ordinal);
        Assert.Contains(
            Lines("""
                    if (src.Child is not null)
                    {
                        if (src.Child.Name is not null)
                        {
                            dst.ChildName = Trim(src.Child.Name);
                        }
                    }
            """),
            generated,
            StringComparison.Ordinal);
    }

    // A constructor argument and an object initializer entry take null for a target taking it, or default
    [Fact]
    public void ExpressionsTakeNullOrDefault()
    {
        var (record, recordProblems) = Build(Source(
            "[MapProperty(nameof(DstRecord.Name), Converter = nameof(Trim))] [MapProperty(nameof(DstRecord.Note), nameof(Src.Code), Converter = nameof(Trim))]",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(recordProblems);
        Assert.Contains("new global::Test.DstRecord(src.Name is not null ? Trim(src.Name) : default!, src.Code is not null ? Trim(src.Code) : null)", record, StringComparison.Ordinal);

        var (init, initProblems) = Build(Source(
            "[MapProperty(nameof(InitDst.Name), Converter = nameof(Trim), NullValue = \"none\")] [MapProperty(nameof(InitDst.Note), nameof(Src.Code), Converter = nameof(Trim))]",
            "public static partial InitDst Map(Src src);"));

        Assert.Empty(initProblems);
        Assert.Contains("Name = src.Name is not null ? Trim(src.Name) : \"none\",", init, StringComparison.Ordinal);
        Assert.Contains("Note = src.Code is not null ? Trim(src.Code) : null,", init, StringComparison.Ordinal);
    }

    // A method whose parameter takes null, and a source that cannot be null, are passed as before
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Note), nameof(Src.Name), Converter = nameof(TrimOrNull))]", "dst.Note = TrimOrNull(src.Name);")]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = nameof(TrimAllowed))]", "dst.Name = TrimAllowed(src.Name);")]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = nameof(TrimOblivious))]", "dst.Name = TrimOblivious(src.Name);")]
    [InlineData("[MapProperty(nameof(Dst.Name), nameof(Src.Plain), Converter = nameof(Trim))]", "dst.Name = Trim(src.Plain);")]
    [InlineData("[MapCondition(nameof(Dst.Note), nameof(IsWanted))] [MapProperty(nameof(Dst.Note), nameof(Src.Code))]", "if (IsWanted(src.Code))")]
    public void MethodTakingNullIsCalledAsBefore(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A condition whose parameter does not take null is not met by a null source
    [Fact]
    public void ConditionIsNotMetByNull()
    {
        var (generated, problems) = Build(Source("[MapCondition(nameof(Dst.Code), nameof(IsShort))] [MapProperty(nameof(Dst.Code))]"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Code is not null && IsShort(src.Code))", generated, StringComparison.Ordinal);
    }

    // A nullable reference that [MapUsing] or the converter returns into a target not annotated as nullable is taken with !
    [Theory]
    [InlineData("[MapUsing(nameof(Dst.Code), nameof(Find))]", "dst.Code = Find(src)!;")]
    [InlineData("[MapUsing(nameof(Dst.Note), nameof(Find))]", "dst.Note = Find(src);")]
    [InlineData("[MapProperty(nameof(Dst.Code), nameof(Src.Plain), Converter = nameof(Label))]", "dst.Code = Label(src.Plain)!;")]
    [InlineData("[MapProperty(nameof(Dst.Note), nameof(Src.Plain), Converter = nameof(Label))]", "dst.Note = Label(src.Plain);")]
    public void NullableResultIsForgiven(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }
}
