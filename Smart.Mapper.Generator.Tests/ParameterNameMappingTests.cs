namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A [MapProperty] naming a constructor parameter by its own name, where a member of another spelling matches it as
// well (the parameter value and the property Value under the ordinal comparison), is what the argument takes. It
// used to be passed over for the automatic mapping of the member without a word, and one with a dotted source was
// assigned as a member of the parameter's name after construction (CS1061).
public class ParameterNameMappingTests
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

    private static string Source(string destination, string attribute) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        public class Src { public int Value { get; set; } public int Other { get; set; } public int X { get; set; } public Child? Child { get; set; } }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            {{attribute}}
            public static partial Dst Map(Src src);
        }
        """;

    [Theory]
    [InlineData("public class Dst { public Dst(int value) { Value = value; } public int Value { get; } }", "[MapProperty(\"value\", nameof(Src.Other))]", "var __d = new global::Test.Dst(src.Other);")]
    [InlineData("public class Dst { public Dst(int value, int x) { Value = value; X = x; } public int Value { get; set; } public int X { get; } }", "[MapProperty(\"value\", nameof(Src.Other))]", "var __d = new global::Test.Dst(src.Other, src.X);")]
    [InlineData("public class Dst { public Dst(int value) { Value = value; } public int Value { get; } }", "[MapProperty(\"value\", \"Child.V\")]", "var __d = new global::Test.Dst(src.Child is not null ? src.Child.V : default!);")]
    [InlineData("public class Dst { public Dst(int value) { Value = value; } public int Value { get; } }", "[MapProperty(nameof(Dst.Value), nameof(Src.Other))]", "var __d = new global::Test.Dst(src.Other);")]
    public void MappingNamingParameterIsTaken(string destination, string attribute, string expected)
    {
        var source = Source(destination, attribute);

        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("__d.Value", generated, StringComparison.Ordinal);
    }
}
