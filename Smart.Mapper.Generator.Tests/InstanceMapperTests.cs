namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A mapper can be an instance method, whose implementation is not static. It calls the instance methods of the
// mapper class and its base classes as well as the static ones: the callbacks, the converters, the conditions, the
// [MapUsing] methods and the mappers of [MapNested] / [MapCollection], and the local function of a [MapExpression]
// is not static, so that the expression can use the instance members. The classes containing the mapper class and
// the global using static imports give static methods only, as C# calls them. A static mapper naming a method the
// mapper class has as instance methods only is reported at the attribute naming it (SMP0105).
public class InstanceMapperTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id + ": " + d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Source(string members, string declaration = "public sealed partial class M", string field = "private readonly int factor = 2;") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        public class Src { public int X { get; set; } public string Text { get; set; } = ""; public Child Item { get; set; } = new(); public List<Child> Items { get; set; } = []; }
        public class Dst { public int X { get; set; } public string Text { get; set; } = ""; public Child Item { get; set; } = new(); public List<Child> Items { get; set; } = []; public int Y { get; set; } }
        public class MapperBase { protected int Offset { get; set; } = 1; protected int AddOffset(int value) => value + Offset; }
        {{declaration}}
        {
            {{field}}

            {{members}}
        }
        """;

    [Fact]
    public void InstanceMapperIsImplementedAsInstanceMethod()
    {
        var (generated, problems) = Build(Source("[Mapper] public partial Dst Map(Src src);\n    [Mapper] internal partial void Fill(Src src, Dst dst);"));

        Assert.Empty(problems);
        Assert.Contains("public partial global::Test.Dst Map(global::Test.Src src)\n", generated, StringComparison.Ordinal);
        Assert.Contains("internal partial void Fill(global::Test.Src src, global::Test.Dst dst)\n", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("static partial", generated, StringComparison.Ordinal);
    }

    [Theory]
    // The callbacks
    [InlineData("[Mapper] [BeforeMap(nameof(Before))] public partial void Map(Src src, Dst dst); private void Before(Src src, Dst dst) => dst.Y = factor;", "Before(src, dst);")]
    [InlineData("[Mapper] [AfterMap(nameof(After))] public partial Dst Map(Src src); private void After(Src src, Dst dst) => dst.Y = src.X * factor;", "After(src, __d);")]
    // A converter, a condition and a [MapUsing] method
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(Twice))] public partial Dst Map(Src src); private int Twice(int value) => value * factor;", "Twice(src.X)")]
    [InlineData("[Mapper] [MapCondition(nameof(Dst.Text), nameof(HasText))] public partial Dst Map(Src src); private bool HasText(string text) => text.Length > factor;", "HasText(src.Text)")]
    [InlineData("[Mapper] [MapUsing(nameof(Dst.Y), nameof(Calc))] public partial Dst Map(Src src); private int Calc(Src src) => src.X * factor;", "Calc(src)")]
    // The mappers of [MapNested] and [MapCollection]
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), Mapper = nameof(MapChild))] public partial Dst Map(Src src); [Mapper] public partial Child MapChild(Child child);", "MapChild(src.Item)")]
    [InlineData("[Mapper] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapChild))] public partial Dst Map(Src src); [Mapper] public partial Child MapChild(Child child);", "MapChild(")]
    // A static method is called as well
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(Triple))] public partial Dst Map(Src src); private static int Triple(int value) => value * 3;", "Triple(src.X)")]
    // An expression using the instance members, in a local function that is not static
    [InlineData("[Mapper] [MapExpression(nameof(Dst.Y), \"src.X * factor\")] public partial Dst Map(Src src);", " int __expression0(global::Test.Src src) => src.X * factor;")]
    public void InstanceMethodsOfMapperClassAreCalled(string members, string expected)
    {
        var (generated, problems) = Build(Source(members));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("static int __expression0", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void InstanceMethodsOfBaseClassAreCalled()
    {
        var source = Source(
            "[Mapper] [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(AddOffset))] public partial Dst Map(Src src);",
            "public sealed partial class M : MapperBase");

        var (generated, problems) = Build(source);

        Assert.Empty(problems);
        Assert.Contains("AddOffset(src.X)", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void InstanceMapperOfStructIsImplemented()
    {
        var source = Source(
            "[Mapper] [AfterMap(nameof(After))] public partial Dst Map(Src src); private readonly void After(Src src, Dst dst) => dst.Y = factor;",
            "public partial struct M",
            "private readonly int factor; public M(int factor) { this.factor = factor; }");

        var (generated, problems) = Build(source);

        Assert.Empty(problems);
        Assert.Contains("public partial global::Test.Dst Map(global::Test.Src src)\n", generated, StringComparison.Ordinal);
    }

    // A nullable return gets [return: NotNullIfNotNull], which an instance mapper calling it relies on as well
    [Fact]
    public void NullableInstanceMapperDeclaresNotNullIfNotNull()
    {
        var source = Source("[Mapper] [MapNested(nameof(Dst.Item), Mapper = nameof(MapChild))] public partial Dst? Map(Src? src); [Mapper] public partial Child? MapChild(Child? child);");

        var (generated, problems) = Build(source);

        Assert.Empty(problems);
        Assert.Contains("[return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(\"src\")]", generated, StringComparison.Ordinal);
    }

    // The class containing the mapper class gives its static methods, as C# calls them, and not its instance ones
    [Fact]
    public void ContainingClassGivesStaticMethodsOnly()
    {
        const string source = """
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int X { get; set; } }
            public class Dst { public int X { get; set; } public int Y { get; set; } }
            public partial class Outer
            {
                private static int Twice(int value) => value * 2;
                private void After(Src src, Dst dst) { }

                public sealed partial class M
                {
                    [Mapper]
                    [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(Twice))]
                    public partial Dst Map(Src src);

                    [Mapper]
                    [AfterMap(nameof(After))]
                    public partial Dst MapAfter(Src src);
                }
            }
            """;

        var (generated, problems) = Build(source);

        Assert.Contains("Twice(src.X)", generated, StringComparison.Ordinal);
        Assert.Contains(problems, static p => p.StartsWith("SMP0107", StringComparison.Ordinal));
        Assert.DoesNotContain(problems, static p => p.StartsWith("SMP0105", StringComparison.Ordinal));
    }

    // A mapper reported with an error is implemented as it is declared, not static
    [Fact]
    public void InstanceMapperWithErrorGetsInstancePlaceholder()
    {
        var (generated, problems) = Build(Source("[Mapper] [AfterMap(\"Missing\")] public partial Dst Map(Src src);"));

        Assert.Equal("SMP0107", Assert.Single(problems).Split(':')[0]);
        Assert.Contains("public partial global::Test.Dst Map(global::Test.Src src)\n", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[Mapper] [BeforeMap(nameof(Before))] /*here*/\n    public static partial Dst Map(Src src); private void Before(Src src, Dst dst) { }")]
    [InlineData("[Mapper] [AfterMap(nameof(After))] /*here*/\n    public static partial Dst Map(Src src); private void After(Src src, Dst dst) { }")]
    [InlineData("[Mapper] [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(Twice))] /*here*/\n    public static partial Dst Map(Src src); private int Twice(int value) => value * 2;")]
    [InlineData("[Mapper] [MapCondition(nameof(Dst.Text), nameof(HasText))] /*here*/\n    public static partial Dst Map(Src src); private bool HasText(string text) => true;")]
    [InlineData("[Mapper] [MapUsing(nameof(Dst.Y), nameof(Calc))] /*here*/\n    public static partial Dst Map(Src src); private int Calc(Src src) => 1;")]
    [InlineData("[Mapper] [MapNested(nameof(Dst.Item), Mapper = nameof(MapChild))] /*here*/\n    public static partial Dst Map(Src src); [Mapper] public partial Child MapChild(Child child);")]
    [InlineData("[Mapper] [MapCollection(nameof(Dst.Items), Mapper = nameof(MapChild))] /*here*/\n    public static partial Dst Map(Src src); [Mapper] public partial Child MapChild(Child child);")]
    public void StaticMapperNamingInstanceMethodIsReported(string members)
    {
        var source = Source(members);

        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(source), static d => d.Id == "SMP0105");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        var markedLine = Array.FindIndex(source.Split('\n'), static line => line.Contains("/*here*/", StringComparison.Ordinal));
        Assert.Equal(markedLine, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
    }

    // A method of the class hiding a static one of a base class is the one the name finds, an instance one here, which a
    // static mapper cannot call (a converter in MethodLookupTests)
    [Fact]
    public void StaticMapperNamingInstanceMethodHidingStaticOneIsReported()
    {
        var source = Source(
                         "[Mapper] [MapCondition(nameof(Dst.Text), nameof(HasText))] public static partial Dst Map(Src src); private new bool HasText(string text) => true;",
                         "public sealed partial class M : StaticBase") +
                     "\npublic class StaticBase { protected static bool HasText(string text) => false; }";

        var (_, problems) = Build(source);

        Assert.Equal("SMP0105", Assert.Single(problems).Split(':')[0]);
    }

    // A collection converter takes the element mapper as a delegate, which an instance method of a ref struct makes none
    // of; the loop calls it
    [Theory]
    [InlineData("[CollectionConverter(typeof(DefaultCollectionConverter))]", "private ChildDto MapChild(Child child) => new() { V = child.V };", "SMP0213")]
    [InlineData("[CollectionConverter(typeof(DefaultCollectionConverter))]", "private static ChildDto MapChild(Child child) => new() { V = child.V };", "")]
    [InlineData("", "private ChildDto MapChild(Child child) => new() { V = child.V };", "")]
    public void InstanceElementMapperOfRefStructIsNotHandedAsDelegate(string converter, string mapChild, string error)
    {
        var source = $$"""
            #nullable enable
            using System.Collections.Generic;
            using Smart.Mapper;
            namespace Test;
            public class Child { public int V { get; set; } }
            public class ChildDto { public int V { get; set; } }
            public class Src { public List<Child> Items { get; set; } = []; }
            public class Dst { public List<ChildDto> Items { get; set; } = []; }
            public ref partial struct M
            {
                [Mapper]
                {{converter}}
                [MapCollection(nameof(Dst.Items), Mapper = nameof(MapChild))]
                public partial Dst Map(Src src);

                {{mapChild}}
            }
            """;

        var (_, problems) = Build(source);

        Assert.Equal(error, String.Join(",", problems.Select(static p => p.Split(':')[0])));
    }

    // The implementation repeats the modifiers the declaration has to match (CS8800, CS8663): virtual, override, sealed
    // and readonly, and so does the one of a mapper reported with an error
    [Theory]
    [InlineData("public partial class M : Base", "[Mapper] public override partial Dst Map(Src src);", "public override partial global::Test.Dst Map(global::Test.Src src)\n", "")]
    [InlineData("public partial class M : Base", "[Mapper] public sealed override partial Dst Map(Src src);", "public sealed override partial global::Test.Dst Map(global::Test.Src src)\n", "")]
    [InlineData("public partial struct M", "[Mapper] public readonly partial Dst Map(Src src);", "public readonly partial global::Test.Dst Map(global::Test.Src src)\n", "")]
    [InlineData("public partial class M : Base", "[Mapper] [AfterMap(\"Missing\")] public override partial Dst Map(Src src);", "public override partial global::Test.Dst Map(global::Test.Src src)\n", "SMP0107")]
    [InlineData("public partial struct M", "[Mapper] [AfterMap(\"Missing\")] public readonly partial Dst Map(Src src);", "public readonly partial global::Test.Dst Map(global::Test.Src src)\n", "SMP0107")]
    public void DeclarationModifiersAreRepeated(string declaration, string members, string expected, string error)
    {
        var source = $$"""
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int X { get; set; } }
            public class Dst { public int X { get; set; } }
            public partial class Base
            {
                [Mapper] public virtual partial Dst Map(Src src);
            }
            {{declaration}}
            {
                {{members}}
            }
            """;

        var (_, problems) = Build(source);
        var generated = String.Concat(GeneratorTestHelper.Run(source).GeneratedSources.Values).Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Equal(error, String.Join(",", problems.Select(static p => p.Split(':')[0])));
        Assert.Contains("public virtual partial global::Test.Dst Map(global::Test.Src src)\n", generated, StringComparison.Ordinal);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A static overload of the name is matched as before, the static mapper not reporting the instance ones
    [Fact]
    public void StaticMapperWithStaticOverloadIsNotReported()
    {
        var source = Source("[Mapper] [MapProperty(nameof(Dst.Y), nameof(Src.X), Converter = nameof(Twice))] public static partial Dst Map(Src src); private static int Twice(int value) => value * 2; private long Twice(long value) => value * 2;");

        var (generated, problems) = Build(source);

        Assert.Empty(problems);
        Assert.Contains("Twice(src.X)", generated, StringComparison.Ordinal);
    }
}
