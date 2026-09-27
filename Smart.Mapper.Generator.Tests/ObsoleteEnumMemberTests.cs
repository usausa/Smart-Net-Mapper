namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// An enum member marked [Obsolete], obsolete as a warning or as an error, is written as a cast of its number, as
// naming it warns (CS0618) or fails (CS0619): a pattern and a result of the switch converting an enum to another enum,
// to a string or from a string, and a constant of [MapConstant] or NullValue, which is written by a member of the same
// value not marked when there is one. The members are matched by name as before, and one not marked is written by its
// name. The same enum on both sides is copied as it is, keeping a value no member has.
public class ObsoleteEnumMemberTests
{
    private const string Status =
        "public enum Status { None, [Obsolete(\"Use Completed\")] Done = 2, Completed = 2, [Obsolete(\"Old\", true)] Gone = 3, [Obsolete(\"Old\")] Minus = -1 } ";

    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string types, string attributes = "") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        {{types}}
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            public static partial Dst Map(Src src);
        }
        """;

    private static int CountOf(string text, string value) => text.Split(value).Length - 1;

    [Theory]
    [InlineData("[Obsolete(\"Old\")]")]
    [InlineData("[Obsolete(\"Old\", true)]")]
    public void EnumToEnumWritesObsoleteMemberAsCast(string obsolete)
    {
        var (generated, problems) = Build(Source(
            $"public enum SrcColor {{ Red, Blue, {obsolete} Green }} public enum DstColor {{ Red, {obsolete} Blue = 5, Green = 7 }} " +
            "public class Src { public SrcColor C { get; set; } public SrcColor? N { get; set; } } public class Dst { public DstColor C { get; set; } public DstColor? N { get; set; } }"));

        Assert.Empty(problems);
        Assert.Equal(2, CountOf(generated, "global::Test.SrcColor.Red => global::Test.DstColor.Red,"));
        Assert.Equal(2, CountOf(generated, "global::Test.SrcColor.Blue => (global::Test.DstColor)5,"));
        Assert.Equal(2, CountOf(generated, "(global::Test.SrcColor)2 => global::Test.DstColor.Green,"));
    }

    [Fact]
    public void EnumToStringWritesObsoleteMemberAsCast()
    {
        var (generated, problems) = Build(Source(
            "public enum SrcColor : long { Red, [Obsolete(\"Old\")] Minus = -1, [Obsolete(\"Old\", true)] Big = 5000000000 } " +
            "public class Src { public SrcColor C { get; set; } } public class Dst { public string C { get; set; } = \"\"; }"));

        Assert.Empty(problems);
        Assert.Contains("global::Test.SrcColor.Red => \"Red\",", generated, StringComparison.Ordinal);
        Assert.Contains("(global::Test.SrcColor)(-1L) => \"Minus\",", generated, StringComparison.Ordinal);
        Assert.Contains("(global::Test.SrcColor)5000000000L => \"Big\",", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void StringToEnumWritesObsoleteMemberAsCast()
    {
        var (generated, problems) = Build(Source(
            "public enum Level : byte { Low, [Obsolete(\"Old\", true)] High = 200 } public enum Offset : sbyte { Zero, [Obsolete(\"Old\")] Minus = -3 } " +
            "public class Src { public string L { get; set; } = \"\"; public string O { get; set; } = \"\"; } public class Dst { public Level L { get; set; } public Offset O { get; set; } }"));

        Assert.Empty(problems);
        Assert.Contains("\"Low\" => global::Test.Level.Low,", generated, StringComparison.Ordinal);
        Assert.Contains("\"High\" => (global::Test.Level)200,", generated, StringComparison.Ordinal);
        Assert.Contains("\"Minus\" => (global::Test.Offset)(-3),", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsignedAndLimitValuesAreWritten()
    {
        var (generated, problems) = Build(Source(
            "public enum U : ulong { A, [Obsolete(\"Old\")] Max = ulong.MaxValue } public enum V : ulong { A, [Obsolete(\"Old\")] Max = ulong.MaxValue } " +
            "public enum L : long { A, [Obsolete(\"Old\")] Min = long.MinValue } " +
            "public class Src { public U C { get; set; } public L E { get; set; } } public class Dst { public V C { get; set; } public string E { get; set; } = \"\"; }"));

        Assert.Empty(problems);
        Assert.Contains("(global::Test.U)18446744073709551615UL => (global::Test.V)18446744073709551615UL,", generated, StringComparison.Ordinal);
        Assert.Contains("(global::Test.L)(-9223372036854775808L) => \"Min\",", generated, StringComparison.Ordinal);
    }

    // A constant is written by the first member of its value not marked, and as a cast when every member of the value
    // is marked
    [Theory]
    [InlineData("[MapConstant(\"A\", (Status)2)]", "__d.A = global::Test.Status.Completed;")]
    [InlineData("[MapConstant(\"A\", (Status)3)]", "__d.A = (global::Test.Status)3;")]
    [InlineData("[MapConstant(\"A\", (Status)(-1))]", "__d.A = (global::Test.Status)(-1);")]
    [InlineData("[MapConstant(\"L\", new[] { Status.None, (Status)3 })]", "__d.L = new global::Test.Status[] { global::Test.Status.None, (global::Test.Status)3 };")]
    [InlineData("[MapProperty(\"N\", \"N\", NullValue = (Status)3)]", "__d.N = src.N ?? (global::Test.Status)3;")]
    public void ConstantWritesObsoleteMemberAsCast(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(
            Status + "public class Src { public Status? N { get; set; } } public class Dst { public Status A { get; set; } public Status[] L { get; set; } = []; public Status N { get; set; } }",
            attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
    }

    // The same enum is copied as it is, whatever its nullability, so a combination of flags is kept
    [Fact]
    public void SameEnumIsCopied()
    {
        var (generated, problems) = Build(Source(
            "[Flags] public enum Access { None = 0, Read = 1, Write = 2 } " +
            "public class Src { public Access P { get; set; } public Access? Q { get; set; } public Access? R { get; set; } public Access S { get; set; } } " +
            "public class Dst { public Access P { get; set; } public Access Q { get; set; } public Access? R { get; set; } public Access? S { get; set; } }"));

        Assert.Empty(problems);
        Assert.DoesNotContain("switch", generated, StringComparison.Ordinal);
        Assert.Contains("__d.P = src.P;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Q = src.Q.GetValueOrDefault();", generated, StringComparison.Ordinal);
        Assert.Contains("__d.R = src.R;", generated, StringComparison.Ordinal);
        Assert.Contains("__d.S = src.S;", generated, StringComparison.Ordinal);
    }
}
