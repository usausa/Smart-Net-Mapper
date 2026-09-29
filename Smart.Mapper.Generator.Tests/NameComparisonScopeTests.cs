namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The name comparison is the one of [Mapper], or else the one of [MapperProfile], or else Ordinal, and applies to
// the automatic mapping and to every member name written in an attribute: the targets (properties and fields,
// each segment of a dotted path), the sources, and the member of [MapFrom]. The profile's used to apply only to
// the automatic mapping, fields were matched exactly, and the member of [MapFrom] too. An exact match wins, so
// Ordinal behaves as before. The names of methods (converters, conditions, [MapUsing], callbacks, mappers) are C#
// identifiers, matched exactly.
public class NameComparisonScopeTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static string Source(string mapper, string attributes, string profile = "") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int Value { get; set; } public int Field; }
        public class Src
        {
            public int Value { get; set; }
            public int Other { get; set; }
            public Child Child { get; set; } = new();
            public int GetCount() => 1;
        }
        public class Dst
        {
            public int Target { get; set; }
            public int Field;
            public int Ignored { get; set; }
            public Child Holder { get; set; } = new();
            public Child HolderField = new();
        }
        {{profile}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);

            private static int Calc(Src src) => src.Other;
        }
        """;

    private const string Profile = "[MapperProfile(NameComparison = StringComparison.OrdinalIgnoreCase)]";

    // The profile's comparison applies to the names written in the attributes
    [Theory]
    [InlineData("[MapProperty(\"target\", \"value\")]", "__d.Target = src.Value;")]
    [InlineData("[MapProperty(\"holder.value\", \"child.value\")]", "__d.Holder.Value = src.Child!.Value;")]
    [InlineData("[MapConstant(\"field\", 1)]", "__d.Field = 1;")]
    [InlineData("[MapExpression(\"holderfield.field\", \"2\")]", "__d.HolderField.Field = __expression0(src);")]
    [InlineData("[MapUsing(\"FIELD\", nameof(Calc))]", "__d.Field = Calc(src);")]
    [InlineData("[MapFrom(\"target\", \"getcount\")]", "__d.Target = src.GetCount();")]
    [InlineData("[MapFrom(\"target\", \"child.value\")]", "__d.Target = src.Child.Value;")]
    public void ProfileComparisonAppliesToAttributeNames(string attribute, string expected)
    {
        var source = Source("[Mapper(AutoMap = false)]", attribute, Profile);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // [MapIgnore] and [MapCondition] resolve their targets the same way
    [Fact]
    public void ProfileComparisonAppliesToIgnoreAndCondition()
    {
        var source = Source(
            "[Mapper]",
            "[MapIgnore(\"ignored\")] [MapProperty(\"target\", \"other\")] [MapCondition(\"TARGET\", nameof(IsPositive))]",
            Profile).Replace("private static int Calc", "private static bool IsPositive(int value) => value > 0;\n    private static int Calc", StringComparison.Ordinal);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("if (IsPositive(src.Other))", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Ignored", generated, StringComparison.Ordinal);
    }

    // The method's comparison wins over the profile's
    [Fact]
    public void MethodComparisonWinsOverProfile()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source("[Mapper(AutoMap = false, NameComparison = StringComparison.Ordinal)]", "[MapProperty(\"target\", nameof(Src.Value))]", Profile));

        Assert.Equal("SMP0102", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
    }

    // Fields are matched ignoring case under the method's comparison, and Ordinal matches them exactly
    [Theory]
    [InlineData("[Mapper(AutoMap = false, NameComparison = StringComparison.OrdinalIgnoreCase)]", "[MapConstant(\"field\", 1)]", "__d.Field = 1;")]
    [InlineData("[Mapper(AutoMap = false, NameComparison = StringComparison.OrdinalIgnoreCase)]", "[MapConstant(\"HOLDERFIELD.FIELD\", 1)]", "__d.HolderField.Field = 1;")]
    [InlineData("[Mapper(AutoMap = false)]", "[MapConstant(nameof(Dst.Field), 1)]", "__d.Field = 1;")]
    public void FieldTargetFollowsComparison(string mapper, string attribute, string expected)
    {
        var source = Source(mapper, attribute);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapConstant(\"field\", 1)]")]
    [InlineData("[MapFrom(nameof(Dst.Target), \"getcount\")]")]
    public void OrdinalDoesNotMatchOtherCasing(string attribute)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source("[Mapper(AutoMap = false)]", attribute));

        Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
    }

    // An exact match wins over one ignoring case
    [Fact]
    public void ExactMatchWins()
    {
        var source = """
            using System;
            using Smart.Mapper;
            namespace Test;
            public class Src { public int X { get; set; } }
            public class Dst { public int value; public int Value { get; set; } }
            public static partial class M
            {
                [Mapper(AutoMap = false, NameComparison = StringComparison.OrdinalIgnoreCase)]
                [MapConstant("value", 1)]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        Assert.Contains("__d.value = 1;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // The names of methods are C# identifiers and stay exact
    [Fact]
    public void MethodNamesStayExact()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source("[Mapper(AutoMap = false)]", "[MapUsing(nameof(Dst.Target), \"calc\")]", Profile));

        Assert.Equal("SMP0201", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
    }
}
