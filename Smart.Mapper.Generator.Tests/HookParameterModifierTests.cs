namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Methods the generated code calls ([MapUsing], converters, [MapCondition], BeforeMap / AfterMap) get each
// argument the way their parameter takes it: ref for ref, in for in and ref readonly, as is otherwise.
// The generator passed every argument as is, so a ref parameter failed with CS1620, and a ref readonly one
// warned with CS9192. A modifier that cannot take its argument (out, or ref for a read-only variable or a
// property value) makes the method a mismatch, reported with the diagnostic of its signature.
public class HookParameterModifierTests
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

    private static string Source(string attributes, string declaration, string members, string types = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Value { get; set; } public string Name { get; set; } = ""; }
        public readonly struct ReadOnlySrc { public int Value { get; init; } }
        public class Dst { public int Value { get; set; } public string Name { get; set; } = ""; }
        public class InitDst { public int Value { get; set; } public string Name { get; init; } = ""; }
        public struct DstStruct { public int Value { get; set; } }
        public class Ctx { public int Offset { get; set; } }
        {{types}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            {{declaration}}
            {{members}}
        }
        """;

    private const string Text = "System.Globalization.CultureInfo.InvariantCulture";

    [Theory]
    // BeforeMap / AfterMap: a struct destination by ref, for the instance of a return-type mapper and for
    // a destination parameter passed by ref; in on class arguments; ref readonly on a custom parameter
    [InlineData("[AfterMap(nameof(After))]", "public static partial DstStruct Map(Src src);", "private static void After(Src s, ref DstStruct d) => d.Value += 1;", "After(src, ref __d);")]
    [InlineData("[AfterMap(nameof(After))]", "public static partial void Map(Src src, ref DstStruct dst);", "private static void After(Src s, ref DstStruct d) => d.Value += 1;", "After(src, ref dst);")]
    [InlineData("[BeforeMap(nameof(Before))]", "public static partial Dst Map(Src src);", "private static void Before(in Src s, in Dst d) { }", "Before(in src, in __d);")]
    [InlineData("[AfterMap(nameof(After))]", "public static partial Dst Map(Src src, Ctx ctx);", "private static void After(Src s, Dst d, ref readonly Ctx c) => d.Value += c.Offset;", "After(src, __d, in ctx);")]
    // MapUsing: in for a readonly struct source, ref readonly and ref, and in an object initializer
    [InlineData("[MapUsing(nameof(Dst.Name), nameof(Describe))]", "public static partial Dst Map(in ReadOnlySrc src);", "private static string Describe(in ReadOnlySrc s) => s.Value.ToString(" + Text + ");", "__d.Name = Describe(in src);")]
    [InlineData("[MapUsing(nameof(Dst.Name), nameof(Describe))]", "public static partial Dst Map(Src src, Ctx ctx);", "private static string Describe(ref readonly Src s, ref Ctx c) => s.Name + c.Offset;", "__d.Name = Describe(in src, ref ctx);")]
    [InlineData("[MapUsing(nameof(InitDst.Name), nameof(Describe))]", "public static partial InitDst Map(Src src, Ctx ctx);", "private static string Describe(in Src s, in Ctx c) => s.Name + c.Offset;", "Name = Describe(in src, in ctx),")]
    [InlineData("[MapUsing(nameof(Dst.Name), nameof(Describe))]", "public static partial Dst Map(ref Src src);", "private static string Describe(ref Src s) => s.Name;", "__d.Name = Describe(ref src);")]
    // Converter and condition: the property value goes as is (a copy for in), the custom parameter by reference
    [InlineData("[MapProperty(nameof(Dst.Name), nameof(Src.Value), Converter = nameof(Format))]", "public static partial Dst Map(Src src, Ctx ctx);", "private static string Format(in int v, in Ctx c) => (v + c.Offset).ToString(" + Text + ");", "__d.Name = Format(src.Value, in ctx);")]
    [InlineData("[MapCondition(nameof(Dst.Name), nameof(HasName))]", "public static partial Dst Map(Src src, Ctx ctx);", "private static bool HasName(in string v, ref readonly Ctx c) => v.Length > c.Offset;", "if (HasName(src.Name, in ctx))")]
    public void ArgumentsFollowHookModifiers(string attributes, string declaration, string members, string call)
    {
        var source = Source(attributes, declaration, members);

        AssertCompiles(source);
        Assert.Contains(call, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A converter class method taking the value by in gets it as is; the compiler passes a copy.
    [Fact]
    public void ValueConverterMethodTakesValueByIn()
    {
        var source = Source(
            "[ValueConverter(typeof(Conv))] [MapProperty(nameof(Dst.Name), nameof(Src.Value))]",
            "public static partial Dst Map(Src src);",
            string.Empty,
            "public static class Conv { public static string ConvertToString(in int value) => value.ToString(" + Text + "); " +
            "public static TDest Convert<TSrc, TDest>(TSrc source) => throw new System.NotSupportedException(); }");

        AssertCompiles(source);
        Assert.Contains("__d.Name = global::Test.Conv.ConvertToString(src.Value);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // Methods without modifiers keep their calls.
    [Fact]
    public void HooksWithoutModifiersKeepCalls()
    {
        var source = Source(
            "[BeforeMap(nameof(Before))] [AfterMap(nameof(After))] [MapUsing(nameof(Dst.Name), nameof(Describe))]",
            "public static partial Dst Map(Src src, Ctx ctx);",
            "private static void Before(Src s, Dst d, Ctx c) { } private static void After(Src s, Dst d, Ctx c) { } " +
            "private static string Describe(Src s, Ctx c) => s.Name;");

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("Before(src, __d, ctx);", generated, StringComparison.Ordinal);
        Assert.Contains("After(src, __d, ctx);", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Name = Describe(src, ctx);", generated, StringComparison.Ordinal);
    }

    [Theory]
    // out never takes an argument
    [InlineData("[AfterMap(nameof(After))]", "public static partial Dst Map(Src src);", "private static void After(Src s, out Dst d) { d = new Dst(); }", "", "SMP0103")]
    [InlineData("[BeforeMap(nameof(Before))]", "public static partial Dst Map(Src src, Ctx ctx);", "private static void Before(Src s, Dst d, out Ctx c) { c = new Ctx(); }", "", "SMP0102")]
    [InlineData("[MapUsing(nameof(Dst.Name), nameof(Describe))]", "public static partial Dst Map(Src src);", "private static string Describe(out Src s) { s = new Src(); return \"\"; }", "", "SMP0201")]
    // ref cannot take a read-only variable (an in or ref readonly parameter of the mapper)
    [InlineData("[MapUsing(nameof(Dst.Name), nameof(Describe))]", "public static partial Dst Map(in ReadOnlySrc src);", "private static string Describe(ref ReadOnlySrc s) => \"\";", "", "SMP0201")]
    [InlineData("[AfterMap(nameof(After))]", "public static partial void Map(Src src, in Dst dst);", "private static void After(Src s, ref Dst d) { }", "", "SMP0103")]
    [InlineData("[MapCondition(nameof(Dst.Name), nameof(HasName))]", "public static partial Dst Map(Src src, in Ctx ctx);", "private static bool HasName(string v, ref Ctx c) => true;", "", "SMP0106")]
    // A property value cannot go by ref, and a ref readonly parameter wants a variable (CS9193)
    [InlineData("[MapProperty(nameof(Dst.Name), nameof(Src.Value), Converter = nameof(Format))]", "public static partial Dst Map(Src src);", "private static string Format(ref int v) => \"\";", "", "SMP0104")]
    [InlineData("[MapProperty(nameof(Dst.Name), nameof(Src.Value), Converter = nameof(Format))]", "public static partial Dst Map(Src src);", "private static string Format(ref readonly int v) => \"\";", "", "SMP0104")]
    [InlineData("[MapCondition(nameof(Dst.Name), nameof(HasName))]", "public static partial Dst Map(Src src);", "private static bool HasName(ref string v) => true;", "", "SMP0106")]
    // Converter class methods get the value too
    [InlineData("[ValueConverter(typeof(Conv))] [MapProperty(nameof(Dst.Name), nameof(Src.Value))]", "public static partial Dst Map(Src src);", "", "public static class Conv { public static string ConvertToString(ref int value) => \"\"; public static TDest Convert<TSrc, TDest>(TSrc source) => throw new System.NotSupportedException(); }", "SMP0104")]
    [InlineData("[ValueConverter(typeof(Conv))] [MapProperty(nameof(Dst.Name), nameof(Src.Value))]", "public static partial Dst Map(Src src);", "", "public static class Conv { public static TDest Convert<TSrc, TDest>(ref TSrc source) => throw new System.NotSupportedException(); }", "SMP0104")]
    public void ModifierThatCannotTakeArgumentEmitsSignatureDiagnostic(string attributes, string declaration, string members, string types, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source(attributes, declaration, members, types));

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    // With an overload that can take the arguments, that one is used, and one taking every argument by
    // value is preferred, as the plain call always chose it.
    [Theory]
    [InlineData("public static partial Dst Map(in ReadOnlySrc src);")]
    [InlineData("public static partial Dst Map(ReadOnlySrc src);")]
    public void OverloadTakingArgumentsByValueIsUsed(string declaration)
    {
        var source = Source(
            "[MapUsing(nameof(Dst.Name), nameof(Describe))]",
            declaration,
            "private static string Describe(ref ReadOnlySrc s) => \"ref\"; " +
            "private static string Describe(ReadOnlySrc s) => s.Value.ToString(" + Text + ");");

        AssertCompiles(source);
        Assert.Contains("__d.Name = Describe(src);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
