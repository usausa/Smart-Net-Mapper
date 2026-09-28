namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A source property of a reference type whose getter may return null, as [MaybeNull] on the property or on the return of
// its getter says, is taken as a nullable one: it goes to a target not annotated as nullable with !, NullValue,
// NullBehavior.Skip and [MapCondition] apply to it, a converter or a mapper not taking null is called for a value only,
// the result of one taking null is not taken as not null, and Strict mode reports it (SMP0502). It used to be read as not
// null, which warned in the generated code (CS8601, CS8604).
public class MaybeNullSourceTests
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

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);", bool strict = false) =>
        $$"""
        #nullable enable
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        public class ChildDto { public int V { get; set; } }
        public class Base { public virtual string Plain { get; set; } = ""; [MaybeNull] public virtual string Other { get; set; } = ""; }
        public class Src : Base
        {
            [MaybeNull] public string Name { get; set; } = "";
            public string Getter { [return: MaybeNull] get; set; } = "";
            [MaybeNull] public Child Child { get; set; } = new();
            [MaybeNull] public Src Parent { get; set; }
            [MaybeNull] public override string Plain { get; set; } = "";
            public override string Other { get; set; } = "";
            [return: MaybeNull] public string Read() => null;
        }
        public class Dst { public string Name { get; set; } = ""; public string Getter { get; set; } = ""; public ChildDto Child { get; set; } = new(); public int Count { get; set; } public string Plain { get; set; } = ""; public string Other { get; set; } = ""; }
        public record Rec(string Name);
        public static partial class M
        {
            [Mapper(AutoMap = false, Strict = {{(strict ? "true" : "false")}})]
            {{attributes}}
            {{mapper}}

            [Mapper] public static partial ChildDto MapChild(Child source);
            [Mapper] public static partial ChildDto? MapChildOrNull(Child? source);

            private static string Upper(string value) => value.ToUpperInvariant();
            private static bool IsSet(string value) => value.Length > 0;

            [return: NotNullIfNotNull(nameof(value))]
            private static string? Trim(string? value) => value?.Trim();
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name))]", "__d.Name = src.Name!;")]
    [InlineData("[MapProperty(nameof(Dst.Getter))]", "__d.Getter = src.Getter!;")]
    [InlineData("[MapProperty(nameof(Dst.Name), NullValue = \"none\")]", "__d.Name = src.Name ?? \"none\";")]
    [InlineData("[MapProperty(nameof(Dst.Name), NullBehavior = NullBehavior.Skip)]", "if (src.Name is not null)")]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = nameof(Upper))]", "if (src.Name is not null)")]
    [InlineData("[MapProperty(nameof(Dst.Name))] [MapCondition(nameof(Dst.Name), nameof(IsSet))]", "if (src.Name is not null && IsSet(src.Name))")]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = nameof(Trim))]", "__d.Name = Trim(src.Name)!;")]
    [InlineData("[MapProperty(nameof(Dst.Count), nameof(Src.Name))]", "__d.Count = src.Name is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(src.Name) : default!;")]
    [InlineData("[MapProperty(nameof(Dst.Name), \"Parent.Name\")]", "if (src.Parent is not null)")]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = nameof(MapChild))]", "__d.Child = src.Child is not null ? MapChild(src.Child!) : default!;")]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = nameof(MapChildOrNull))]", "__d.Child = MapChildOrNull(src.Child)!;")]
    [InlineData("[MapFrom(nameof(Dst.Name), \"Parent.Name\")]", "__d.Name = src.Parent.Name!;")]
    [InlineData("[MapFrom(nameof(Dst.Name), nameof(Src.Read))]", "__d.Name = src.Read()!;")]
    [InlineData("[MapProperty(nameof(Dst.Plain))]", "__d.Plain = src.Plain!;")]
    public void MaybeNullSourceIsTakenAsNullable(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The attributes of the property the path binds to count: an override without [MaybeNull] is read as not null, as the
    // compiler reads it
    [Fact]
    public void OverrideWithoutAttributeIsNotNullable()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Other))]"));

        Assert.Empty(problems);
        Assert.Contains("__d.Other = src.Other;", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MaybeNullConstructorArgumentGetsDefault()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Rec.Name), Converter = nameof(Upper))]", "public static partial Rec Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains("new global::Test.Rec(src.Name is not null ? Upper(src.Name) : default!)", generated, StringComparison.Ordinal);
    }

    // Strict mode reports it as it does a nullable one, and not with what the mapping says the target gets
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name))]", true)]
    [InlineData("[MapProperty(nameof(Dst.Getter))]", true)]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = nameof(Trim))]", true)]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = nameof(MapChild))]", true)]
    [InlineData("[MapProperty(nameof(Dst.Name), NullValue = \"none\")]", false)]
    [InlineData("[MapProperty(nameof(Dst.Name), NullBehavior = NullBehavior.Skip)]", false)]
    [InlineData("[MapProperty(nameof(Dst.Other))]", false)]
    public void StrictReportsMaybeNullSource(string attributes, bool reported)
    {
        var warnings = GeneratorTestHelper.GetDiagnostics(Source(attributes, strict: true)).Where(static d => d.Id == "SMP0502").ToList();

        if (reported)
        {
            Assert.Contains("target=[", Assert.Single(warnings).GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }
        else
        {
            Assert.Empty(warnings);
        }
    }
}
