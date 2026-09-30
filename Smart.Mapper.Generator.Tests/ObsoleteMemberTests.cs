namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A property marked [Obsolete] is left out of the automatic mapping, as a source and as a destination, whether
// obsolete as a warning or as an error (on the property, an accessor, or the property an override overrides), and
// strict mode does not report it. It used to be mapped, and the generated code warned (CS0618) or failed (CS0619).
// A member an attribute names is used when obsolete as a warning, without a warning in the generated file (which
// disables CS0612 / CS0618 as the other generators do), and reported with the existing
// diagnostic of the attribute when obsolete as an error: a property, a field, a segment of a dotted path, the method
// of [MapFrom], and the methods of the mapper class the attributes name. An intermediate member of a dotted target is
// not created through a constructor obsolete as an error.
public class ObsoleteMemberTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static string Source(string types, string attributes = "", string mapper = "[Mapper]", string members = "") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);
            {{members}}
        }
        """;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    [Theory]
    [InlineData("public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { [Obsolete(\"Old\")] public int A { get; set; } public int B { get; set; } }")]
    [InlineData("public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { [Obsolete(\"Old\", true)] public int A { get; set; } public int B { get; set; } }")]
    [InlineData("public class Src { [Obsolete(\"Old\")] public int A { get; set; } public int B { get; set; } } public class Dst { public int A { get; set; } public int B { get; set; } }")]
    [InlineData("public class Src { [Obsolete(\"Old\", true)] public int A { get; set; } public int B { get; set; } } public class Dst { public int A { get; set; } public int B { get; set; } }")]
    [InlineData("public class Src { public int A { get; set; } public int B { get; set; } } public class Dst { public int A { get; [Obsolete(\"Old\")] set; } public int B { get; set; } }")]
    [InlineData("public class Src { public int A { get; set; } public int B { get; set; } } public class Base { [Obsolete(\"Old\")] public virtual int A { get; set; } } public class Dst : Base { [Obsolete(\"Old\")] public override int A { get; set; } public int B { get; set; } }")]
    public void AutomaticMappingLeavesObsoleteOut(string types)
    {
        var (generated, problems) = Build(Source(types));

        Assert.Empty(problems);
        Assert.Contains("__d.B = src.B;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.A", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void StrictLeavesObsoleteOut()
    {
        var (_, problems) = Build(Source(
            "public class Src { public int B { get; set; } } public class Dst { [Obsolete(\"Old\")] public int A { get; set; } public int B { get; set; } }",
            mapper: "[Mapper(Strict = true)]"));

        Assert.Empty(problems);
    }

    // Named by an attribute, one obsolete as a warning is used, and the generated file does not warn
    [Theory]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { [Obsolete(\"Old\")] public int A { get; set; } }",
        "[MapProperty(\"A\", \"A\")]",
        "",
        "__d.A = src.A;")]
    [InlineData(
        "public class Src { [Obsolete(\"Old\")] public int A { get; set; } } public class Dst { public int X { get; set; } }",
        "[MapProperty(\"X\", \"A\")]",
        "",
        "__d.X = src.A;")]
    [InlineData(
        "public class Src { [Obsolete(\"Old\")] public int GetA() => 1; } public class Dst { public int A { get; set; } }",
        "[MapFrom(\"A\", \"GetA\")]",
        "",
        "__d.A = src.GetA();")]
    [InlineData(
        "public class Src { } public class Dst { [Obsolete(\"Old\")] public int Level; }",
        "[MapConstant(\"Level\", 1)]",
        "",
        "__d.Level = 1;")]
    [InlineData(
        "public class Src { public int A { get; set; } } public class Dst { public int A { get; set; } }",
        "[MapProperty(\"A\", \"A\", Converter = nameof(Conv))]",
        "[Obsolete(\"Old\")] private static int Conv(int value) => value;",
        "__d.A = Conv(src.A);")]
    public void NamedObsoleteMemberIsUsed(string types, string attributes, string members, string expected)
    {
        var (generated, problems) = Build(Source(types, attributes, "[Mapper(AutoMap = false)]", members));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // Named by an attribute, one obsolete as an error is reported with the existing diagnostic of the attribute
    [Theory]
    [InlineData("public class Src { public int A { get; set; } } public class Dst { [Obsolete(\"Old\", true)] public int A { get; set; } }", "[MapProperty(\"A\", \"A\")]", "", "SMP0102")]
    [InlineData("public class Src { [Obsolete(\"Old\", true)] public int A { get; set; } } public class Dst { public int A { get; set; } }", "[MapProperty(\"A\", \"A\")]", "", "SMP0108")]
    [InlineData("public class Src { public Child Item { get; set; } = new(); } public class Dst { [Obsolete(\"Old\", true)] public Child Item { get; set; } = new(); }", "[MapProperty(\"Item.V\", \"Item.V\")]", "", "SMP0102")]
    [InlineData("public class Src { [Obsolete(\"Old\", true)] public int GetA() => 1; } public class Dst { public int A { get; set; } }", "[MapFrom(\"A\", \"GetA\")]", "", "SMP0204")]
    [InlineData("public class Src { } public class Dst { [Obsolete(\"Old\", true)] public int Level; }", "[MapConstant(\"Level\", 1)]", "", "SMP0102")]
    [InlineData("public class Src { public int A { get; set; } } public class Dst { public int A { get; set; } }", "[MapProperty(\"A\", \"A\", Converter = nameof(Conv))]", "[Obsolete(\"Old\", true)] private static int Conv(int value) => value;", "SMP0110")]
    [InlineData("public class Src { public int A { get; set; } } public class Dst { public int A { get; set; } }", "[MapProperty(\"A\", \"A\")] [MapCondition(\"A\", nameof(Can))]", "[Obsolete(\"Old\", true)] private static bool Can(Src src) => true;", "SMP0112")]
    [InlineData("public class Src { } public class Dst { public int A { get; set; } }", "[MapUsing(\"A\", nameof(Get))]", "[Obsolete(\"Old\", true)] private static int Get(Src src) => 1;", "SMP0201")]
    [InlineData("public class Src { } public class Dst { }", "[BeforeMap(nameof(Before))]", "[Obsolete(\"Old\", true)] private static void Before(Src src, Dst dst) { }", "SMP0106")]
    [InlineData("public class Src { } public class Dst { }", "[AfterMap(nameof(After))]", "[Obsolete(\"Old\", true)] private static void After(Src src, Dst dst) { }", "SMP0107")]
    [InlineData("public class Src { public Child Item { get; set; } = new(); } public class Dst { public Child Item { get; set; } = new(); }", "[MapNested(\"Item\", Mapper = nameof(MapChild))]", "[Obsolete(\"Old\", true)] private static Child MapChild(Child source) => source;", "SMP0214")]
    [InlineData("public class Src { public Child[] Items { get; set; } = []; } public class Dst { public Child[] Items { get; set; } = []; }", "[MapCollection(\"Items\", Mapper = nameof(MapChild))]", "[Obsolete(\"Old\", true)] private static Child MapChild(Child source) => source;", "SMP0213")]
    public void NamedObsoleteErrorMemberEmitsDiagnostic(string types, string attributes, string members, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(types, attributes, "[Mapper(AutoMap = false)]", members));

        Assert.Equal(id, Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // An intermediate member is not created through a constructor obsolete as an error, new T() binding to it
    // although another one takes no argument either; the path writes into the member the destination holds
    [Theory]
    [InlineData("public class Holder { [Obsolete(\"Old\", true)] public Holder() { } public Holder(int v) { V = v; } public int V { get; set; } }")]
    [InlineData("public class Holder { [Obsolete(\"Old\", true)] public Holder() { } public Holder(int v = 0) { V = v; } public int V { get; set; } }")]
    public void IntermediateIsNotCreatedThroughObsoleteErrorConstructor(string holder)
    {
        var (generated, problems) = Build(Source(
            holder + " public class Src { public int V { get; set; } } public class Dst { public Holder? Item { get; set; } }",
            "[MapProperty(\"Item.V\", \"V\")]",
            "[Mapper(AutoMap = false)]"));

        Assert.Empty(problems);
        Assert.Contains("if (__d.Item is not null)", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("new global::Test.Holder()", generated, StringComparison.Ordinal);
    }

    // One obsolete as a warning is the only way to create it, so it is called
    [Fact]
    public void IntermediateIsCreatedThroughObsoleteWarningConstructor()
    {
        var (generated, problems) = Build(Source(
            "public class Holder { [Obsolete(\"Old\")] public Holder() { } public int V { get; set; } } public class Src { public int V { get; set; } } public class Dst { public Holder? Item { get; set; } }",
            "[MapProperty(\"Item.V\", \"V\")]",
            "[Mapper(AutoMap = false)]"));

        Assert.Empty(problems);
        Assert.Contains("__d.Item ??= new global::Test.Holder();", generated, StringComparison.Ordinal);
    }

    // A required member has to be set, so one the automatic mapping leaves out needs an attribute
    [Fact]
    public void RequiredObsoleteMemberNeedsAttribute()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(
            "public class Src { public int A { get; set; } } public class Dst { [Obsolete(\"Old\")] public required int A { get; set; } }"));

        Assert.Equal("SMP0308", Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
    }
}
