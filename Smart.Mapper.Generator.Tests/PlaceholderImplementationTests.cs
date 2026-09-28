namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A mapper reported with an error gets an implementation throwing NotImplementedException in place of none, so that
// only the error is reported, not the implementation missing along with it (CS8795). It repeats the declaration as the
// generated code does: the containing types, the modifiers (none for a declaration without an accessibility modifier,
// which one writing private would not match, CS8799), the nullable annotations and the type parameters with their
// constraints. The build fails on the error, so the body never runs. A mapper the definition check reports (SMP0001),
// which the declaration could not repeat, gets none.
public class PlaceholderImplementationTests
{
    private static List<string> Errors(string source) =>
        GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Select(static d => d.Id)
            .ToList();

    private static string Source(string declarations) =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Value { get; set; } }
        public class Dst { public int Value { get; set; } }
        public static partial class Outer<TOuter>
        {
            internal static partial class M
            {
        {{declarations}}
            }
        }
        """;

    [Theory]
    [InlineData("[Mapper(AutoMap = false)] [MapProperty(\"Missing\", nameof(Src.Value))] public static partial Dst Map(Src source);", "SMP0214", "public static partial global::Test.Dst Map(global::Test.Src source)")]
    [InlineData("[Mapper] public static partial void Map(Src source);", "SMP0002", "public static partial void Map(global::Test.Src source)")]
    [InlineData("[Mapper] public static partial Dst Map(Src source, out int count);", "SMP0005", "public static partial global::Test.Dst Map(global::Test.Src source, out int count)")]
    [InlineData("[Mapper] public static partial Dst? Map(Src? source, int __value);", "SMP0004", "public static partial global::Test.Dst? Map(global::Test.Src? source, int __value)")]
    [InlineData("[Mapper] [BeforeMap(\"Missing\")] internal static partial TDst Map<TDst>(Src source, params int[] values) where TDst : class, new();", "SMP0102", "internal static partial TDst Map<TDst>(global::Test.Src source, params int[] values) where TDst : class, new()")]
    [InlineData("[Mapper] public static partial ref Dst Map(Src source);", "SMP0008", "public static partial ref global::Test.Dst Map(global::Test.Src source)")]
    [InlineData("[Mapper] public static partial ref readonly Dst Map(Src source);", "SMP0008", "public static partial ref readonly global::Test.Dst Map(global::Test.Src source)")]
    [InlineData("[Mapper] public static partial List<Dst> Map(List<Src> source);", "SMP0007", "public static partial global::System.Collections.Generic.List<global::Test.Dst> Map(global::System.Collections.Generic.List<global::Test.Src> source)")]
    public void MapperWithErrorGetsPlaceholder(string declaration, string error, string signature)
    {
        var source = Source(declaration);

        Assert.Equal([error], Errors(source));
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("partial class Outer<TOuter>", generated, StringComparison.Ordinal);
        Assert.Contains(signature, generated, StringComparison.Ordinal);
        Assert.Contains("throw new global::System.NotImplementedException(\"Map is not generated, as Smart.Mapper reported an error for it.\");", generated, StringComparison.Ordinal);
    }

    // The other mappers of the class are generated as they are
    [Fact]
    public void OtherMappersAreGenerated()
    {
        var source = Source(
            "[Mapper(AutoMap = false)] [MapProperty(\"Missing\", nameof(Src.Value))] public static partial Dst Map(Src source);\n" +
            "[Mapper] public static partial Dst MapOther(Src source);");

        Assert.Equal(["SMP0214"], Errors(source));
        Assert.Contains("__d.Value = source.Value;", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Fact]
    public void DefinitionReportedGetsNone()
    {
        var source = Source("[Mapper] public static Dst Map(Src source) => new();\n[Mapper] public static partial Dst MapOther(Src source);");

        Assert.Contains("SMP0001", Errors(source));
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains(" MapOther(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(" Map(", generated, StringComparison.Ordinal);
    }

    // A declaration without an accessibility modifier gets one without it as well, so that only the error is reported
    [Fact]
    public void DeclarationWithoutAccessibilityGetsPlaceholderWithoutIt()
    {
        var source = Source("[Mapper] static partial void Map(Src source);");

        Assert.Equal(["SMP0002"], Errors(source));
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("\n        static partial void Map(global::Test.Src source)", generated.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("throw new global::System.NotImplementedException(", generated, StringComparison.Ordinal);
    }
}
