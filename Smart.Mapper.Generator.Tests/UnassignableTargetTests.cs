namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A property whose setter the mapper class cannot call (get-only, or a private setter) is left out of the
// automatic mapping, as a get-only one was, unless a constructor assigns it. Named explicitly, it is
// reported as an unassignable target (SMP0102) instead of failing in the generated code with CS0200 /
// CS0272. Strict mode does not report it as unmapped, as it did not for get-only properties.
public class UnassignableTargetTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || (d.Severity == DiagnosticSeverity.Warning))
            .Where(static d => IsGenerated(d) || (d.Severity == DiagnosticSeverity.Error) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
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

    private static string Source(string mapper, string attributes, string types = "", string members = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Child { public string Name { get; private set; } = ""; }
        public class Src
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
            public string Fixed { get; set; } = "";
            public string GetName() => Name;
        }
        public class Dst
        {
            public int Id { get; set; }
            public string Name { get; private set; } = "";
            public string Code { get; internal set; } = "";
            public string Fixed { get; } = "";
            public Child Child { get; set; } = new();
        }
        {{types}}
        public static partial class M
        {
            {{mapper}}
            {{attributes}}
            public static partial Dst Map(Src src);
            {{members}}
        }
        """;

    // Automatic mapping leaves out the private and get-only setters and keeps the internal one.
    [Fact]
    public void AutomaticMappingSkipsSetterMapperCannotCall()
    {
        var source = Source("[Mapper]", string.Empty);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Id = src.Id;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Code = src.Code;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Name", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Fixed", generated, StringComparison.Ordinal);
    }

    // Strict mode reports an unmapped property the mapper can assign, not one it cannot.
    [Fact]
    public void StrictModeSkipsSetterMapperCannotCall()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(Source("[Mapper(Strict = true, AutoMap = false)]", "[MapProperty(nameof(Dst.Id))] [MapIgnore(nameof(Dst.Child))]"));

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0501", diagnostic.Id);
        Assert.Contains("Code", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // A constructor that construction calls still assigns such a property.
    [Fact]
    public void ConstructorStillAssignsPrivateSetter()
    {
        var source = Source(
            "[Mapper]",
            string.Empty,
            "public class CtorDst { public CtorDst(string name) { Name = name; } public string Name { get; private set; } }").Replace("public static partial Dst Map(Src src);", "public static partial CtorDst Map(Src src);", StringComparison.Ordinal);

        AssertCompiles(source);
        Assert.Contains("var __d = new global::Test.CtorDst(src.Name);", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name))]", "")]
    [InlineData("[MapProperty(\"Child.Name\", nameof(Src.Name))]", "")]
    [InlineData("[MapConstant(nameof(Dst.Name), \"x\")]", "")]
    [InlineData("[MapConstant(nameof(Dst.Fixed), \"x\")]", "")]
    [InlineData("[MapExpression(nameof(Dst.Name), \"\\\"x\\\"\")]", "")]
    [InlineData("[MapUsing(nameof(Dst.Name), nameof(Describe))]", "static string Describe(Src s) => s.Name;")]
    [InlineData("[MapUsing(nameof(Dst.Fixed), nameof(Describe))]", "static string Describe(Src s) => s.Name;")]
    [InlineData("[MapFrom(nameof(Dst.Name), nameof(Src.GetName))]", "")]
    public void ExplicitTargetMapperCannotAssignEmitsDiagnostic(string attributes, string members)
    {
        AssertDiagnostic(Source("[Mapper(AutoMap = false)]", attributes, members: members), "SMP0102");
    }

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Code))]", "__d.Code = src.Code;")]
    [InlineData("[MapConstant(nameof(Dst.Code), \"x\")]", "__d.Code = \"x\";")]
    public void ExplicitTargetWithInternalSetterCompiles(string attributes, string assignment)
    {
        var source = Source("[Mapper(AutoMap = false)]", attributes);

        AssertCompiles(source);
        Assert.Contains(assignment, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
