namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The implementation of a partial method has to repeat the nullable annotations of its declaration.
// The generator wrote the parameter and return types without them, so a declaration such as
// Map(Src src, Ctx? ctx) or Dst? Map(Src src) got CS8611 / CS8826, which WarningsAsErrors=nullable turns
// into errors.
public class NullableSignatureTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    // What WarningsAsErrors=nullable would reject: errors anywhere, and warnings in the generated source,
    // where CS8611 / CS8826 and any nullable warning of the generated body are reported.
    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static string Source(string declaration, string attributes = "", string members = "", string context = "#nullable enable") =>
        $$"""
        {{context}}
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Value { get; set; } }
        public class Dst { public int Value { get; set; } public string Text { get; set; } = ""; }
        public struct DstStruct { public int Value { get; set; } }
        public class Ctx { public int Offset { get; set; } }
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{declaration}}
            {{members}}
        }
        """;

    // Parameters, the return type, and annotations inside type arguments.
    [Theory]
    [InlineData("public static partial Dst Map(Src src, Ctx? ctx);", "Map(global::Test.Src src, global::Test.Ctx? ctx)")]
    [InlineData("public static partial Dst? Map(Src src);", "public static partial global::Test.Dst? Map(global::Test.Src src)")]
    [InlineData("public static partial Dst Map(Src? src);", "Map(global::Test.Src? src)")]
    [InlineData("public static partial void Map(Src src, Dst? dst);", "Map(global::Test.Src src, global::Test.Dst? dst)")]
    [InlineData("public static partial Dst Map(Src src, List<string?> tags);", "Map(global::Test.Src src, global::System.Collections.Generic.List<string?> tags)")]
    [InlineData("public static partial Dst? Map(Src? src, Dictionary<string, object?>? bag);", "public static partial global::Test.Dst? Map(global::Test.Src? src, global::System.Collections.Generic.Dictionary<string, object?>? bag)")]
    [InlineData("public static partial Dst ToDst(this Src? src);", "ToDst(this global::Test.Src? src)")]
    public void DeclaredAnnotationsAreRepeated(string declaration, string signature)
    {
        var source = Source(declaration);

        AssertCompiles(source);
        Assert.Contains(signature, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // The instance is created under the plain type name, and the nullable return type takes it as is.
    [Fact]
    public void NullableReturnTypeCreatesPlainType()
    {
        var source = Source("public static partial Dst? Map(Src src);");

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("var __d = new global::Test.Dst();", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("is null", generated, StringComparison.Ordinal);
    }

    // The generated code reads the source and writes the destination, so a nullable one is checked before
    // anything is mapped. Null maps nothing: a return-type mapper returns default (default! for a return
    // type that is not nullable, which keeps the generated code free of warnings), and a void mapper
    // returns without touching the destination.
    [Theory]
    [InlineData("public static partial Dst? Map(Src? src);", "if (src is null)", "return default;")]
    [InlineData("public static partial Dst Map(Src? src);", "if (src is null)", "return default!;")]
    [InlineData("public static partial DstStruct Map(Src? src);", "if (src is null)", "return default;")]
    [InlineData("public static partial void Map(Src? src, Dst dst);", "if (src is null)", "return;")]
    [InlineData("public static partial void Map(Src src, Dst? dst);", "if (dst is null)", "return;")]
    [InlineData("public static partial void Map(Src? src, Dst? dst);", "if (src is null || dst is null)", "return;")]
    public void NullSourceOrDestinationMapsNothing(string declaration, string check, string exit)
    {
        var source = Source(declaration);

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var checkIndex = generated.IndexOf(check, StringComparison.Ordinal);
        var exitIndex = generated.IndexOf(exit, checkIndex + 1, StringComparison.Ordinal);
        var readIndex = generated.IndexOf("= src.Value;", StringComparison.Ordinal);
        Assert.True((checkIndex >= 0) && (checkIndex < exitIndex) && (exitIndex < readIndex), generated);
    }

    // The check comes before BeforeMap and before the instance is created.
    [Fact]
    public void NullCheckComesBeforeMapping()
    {
        var source = Source(
            "public static partial Dst? Map(Src? src);",
            "[BeforeMap(nameof(Before))]",
            "private static void Before(Src s, Dst d) { }");

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var checkIndex = generated.IndexOf("if (src is null)", StringComparison.Ordinal);
        Assert.True(
            (checkIndex >= 0) &&
            (checkIndex < generated.IndexOf("var __d = new global::Test.Dst();", StringComparison.Ordinal)) &&
            (checkIndex < generated.IndexOf("Before(src, __d);", StringComparison.Ordinal)),
            generated);
    }

    [Fact]
    public void NullableCustomParameterIsNotChecked()
    {
        var source = Source("public static partial Dst Map(Src src, Ctx? ctx);");

        AssertCompiles(source);
        Assert.DoesNotContain("is null", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A local function takes a custom parameter with its annotation, so a nullable one is passed without a
    // warning. The source has been checked for null by then and is taken as not null, so an expression can
    // read it without one.
    [Fact]
    public void ExpressionFunctionTakesAnnotatedParameters()
    {
        var source = Source(
            "public static partial Dst Map(Src? src, Ctx? ctx);",
            "[MapExpression(nameof(Dst.Value), \"src.Value + (ctx?.Offset ?? 0)\")]");

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("static int __expression0(global::Test.Src src, global::Test.Ctx? ctx) =>", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Value = __expression0(src, ctx);", generated, StringComparison.Ordinal);
    }

    // A hook that takes the nullable custom parameter as nullable is called without a warning.
    [Fact]
    public void HookTakesNullableCustomParameter()
    {
        var source = Source(
            "public static partial Dst Map(Src? src, Ctx? ctx);",
            "[MapUsing(nameof(Dst.Text), nameof(Describe))] [AfterMap(nameof(After))]",
            "private static string Describe(Src s, Ctx? c) => s.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + c?.Offset; " +
            "private static void After(Src s, Dst d, Ctx? c) => d.Value += c?.Offset ?? 0;");

        AssertCompiles(source);
    }

    // Declared with nullable disabled, the types carry no annotation and the output stays as it was.
    [Fact]
    public void NullableDisabledDeclarationKeepsOutput()
    {
        var source = Source(
            "public static partial Dst Map(Src src, Ctx ctx);",
            "[MapExpression(nameof(Dst.Value), \"src.Value + ctx.Offset\")]",
            context: "#nullable disable");

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("public static partial global::Test.Dst Map(global::Test.Src src, global::Test.Ctx ctx)", generated, StringComparison.Ordinal);
        Assert.Contains("static int __expression0(global::Test.Src src, global::Test.Ctx ctx) =>", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("is null", generated, StringComparison.Ordinal);
    }
}
