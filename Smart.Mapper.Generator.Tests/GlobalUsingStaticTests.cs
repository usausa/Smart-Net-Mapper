namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// After the classes, a method the attributes name is looked up in the types the global using static directives import,
// which the generated file sees as well, so that a converter shared across mappers can live in a class of its own. A
// using static directive of one file only is not seen from the generated file and is not looked in. As in C#, the
// methods declared in the imported types are imported (not those they inherit, nor extension methods), and a class
// having a member of the name the call can invoke goes before them.
public class GlobalUsingStaticTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Source(string usings, string attributes, string members = "") =>
        $$"""
        {{usings}}
        #nullable enable
        using Smart.Mapper;
        namespace Test
        {
            public class Src { public string Name { get; set; } = ""; public int Count { get; set; } }
            public class Dst { public string Name { get; set; } = ""; public string Count { get; set; } = ""; }
            public static partial class M
            {
                [Mapper(AutoMap = false)]
                {{attributes}}
                public static partial void Map(Src src, Dst dst);

                {{members}}
            }
        }
        namespace Shared
        {
            public class HelperBase
            {
                public static string Inherited(string value) => value;
            }

            public class Helpers : HelperBase
            {
                public static string Upper(string value) => value.ToUpperInvariant();
                public static string Show(int value) => "int";
                public static void Touch(object source, object destination) { }
            }

            public static class MoreHelpers
            {
                public static int Show(long value) => 0;
                public static string Tail(this string value) => value;
            }
        }
        """;

    private const string Global = "global using static Shared.Helpers; global using static Shared.MoreHelpers;";

    // A converter, and a callback, of a type a global using static directive imports
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name), Converter = \"Upper\")]", "dst.Name = Upper(src.Name);")]
    [InlineData("[MapProperty(nameof(Dst.Count), Converter = \"Show\")]", "dst.Count = Show(src.Count);")]
    [InlineData("[AfterMap(\"Touch\")]", "Touch(src, dst);")]
    public void MethodOfGlobalUsingStaticIsFound(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(Global, attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A using static directive of one file, a method the imported type inherits, an extension method, and a method of
    // the name in the mapper class, which goes first, are not taken
    [Theory]
    [InlineData("using static Shared.Helpers;", "[MapProperty(nameof(Dst.Name), Converter = \"Upper\")]", "")]
    [InlineData(Global, "[MapProperty(nameof(Dst.Name), Converter = \"Inherited\")]", "")]
    [InlineData(Global, "[MapProperty(nameof(Dst.Name), Converter = \"Tail\")]", "")]
    [InlineData(Global, "[MapProperty(nameof(Dst.Name), Converter = \"Upper\")]", "private static string Upper(int value) => \"\";")]
    public void OtherMethodIsNotTaken(string usings, string attributes, string members)
    {
        var (_, problems) = Build(Source(usings, attributes, members));

        Assert.Equal(["SMP0110"], problems);
    }
}
