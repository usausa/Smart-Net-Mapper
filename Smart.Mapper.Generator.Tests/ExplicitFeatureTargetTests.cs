namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// init-only / required メンバーを対象とする明示マッピング機能の受理・拒否を検証する。
// Verifies acceptance and rejection of explicit feature mappings targeting init-only / required members.
//   - constant/expression/using/from × init/required × return mapper: accepted, assigned via object initializer
//   - any init-only target × void mapper: rejected with SMP0018 (cannot assign init-only on an existing instance)
//   - MapCollection/MapNested × init-only (or required × return): rejected with SMP0028 (loop cannot run in an initializer)
public class ExplicitFeatureTargetTests
{
    private static IReadOnlyList<Diagnostic> AllDiagnostics(string source) =>
        GeneratorTestHelper.GetDiagnosticsAll(source);

    private static void AssertCompiles(string source)
    {
        var errors = AllDiagnostics(source)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private static void AssertRejected(string source, string smpId)
    {
        var diagnostics = AllDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == smpId);
    }

    private static string FeatureSource(string propDecl, bool returns, string attr, string helper = "")
    {
        var mapperSig = returns
            ? "public static partial Dst Map(Src src);"
            : "public static partial void Map(Src src, Dst dst);";
        return
            "#nullable enable\ninternal static partial class M\n{\n" +
            "    [global::Smart.Mapper.Mapper]\n" +
            $"    {attr}\n    {mapperSig}\n    {helper}\n}}\n" +
            "public class Src { public int Id { get; set; } public string Name { get; set; } = \"\"; }\n" +
            $"public class Dst {{ public int Id {{ get; set; }} {propDecl} }}\n";
    }

    [Theory]
    [InlineData("public string Extra { get; init; } = \"\";")]
    [InlineData("public required string Extra { get; set; }")]
    public void Constant_On_InitOrRequired_ReturnMapper_Compiles(string propDecl) =>
        AssertCompiles(FeatureSource(propDecl, returns: true, "[global::Smart.Mapper.MapConstant(\"Extra\", \"x\")]"));

    [Theory]
    [InlineData("public string Extra { get; init; } = \"\";")]
    [InlineData("public required string Extra { get; set; }")]
    public void Expression_On_InitOrRequired_ReturnMapper_Compiles(string propDecl) =>
        AssertCompiles(FeatureSource(propDecl, returns: true, "[global::Smart.Mapper.MapExpression(\"Extra\", \"src.Name + \\\"!\\\"\")]"));

    [Theory]
    [InlineData("public string Extra { get; init; } = \"\";")]
    [InlineData("public required string Extra { get; set; }")]
    public void Using_On_InitOrRequired_ReturnMapper_Compiles(string propDecl) =>
        AssertCompiles(FeatureSource(propDecl, returns: true, "[global::Smart.Mapper.MapUsing(\"Extra\", nameof(Build))]", "public static string Build(Src s) => s.Name;"));

    [Theory]
    [InlineData("public string Extra { get; init; } = \"\";")]
    [InlineData("public required string Extra { get; set; }")]
    public void From_On_InitOrRequired_ReturnMapper_Compiles(string propDecl) =>
        AssertCompiles(FeatureSource(propDecl, returns: true, "[global::Smart.Mapper.MapFrom(\"Extra\", \"Name\")]"));

    [Fact]
    public void Constant_On_InitOnly_VoidMapper_IsRejected() =>
        AssertRejected(FeatureSource("public string Extra { get; init; } = \"\";", returns: false, "[global::Smart.Mapper.MapConstant(\"Extra\", \"x\")]"), "SMP0018");

    [Fact]
    public void AutoMap_On_InitOnly_VoidMapper_IsRejected() =>
        AssertRejected(
            "#nullable enable\ninternal static partial class M\n{\n" +
            "    [global::Smart.Mapper.Mapper]\n    public static partial void Map(Src src, Dst dst);\n}\n" +
            "public class Src { public string Extra { get; set; } = \"\"; }\n" +
            "public class Dst { public string Extra { get; init; } = \"\"; }\n",
            "SMP0018");

    private static string CollectionSource(string itemsDecl, bool returns)
    {
        var mapperSig = returns
            ? "public static partial Dst Map(Src src);"
            : "public static partial void Map(Src src, Dst dst);";
        return
            "#nullable enable\nusing System.Collections.Generic;\n" +
            "internal static partial class M\n{\n" +
            "    [global::Smart.Mapper.Mapper]\n" +
            "    [global::Smart.Mapper.MapCollection(\"Items\", \"Items\", Mapper = nameof(MapVal))]\n" +
            $"    {mapperSig}\n" +
            "    public static E2 MapVal(E1 s) => new E2 { V = s.V };\n" +
            "}\n" +
            "public class E1 { public int V { get; set; } }\n" +
            "public class E2 { public int V { get; set; } }\n" +
            "public class Src { public int Id { get; set; } public List<E1> Items { get; set; } = default!; }\n" +
            $"public class Dst {{ public int Id {{ get; set; }} {itemsDecl} }}\n";
    }

    [Fact]
    public void Collection_On_InitOnly_IsRejected() =>
        AssertRejected(CollectionSource("public List<E2> Items { get; init; } = default!;", returns: true), "SMP0028");

    [Fact]
    public void Collection_On_Required_ReturnMapper_IsRejected() =>
        AssertRejected(CollectionSource("public required List<E2> Items { get; set; }", returns: true), "SMP0028");

    [Fact]
    public void Collection_On_Required_VoidMapper_Compiles() =>
        AssertCompiles(CollectionSource("public required List<E2> Items { get; set; }", returns: false));
}
