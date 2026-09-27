namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A protected member is reached only on an instance of a type deriving from the class that reaches it, so a mapper
// class deriving from the destination or the source cannot call a protected constructor or accessor on the instance
// it maps (CS0122 / CS1540), although the member is accessible to it. They used to be taken as callable there; they
// are left out as private ones are. A mapper declared in the type itself calls them.
public class ProtectedAccessTests
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

    private static string Source(string types, string mapper) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public int A { get; set; } public int B { get; set; } }
        {{types}}
        {{mapper}}
        """;

    [Theory]
    [InlineData(
        "public class Dst { public Dst(int a) { A = a; } protected Dst(int a, int b) { A = a; B = b; } public int A { get; } public int B { get; } }",
        "public partial class M : Dst { public M() : base(0) { } [Mapper] public static partial Dst Map(Src src); }",
        "var __d = new global::Test.Dst(src.A);",
        "src.B")]
    [InlineData(
        "public class Dst { public int A { get; protected set; } public int B { get; set; } }",
        "public partial class M : Dst { [Mapper] public static partial Dst Map(Src src); }",
        "__d.B = src.B;",
        "src.A")]
    [InlineData(
        "public class Dst { public int A { get; protected init; } public int B { get; set; } }",
        "public partial class M : Dst { [Mapper] public static partial Dst Map(Src src); }",
        "__d.B = src.B;",
        "src.A")]
    [InlineData(
        "public class Dst { public int A { get; protected set; } public int B { get; set; } }",
        "public partial class M : Dst { [Mapper] public static partial void Map(Src src, Dst dst); }",
        "dst.B = src.B;",
        "src.A")]
    public void ProtectedMemberOfDestinationIsLeftOut(string types, string mapper, string expected, string unexpected)
    {
        var source = Source(types, mapper);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpected, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ProtectedGetterOfSourceIsLeftOut()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int A { protected get; set; } public int B { get; set; } }
            public class Dst { public int A { get; set; } public int B { get; set; } }
            public partial class M : Src { [Mapper] public static partial Dst Map(Src src); }
            """;

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("__d.B = src.B;", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.A", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        "public class Dst { protected Dst() { } public int A { get; set; } }",
        "public partial class M : Dst { [Mapper] public static partial Dst Map(Src src); }",
        "SMP0305")]
    [InlineData(
        "public class Dst { public int A { get; protected set; } }",
        "public partial class M : Dst { [Mapper(AutoMap = false)] [MapProperty(\"A\", \"A\")] public static partial Dst Map(Src src); }",
        "SMP0214")]
    [InlineData(
        "public class Dst { public int A { get; protected set; } }",
        "public partial class M : Dst { [Mapper(AutoMap = false)] [MapConstant(\"A\", 1)] public static partial Dst Map(Src src); }",
        "SMP0214")]
    public void ProtectedMemberNamedOrNeededEmitsDiagnostic(string types, string mapper, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(types, mapper));

        Assert.Equal(id, Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // Within the type itself, the instance it creates is one of its own
    [Fact]
    public void MapperInTypeCallsProtectedMembers()
    {
        var source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int A { get; set; } public int B { get; set; } public int C { get; set; } }
            public partial class Dst
            {
                public Dst() { }
                protected Dst(int a, int b) { A = a; B = b; }
                public int A { get; protected set; }
                public int B { get; }
                public int C { get; protected set; }

                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("var __d = new global::Test.Dst(src.A, src.B);", generated, StringComparison.Ordinal);
        Assert.Contains("__d.C = src.C;", generated, StringComparison.Ordinal);
    }
}
