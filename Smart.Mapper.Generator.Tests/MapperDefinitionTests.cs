namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The declarations a mapper cannot be implemented for: in a file-local type, or one containing it, which the generated
// file cannot declare again (SMP0001, where the implementation it used to add failed, CS0759), and one returning by
// reference, which cannot return the destination it creates (SMP0002, where it used to fail on the implementation,
// CS8818). A declaration without an accessibility modifier is implemented without one, as it is declared (CS8799 with
// the private it used to be given).
public class MapperDefinitionTests
{
    private static List<string> Errors(string source) =>
        GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Select(static d => d.Id)
            .ToList();

    private const string Types =
        """
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Value { get; set; } }
        public class Dst { public int Value { get; set; } }
        """;

    [Theory]
    [InlineData("file static partial class M\n{\n    [Mapper] public static partial Dst Map(Src source);\n}")]
    [InlineData("file static partial class Outer\n{\n    public static partial class M\n    {\n        [Mapper] public static partial Dst Map(Src source);\n    }\n}")]
    public void FileLocalContainingTypeIsReported(string declarations)
    {
        var source = Types + declarations;

        // The declaration is left without an implementation, as for the other definitions SMP0001 reports
        Assert.Equal(["CS8795", "SMP0001"], Errors(source).OrderBy(static x => x, StringComparer.Ordinal));
        Assert.Contains("not file-local", GeneratorTestHelper.GetDiagnostics(source).Single().GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain("partial class", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public static partial ref Dst Map(Src source);", "ref Dst")]
    [InlineData("public static partial ref readonly Dst Map(Src source);", "ref readonly Dst")]
    public void RefReturnIsReportedAtReturnType(string declaration, string returnType)
    {
        var source = Types + "public static partial class M\n{\n    [Mapper] " + declaration + "\n}";

        Assert.Equal(["SMP0002"], Errors(source));
        var diagnostic = GeneratorTestHelper.GetDiagnostics(source).Single();
        Assert.Equal(returnType, diagnostic.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan));
    }

    // Without an accessibility modifier, a void mapper (the only kind that can be declared so) is implemented without one
    [Fact]
    public void DeclarationWithoutAccessibilityIsImplementedWithoutIt()
    {
        var source = Types +
                     "public static partial class M\n{\n    [Mapper] static partial void Map(Src source, Dst destination);\n\n" +
                     "    public static int Use() { var d = new Dst(); Map(new Src { Value = 1 }, d); return d.Value; }\n}";

        Assert.Empty(Errors(source));
        var generated = GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Contains("\n    static partial void Map(global::Test.Src source, global::Test.Dst destination)\n", generated, StringComparison.Ordinal);
        Assert.Contains("destination.Value = source.Value;", generated, StringComparison.Ordinal);
    }

    // The other modifiers the implementation has to repeat: new (CS8800) and unsafe (CS0764)
    [Theory]
    [InlineData("public static new partial Dst Map(Src source);", "public new static partial global::Test.Dst Map(global::Test.Src source)")]
    [InlineData("public static unsafe partial Dst Map(Src source);", "public unsafe static partial global::Test.Dst Map(global::Test.Src source)")]
    [InlineData("static unsafe partial void Map(Src source, Dst destination);", "unsafe static partial void Map(global::Test.Src source, global::Test.Dst destination)")]
    public void DeclarationModifiersAreRepeated(string declaration, string signature)
    {
        var source = Types +
                     "public class Base { public static Dst Map(Src source) => new(); }\n" +
                     "public partial class M : Base\n{\n    [Mapper] " + declaration + "\n}";

        // Unsafe code may not be enabled for the compilation of the test (CS0227), which the declaration has as well
        Assert.DoesNotContain(Errors(source), static id => id != "CS0227");
        Assert.Contains(signature, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
