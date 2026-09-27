namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The methods the attributes name are looked up as C# looks up the simple name the generated code calls them by: in the
// mapper class and its base classes (protected ones included, as the mapper class can call them), or else in the class
// containing it and its base classes, and so on outward, where only the mapper class was looked in (SMP0104 and the
// others otherwise). The first class having a member of the name the call can invoke is the only one looked in, as in
// C#, and a member the call cannot invoke (a property or a field not of a delegate type, a nested type) is passed over,
// where it hid the methods outside. The call has to bind to the method matched: one of a derived class hides those of
// its base classes of the same signature, and the call leaves out the methods of a base class when one of a derived
// class takes the arguments. Instance methods are not taken, as before.
public class MethodLookupTests
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

    private static string Source(string attributes, string members = "", string baseMembers = "", string outerMembers = "", string outerBaseMembers = "") =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int Id { get; set; } }
        public class ChildDto { public int Id { get; set; } }
        public class Src
        {
            public string Name { get; set; } = "";
            public string? Nick { get; set; }
            public Child Child { get; set; } = new();
            public List<Child> Children { get; set; } = new();
        }
        public class Dst
        {
            public string Name { get; set; } = "";
            public ChildDto Child { get; set; } = new();
            public List<ChildDto> Children { get; set; } = new();
        }
        public abstract class MapperBase
        {
            {{baseMembers}}
        }
        public abstract class OuterBase
        {
            {{outerBaseMembers}}
        }
        public partial class Outer : OuterBase
        {
            {{outerMembers}}

            public partial class M : MapperBase
            {
                [Mapper(AutoMap = false)]
                {{attributes}}
                public static partial void Map(Src src, Dst dst);

                {{members}}
            }
        }
        """;

    // By its name, which nameof cannot give for a method the mapper class cannot access (CS0122)
    private const string Converter = "[MapProperty(nameof(Dst.Name), Converter = \"Upper\")]";

    private const string Upper = "static string Upper(string value) => value.ToUpperInvariant();";

    // A converter of a base class, of the containing class, or of a base class of that
    [Theory]
    [InlineData("protected " + Upper, "", "")]
    [InlineData("", "private " + Upper, "")]
    [InlineData("", "", "protected " + Upper)]
    public void ConverterOfBaseOrContainingClassIsFound(string baseMembers, string outerMembers, string outerBaseMembers)
    {
        var (generated, problems) = Build(Source(Converter, baseMembers: baseMembers, outerMembers: outerMembers, outerBaseMembers: outerBaseMembers));

        Assert.Empty(problems);
        Assert.Contains("dst.Name = Upper(src.Name);", generated, StringComparison.Ordinal);
    }

    // The other methods an attribute names are found the same way
    [Theory]
    [InlineData(
        "[MapCondition(nameof(Dst.Name), nameof(Check))] [MapProperty(nameof(Dst.Name))]",
        "protected static bool Check(string value) => value.Length > 0;",
        "if (Check(src.Name))")]
    [InlineData(
        "[MapUsing(nameof(Dst.Name), nameof(Describe))]",
        "protected static string Describe(Src source) => source.Name;",
        "dst.Name = Describe(src);")]
    [InlineData(
        "[BeforeMap(nameof(Prepare))]",
        "protected static void Prepare(Src source, Dst destination) { }",
        "Prepare(src, dst);")]
    [InlineData(
        "[MapNested(nameof(Dst.Child), Mapper = nameof(ToDto))]",
        "protected static ChildDto ToDto(Child child) => new() { Id = child.Id };",
        "dst.Child = ToDto(src.Child);")]
    [InlineData(
        "[MapCollection(nameof(Dst.Children), Mapper = nameof(ToDto))]",
        "protected static ChildDto ToDto(Child child) => new() { Id = child.Id };",
        "__dst[__i] = ToDto(__src[__i]);")]
    public void MethodOfBaseClassIsFound(string attributes, string baseMembers, string expected)
    {
        var (fromBase, baseProblems) = Build(Source(attributes, baseMembers: baseMembers));
        var (fromOuter, outerProblems) = Build(Source(attributes, outerMembers: baseMembers.Replace("protected", "private", StringComparison.Ordinal)));

        Assert.Empty(baseProblems);
        Assert.Contains(expected, fromBase, StringComparison.Ordinal);
        Assert.Empty(outerProblems);
        Assert.Contains(expected, fromOuter, StringComparison.Ordinal);
    }

    // A member of the name the call can invoke in the mapper class or its base classes hides the methods of the
    // containing class, also when it does not take the value (CS1503), or is a delegate the call would invoke
    [Theory]
    [InlineData("private static string Upper(int value) => \"\";", "")]
    [InlineData("private static Func<string, string> Upper => static value => value;", "")]
    [InlineData("", "protected static string Upper(int value) => \"\";")]
    public void InnerMemberHidesContainingClassMethod(string members, string baseMembers)
    {
        var (_, problems) = Build(Source(Converter, members: members, baseMembers: baseMembers, outerMembers: "private " + Upper));

        Assert.Equal(["SMP0104"], problems);
    }

    // A member the call cannot invoke is passed over, as C# passes it over for a call: a property or a field of another
    // type, or a nested type, in the mapper class or a base class, which neither hides the methods of the containing
    // class nor those of a base class
    [Theory]
    [InlineData("private static int Upper => 0;", "", "private " + Upper)]
    [InlineData("private static readonly int Upper = 0;", "", "private " + Upper)]
    [InlineData("public class Upper { }", "", "private " + Upper)]
    [InlineData("", "protected static int Upper => 0;", "private " + Upper)]
    [InlineData("private static int Upper => 0;", "protected " + Upper, "")]
    public void NonInvocableMemberIsPassedOver(string members, string baseMembers, string outerMembers)
    {
        var (generated, problems) = Build(Source(Converter, members: members, baseMembers: baseMembers, outerMembers: outerMembers));

        Assert.Empty(problems);
        Assert.Contains("dst.Name = Upper(src.Name);", generated, StringComparison.Ordinal);
    }

    // A private method of a base class, which the mapper class cannot call, is not looked in, and the containing class
    // is
    [Theory]
    [InlineData("private " + Upper, "private " + Upper, null)]
    [InlineData("private " + Upper, "", "SMP0104")]
    public void InaccessibleMethodIsNotFound(string baseMembers, string outerMembers, string? expected)
    {
        var (generated, problems) = Build(Source(Converter, baseMembers: baseMembers, outerMembers: outerMembers));

        if (expected is null)
        {
            Assert.Empty(problems);
            Assert.Contains("dst.Name = Upper(src.Name);", generated, StringComparison.Ordinal);
        }
        else
        {
            Assert.Equal([expected], problems);
        }
    }

    // A method of a derived class hides the one of a base class of the same signature, which would make the call
    // ambiguous or return another type otherwise
    [Fact]
    public void DerivedMethodHidesBaseMethodOfSameSignature()
    {
        var (generated, problems) = Build(Source(
            Converter,
            members: "private static new string Upper(string value) => value.ToUpperInvariant();",
            baseMembers: "protected static int Upper(string value) => 0;"));

        Assert.Empty(problems);
        Assert.Contains("dst.Name = Upper(src.Name);", generated, StringComparison.Ordinal);
    }

    // A method of a derived class taking the value leaves out those of its base classes in the call, even one taking
    // the type of the value: here the one taking null, whose call has no null check, over one that does not take null
    [Fact]
    public void DerivedMethodTakingValueIsUsed()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Dst.Name), nameof(Src.Nick), Converter = nameof(Upper))]",
            members: "private static string Upper(object? value) => value?.ToString() ?? \"\";",
            baseMembers: "protected " + Upper));

        Assert.Empty(problems);
        Assert.Contains("dst.Name = Upper(src.Nick);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("src.Nick is not null", generated, StringComparison.Ordinal);
    }

    // An instance method is not taken, and hides a static method of a base class of the same signature, as the static
    // mapper could not call it (CS0120)
    [Fact]
    public void InstanceMethodIsNotTaken()
    {
        var (_, problems) = Build(Source(
            Converter,
            members: "private string Upper(string value) => value;",
            baseMembers: "protected " + Upper));

        Assert.Equal(["SMP0104"], problems);
    }
}
