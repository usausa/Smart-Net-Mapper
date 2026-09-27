namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A C# keyword used as a name (@class) is written with its @ wherever the generated code writes a name: the
// members it reads and assigns, also along dotted paths, the members of enums in the switches that convert
// them, the methods it calls, the parameters, the mapper method, the class and the namespace (a keyword in
// which used to stop the generator, as the generated file was named with the @, CS8785). Without the @ the
// generated code did not compile.
public class KeywordIdentifierTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && (IsGenerated(d) || d.Id is "CS8784" or "CS8785")))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static void AssertGenerates(string source, params string[] expected)
    {
        AssertCompiles(source);
        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        foreach (var text in expected)
        {
            Assert.Contains(text, generated, StringComparison.Ordinal);
        }
    }

    private const string Types = """
        public enum First { @class, @int, Plain }
        public enum Second { @class, @int, Plain }
        public class Item { public int @int { get; set; } }
        public class Src
        {
            public int @class { get; set; }
            public First @enum { get; set; }
            public First @string { get; set; }
            public string @object { get; set; } = "";
            public int? @this { get; set; }
            public Item @base { get; set; } = new();
            public System.Collections.Generic.List<Item> @event { get; set; } = [];
            public Item @new { get; set; } = new();
            public int @params() => 1;
        }
        public class Dst
        {
            public int @class { get; set; }
            public Second @enum { get; set; }
            public string @string { get; set; } = "";
            public Second @object { get; set; }
            public int @this { get; set; }
            public Item @base { get; set; } = new();
            public System.Collections.Generic.List<Item> @event { get; set; } = [];
            public Item @new { get; set; } = new();
            public int @params { get; set; }
            public int @fixed { get; set; }
            public int @lock { get; set; }
            public int @ref { get; set; }
            public int @static { get; set; }
        }
        """;

    private static string Source(string mapper, string ns = "Test") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace {{ns}};
        {{Types}}
        {{mapper}}
        """;

    [Fact]
    public void KeywordMembersAreEscapedInAssignmentsAndEnumSwitches()
    {
        var source = Source("""
            public static partial class M
            {
                [Mapper]
                [MapIgnore(nameof(Dst.@base))]
                [MapIgnore(nameof(Dst.@event))]
                [MapIgnore(nameof(Dst.@new))]
                public static partial Dst Map(Src src);
            }
            """);

        AssertGenerates(
            source,
            "__d.@class = src.@class;",
            "global::Test.First.@class => global::Test.Second.@class,",
            "global::Test.First.@int => \"int\",",
            "_ => src.@string.ToString()",
            "\"class\" => global::Test.Second.@class,",
            "global::System.Enum.Parse<global::Test.Second>(src.@object)",
            "__d.@this = src.@this.GetValueOrDefault();");
    }

    [Fact]
    public void KeywordNamesAreEscapedInPathsFeaturesAndCalls()
    {
        var source = Source("""
            public static partial class M
            {
                [Mapper]
                public static partial Item MapItem(Item @int);

                [Mapper(AutoMap = false)]
                [MapProperty("base.int", "base.int", Converter = nameof(@checked))]
                [MapCondition("base.int", nameof(@if))]
                [MapCollection(nameof(Dst.@event), Mapper = nameof(MapItem))]
                [MapNested(nameof(Dst.@new), Mapper = nameof(MapItem))]
                [MapFrom(nameof(Dst.@params), nameof(Src.@params))]
                [MapConstant(nameof(Dst.@fixed), 1)]
                [MapExpression(nameof(Dst.@lock), "src.@class")]
                [MapUsing(nameof(Dst.@ref), nameof(@double))]
                [BeforeMap(nameof(@do))]
                public static partial Dst Map(Src src);

                private static int @checked(int value) => value;
                private static bool @if(int value) => true;
                private static int @double(Src s) => 2;
                private static void @do(Src s, Dst d) { }
            }
            """);

        AssertGenerates(
            source,
            "public static partial global::Test.Item MapItem(global::Test.Item @int)",
            "__d.@int = @int.@int;",
            "@do(src, __d);",
            "__d.@base ??= new global::Test.Item();",
            "if (@if(src.@base!.@int))",
            "__d.@base.@int = @checked(src.@base!.@int);",
            "__d.@fixed = 1;",
            "__d.@ref = @double(src);",
            "__d.@params = src.@params();",
            "__d.@new = MapItem(src.@new);",
            "CollectionsMarshal.AsSpan(src.@event)",
            "__d.@event = __list;");
    }

    [Fact]
    public void KeywordParameterAndMethodNamesAreEscaped()
    {
        var source = Source("""
            public class Ctx { public int V { get; set; } }
            public static partial class M
            {
                [Mapper(AutoMap = false)]
                [MapUsing(nameof(Dst.@ref), nameof(Calc))]
                [MapExpression(nameof(Dst.@lock), "@in.V")]
                public static partial void @void(Src @class, Dst @struct, Ctx @in);

                private static int Calc(Src s, Ctx c) => c.V;
            }
            """);

        AssertGenerates(
            source,
            "public static partial void @void(global::Test.Src @class, global::Test.Dst @struct, global::Test.Ctx @in)",
            "@struct.@lock = __expression0(@class, @struct, @in);",
            "@struct.@ref = Calc(@class, @in);",
            "static int __expression0(global::Test.Src @class, global::Test.Dst @struct, global::Test.Ctx @in) => @in.V;");
    }

    [Fact]
    public void KeywordClassAndTypeParameterAreEscaped()
    {
        var source = Source("""
            public static partial class @class<@int>
            {
                [Mapper(AutoMap = false)]
                [MapProperty(nameof(Dst.@class))]
                public static partial Dst Map(Src src);
            }
            """);

        AssertGenerates(source, "partial class @class<@int>");
    }

    // The namespace names the generated file without its @, which a file name cannot have
    [Fact]
    public void KeywordNamespaceIsEscaped()
    {
        var source = Source(
            """
            public static partial class M
            {
                [Mapper(AutoMap = false)]
                [MapProperty(nameof(Dst.@class))]
                public static partial Dst Map(Src src);
            }
            """,
            ns: "Test.@namespace");

        AssertGenerates(source, "namespace Test.@namespace;", "global::Test.@namespace.Dst Map(global::Test.@namespace.Src src)");
    }
}
