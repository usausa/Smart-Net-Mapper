namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A struct property along a dotted target path is a value: assigning a member of it in place does not compile
// (CS1612), and ??= does not apply to it (CS0019). It is copied into a local, written into and assigned back,
// and consecutive assignments into the same struct share the copy. A struct field is a variable and is written
// into directly. A struct that cannot be written back, a get-only property or a readonly field, is reported
// (SMP0102); an init-only one is created in the object initializer of a return mapper, and reported in a void
// mapper (SMP0302).
public class StructTargetPathTests
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

    // The generated method body with the indentation removed, one statement or brace per line
    private static string Body(string source)
    {
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        var start = generated.IndexOf(" Map(", StringComparison.Ordinal);
        return String.Join("\n", generated[start..].Split('\n').Skip(1).Select(static l => l.Trim()).Where(static l => l.Length > 0));
    }

    private static string Source(string attributes, bool returns = false, string signature = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Inner { public int Plain { get; set; } }
        public struct Point { public int X { get; set; } public int Y { get; set; } public Inner? Child { get; set; } public Deep Deep { get; set; } public Deep DeepField; }
        public struct Deep { public int Z { get; set; } }
        public readonly struct Fixed { public int X { get; init; } }
        public class Src { public int X { get; set; } public int Y { get; set; } public SrcIn? Nested { get; set; } }
        public class SrcIn { public int Z { get; set; } }
        public class Dst
        {
            public Point Point { get; set; }
            public Point GetOnly { get; }
            public Point InitOnly { get; init; }
            public Point Field;
            public readonly Point ReadOnlyField;
            public Fixed Fixed { get; set; }
        }
        public struct StructDst { public Point Point { get; set; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{(signature.Length > 0 ? signature : returns ? "public static partial Dst Map(Src src);" : "public static partial void Map(Src src, Dst dst);")}}
            private static bool IsPositive(int value) => value > 0;
        }
        """;

    [Fact]
    public void StructPropertyIsCopiedAndAssignedBack()
    {
        var source = Source("[MapProperty(\"Point.X\", nameof(Src.X))]");

        AssertCompiles(source);
        Assert.Contains("{\nvar __copy0 = dst.Point;\n__copy0.X = src.X;\ndst.Point = __copy0;\n}", Body(source), StringComparison.Ordinal);
    }

    // The members of one struct share the copy; a class inside it is created in the copy, and a struct inside
    // it is copied from the copy
    [Fact]
    public void AssignmentsIntoSameStructShareCopy()
    {
        var source = Source("""
            [MapProperty("Point.X", nameof(Src.X))]
            [MapProperty("Point.Y", nameof(Src.Y))]
            [MapCondition("Point.Y", nameof(IsPositive))]
            [MapProperty("Point.Child.Plain", nameof(Src.X))]
            [MapProperty("Point.Deep.Z", nameof(Src.Y))]
            """);

        AssertCompiles(source);
        Assert.Contains(
            "{\nvar __copy0 = dst.Point;\n" +
            "__copy0.X = src.X;\n" +
            "if (IsPositive(src.Y))\n{\n__copy0.Y = src.Y;\n}\n" +
            "__copy0.Child ??= new global::Test.Inner();\n__copy0.Child.Plain = src.X;\n" +
            "{\nvar __copy1 = __copy0.Deep;\n__copy1.Z = src.Y;\n__copy0.Deep = __copy1;\n}\n" +
            "dst.Point = __copy0;\n}",
            Body(source),
            StringComparison.Ordinal);
    }

    // Under the null check of a dotted source path, and in a return mapper and a struct passed by reference
    [Theory]
    [InlineData("[MapProperty(\"Point.X\", \"Nested.Z\")]", false, "", "if (src.Nested is not null)\n{\n{\nvar __copy0 = dst.Point;\n__copy0.X = src.Nested.Z;\ndst.Point = __copy0;\n}\n}")]
    [InlineData("[MapProperty(\"Point.X\", nameof(Src.X))]", true, "", "var __d = new global::Test.Dst();\n{\nvar __copy0 = __d.Point;\n__copy0.X = src.X;\n__d.Point = __copy0;\n}")]
    [InlineData("[MapProperty(\"Point.X\", nameof(Src.X))]", false, "public static partial void Map(Src src, ref StructDst dst);", "{\nvar __copy0 = dst.Point;\n__copy0.X = src.X;\ndst.Point = __copy0;\n}")]
    public void StructPropertyIsCopiedInEveryContext(string attributes, bool returns, string signature, string expected)
    {
        var source = Source(attributes, returns, signature);

        AssertCompiles(source);
        Assert.Contains(expected, Body(source), StringComparison.Ordinal);
    }

    // A struct field is a variable, written into directly; a struct field of a copied struct is written into the
    // copy
    [Fact]
    public void StructFieldIsWrittenDirectly()
    {
        var source = Source("""
            [MapConstant("Field.X", 1)]
            [MapConstant("Point.DeepField.Z", 2)]
            """);

        AssertCompiles(source);
        var body = Body(source);
        Assert.Contains("dst.Field.X = 1;", body, StringComparison.Ordinal);
        Assert.Contains("{\nvar __copy0 = dst.Point;\n__copy0.DeepField.Z = 2;\ndst.Point = __copy0;\n}", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[MapProperty(\"GetOnly.X\", nameof(Src.X))]", false)]
    [InlineData("[MapProperty(\"GetOnly.X\", nameof(Src.X))]", true)]
    [InlineData("[MapConstant(\"ReadOnlyField.X\", 1)]", false)]
    [InlineData("[MapProperty(\"Fixed.X\", nameof(Src.X))]", false)]
    [InlineData("[MapProperty(\"InitOnly.X\", nameof(Src.X))]", false)]
    public void StructThatCannotBeWrittenBackEmitsDiagnostic(string attribute, bool returns)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(attribute, returns));

        var diagnostic = Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        // A void mapper could set an init-only struct in the initializer of a return mapper (SMP0302)
        Assert.Equal(attribute.Contains("Fixed", StringComparison.Ordinal) || attribute.Contains("InitOnly", StringComparison.Ordinal) ? "SMP0302" : "SMP0102", diagnostic.Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // A return mapper creates an init-only struct, or a readonly struct's init-only member, in the initializer
    [Theory]
    [InlineData("[MapProperty(\"InitOnly.X\", nameof(Src.X))]", "InitOnly = new global::Test.Point()\n{\nX = src.X,\n},")]
    [InlineData("[MapProperty(\"Fixed.X\", nameof(Src.X))]", "Fixed = new global::Test.Fixed()\n{\nX = src.X,\n},")]
    public void InitOnlyStructIsCreatedInInitializer(string attribute, string expected)
    {
        var source = Source(attribute, returns: true);

        AssertCompiles(source);
        Assert.Contains(expected, Body(source), StringComparison.Ordinal);
    }
}
