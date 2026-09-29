namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The target of [MapConstant], [MapExpression] and [MapUsing] has to be a property or field the mapper class
// can assign, as __d.<target> binds it. One that is not found (a misspelled or differently cased name, a
// method, a static member, a missing segment of a dotted path) or that cannot be assigned (a readonly field)
// is reported (SMP0102) instead of failing in the generated code (CS1061 / CS1656 / CS0176 / CS0191).
public class MissingTargetTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static void AssertDiagnostic(string source, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    private static string Source(string attributes, bool returns = true, string mapper = "[Mapper(AutoMap = false)]") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int Value { get; set; } }
        public class Src { public int Number { get; set; } }
        public class Dst
        {
            public int Number { get; set; }
            public Child Child { get; set; } = new();
            public int Field;
            public readonly int Fixed;
            public const int Constant = 1;
            public static int Shared { get; set; }
            internal int Internal { get; set; }
        }
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
            private static int Calc(Src src) => src.Number;
        }
        """;

    [Theory]
    [InlineData("[MapConstant(\"Missing\", 1)]", true)]
    [InlineData("[MapConstant(\"Missing\", 1)]", false)]
    [InlineData("[MapExpression(\"Missing\", \"1\")]", true)]
    [InlineData("[MapExpression(\"Missing\", \"1\")]", false)]
    [InlineData("[MapUsing(\"Missing\", nameof(Calc))]", true)]
    [InlineData("[MapUsing(\"Missing\", nameof(Calc))]", false)]
    [InlineData("[MapConstant(\"number\", 1)]", true)]
    [InlineData("[MapConstant(\"\", 1)]", true)]
    [InlineData("[MapConstant(\"ToString\", 1)]", true)]
    [InlineData("[MapConstant(nameof(Dst.Shared), 1)]", true)]
    [InlineData("[MapConstant(nameof(Dst.Constant), 1)]", true)]
    [InlineData("[MapConstant(nameof(Dst.Fixed), 1)]", true)]
    [InlineData("[MapConstant(\"Child.Missing\", 1)]", true)]
    [InlineData("[MapExpression(\"Missing.Value\", \"1\")]", true)]
    [InlineData("[MapUsing(\"Child.Missing\", nameof(Calc))]", false)]
    public void TargetThatIsNotFoundEmitsDiagnostic(string attribute, bool returns)
    {
        AssertDiagnostic(Source(attribute, returns), "SMP0102");
    }

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Field), 2)]", "__d.Field = 2;")]
    [InlineData("[MapConstant(nameof(Dst.Internal), 2)]", "__d.Internal = 2;")]
    [InlineData("[MapConstant(\"Child.Value\", 2)]", "__d.Child.Value = 2;")]
    [InlineData("[MapExpression(\"Child.Value\", \"src.Number + 1\")]", "__d.Child.Value = ")]
    [InlineData("[MapUsing(\"Child.Value\", nameof(Calc))]", "__d.Child.Value = Calc(src);")]
    public void TargetThatExistsCompiles(string attribute, string expected)
    {
        var source = Source(attribute);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A name spelled with other casing is found when the mapper compares names ignoring case
    [Fact]
    public void TargetMatchedIgnoringCaseCompiles()
    {
        var source = Source("[MapConstant(\"number\", 1)]", mapper: "[Mapper(AutoMap = false, NameComparison = StringComparison.OrdinalIgnoreCase)]");

        AssertCompiles(source);
        Assert.Contains("__d.Number = 1;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A constant is checked against the type of the field or the end of the dotted path it is assigned to
    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Field), \"x\")]")]
    [InlineData("[MapConstant(\"Child.Value\", \"x\")]")]
    public void ConstantForFieldOrPathIsChecked(string attribute)
    {
        AssertDiagnostic(Source(attribute), "SMP0216");
    }
}
