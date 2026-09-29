namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// The [MapConstant] value and the NullValue of [MapProperty] are written as C# expressions of their own type:
// typeof for a type, an array creation for an array (which used to stop the generator, CS8785), a member or
// a cast for an enum (which used to be written as its number, CS0266), and literals spelled the same whatever
// culture the generator runs under, with NaN / infinity as the members of float / double and the characters
// of strings and chars escaped. A value that cannot be written, such as a file-local type, is reported
// (SMP0215).
public class ConstantExpressionTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    // The generator failing with an exception (CS8784 / CS8785)
    private static bool IsGeneratorFailure(Diagnostic diagnostic) =>
        diagnostic.Id is "CS8784" or "CS8785";

    private static void AssertCompiles(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && (IsGenerated(d) || IsGeneratorFailure(d))))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

        Assert.True(problems.Count == 0, String.Join("\n", problems));
    }

    private static void AssertDiagnostic(string source, string id)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
        Assert.Equal(id, diagnostic.Id);
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

    private static string Source(string attributes, string types = "") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public enum Kind { A, B, C = 5 }
        [Flags] public enum Access { None = 0, Read = 1, Write = 2, Run = 4 }
        public enum Wide : long { Min = long.MinValue, Minus = -1 }
        public enum Huge : ulong { Max = ulong.MaxValue }
        public enum Word { @class, @int }
        public class Src
        {
            public Kind? Kind { get; set; }
            public Type? Type { get; set; }
            public int[]? Numbers { get; set; }
            public double? Ratio { get; set; }
            public string? Text { get; set; }
            public char? Letter { get; set; }
        }
        public class Dst
        {
            public Kind Kind { get; set; }
            public Access Access { get; set; }
            public Wide Wide { get; set; }
            public Huge Huge { get; set; }
            public Word Word { get; set; }
            public int Number { get; set; }
            public Type? Type { get; set; }
            public object? Value { get; set; }
            public int[]? Numbers { get; set; }
            public long[]? Longs { get; set; }
            public IEnumerable<int>? Sequence { get; set; }
            public string?[]? Texts { get; set; }
            public Kind[]? Kinds { get; set; }
            public Type[]? Types { get; set; }
            public object?[]? Values { get; set; }
            public float Scale { get; set; }
            public double Ratio { get; set; }
            public string Text { get; set; } = "";
            public char Letter { get; set; }
            public char Other { get; set; }
        }
        {{types}}
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    //--------------------------------------------------------------------------------
    // typeof
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Type), typeof(string))]", "__d.Type = typeof(string);")]
    [InlineData("[MapConstant(nameof(Dst.Type), typeof(Dictionary<,>))]", "__d.Type = typeof(global::System.Collections.Generic.Dictionary<,>);")]
    [InlineData("[MapConstant(nameof(Dst.Value), typeof(List<Kind?>))]", "__d.Value = typeof(global::System.Collections.Generic.List<global::Test.Kind?>);")]
    [InlineData("[MapConstant<Type>(nameof(Dst.Type), typeof(int[]))]", "__d.Type = typeof(int[]);")]
    [InlineData("[MapProperty(nameof(Dst.Type), NullValue = typeof(Src))]", "__d.Type = src.Type ?? typeof(global::Test.Src);")]
    public void TypeIsWrittenAsTypeof(string attribute, string expected)
    {
        AssertGenerates(Source(attribute), expected);
    }

    //--------------------------------------------------------------------------------
    // Arrays
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Numbers), new[] { 1, -2 })]", "__d.Numbers = new int[] { 1, -2 };")]
    [InlineData("[MapConstant<int[]>(nameof(Dst.Numbers), new[] { 1, 2 })]", "__d.Numbers = new int[] { 1, 2 };")]
    [InlineData("[MapConstant(nameof(Dst.Numbers), new int[0])]", "__d.Numbers = new int[0];")]
    [InlineData("[MapConstant(nameof(Dst.Sequence), new[] { 1, 2 })]", "__d.Sequence = new int[] { 1, 2 };")]
    [InlineData("[MapConstant(nameof(Dst.Texts), new[] { \"a\\\"b\", null })]", "__d.Texts = new string?[] { \"a\\\"b\", null };")]
    [InlineData("[MapConstant(nameof(Dst.Kinds), new[] { Kind.B, (Kind)7 })]", "__d.Kinds = new global::Test.Kind[] { global::Test.Kind.B, (global::Test.Kind)7 };")]
    [InlineData("[MapConstant(nameof(Dst.Types), new[] { typeof(int), typeof(string) })]", "__d.Types = new global::System.Type[] { typeof(int), typeof(string) };")]
    [InlineData("[MapConstant(nameof(Dst.Values), new object?[] { 1, \"x\", null, typeof(int), Kind.B, new[] { 2 } })]", "__d.Values = new object?[] { 1, \"x\", null, typeof(int), global::Test.Kind.B, new int[] { 2 } };")]
    [InlineData("[MapProperty(nameof(Dst.Numbers), NullValue = new[] { 9 })]", "__d.Numbers = src.Numbers ?? new int[] { 9 };")]
    public void ArrayIsWrittenAsArrayCreation(string attribute, string expected)
    {
        AssertGenerates(Source(attribute), expected);
    }

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Longs), new[] { 1, 2 })]")]
    [InlineData("[MapConstant(nameof(Dst.Kinds), new[] { 1 })]")]
    [InlineData("[MapConstant(nameof(Dst.Number), new[] { 1 })]")]
    public void ArrayThatCannotBeAssignedEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute), "SMP0216");
    }

    // An array constant used to stop the generator, so that no mapper of the compilation was generated. Both
    // mappers are implemented now, as no partial method is left without its implementation (CS8795).
    [Fact]
    public void ArrayConstantKeepsOtherMappersGenerated()
    {
        var source = Source(
            "[MapConstant(nameof(Dst.Numbers), new[] { 1, 2 })]",
            """
            public static partial class Other
            {
                [Mapper]
                public static partial Src Copy(Src source);
            }
            """);

        AssertCompiles(source);
    }

    //--------------------------------------------------------------------------------
    // Enums
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Kind), Kind.B)]", "__d.Kind = global::Test.Kind.B;")]
    [InlineData("[MapConstant<Kind>(nameof(Dst.Kind), Kind.C)]", "__d.Kind = global::Test.Kind.C;")]
    [InlineData("[MapConstant(nameof(Dst.Kind), (Kind)9)]", "__d.Kind = (global::Test.Kind)9;")]
    [InlineData("[MapConstant(nameof(Dst.Access), Access.Read | Access.Run)]", "__d.Access = (global::Test.Access)5;")]
    [InlineData("[MapConstant(nameof(Dst.Access), Access.Write)]", "__d.Access = global::Test.Access.Write;")]
    [InlineData("[MapConstant(nameof(Dst.Wide), Wide.Min)]", "__d.Wide = global::Test.Wide.Min;")]
    [InlineData("[MapConstant(nameof(Dst.Wide), (Wide)(-5))]", "__d.Wide = (global::Test.Wide)(-5L);")]
    [InlineData("[MapConstant(nameof(Dst.Huge), Huge.Max)]", "__d.Huge = global::Test.Huge.Max;")]
    [InlineData("[MapConstant(nameof(Dst.Word), Word.@int)]", "__d.Word = global::Test.Word.@int;")]
    [InlineData("[MapConstant(nameof(Dst.Value), Kind.B)]", "__d.Value = global::Test.Kind.B;")]
    [InlineData("[MapProperty(nameof(Dst.Kind), NullValue = Kind.C)]", "__d.Kind = src.Kind ?? global::Test.Kind.C;")]
    public void EnumIsWrittenAsMemberOrCast(string attribute, string expected)
    {
        AssertGenerates(Source(attribute), expected);
    }

    // An enum is now checked like any other constant: it does not convert to another enum or a number.
    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Access), Kind.B)]")]
    [InlineData("[MapConstant(nameof(Dst.Number), Kind.B)]")]
    [InlineData("[MapProperty(nameof(Dst.Kind), NullValue = Access.Read)]")]
    public void EnumThatCannotBeAssignedEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute), "SMP0216");
    }

    //--------------------------------------------------------------------------------
    // Numbers
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Scale), float.NaN)]", "__d.Scale = float.NaN;")]
    [InlineData("[MapConstant(nameof(Dst.Scale), float.PositiveInfinity)]", "__d.Scale = float.PositiveInfinity;")]
    [InlineData("[MapConstant(nameof(Dst.Ratio), double.NegativeInfinity)]", "__d.Ratio = double.NegativeInfinity;")]
    [InlineData("[MapConstant(nameof(Dst.Ratio), double.NaN)]", "__d.Ratio = double.NaN;")]
    [InlineData("[MapProperty(nameof(Dst.Ratio), NullValue = double.NaN)]", "__d.Ratio = src.Ratio ?? double.NaN;")]
    [InlineData("[MapConstant(nameof(Dst.Scale), float.MaxValue)]", "__d.Scale = 3.4028235E+38f;")]
    [InlineData("[MapConstant(nameof(Dst.Ratio), 0.30000000000000004)]", "__d.Ratio = 0.30000000000000004d;")]
    [InlineData("[MapConstant(nameof(Dst.Ratio), -0.0)]", "__d.Ratio = -0d;")]
    public void FloatingPointIsWrittenToRoundTrip(string attribute, string expected)
    {
        AssertGenerates(Source(attribute), expected);
    }

    // The numbers are spelled the same under a culture with a decimal comma (de-DE) and one with a minus sign
    // of its own (sv-SE), which used to make them fail to compile or be reported as unassignable.
    [Theory]
    [InlineData("de-DE")]
    [InlineData("sv-SE")]
    public void NumbersDoNotDependOnCulture(string culture)
    {
        var source = Source(
            """
            [MapConstant(nameof(Dst.Ratio), -1234.5)]
            [MapConstant(nameof(Dst.Scale), 1.5e-7f)]
            [MapConstant(nameof(Dst.Number), -3)]
            [MapConstant(nameof(Dst.Value), -12345678L)]
            [MapConstant(nameof(Dst.Wide), (Wide)(-7))]
            """);

        var current = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            AssertGenerates(
                source,
                "__d.Ratio = -1234.5d;",
                "__d.Scale = 1.5E-07f;",
                "__d.Number = -3;",
                "__d.Value = -12345678L;",
                "__d.Wide = (global::Test.Wide)(-7L);");
        }
        finally
        {
            CultureInfo.CurrentCulture = current;
        }
    }

    //--------------------------------------------------------------------------------
    // Strings and chars
    //--------------------------------------------------------------------------------

    [Fact]
    public void StringIsEscaped()
    {
        var source = Source("[MapConstant(nameof(Dst.Text), \"q\\\"b\\\\s\\r\\n\\t\\0\\u0001\\U00002028\\U0001F600x\")]");

        AssertGenerates(source, "__d.Text = \"q\\\"b\\\\s\\r\\n\\t\\0\\u0001\\u2028\U0001F600x\";");
    }

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Letter), '\\'')]", "__d.Letter = '\\'';")]
    [InlineData("[MapConstant(nameof(Dst.Letter), '\\\\')]", "__d.Letter = '\\\\';")]
    [InlineData("[MapConstant(nameof(Dst.Letter), '\\n')]", "__d.Letter = '\\n';")]
    [InlineData("[MapConstant(nameof(Dst.Letter), '\"')]", "__d.Letter = '\"';")]
    [InlineData("[MapProperty(nameof(Dst.Letter), NullValue = '\\'')]", "__d.Letter = src.Letter ?? '\\'';")]
    [InlineData("[MapProperty(nameof(Dst.Text), NullValue = \"line1\\nline2\")]", "__d.Text = src.Text ?? \"line1\\nline2\";")]
    public void CharAndStringAreEscaped(string attribute, string expected)
    {
        AssertGenerates(Source(attribute), expected);
    }

    // The culture name and the formats the generated code passes on are escaped the same way.
    [Fact]
    public void FormatIsEscaped()
    {
        const string source = """
            using System;
            using Smart.Mapper;
            namespace Test;
            public class Src { public int Amount { get; set; } public DateTime Date { get; set; } }
            public class Dst { public string Amount { get; set; } = ""; public string Date { get; set; } = ""; }
            public static partial class M
            {
                [Mapper(Culture = "ja-JP", NumberFormat = "#,##0\"円\"", DateTimeFormat = "yyyy\\/MM")]
                public static partial Dst Map(Src src);
            }
            """;

        AssertGenerates(
            source,
            "GetCultureInfo(\"ja-JP\")",
            "__culture_ja_JP, \"#,##0\\\"円\\\"\")",
            "__culture_ja_JP, \"yyyy\\\\/MM\")");
    }

    //--------------------------------------------------------------------------------
    // Values that cannot be written
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData("[MapConstant(nameof(Dst.Type), typeof(Hidden))]")]
    [InlineData("[MapConstant(nameof(Dst.Types), new[] { typeof(int), typeof(Hidden) })]")]
    [InlineData("[MapProperty(nameof(Dst.Type), NullValue = typeof(Hidden))]")]
    public void FileLocalTypeEmitsDiagnostic(string attribute)
    {
        AssertDiagnostic(Source(attribute, "file class Hidden { }"), "SMP0215");
    }

    [Fact]
    public void ArgumentWithErrorEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(Source("[MapConstant(nameof(Dst.Type), typeof(Missing))]"));

        Assert.Contains(diagnostics, static d => d.Id == "SMP0215");
        Assert.DoesNotContain(diagnostics, IsGeneratorFailure);
    }

    // A NullValue the generated code does not write, with NullBehavior.Skip, is not checked, as before. With a
    // converter method, it is written for a null source.
    [Fact]
    public void NullValueNotWrittenIsNotChecked()
    {
        var source = Source(
            "[MapProperty(nameof(Dst.Type), NullBehavior = NullBehavior.Skip, NullValue = typeof(Hidden))]",
            "file class Hidden { }");

        AssertCompiles(source);
    }

    [Fact]
    public void NullValueWithConverterIsChecked()
    {
        var source = Source(
            "[MapProperty(nameof(Dst.Type), Converter = nameof(Convert), NullValue = typeof(Hidden))]",
            """
            file class Hidden { }
            public static partial class M
            {
                private static Type? Convert(Type? value) => value;
            }
            """);

        AssertDiagnostic(source, "SMP0215");
    }
}
