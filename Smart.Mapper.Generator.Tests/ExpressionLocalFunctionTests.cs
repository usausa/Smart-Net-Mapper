namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Each [MapExpression] is compiled as a static local function that takes the mapper's parameters under
// the same names. Expressions used to be assigned inline, so two of them declaring the same variable
// (out var n, is int n) collided in the method body (CS0128).
public class ExpressionLocalFunctionTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    // Errors anywhere, and warnings in the generated source, where a nullable mismatch of the emitted
    // functions would show up.
    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    [Theory]
    [InlineData("public static partial Dst Map(Src src);", "__d.First = __expression0(src);", "static int __expression0(global::Test.Src src) =>")]
    [InlineData("public static partial void Map(Src src, Dst dst);", "dst.First = __expression0(src, dst);", "static int __expression0(global::Test.Src src, global::Test.Dst dst) =>")]
    public void ExpressionsDeclaringSameVariablesCompile(string declaration, string assignment, string function)
    {
        var source = $$"""
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src
            {
                public string? First { get; set; }
                public string? Second { get; set; }
                public object? Boxed { get; set; }
                public object? OtherBoxed { get; set; }
            }
            public class Dst
            {
                public int First { get; set; }
                public int Second { get; set; }
                public int Boxed { get; set; }
                public int OtherBoxed { get; set; }
            }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.First), "int.TryParse(src.First, out var n) ? n : -1")]
                [MapExpression(nameof(Dst.Second), "int.TryParse(src.Second, out var n) ? n : -1")]
                [MapExpression(nameof(Dst.Boxed), "src.Boxed is int n ? n : -1")]
                [MapExpression(nameof(Dst.OtherBoxed), "src.OtherBoxed is int n ? n : -1")]
                {{declaration}}
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains(assignment, generated, StringComparison.Ordinal);
        Assert.Contains(function + " int.TryParse(src.First, out var n) ? n : -1;", generated, StringComparison.Ordinal);
        Assert.Contains("__expression3(", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void InitOnlyAndRequiredTargetsCallFunctionsInInitializer()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public string? First { get; set; } public string? Second { get; set; } }
            public class Dst { public required int First { get; set; } public int Second { get; init; } }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.First), "int.TryParse(src.First, out var n) ? n : -1")]
                [MapExpression(nameof(Dst.Second), "int.TryParse(src.Second, out var n) ? n : -1")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("First = __expression0(src),", generated, StringComparison.Ordinal);
        Assert.Contains("Second = __expression1(src),", generated, StringComparison.Ordinal);
    }

    // Being static, the function can take the in parameter, which a capturing one could not use.
    [Fact]
    public void InSourceAndCustomParameterArePassedThrough()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public readonly struct Src { public string? Text { get; init; } }
            public class Dst { public int Value { get; set; } public string? Text { get; set; } }
            public class Ctx { public int Offset { get; set; } }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.Value), "int.TryParse(src.Text, out var n) ? n + ctx.Offset : -1")]
                public static partial Dst Map(in Src src, Ctx ctx);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.Value = __expression0(in src, ctx);", generated, StringComparison.Ordinal);
        Assert.Contains("static int __expression0(in global::Test.Src src, global::Test.Ctx ctx) =>", generated, StringComparison.Ordinal);
    }

    // A local function cannot be an extension method, so this is left out.
    [Fact]
    public void ExtensionMapperFunctionOmitsThis()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public string? Text { get; set; } }
            public class Dst { public int Length { get; set; } }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.Length), "src.Text?.Length ?? 0")]
                public static partial Dst ToDst(this Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("ToDst(this global::Test.Src src)", generated, StringComparison.Ordinal);
        Assert.Contains("static int __expression0(global::Test.Src src) =>", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ReturnTypeKeepsNullableAnnotations()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            public class Src { public string? Text { get; set; } }
            public class Dst
            {
                public string? Label { get; set; }
                public List<string?> Items { get; set; } = [];
                public int? Count { get; set; }
            }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.Label), "src.Text")]
                [MapExpression(nameof(Dst.Items), "[src.Text]")]
                [MapExpression(nameof(Dst.Count), "src.Text?.Length")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("static string? __expression0(", generated, StringComparison.Ordinal);
        Assert.Contains("static global::System.Collections.Generic.List<string?> __expression1(", generated, StringComparison.Ordinal);
        Assert.Contains("static int? __expression2(", generated, StringComparison.Ordinal);
    }

    // A target declared with nullable disabled keeps its oblivious type, so null compiles without a
    // warning, as the direct assignment did.
    [Fact]
    public void ObliviousTargetTypeCompilesWithoutWarnings()
    {
        var source = """
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            #nullable disable
            public class Dst { public string Name { get; set; } public List<string> Tags { get; set; } public int Count { get; set; } }
            #nullable enable
            public class Src { public int Count { get; set; } }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.Name), "null")]
                [MapExpression(nameof(Dst.Tags), "null")]
                [MapExpression(nameof(Dst.Count), "src.Count + 1")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("#nullable disable annotations", generated, StringComparison.Ordinal);
        Assert.Contains("#nullable enable annotations", generated, StringComparison.Ordinal);
    }

    // The return type is the target's own type, so nullable analysis still applies: null for a
    // non-nullable target warns, as the direct assignment did.
    [Fact]
    public void NullForNonNullableTargetStillWarns()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst { public string Name { get; set; } = ""; }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.Name), "null")]
                public static partial Dst Map(Src src);
            }
            """;

        var warnings = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d))
            .Select(static d => d.Id)
            .ToList();

        Assert.Contains("CS8603", warnings);
    }

    // The function returns the target's type, so the conversions a direct assignment allowed still apply.
    [Fact]
    public void ImplicitConversionsToTargetTypeApply()
    {
        var source = """
            #nullable enable
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Child { public int V { get; set; } }
            public enum Kind { None, Some }
            public class Dst
            {
                public long Wide { get; set; }
                public double Real { get; set; }
                public int? Maybe { get; set; }
                public Child Child { get; set; } = new();
                public List<int> Numbers { get; set; } = [];
                public Kind Kind { get; set; }
            }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.Wide), "src.Value")]
                [MapExpression(nameof(Dst.Real), "1")]
                [MapExpression(nameof(Dst.Maybe), "null")]
                [MapExpression(nameof(Dst.Child), "new() { V = src.Value }")]
                [MapExpression(nameof(Dst.Numbers), "[src.Value, 2]")]
                [MapExpression(nameof(Dst.Kind), "0")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
    }

    // The calls keep the order of Order and then declaration; the functions follow the declaration order.
    [Fact]
    public void CallsFollowOrder()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst { public int A { get; set; } public int B { get; set; } public int C { get; set; } }
            public static partial class M
            {
                [Mapper]
                [MapExpression(nameof(Dst.A), "src.Value + 1", Order = 2)]
                [MapExpression(nameof(Dst.B), "src.Value + 2", Order = 1)]
                [MapExpression(nameof(Dst.C), "src.Value + 3")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var callC = generated.IndexOf("__d.C = __expression2(src);", StringComparison.Ordinal);
        var callB = generated.IndexOf("__d.B = __expression1(src);", StringComparison.Ordinal);
        var callA = generated.IndexOf("__d.A = __expression0(src);", StringComparison.Ordinal);
        Assert.True((callC >= 0) && (callC < callB) && (callB < callA), generated);
        var function0 = generated.IndexOf("static int __expression0(", StringComparison.Ordinal);
        var function2 = generated.IndexOf("static int __expression2(", StringComparison.Ordinal);
        Assert.True((callA < function0) && (function0 < function2), generated);
    }

    // An unresolved target has no type to declare the function with, so the expression stays inline and
    // the missing member is reported as before.
    [Fact]
    public void UnresolvedTargetReportsMissingMember()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Value { get; set; } }
            public class Dst { public int Value { get; set; } }
            public static partial class M
            {
                [Mapper]
                [MapExpression("Missing", "1")]
                public static partial Dst Map(Src src);
            }
            """;

        var errors = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Select(static d => d.Id)
            .ToList();

        Assert.Equal("CS1061", Assert.Single(errors));
    }
}
