namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A void mapper fills the instance it is given and never constructs, so the constructors of the destination do
// not apply to it: a constructor taking a member without a settable property no longer rejects the mapper
// (SMP0302), whether a parameterless constructor exists or not, and that member is left out like any get-only
// one. SMP0302 is reported only for a member it is asked to assign that is init-only, or that only a
// constructor assigns (the target of [MapProperty] matching a parameter of the constructor a return mapper
// would call).
public class VoidMapperConstructorTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static void AssertDiagnostic(string source, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
    }

    private static string Source(string destination, string attributes = "", string mapper = "[Mapper]", string signature = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Item { public int Value { get; set; } }
        public class Src
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public List<Item> Items { get; set; } = [];
        }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            public static partial Item MapItem(Item source);

            {{mapper}}
            {{attributes}}
            {{signature}}
        }
        """;

    private const string GetOnlyByConstructor =
        "public class Dst { public Dst(string id) { Id = id; } public string Id { get; } public string Name { get; set; } = \"\"; }";

    [Theory]
    // A get-only member the constructor takes
    [InlineData(GetOnlyByConstructor)]
    // The same, with a parameterless constructor too
    [InlineData("public class Dst { public Dst() { Id = \"\"; } public Dst(string id) { Id = id; } public string Id { get; } public string Name { get; set; } = \"\"; }")]
    // A parameter without a member
    [InlineData("public class Dst { public Dst(string code) { } public string Name { get; set; } = \"\"; }")]
    // A record whose positional members the source does not have
    [InlineData("public record Dst(int Number) { public string Name { get; set; } = \"\"; }")]
    // A struct passed by reference
    [InlineData("public struct Dst { public Dst(string id) { Id = id; Name = \"\"; } public string Id { get; } public string Name { get; set; } }")]
    public void ConstructorDoesNotApplyToVoidMapper(string destination)
    {
        var signature = destination.StartsWith("public struct", StringComparison.Ordinal)
            ? "public static partial void Map(Src src, ref Dst dst);"
            : "public static partial void Map(Src src, Dst dst);";
        var source = Source(destination, signature: signature);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("dst.Name = src.Name;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dst.Id", generated, StringComparison.Ordinal);
    }

    // A get-only collection the constructor takes is refilled in place, and a member the constructor takes can
    // be ignored
    [Theory]
    [InlineData(
        "public class Dst { public Dst(List<Item> items) { Items = items; } public List<Item> Items { get; } }",
        "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapItem), Strategy = CollectionStrategy.InPlace)]",
        "dst.Items.Clear();")]
    [InlineData(GetOnlyByConstructor, "[MapIgnore(nameof(Dst.Id))]", "dst.Name = src.Name;")]
    public void MemberConstructorTakesIsFilledOrIgnored(string destination, string attributes, string expected)
    {
        var source = Source(destination, attributes);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    // [MapProperty] to a get-only member or a parameter only the constructor assigns
    [InlineData(GetOnlyByConstructor, "[MapProperty(nameof(Dst.Id), nameof(Src.Id))]", "[Mapper(AutoMap = false)]")]
    [InlineData("public class Dst { public Dst(string code) { } public string Name { get; set; } = \"\"; }", "[MapProperty(\"code\", nameof(Src.Id))]", "[Mapper(AutoMap = false)]")]
    // An init-only member, mapped automatically or by an attribute
    [InlineData("public record Dst(string Id) { public string Name { get; set; } = \"\"; }", "", "[Mapper]")]
    [InlineData("public class Dst { public string Id { get; init; } = \"\"; }", "[MapConstant(nameof(Dst.Id), \"x\")]", "[Mapper(AutoMap = false)]")]
    public void InitOnlyOrConstructorOnlyMemberEmitsDiagnostic(string destination, string attributes, string mapper)
    {
        AssertDiagnostic(Source(destination, attributes, mapper), "SMP0302");
    }

    // A get-only member no constructor takes, and a constant for a member only the constructor assigns, are
    // reported as unassignable targets, as they are for a return mapper
    [Theory]
    [InlineData("public class Dst { public string Id { get; } = \"\"; }", "[MapProperty(nameof(Dst.Id), nameof(Src.Id))]")]
    [InlineData(GetOnlyByConstructor, "[MapConstant(nameof(Dst.Id), \"x\")]")]
    public void UnassignableMemberEmitsDiagnostic(string destination, string attributes)
    {
        AssertDiagnostic(Source(destination, attributes, "[Mapper(AutoMap = false)]"), "SMP0102");
    }

    // A return mapper still constructs through the constructor
    [Fact]
    public void ReturnMapperStillConstructs()
    {
        var source = Source(GetOnlyByConstructor, signature: "public static partial Dst Map(Src src);");

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.Dst(src.Id);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
