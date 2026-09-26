namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The implementation of a partial method has to repeat the parameter modifiers of its declaration
// (in, ref readonly, ref, scoped, params). The generator used to emit in for a readonly struct source
// whether or not it was declared, and no modifier on the other parameters, so a readonly struct source
// without in, a mutable struct source with in, or a custom parameter with in did not compile
// (CS0759 / CS8795).
public class ParameterModifierTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    // Errors anywhere, and warnings in the generated source (CS9192 for a ref readonly argument passed
    // without in, for example).
    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    // Extra is computed by the expression when one is given, and left alone otherwise.
    private static string Source(string declaration, string? expression)
    {
        var extra = expression is null ? "[MapIgnore(\"Extra\")]" : $"[MapExpression(\"Extra\", \"{expression}\")]";
        return $$"""
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public readonly struct ReadOnlySrc { public int Value { get; init; } }
            public struct MutableSrc { public int Value { get; set; } }
            public class Src { public int Value { get; set; } }
            public class Dst { public int Value { get; set; } public int Extra { get; set; } }
            public struct DstStruct { public int Value { get; set; } public int Extra { get; set; } }
            public struct Ctx { public int Offset { get; set; } }
            public static partial class M
            {
                [Mapper]
                {{extra}}
                {{declaration}}
            }
            """;
    }

    // Each case is compiled without and with a [MapExpression], whose local function takes the same
    // parameters (without this) and is called with the argument modifier each one needs: ref for ref,
    // in for in and ref readonly (CS9192 otherwise).
    [Theory]
    // Source: a readonly struct with and without in, a mutable struct with in, ref readonly, ref
    [InlineData("public static partial Dst Map(in ReadOnlySrc src);", "src.Value + 1", "Map(in global::Test.ReadOnlySrc src)", "__expression0(in global::Test.ReadOnlySrc src)", "__d.Extra = __expression0(in src);")]
    [InlineData("public static partial Dst Map(ReadOnlySrc src);", "src.Value + 1", "Map(global::Test.ReadOnlySrc src)", "__expression0(global::Test.ReadOnlySrc src)", "__d.Extra = __expression0(src);")]
    [InlineData("public static partial Dst Map(in MutableSrc src);", "src.Value + 1", "Map(in global::Test.MutableSrc src)", "__expression0(in global::Test.MutableSrc src)", "__d.Extra = __expression0(in src);")]
    [InlineData("public static partial Dst Map(ref readonly MutableSrc src);", "src.Value + 1", "Map(ref readonly global::Test.MutableSrc src)", "__expression0(ref readonly global::Test.MutableSrc src)", "__d.Extra = __expression0(in src);")]
    [InlineData("public static partial Dst Map(ref MutableSrc src);", "src.Value + 1", "Map(ref global::Test.MutableSrc src)", "__expression0(ref global::Test.MutableSrc src)", "__d.Extra = __expression0(ref src);")]
    // Extension mapper: this stays on the implementation only
    [InlineData("public static partial Dst ToDst(this ref MutableSrc src);", "src.Value + 1", "ToDst(this ref global::Test.MutableSrc src)", "__expression0(ref global::Test.MutableSrc src)", "__d.Extra = __expression0(ref src);")]
    // Custom parameter: in, ref readonly, scoped in, ref, params
    [InlineData("public static partial Dst Map(Src src, in Ctx ctx);", "src.Value + ctx.Offset", "Map(global::Test.Src src, in global::Test.Ctx ctx)", "__expression0(global::Test.Src src, in global::Test.Ctx ctx)", "__d.Extra = __expression0(src, in ctx);")]
    [InlineData("public static partial Dst Map(Src src, ref readonly Ctx ctx);", "src.Value + ctx.Offset", "Map(global::Test.Src src, ref readonly global::Test.Ctx ctx)", "__expression0(global::Test.Src src, ref readonly global::Test.Ctx ctx)", "__d.Extra = __expression0(src, in ctx);")]
    [InlineData("public static partial Dst Map(Src src, scoped in Ctx ctx);", "src.Value + ctx.Offset", "Map(global::Test.Src src, scoped in global::Test.Ctx ctx)", "__expression0(global::Test.Src src, scoped in global::Test.Ctx ctx)", "__d.Extra = __expression0(src, in ctx);")]
    [InlineData("public static partial Dst Map(Src src, ref Ctx ctx);", "src.Value + ctx.Offset", "Map(global::Test.Src src, ref global::Test.Ctx ctx)", "__expression0(global::Test.Src src, ref global::Test.Ctx ctx)", "__d.Extra = __expression0(src, ref ctx);")]
    [InlineData("public static partial Dst Map(Src src, params int[] offsets);", "src.Value + offsets.Length", "Map(global::Test.Src src, params int[] offsets)", "__expression0(global::Test.Src src, params int[] offsets)", "__d.Extra = __expression0(src, offsets);")]
    // Void destination: ref on a struct, in on a class (the members of the instance stay writable)
    [InlineData("public static partial void Map(Src src, ref DstStruct dst);", "src.Value + dst.Value", "Map(global::Test.Src src, ref global::Test.DstStruct dst)", "__expression0(global::Test.Src src, ref global::Test.DstStruct dst)", "dst.Extra = __expression0(src, ref dst);")]
    [InlineData("public static partial void Map(Src src, scoped ref DstStruct dst);", "src.Value + dst.Value", "Map(global::Test.Src src, scoped ref global::Test.DstStruct dst)", "__expression0(global::Test.Src src, scoped ref global::Test.DstStruct dst)", "dst.Extra = __expression0(src, ref dst);")]
    [InlineData("public static partial void Map(Src src, in Dst dst);", "src.Value + dst.Value", "Map(global::Test.Src src, in global::Test.Dst dst)", "__expression0(global::Test.Src src, in global::Test.Dst dst)", "dst.Extra = __expression0(src, in dst);")]
    public void DeclaredModifiersAreRepeated(string declaration, string expression, string implementation, string function, string call)
    {
        var plain = Source(declaration, null);
        AssertCompiles(plain);
        Assert.Contains(implementation, GeneratorTestHelper.GetGeneratedSource(plain), StringComparison.Ordinal);

        var withExpression = Source(declaration, expression);
        AssertCompiles(withExpression);
        var generated = GeneratorTestHelper.GetGeneratedSource(withExpression);
        Assert.Contains(implementation, generated, StringComparison.Ordinal);
        Assert.Contains("static int " + function + " => " + expression + ";", generated, StringComparison.Ordinal);
        Assert.Contains(call, generated, StringComparison.Ordinal);
    }

    // The generated code reads every parameter and assigns the destination members, which an out
    // parameter, or a struct destination passed as a readonly reference, does not allow.
    [Theory]
    [InlineData("public static partial Dst Map(out Src src);", "src", "out")]
    [InlineData("public static partial void Map(Src src, out Dst dst);", "dst", "out")]
    [InlineData("public static partial Dst Map(Src src, out Ctx ctx);", "ctx", "out")]
    [InlineData("public static partial void Map(Src src, in DstStruct dst);", "dst", "in")]
    [InlineData("public static partial void Map(Src src, ref readonly DstStruct dst);", "dst", "ref readonly")]
    public void Smp0005UnsupportedParameterModifierEmitsDiagnostic(string declaration, string parameterName, string modifier)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(declaration, null));

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "SMP0005");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains($"parameter=[{parameterName}]", message, StringComparison.Ordinal);
        Assert.Contains($"modifier=[{modifier}]", message, StringComparison.Ordinal);
    }

    // A readonly reference is fine for the source and custom parameters, which are only read, and for a
    // class destination, whose members stay writable.
    [Theory]
    [InlineData("public static partial Dst Map(in MutableSrc src);")]
    [InlineData("public static partial Dst Map(Src src, ref readonly Ctx ctx);")]
    [InlineData("public static partial void Map(Src src, in Dst dst);")]
    [InlineData("public static partial void Map(Src src, ref readonly Dst dst);")]
    public void Smp0005NotReportedForReadableParameters(string declaration)
    {
        var source = Source(declaration, null);

        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnostics(source), d => d.Id == "SMP0005");
        AssertCompiles(source);
    }
}
