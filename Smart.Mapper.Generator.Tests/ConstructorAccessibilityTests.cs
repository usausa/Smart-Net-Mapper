namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A return mapper creates the destination through a constructor the mapper class can call. The longest constructor
// used to be taken whatever its accessibility, so a private or protected one failed in the generated code (CS0122);
// it is passed over for the longest one the mapper class can call, as is one obsolete as an error (CS0619). A
// destination that cannot be created at all, an abstract class (whatever its constructors are declared as), an
// interface, or one without a constructor the mapper class can call, used to fail there as well (CS0144 / CS0122)
// and is reported (SMP0303). A void mapper, which never constructs, is not concerned.
public class ConstructorAccessibilityTests
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

    private static string Source(string destination, string returnType = "Dst", bool returns = true) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public int X { get; set; } public int Other { get; set; } }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            {{(returns ? $"public static partial {returnType} Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
        }
        """;

    [Theory]
    [InlineData("public class Dst { public Dst(int x) { X = x; } private Dst(int x, int other) { X = x + other; } public int X { get; } }", "var __d = new global::Test.Dst(src.X);")]
    [InlineData("public class Dst { public Dst(int x) { X = x; } protected Dst(int x, int other) { X = x + other; } public int X { get; } }", "var __d = new global::Test.Dst(src.X);")]
    [InlineData("public class Dst { public Dst(int x) { X = x; } internal Dst(int x, int other) { X = x + other; } public int X { get; } }", "var __d = new global::Test.Dst(src.X, src.Other);")]
    [InlineData("public class Dst { public Dst(int x) { X = x; } [System.Obsolete(\"Old\", true)] public Dst(int x, int other) { X = x + other; } public int X { get; } }", "var __d = new global::Test.Dst(src.X);")]
    [InlineData("public class Dst { [System.Obsolete(\"For serializers\", true)] public Dst() { } public Dst(int x) { X = x; } public int X { get; set; } }", "var __d = new global::Test.Dst(src.X);")]
    public void ConstructorMapperCanCallIsTaken(string destination, string expected)
    {
        var source = Source(destination);

        AssertCompiles(source);
        Assert.Contains(expected, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public abstract class Dst { public int X { get; set; } }", "Dst")]
    [InlineData("public interface IDst { int X { get; set; } } public class Dst { }", "IDst")]
    [InlineData("public class Dst { private Dst() { } public int X { get; set; } }", "Dst")]
    [InlineData("public class Dst { private Dst(int x) { X = x; } public int X { get; } }", "Dst")]
    [InlineData("public abstract class Dst { public Dst(int x) { X = x; } public int X { get; } }", "Dst")]
    [InlineData("public class Dst { [System.Obsolete(\"Old\", true)] public Dst(int x) { X = x; } public int X { get; } }", "Dst")]
    public void UncreatableDestinationEmitsDiagnostic(string destination, string returnType)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(destination, returnType));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal("SMP0303", diagnostic.Id);
        Assert.Contains($"type=[{returnType}]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    [Fact]
    public void VoidMapperIsNotConcerned()
    {
        AssertCompiles(Source("public abstract class Dst { public int X { get; set; } }", returns: false));
    }
}
