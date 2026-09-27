namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A mapper method in a nested type is implemented in the partial declarations of its containing types, outermost
// first, each declared with its kind (class, struct, record, record struct) and type parameters; the method and the
// fields it uses go into the innermost. It used to be generated at the namespace level (CS0759). The generated file
// is named after the whole chain, so that A.Inner and B.Inner get files of their own. A containing type that is not
// partial could not take the declaration (CS0260), so it is reported (SMP0001).
public class NestedContainingTypeTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && (IsGenerated(d) || d.Id is "CS8784" or "CS8785")) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    // The generated code with the indentation removed, one line each
    private static string Lines(string source) =>
        String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string types) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public int X { get; set; } }
        public class Dst { public int X { get; set; } public string Text { get; set; } = ""; }
        {{types}}
        """;

    [Fact]
    public void MethodIsImplementedInContainingTypes()
    {
        var source = Source("""
            public static partial class Outer
            {
                public static partial class Inner
                {
                    [Mapper]
                    public static partial Dst Map(Src src);
                }
            }
            """);

        AssertCompiles(source);
        var lines = Lines(source);
        Assert.Contains("namespace Test;\npartial class Outer\n{\npartial class Inner\n{\n", lines, StringComparison.Ordinal);
        Assert.Contains("public static partial global::Test.Dst Map(global::Test.Src src)", lines, StringComparison.Ordinal);
        Assert.EndsWith("return __d;\n}\n}\n}", lines, StringComparison.Ordinal);
    }

    // The kinds and type parameters of the declarations, and the culture field in the innermost type
    [Fact]
    public void DeclarationsRepeatKindsAndTypeParameters()
    {
        var source = Source("""
            public partial class Outer<T>
            {
                public partial record Middle<U>
                {
                    public partial struct Inner
                    {
                        [Mapper(Culture = "ja-JP")]
                        [MapProperty(nameof(Dst.Text), nameof(Src.X))]
                        public static partial Dst Map(Src src);
                    }
                }
            }
            """);

        AssertCompiles(source);
        Assert.Contains(
            "partial class Outer<T>\n{\npartial record Middle<U>\n{\npartial struct Inner\n{\nprivate static readonly global::System.Globalization.CultureInfo __culture_ja_JP",
            Lines(source),
            StringComparison.Ordinal);
    }

    // A record struct, and a record, at the top level as well
    [Theory]
    [InlineData("public partial record struct Mappers { [Mapper] public static partial Dst Map(Src src); }", "partial record struct Mappers")]
    [InlineData("public partial record Mappers { [Mapper] public static partial Dst Map(Src src); }", "partial record Mappers")]
    [InlineData("public partial struct Outer { public static partial class Inner { [Mapper] public static partial Dst Map(Src src); } }", "partial struct Outer\n{\npartial class Inner")]
    public void DeclarationKindsMatch(string types, string expected)
    {
        var source = Source(types);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // Two nested types of one name get files and declarations of their own
    [Fact]
    public void SameNameUnderDifferentTypesDoesNotCollide()
    {
        var source = Source("""
            public static partial class First { public static partial class Inner { [Mapper] public static partial Dst Map(Src src); } }
            public static partial class Second { public static partial class Inner { [Mapper] public static partial Dst Map(Src src); } }
            """);

        AssertCompiles(source);
        var names = GeneratorTestHelper.Run(source).GeneratedSources.Keys.Select(static k => System.IO.Path.GetFileName(k)).ToList();
        Assert.Contains("Test_First_Inner.g.cs", names);
        Assert.Contains("Test_Second_Inner.g.cs", names);
    }

    [Theory]
    [InlineData("public static class Outer { public static partial class Inner { [Mapper] public static partial Dst Map(Src src); } }")]
    [InlineData("public static class Outer { public static partial class Middle { public static partial class Inner { [Mapper] public static partial Dst Map(Src src); } } }")]
    public void ContainingTypeNotPartialEmitsDiagnostic(string types)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(types));

        Assert.Equal("SMP0001", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }
}
