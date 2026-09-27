namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A member along a dotted target path whose type the mapper cannot create (abstract, an interface, without a
// constructor it can call without arguments, or with required members its constructor leaves unset) is not
// created with ??= new T() (CS0144 / CS7036 / CS9035). As for a member it cannot assign, the instance it holds
// is written into, and nothing is assigned through it when it is null.
public class UncreatableTargetPathTests
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

    private static string Body(string source)
    {
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var start = generated.IndexOf(" Map(", StringComparison.Ordinal);
        return String.Join("\n", generated[start..].Split('\n').Skip(1).Select(static l => l.Trim()).Where(static l => l.Length > 0));
    }

    private static string Source(string attributes, bool returns = false) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public abstract class Abstract { public int Value { get; set; } }
        public interface IChild { int Value { get; set; } }
        public class WithArgument { public WithArgument(int seed) { } public int Value { get; set; } }
        public class WithRequired { public required int Key { get; set; } public int Value { get; set; } }
        public class Creatable { public int Value { get; set; } }
        public class Src { public int X { get; set; } }
        public class Dst
        {
            public Abstract? Abstract { get; set; }
            public IChild? Interface { get; set; }
            public WithArgument? Argument { get; set; }
            public WithRequired? Required { get; set; }
            public Creatable? Creatable { get; set; }
        }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{(returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
            private static int Calc(Src src) => src.X;
        }
        """;

    [Theory]
    [InlineData("Abstract")]
    [InlineData("Interface")]
    [InlineData("Argument")]
    [InlineData("Required")]
    public void UncreatableMemberIsWrittenIntoWhenItHoldsInstance(string member)
    {
        var source = Source($"[MapProperty(\"{member}.Value\", nameof(Src.X))]");

        AssertCompiles(source);
        var body = Body(source);
        Assert.Contains($"if (dst.{member} is not null)\n{{\ndst.{member}.Value = src.X;\n}}", body, StringComparison.Ordinal);
        Assert.DoesNotContain("??=", body, StringComparison.Ordinal);
    }

    // The same for the targets of the other attributes, and in a return mapper
    [Theory]
    [InlineData("[MapConstant(\"Abstract.Value\", 1)]", false, "if (dst.Abstract is not null)\n{\ndst.Abstract.Value = 1;\n}")]
    [InlineData("[MapExpression(\"Interface.Value\", \"src.X\")]", false, "if (dst.Interface is not null)\n{\ndst.Interface.Value = __expression0(src, dst);\n}")]
    [InlineData("[MapUsing(\"Required.Value\", nameof(Calc))]", true, "if (__d.Required is not null)\n{\n__d.Required.Value = Calc(src);\n}")]
    public void UncreatableMemberOfOtherTargetsIsWrittenInto(string attribute, bool returns, string expected)
    {
        var source = Source(attribute, returns);

        AssertCompiles(source);
        Assert.Contains(expected, Body(source), StringComparison.Ordinal);
    }

    // A member the mapper can create is created as before
    [Fact]
    public void CreatableMemberIsCreated()
    {
        var source = Source("[MapProperty(\"Creatable.Value\", nameof(Src.X))]");

        AssertCompiles(source);
        Assert.Contains("dst.Creatable ??= new global::Test.Creatable();\ndst.Creatable.Value = src.X;", Body(source), StringComparison.Ordinal);
    }
}
