namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The value of a constructor argument is checked and converted for the type of the parameter, which need not be
// the type of the member it assigns. It used to be the member's (new Dst(src.Value) for a string parameter, CS1503).
// The automatic mapping, [MapProperty] with its Converter, NullValue and Culture / format, and the attributes whose
// values go to the argument all go by the parameter, under the rules and diagnostics of a property of that type.
public class ConstructorParameterTypeTests
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

    private static string Lines(string source) =>
        String.Join("\n", GeneratorTestHelper.GetGeneratedSource(source).Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string attributes, string destination = TextDestination) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Globalization;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int B { get; set; } }
        public class Other { public int B { get; set; } }
        public class Src
        {
            public int Value { get; set; }
            public int? Optional { get; set; }
            public DateTime When { get; set; }
            public Child Item { get; set; } = new();
            public List<Child> Items { get; set; } = [];
            public int Count() => 3;
        }
        {{destination}}
        public static partial class M
        {
            [Mapper]
            public static partial Other MapOther(Child source);

            [Mapper]
            public static partial Child MapChild(Child source);

            [Mapper]
            {{attributes}}
            public static partial Dst Map(Src src);

            private static string Text(int value) => "#" + value.ToString(CultureInfo.InvariantCulture);
            private static int Number(int value) => value;
            private static string TextOf(Src src) => "5";
            private static int NumberOf(Src src) => 5;
        }
        """;

    // The member is an int, the parameter a string
    private const string TextDestination = "public class Dst { public Dst(string value) { Value = int.Parse(value, CultureInfo.InvariantCulture); } public int Value { get; } }";

    [Theory]
    [InlineData("", "new global::Test.Dst(global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.Value))")]
    [InlineData("[MapProperty(nameof(Dst.Value), nameof(Src.Value), Converter = nameof(Text))]", "new global::Test.Dst(Text(src.Value))")]
    [InlineData("[MapProperty(nameof(Dst.Value), nameof(Src.Optional), NullValue = \"0\")]", "new global::Test.Dst(src.Optional is not null ? global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.Optional.GetValueOrDefault()) : \"0\")")]
    [InlineData("[MapProperty(nameof(Dst.Value), nameof(Src.When), Culture = \"ja-JP\", DateTimeFormat = \"yyyy\")]", "new global::Test.Dst(global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.When, __culture_ja_JP, \"yyyy\"))")]
    [InlineData("[MapProperty(nameof(Dst.Value), \"Item.B\")]", "new global::Test.Dst(global::Smart.Mapper.DefaultValueConverter.ConvertToString(src.Item!.B))")]
    [InlineData("[MapConstant(nameof(Dst.Value), \"7\")]", "new global::Test.Dst(\"7\")")]
    [InlineData("[MapExpression(nameof(Dst.Value), \"src.Value.ToString()\")]", "static string __expression0(global::Test.Src src) => src.Value.ToString();")]
    [InlineData("[MapUsing(nameof(Dst.Value), nameof(TextOf))]", "new global::Test.Dst(TextOf(src))")]
    public void ArgumentIsConvertedToParameterType(string attributes, string expected)
    {
        var source = Source(attributes);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // A value of the member's type does not fit the parameter, as it would not fit a property of that type
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Value), nameof(Src.Value), Converter = nameof(Number))]", "SMP0105")]
    [InlineData("[MapProperty(nameof(Dst.Value), nameof(Src.Optional), NullValue = 0)]", "SMP0218")]
    [InlineData("[MapConstant(nameof(Dst.Value), 7)]", "SMP0218")]
    [InlineData("[MapUsing(nameof(Dst.Value), nameof(NumberOf))]", "SMP0202")]
    [InlineData("[MapFrom(nameof(Dst.Value), nameof(Src.Count))]", "SMP0205")]
    public void ValueOfMemberTypeEmitsDiagnostic(string attributes, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source(attributes));

        Assert.Equal(id, Assert.Single(diagnostics, static d => d.Id.StartsWith("SMP", StringComparison.Ordinal)).Id);
        Assert.DoesNotContain(diagnostics, static d => IsGenerated(d));
    }

    // [MapNested] and [MapCollection] make the value of the parameter's type before construction
    [Theory]
    [InlineData(
        "[MapNested(nameof(Dst.Item), Mapper = nameof(MapOther))]",
        "public class Dst { public Dst(Other item) { Item = new Child { B = item.B }; } public Child Item { get; } }",
        "global::Test.Other __arg0;\n__arg0 = MapOther(src.Item);\nvar __d = new global::Test.Dst(__arg0);")]
    [InlineData(
        "[MapCollection(nameof(Dst.Items), Mapper = nameof(MapChild))]",
        "public class Dst { public Dst(Child[] items) { Items = new List<Child>(items); } public List<Child> Items { get; } }",
        "global::Test.Child[] __arg0;")]
    public void MadeValueHasParameterType(string attributes, string destination, string expected)
    {
        var source = Source(attributes, destination);

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // The nullability of the parameter decides as well
    [Theory]
    [InlineData("public class Dst { public Dst(string? text) { Text = text ?? \"\"; } public string Text { get; } }", "new global::Test.Dst(src.Text);")]
    [InlineData("public class Dst { public Dst(string text) { Text = text; } public string? Text { get; } }", "new global::Test.Dst(src.Text!);")]
    public void ParameterNullabilityDecides(string destination, string expected)
    {
        var source = $$"""
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public string? Text { get; set; } }
            {{destination}}
            public static partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }

    // A parameter that takes the member's type implicitly goes by its own type as well: the value used to be made
    // for the member first, so that null reached an int? parameter as 0, and a long one as a long cut to an int
    [Theory]
    [InlineData("public class Dst { public Dst(int? count) { Count = count ?? -1; } public int Count { get; } }", "new global::Test.Dst(src.Count);")]
    [InlineData("public class Dst { public Dst(long total) { Total = (int)total; } public int Total { get; } }", "new global::Test.Dst(src.Total);")]
    public void WiderParameterTakesValueAsIs(string destination, string expected)
    {
        var source = $$"""
            #nullable enable
            using Smart.Mapper;
            namespace Test;
            public class Src { public int? Count { get; set; } public long Total { get; set; } }
            {{destination}}
            public static partial class M
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        AssertCompiles(source);
        Assert.Contains(expected, Lines(source), StringComparison.Ordinal);
    }
}
