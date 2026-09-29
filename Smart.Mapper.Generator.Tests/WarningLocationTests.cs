namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// The warnings are reported where they arise, as the errors are: SMP0501 at the mapper method, the location of
// SMP0308, and SMP0403 at the [MapExpression] attribute. They used to be reported without a location, so the error
// list could not go to them and a #pragma warning disable around the method did not suppress them.
public class WarningLocationTests
{
    private const string StrictSource =
        """
        using Smart.Mapper;
        namespace Test;
        public class Src { public int B { get; set; } }
        public class Dst { public int A { get; set; } public int B { get; set; } }
        public static partial class M
        {
            [Mapper(Strict = true)]
            public static partial Dst Map(Src src);
        }
        """;

    private const string ExpressionSource =
        """
        using Smart.Mapper;
        namespace Test;
        public class Src { public string Name { get; set; } = ""; }
        public class Dst { public string Name { get; set; } = ""; public string Kind { get; set; } = ""; }
        public static partial class M
        {
            [Mapper]
            [MapExpression(nameof(Dst.Kind), "System.Type.GetType(src.Name)!.Name")]
            public static partial Dst Map(Src src);
        }
        """;

    private const string SuppressedSource =
        """
        using Smart.Mapper;
        namespace Test;
        public class Src { public int B { get; set; } }
        public class Dst { public int A { get; set; } public int B { get; set; } }
        public static partial class M
        {
        #pragma warning disable SMP0501
            [Mapper(Strict = true)]
            public static partial Dst Map(Src src);
        #pragma warning restore SMP0501
        }
        """;

    private static int LineOf(string source, string text) =>
        Array.FindIndex(source.Split('\n'), line => line.Contains(text, StringComparison.Ordinal));

    [Fact]
    public void UnmappedPropertyIsReportedAtMethod()
    {
        var result = GeneratorTestHelper.Run(StrictSource);

        var diagnostic = Assert.Single(result.GeneratorDiagnostics, static d => d.Id == "SMP0501");
        Assert.True(diagnostic.Location.IsInSource);
        var method = diagnostic.Location.SourceTree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        Assert.Equal(method.Span, diagnostic.Location.SourceSpan);
        Assert.Equal(LineOf(StrictSource, "[Mapper(Strict = true)]"), diagnostic.Location.GetLineSpan().StartLinePosition.Line);
    }

    [Fact]
    public void ReflectionIsReportedAtAttribute()
    {
        var result = GeneratorTestHelper.Run(ExpressionSource);

        var diagnostic = Assert.Single(result.GeneratorDiagnostics, static d => d.Id == "SMP0403");
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Equal(LineOf(ExpressionSource, "[MapExpression("), diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.StartsWith("MapExpression(", diagnostic.Location.SourceTree.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan), StringComparison.Ordinal);
    }

    // The compiler suppresses a diagnostic whose location a #pragma warning disable naming it precedes, with no
    // restore in between; it checks the directives when it reports the diagnostics of the generators, after the
    // driver the tests run, so the directives are checked here the same way
    [Fact]
    public void PragmaCoversWarning()
    {
        var suppressed = Assert.Single(GeneratorTestHelper.Run(SuppressedSource).GeneratorDiagnostics, static d => d.Id == "SMP0501");
        var reported = Assert.Single(GeneratorTestHelper.Run(StrictSource).GeneratorDiagnostics, static d => d.Id == "SMP0501");

        Assert.True(IsDisabledByPragma(suppressed));
        Assert.False(IsDisabledByPragma(reported));
    }

    private static bool IsDisabledByPragma(Diagnostic diagnostic)
    {
        Assert.True(diagnostic.Location.IsInSource);

        var position = diagnostic.Location.SourceSpan.Start;
        var disabled = false;
        var directives = diagnostic.Location.SourceTree.GetRoot(TestContext.Current.CancellationToken)
            .DescendantTrivia()
            .Select(static trivia => trivia.GetStructure())
            .OfType<PragmaWarningDirectiveTriviaSyntax>()
            .TakeWhile(directive => directive.SpanStart < position);
        foreach (var directive in directives)
        {
            if (directive.ErrorCodes.Any(code => code.ToString() == diagnostic.Id))
            {
                disabled = directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword);
            }
        }

        return disabled;
    }
}
