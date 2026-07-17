namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// Generated collection-mapping code must be self-contained: the emitted .g.cs declares no usings,
// so every BCL call it makes must be fully qualified and must compile without the consuming project
// providing any usings. These are regressions for three previously-broken paths:
//   - FrozenSet target emitted a bare .ToFrozenSet() (needs System.Collections.Frozen).
//   - Array source emitted a bare .AsSpan()      (needs System / MemoryExtensions).
//   - Memory<T> / ReadOnlyMemory<T> source was rejected as "not a collection" (no code generated).
public class CollectionSelfContainedTests
{
    private static string Source(string fqSourceType, string fqTargetType) =>
        "#nullable enable\n" +
        "internal static partial class M\n" +
        "{\n" +
        "    [global::Smart.Mapper.Mapper]\n" +
        "    [global::Smart.Mapper.MapCollection(nameof(Dst.Items), nameof(Src.Items), Mapper = nameof(MapElem))]\n" +
        "    public static partial Dst Map(Src src);\n" +
        "    public static E2 MapElem(E1 s) => new E2 { V = s.V };\n" +
        "}\n" +
        "public class E1 { public int V { get; set; } }\n" +
        "public class E2 { public int V { get; set; } }\n" +
        $"public class Src {{ public {fqSourceType} Items {{ get; set; }} = default!; }}\n" +
        $"public class Dst {{ public {fqTargetType} Items {{ get; set; }} = default!; }}\n";

    // No global usings are supplied, so any non-fully-qualified BCL call in the generated code fails to compile.
    private static void AssertGeneratedCompiles(string fqSourceType, string fqTargetType)
    {
        var errors = GeneratorTestHelper.GetDiagnosticsAll(Source(fqSourceType, fqTargetType))
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void ArraySource_GeneratesSelfContainedCode() =>
        AssertGeneratedCompiles("E1[]", "global::System.Collections.Generic.List<E2>");

    [Fact]
    public void FrozenSetTarget_GeneratesSelfContainedCode() =>
        AssertGeneratedCompiles("E1[]", "global::System.Collections.Frozen.FrozenSet<E2>");

    [Fact]
    public void MemorySource_IsSupported() =>
        AssertGeneratedCompiles("global::System.Memory<E1>", "global::System.Collections.Generic.List<E2>");

    [Fact]
    public void ReadOnlyMemorySource_IsSupported() =>
        AssertGeneratedCompiles("global::System.ReadOnlyMemory<E1>", "global::System.Collections.Generic.List<E2>");
}
