namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A converter, a condition and a [MapUsing] method take the value by an implicit conversion as well, where only a
// parameter of the type of the value matched (SMP0110 / SMP0112 / SMP0201): to a base class or an interface (a
// converter taking IEnumerable<string> for a List<string>), boxing, a wider number, a nullable struct, or a
// user-defined implicit conversion, to a parameter taken by value; a parameter by in takes the type of the value only.
// A nullable struct goes to a method taking the struct it holds, or a type that struct converts to implicitly, as its
// Value, as a value only, as to a parameter not taking null: a converter gives NullValue for null, or leaves the target
// as it is without one (a constructor argument or an object initializer entry gets null or default), and a condition is
// not met; a method taking the struct itself, then one taking the nullable struct by a conversion, go first. The method
// is the one the call binds to, as C# binds it: one taking the type of the value goes first, and otherwise the one
// whose parameter is the most specific; one taking the type of the value the call does not bind to gives way to the one
// it binds to. An ambiguous call, one binding to a method taking the value by an explicit conversion or one obsolete as
// an error, and one binding to a method not matched (generic, with optional parameters or params, obsolete as an error)
// match nothing, and are reported as before, without an error in the generated code.
public class ParameterConversionTests
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

    private static string Lines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Source(string attributes, string methods, string mapper = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public interface IHasName { string Name { get; } }
        public interface IAudited { DateTime UpdatedAt { get; } }
        public abstract class EntityBase { public int Id { get; set; } }
        public sealed class Person : EntityBase, IHasName, IAudited { public string Name { get; set; } = ""; public DateTime UpdatedAt { get; set; } }
        public sealed class Company { public string Title { get; set; } = ""; }
        public sealed class Money : IFormattable
        {
            public decimal Amount { get; set; }
            public static implicit operator decimal(Money value) => value.Amount;
            public string ToString(string? format, IFormatProvider? formatProvider) => Amount.ToString(format, formatProvider);
        }
        public sealed class Context { public string Prefix { get; set; } = ""; }
        public sealed class Legacy
        {
            [Obsolete("", true)]
            public static implicit operator string(Legacy value) => "";
        }
        public struct Point { public int X { get; set; } }
        public readonly struct Code
        {
            public int Value { get; init; }
            public static implicit operator int(Code value) => value.Value;
        }
        public readonly struct OldCode
        {
            [Obsolete("", true)]
            public static implicit operator int(OldCode value) => 0;
        }
        public class Src
        {
            public Code? Tag { get; set; }
            public OldCode? OldTag { get; set; }
            public Person Owner { get; set; } = new();
            public Person? Manager { get; set; }
            public EntityBase Entity { get; set; } = new Person();
            public List<string> Tags { get; set; } = new();
            public Money Price { get; set; } = new();
            public Money? Cash { get; set; }
            public Legacy Old { get; set; } = new();
            public int? Quantity { get; set; }
            public int Count { get; set; }
            public DateTime When { get; set; }
        }
        public class Dst { public string Text { get; set; } = ""; public string? Note { get; set; } public int? Quantity { get; set; } public List<string> Tags { get; set; } = new(); }
        public record DstRecord(string Text, string? Note);
        public class InitDst { public required string Text { get; init; } public string? Note { get; init; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            {{methods}}
        }
        """;

    // A base class or an interface the value converts to by an implicit reference conversion
    [Theory]
    [InlineData(
        "[MapUsing(nameof(Dst.Text), nameof(Label))]",
        "private static string Label(IHasName x) => x.Name;",
        "public static partial void Map(Person src, Dst dst);",
        "dst.Text = Label(src);")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Entity), Converter = nameof(Describe))]",
        "private static string Describe(object x) => x.ToString() ?? \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "dst.Text = Describe(src.Entity);")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Tags), Converter = nameof(Join))]",
        "private static string Join(IEnumerable<string> values) => string.Join(\",\", values);",
        "public static partial void Map(Src src, Dst dst);",
        "dst.Text = Join(src.Tags);")]
    [InlineData(
        "[MapCondition(nameof(Dst.Tags), nameof(HasAny))] [MapProperty(nameof(Dst.Tags))]",
        "private static bool HasAny(IReadOnlyCollection<string> values) => values.Count > 0;",
        "public static partial void Map(Src src, Dst dst);",
        "if (HasAny(src.Tags))")]
    public void ReferenceConversionIsTaken(string attributes, string methods, string mapper, string expected)
    {
        var (generated, problems) = Build(Source(attributes, methods, mapper));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // Another implicit conversion: boxing, a wider number, a nullable struct, and a user-defined one, which the call
    // binds to over an interface the class implements
    [Theory]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.When), Converter = nameof(Show))]",
        "private static string Show(IFormattable x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "dst.Text = Show(src.When);")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]",
        "private static string Show(object x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "dst.Text = Show(src.Count);")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]",
        "private static string Show(long x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "dst.Text = Show(src.Count);")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]",
        "private static string Show(int? x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "dst.Text = Show(src.Count);")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Price), Converter = nameof(Show))]",
        "private static string Show(decimal x) => \"\"; private static int Show(IFormattable x) => 0;",
        "public static partial void Map(Src src, Dst dst);",
        "dst.Text = Show(src.Price);")]
    [InlineData(
        "[MapCondition(nameof(Dst.Text), nameof(Check))] [MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]",
        "private static bool Check(double x) => x > 0; private static string Show(long x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "if (Check(src.Count))")]
    [InlineData(
        "[MapUsing(nameof(Dst.Text), nameof(Label))]",
        "private static string Label(object x) => \"\";",
        "public static partial void Map(Point src, Dst dst);",
        "dst.Text = Label(src);")]
    public void OtherImplicitConversionIsTaken(string attributes, string methods, string mapper, string expected)
    {
        var (generated, problems) = Build(Source(attributes, methods, mapper));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // A nullable source goes through a conversion as it is, to a parameter taking null, and for a value only to one
    // that does not, or through a conversion operator whose parameter does not
    [Fact]
    public void NullableSourceGoesThroughConversion()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Quantity), Converter = nameof(Show))] " +
            "[MapProperty(nameof(Dst.Note), nameof(Src.Quantity), Converter = nameof(ShowOrNull))] " +
            "[MapProperty(nameof(Dst.Tags), nameof(Src.Cash), Converter = nameof(Split))]",
            "private static string Show(object x) => x.ToString() ?? \"\"; private static string? ShowOrNull(long? x) => x?.ToString(); " +
            "private static List<string> Split(decimal x) => new();"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    if (src.Quantity is not null)
                    {
                        dst.Text = Show(src.Quantity);
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains("dst.Note = ShowOrNull(src.Quantity);", generated, StringComparison.Ordinal);
        Assert.Contains(
            Lines("""
                    if (src.Cash is not null)
                    {
                        dst.Tags = Split(src.Cash);
                    }
            """),
            generated,
            StringComparison.Ordinal);
    }

    // With the custom parameters, which go to parameters of their own types as before
    [Fact]
    public void ReferenceConversionTakesCustomParameters()
    {
        const string methods =
            "private static string Label(IHasName x) => x.Name; private static string Label(IHasName x, Context context) => context.Prefix + x.Name; " +
            "private static bool Check(IHasName x, Context context) => true;";
        var (usingGenerated, usingProblems) = Build(Source(
            "[MapUsing(nameof(Dst.Text), nameof(Label))]",
            methods,
            "public static partial void Map(Person src, Dst dst, Context context);"));

        Assert.Empty(usingProblems);
        Assert.Contains("dst.Text = Label(src, context);", usingGenerated, StringComparison.Ordinal);

        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Label))] [MapCondition(nameof(Dst.Text), nameof(Check))]",
            methods,
            "public static partial void Map(Src src, Dst dst, Context context);"));

        Assert.Empty(problems);
        Assert.Contains("if (Check(src.Owner, context))", generated, StringComparison.Ordinal);
        Assert.Contains("dst.Text = Label(src.Owner, context);", generated, StringComparison.Ordinal);
    }

    // A nullable source goes to a parameter not annotated as nullable for a value only
    [Fact]
    public void NullableReferenceIsPassedForValueOnly()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Manager), Converter = nameof(Label))] " +
            "[MapProperty(nameof(Dst.Note), nameof(Src.Manager), Converter = nameof(Label), NullValue = \"none\")]",
            "private static string Label(IHasName x) => x.Name;"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    if (src.Manager is not null)
                    {
                        dst.Text = Label(src.Manager);
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains("dst.Note = src.Manager is not null ? Label(src.Manager) : \"none\";", generated, StringComparison.Ordinal);
    }

    // A nullable struct goes as its Value, for a value only: NullValue, or the target left as it is, and the condition
    // not met
    [Fact]
    public void NullableStructGoesAsValue()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Quantity), Converter = nameof(Format))] " +
            "[MapProperty(nameof(Dst.Note), nameof(Src.Quantity), Converter = nameof(Format), NullValue = \"-\")] " +
            "[MapCondition(nameof(Dst.Quantity), nameof(IsPositive))] [MapProperty(nameof(Dst.Quantity))]",
            "private static string Format(int value) => value.ToString(); private static bool IsPositive(int value) => value > 0;"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    if (src.Quantity is not null)
                    {
                        dst.Text = Format(src.Quantity.Value);
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains("dst.Note = src.Quantity is not null ? Format(src.Quantity.Value) : \"-\";", generated, StringComparison.Ordinal);
        Assert.Contains("if (src.Quantity is not null && IsPositive(src.Quantity.Value))", generated, StringComparison.Ordinal);
    }

    // A constructor argument and an object initializer entry take null for a target taking it, or default, and the
    // custom parameters follow the value
    [Fact]
    public void NullableStructGoesAsValueInExpressions()
    {
        var (record, recordProblems) = Build(Source(
            "[MapProperty(nameof(DstRecord.Text), nameof(Src.Quantity), Converter = nameof(Format))] [MapProperty(nameof(DstRecord.Note), nameof(Src.Quantity), Converter = nameof(Format))]",
            "private static string Format(int value, Context context) => context.Prefix + value;",
            "public static partial DstRecord Map(Src src, Context context);"));

        Assert.Empty(recordProblems);
        Assert.Contains(
            "new global::Test.DstRecord(src.Quantity is not null ? Format(src.Quantity.Value, context) : default!, src.Quantity is not null ? Format(src.Quantity.Value, context) : null)",
            record,
            StringComparison.Ordinal);

        var (init, initProblems) = Build(Source(
            "[MapProperty(nameof(InitDst.Text), nameof(Src.Quantity), Converter = nameof(Format), NullValue = \"-\")] [MapProperty(nameof(InitDst.Note), nameof(Src.Quantity), Converter = nameof(Format))]",
            "private static string Format(int value) => value.ToString();",
            "public static partial InitDst Map(Src src);"));

        Assert.Empty(initProblems);
        Assert.Contains("Text = src.Quantity is not null ? Format(src.Quantity.Value) : \"-\",", init, StringComparison.Ordinal);
        Assert.Contains("Note = src.Quantity is not null ? Format(src.Quantity.Value) : null,", init, StringComparison.Ordinal);
    }

    // One taking the type of the value goes first: the nullable struct itself over the value it holds, and the class
    // over its base class (which returns another type here, reported if it were chosen)
    [Theory]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Quantity), Converter = nameof(Format))]",
        "private static string Format(int? value) => value?.ToString() ?? \"\"; private static string Format(int value) => value.ToString();",
        "dst.Text = Format(src.Quantity);")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Describe))]",
        "private static string Describe(Person? x) => \"person\"; private static int Describe(EntityBase x) => 0;",
        "dst.Text = Describe(src.Owner);")]
    public void ExactMatchGoesFirst(string attributes, string methods, string expected)
    {
        var (generated, problems) = Build(Source(attributes, methods));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // Otherwise the most specific parameter: here the one taking null, whose call has no null check
    [Fact]
    public void MostSpecificParameterIsChosen()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Manager), Converter = nameof(Describe))]",
            "private static string Describe(EntityBase? x) => \"entity\"; private static string Describe(object x) => \"object\"; private static string Describe(Company x) => x.Title;"));

        Assert.Empty(problems);
        Assert.Contains("dst.Text = Describe(src.Manager);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.Manager is not null", generated, StringComparison.Ordinal);
    }

    // An ambiguous call matches nothing
    [Theory]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Show))]",
        "private static string Show(IHasName x) => x.Name; private static string Show(IAudited x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0110")]
    [InlineData(
        "[MapCondition(nameof(Dst.Note), nameof(Check))] [MapProperty(nameof(Dst.Note), nameof(Src.Owner), Converter = nameof(Show))]",
        "private static bool Check(IHasName x) => true; private static bool Check(IAudited x) => true; private static string Show(Person x) => x.Name;",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0112")]
    [InlineData(
        "[MapUsing(nameof(Dst.Text), nameof(Label))]",
        "private static string Label(IHasName x) => x.Name; private static string Label(IAudited x) => \"\";",
        "public static partial void Map(Person src, Dst dst);",
        "SMP0201")]
    public void AmbiguousCallIsReported(string attributes, string methods, string mapper, string expected)
    {
        var (_, problems) = Build(Source(attributes, methods, mapper));

        Assert.Equal([expected], problems);
    }

    // The method the call binds to returns another type: the exact one, or the most specific one
    [Theory]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Show))]",
        "private static int Show(Person x) => 0; private static string Show(IHasName x) => x.Name;",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0111")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Show))]",
        "private static int Show(EntityBase x) => 0; private static string Show(object x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0111")]
    [InlineData(
        "[MapCondition(nameof(Dst.Note), nameof(Check))] [MapProperty(nameof(Dst.Note), nameof(Src.Owner), Converter = nameof(Show))]",
        "private static int Check(IHasName x) => 0; private static bool Check(object x) => true; private static string Show(Person x) => x.Name;",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0112")]
    [InlineData(
        "[MapUsing(nameof(Dst.Text), nameof(Label))]",
        "private static int Label(IHasName x) => 0; private static string Label(object x) => \"\";",
        "public static partial void Map(Person src, Dst dst);",
        "SMP0202")]
    public void BoundMethodReturningAnotherTypeIsReported(string attributes, string methods, string mapper, string expected)
    {
        var (_, problems) = Build(Source(attributes, methods, mapper));

        Assert.Equal([expected], problems);
    }

    // An explicit conversion (a base class to a derived one), and a user-defined one obsolete as an error, are not
    // taken
    [Theory]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Entity), Converter = nameof(Show))]",
        "private static string Show(Person x) => x.Name;",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0110")]
    [InlineData(
        "[MapCondition(nameof(Dst.Text), nameof(Check))] [MapProperty(nameof(Dst.Text), nameof(Src.Entity), Converter = nameof(Show))]",
        "private static bool Check(Person x) => true; private static string Show(EntityBase x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0112")]
    [InlineData(
        "[MapUsing(nameof(Dst.Text), nameof(Label))]",
        "private static string Label(Person x) => x.Name;",
        "public static partial void Map(EntityBase src, Dst dst);",
        "SMP0201")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Old), Converter = nameof(Show))]",
        "private static string Show(string x) => x;",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0110")]
    public void ExplicitConversionIsNotTaken(string attributes, string methods, string mapper, string expected)
    {
        var (_, problems) = Build(Source(attributes, methods, mapper));

        Assert.Equal([expected], problems);
    }

    // A method not matched that the call may bind to over the one taking the value by a reference conversion: generic,
    // taking the value with an optional parameter or params, custom parameters of other types, or obsolete as an error
    [Theory]
    [InlineData("private static string Show<T>(T x) => \"\";")]
    [InlineData("private static string Show(Person x, bool full = false) => \"\";")]
    [InlineData("private static string Show(params Person[] x) => \"\";")]
    [InlineData("private static string Show(params ReadOnlySpan<Person> x) => \"\";")]
    [InlineData("[Obsolete(\"\", true)] private static string Show(Person x) => \"\";")]
    public void MethodTheCallMayBindToIsReported(string other)
    {
        var (_, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Show))]",
            "private static string Show(IHasName x) => x.Name; " + other));

        Assert.Equal(["SMP0110"], problems);
    }

    // A method the call does not bind to over the one taking the value by a reference conversion leaves the match as it
    // is: params of a less specific type, a generic one the value does not give its type argument, and one with an
    // optional parameter of another type
    [Theory]
    [InlineData("private static string Show(params object[] x) => \"\";")]
    [InlineData("private static string Show<T>(IEnumerable<T> x) => \"\";")]
    [InlineData("private static string Show<T>(Person x) => \"\";")]
    [InlineData("private static string Show(Company x, bool full = false) => x.Title;")]
    public void MethodTheCallDoesNotBindToDoesNotMatter(string other)
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Show))]",
            "private static string Show(IHasName x) => x.Name; " + other));

        Assert.Empty(problems);
        Assert.Contains("dst.Text = Show(src.Owner);", generated, StringComparison.Ordinal);
    }

    // Methods of other types, of other numbers of parameters, and of other custom parameters that do not take the value
    // leave the match as it is; of the same types, the one taking by value goes before the one taking by in
    [Fact]
    public void UnrelatedOverloadsDoNotMatter()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Show))] [MapCondition(nameof(Dst.Text), nameof(Check))]",
            "private static string Show(IHasName x, Context context) => context.Prefix + x.Name; private static int Show(IHasName x, in Context context) => 0; " +
            "private static string Show(Company x, Context context) => x.Title; private static string Show(Company x) => x.Title; " +
            "private static bool Check(IHasName x) => true; private static bool Check(Company x) => true; private static bool Check(IHasName x, int a, int b) => true;",
            "public static partial void Map(Src src, Dst dst, Context context);"));

        Assert.Empty(problems);
        Assert.Contains("if (Check(src.Owner))", generated, StringComparison.Ordinal);
        Assert.Contains("dst.Text = Show(src.Owner, context);", generated, StringComparison.Ordinal);
    }

    // A parameter by in takes the type of the value only, or the struct a nullable struct holds
    [Theory]
    [InlineData(
        "[MapUsing(nameof(Dst.Text), nameof(Label))]",
        "private static string Label(in IHasName x) => x.Name;",
        "public static partial void Map(in Person src, Dst dst);",
        "SMP0201")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Owner), Converter = nameof(Show))]",
        "private static string Show(in IHasName x) => x.Name;",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0110")]
    [InlineData(
        "[MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]",
        "private static string Show(in long x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0110")]
    [InlineData(
        "[MapCondition(nameof(Dst.Text), nameof(Check))] [MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]",
        "private static bool Check(in object x) => true; private static string Show(int x) => \"\";",
        "public static partial void Map(Src src, Dst dst);",
        "SMP0112")]
    public void InParameterTakesOwnTypeOnly(string attributes, string methods, string mapper, string expected)
    {
        var (_, problems) = Build(Source(attributes, methods, mapper));

        Assert.Equal([expected], problems);
    }

    [Fact]
    public void InParameterTakesValueOfNullableStruct()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Quantity), Converter = nameof(Show))]",
            "private static string Show(in int x) => \"\";"));

        Assert.Empty(problems);
        Assert.Contains("dst.Text = Show(src.Quantity.Value);", generated, StringComparison.Ordinal);
    }

    // The value a nullable struct holds goes, as its Value after a null check, to a method taking a type it converts to
    // implicitly (a wider number, a user-defined conversion): the target is left as it is, or takes NullValue, a
    // constructor argument null or default, and the condition is not met
    [Fact]
    public void ValueOfNullableStructGoesThroughConversion()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Quantity), Converter = nameof(Show))] " +
            "[MapProperty(nameof(Dst.Note), nameof(Src.Tag), Converter = nameof(Show), NullValue = \"-\")] " +
            "[MapCondition(nameof(Dst.Quantity), nameof(IsPositive))] [MapProperty(nameof(Dst.Quantity))]",
            "private static string Show(long x) => \"\"; private static bool IsPositive(double x) => x > 0;"));

        Assert.Empty(problems);
        Assert.Contains(
            Lines("""
                    if (src.Quantity is not null)
                    {
                        dst.Text = Show(src.Quantity.Value);
                    }
            """),
            generated,
            StringComparison.Ordinal);
        Assert.Contains("dst.Note = src.Tag is not null ? Show(src.Tag.Value) : \"-\";", generated, StringComparison.Ordinal);
        Assert.Contains("if (src.Quantity is not null && IsPositive(src.Quantity.Value))", generated, StringComparison.Ordinal);

        var (record, recordProblems) = Build(Source(
            "[MapProperty(nameof(DstRecord.Text), nameof(Src.Quantity), Converter = nameof(Show))] [MapProperty(nameof(DstRecord.Note), nameof(Src.Quantity), Converter = nameof(Show))]",
            "private static string Show(double x) => \"\";",
            "public static partial DstRecord Map(Src src);"));

        Assert.Empty(recordProblems);
        Assert.Contains(
            "new global::Test.DstRecord(src.Quantity is not null ? Show(src.Quantity.Value) : default!, src.Quantity is not null ? Show(src.Quantity.Value) : null)",
            record,
            StringComparison.Ordinal);
    }

    // A method taking the struct itself goes first, then one taking the nullable struct by a conversion; the value it
    // holds goes by a conversion to one of the others only (here the one returning another type would be reported)
    [Theory]
    [InlineData("private static string Show(int x) => \"\"; private static int Show(long x) => 0;", "dst.Text = Show(src.Quantity.Value);")]
    [InlineData("private static string Show(long? x) => \"\"; private static int Show(long x) => 0;", "dst.Text = Show(src.Quantity);")]
    public void StructItselfAndNullableStructGoFirst(string methods, string expected)
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Text), nameof(Src.Quantity), Converter = nameof(Show))]", methods));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The value a nullable struct holds does not go by a conversion obsolete as an error, nor to a parameter by in of
    // another type
    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Text), nameof(Src.OldTag), Converter = nameof(Show))]", "private static string Show(int x) => \"\";")]
    [InlineData("[MapProperty(nameof(Dst.Text), nameof(Src.Quantity), Converter = nameof(Show))]", "private static string Show(in long x) => \"\";")]
    public void ValueOfNullableStructConversionIsReported(string attributes, string methods)
    {
        var (_, problems) = Build(Source(attributes, methods));

        Assert.Equal(["SMP0110"], problems);
    }

    // The overload of the type of the value chosen, taking every argument by value first, has to be the one the call
    // binds to: one taking by value that returns another type is reported by its return type, and one obsolete as an
    // error as not matching, where the call bound to them. One of a derived class the call binds to, which leaves out
    // those of its base classes, is used instead.
    [Theory]
    [InlineData("private static long Show(int x) => x; private static string Show(in int x) => \"\";", "SMP0111")]
    [InlineData("[Obsolete(\"\", true)] private static string Show(int x) => \"\"; private static string Show(in int x) => \"\";", "SMP0110")]
    public void ExactChoiceTheCallDoesNotBindToIsReported(string methods, string expected)
    {
        var (_, problems) = Build(Source("[MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]", methods));

        Assert.Equal([expected], problems);
    }

    [Fact]
    public void ExactChoiceTakingByValueIsKept()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Text), nameof(Src.Count), Converter = nameof(Show))]",
            "private static string Show(int x) => \"\"; private static string Show(in int x) => \"\";"));

        Assert.Empty(problems);
        Assert.Contains("dst.Text = Show(src.Count);", generated, StringComparison.Ordinal);
    }
}
