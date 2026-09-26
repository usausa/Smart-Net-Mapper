namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Every name the generated code declares starts with __ (the __d instance of a return-type mapper,
// __src, __expression0, ...). The instance used to be named destination, so a parameter of that name
// collided with it (CS0136). Parameter names starting with __ are rejected instead (SMP0004).
public class ReservedNameTests
{
    private static void AssertCompiles(string source)
    {
        var errors = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(errors.Count == 0, String.Join("\n", errors));
    }

    [Fact]
    public void SourceParameterNamedDestinationCompiles()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst { public int Value { get; set; } }
            public static partial class M
            {
                [Mapper]
                public static partial Dst Map(Src destination);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("var __d = new global::Test.Dst();", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Value = destination.Value;", generated, StringComparison.Ordinal);
        Assert.Contains("return __d;", generated, StringComparison.Ordinal);
    }

    // The instance is handed to callbacks as __d, while the custom parameter keeps its own name.
    [Fact]
    public void CustomParameterNamedDestinationCompiles()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst { public int Value { get; set; } public string Text { get; set; } = ""; public bool Done { get; set; } }
            public class Ctx { public string Prefix { get; set; } = ""; }
            public static partial class M
            {
                [Mapper]
                [MapUsing(nameof(Dst.Text), nameof(BuildText))]
                [AfterMap(nameof(After))]
                public static partial Dst Map(Src src, Ctx destination);
                private static string BuildText(Src src, Ctx ctx) => ctx.Prefix + src.Value;
                private static void After(Src src, Dst dst, Ctx ctx) => dst.Done = true;
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Text = BuildText(src, destination);", generated, StringComparison.Ordinal);
        Assert.Contains("After(src, __d, destination);", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public static partial Dst Map(Src __src);", "__src")]
    [InlineData("public static partial Dst Map(Src __d);", "__d")]
    [InlineData("public static partial void Map(Src src, Dst __dst);", "__dst")]
    [InlineData("public static partial Dst Map(Src src, Ctx __ctx);", "__ctx")]
    [InlineData("public static partial void Map(Src src, Dst dst, Ctx __ctx);", "__ctx")]
    public void Smp0004ReservedParameterNameEmitsDiagnostic(string declaration, string parameterName)
    {
        var source = $$"""
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst { public int Value { get; set; } }
            public class Ctx { }
            public static partial class M
            {
                [Mapper]
                {{declaration}}
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SMP0004");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains($"parameter=[{parameterName}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // Only the leading double underscore is reserved.
    [Theory]
    [InlineData("_src")]
    [InlineData("src__")]
    [InlineData("s__rc")]
    public void ParameterNameNotStartingWithDoubleUnderscoreCompiles(string parameterName)
    {
        var source = $$"""
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst { public int Value { get; set; } }
            public static partial class M
            {
                [Mapper]
                public static partial Dst Map(Src {{parameterName}});
            }
            """;

        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnostics(source), d => d.Id == "SMP0004");
        AssertCompiles(source);
    }
}
