namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// An intermediate member of a dotted target is created only when a value is assigned through it. An assignment under a
// check of its own (NullBehavior.Skip, [MapCondition], a converter not called for a null source) creates it inside the
// check, right before it assigns, where it was created up front, so that a target left as it is kept a null
// intermediate member, as an update giving no value expects. An assignment under the null check of its source path
// creates it inside that check, as before, and in both branches with NullValue; one without a check going through
// the same member creates it up front as before, which the others do not create again, and a return-type mapper
// creates it the same way.
public class GuardedTargetCreationTests
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

    // The body of the mapper, one statement a line, without the indentation
    private static string Body(string generated) =>
        String.Join("\n", generated.Split('\n').Select(static l => l.Trim()).Where(static l => l.Length > 0));

    private static string Source(string attributes, string mapper = "public static partial void Map(Src src, Dst dst);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Other { public string City { get; set; } = ""; public string? Zip { get; set; } }
        public class Src
        {
            public string? City { get; set; }
            public string? Zip { get; set; }
            public string Street { get; set; } = "";
            public Other? Other { get; set; }
        }
        public class Address { public string City { get; set; } = ""; public string? Zip { get; set; } public string Street { get; set; } = ""; }
        public class Area { public Address? Address { get; set; } }
        public class Dst { public Address? Address { get; set; } public Area? Area { get; set; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static bool IsLong(string? value) => value?.Length > 3;
            private static string Trim(string value) => value.Trim();
        }
        """;

    // An assignment under a check of its own creates the members it goes through inside the check
    [Theory]
    [InlineData(
        "[MapProperty(\"Address.City\", nameof(Src.City), NullBehavior = NullBehavior.Skip)]",
        "if (src.City is not null)\n{\ndst.Address ??= new global::Test.Address();\ndst.Address.City = src.City!;\n}")]
    [InlineData(
        "[MapProperty(\"Address.Street\", nameof(Src.Street))] [MapCondition(\"Address.Street\", nameof(IsLong))]",
        "if (IsLong(src.Street))\n{\ndst.Address ??= new global::Test.Address();\ndst.Address.Street = src.Street;\n}")]
    [InlineData(
        "[MapProperty(\"Address.City\", nameof(Src.City), Converter = nameof(Trim))]",
        "if (src.City is not null)\n{\ndst.Address ??= new global::Test.Address();\ndst.Address.City = Trim(src.City);\n}")]
    [InlineData(
        "[MapProperty(\"Area.Address.City\", nameof(Src.City), NullBehavior = NullBehavior.Skip)]",
        "if (src.City is not null)\n{\ndst.Area ??= new global::Test.Area();\ndst.Area.Address ??= new global::Test.Address();\ndst.Area.Address.City = src.City!;\n}")]
    [InlineData(
        "[MapProperty(\"Address.Zip\", \"Other.Zip\", NullBehavior = NullBehavior.Skip)]",
        "if (src.Other is not null)\n{\nif (src.Other.Zip is not null)\n{\ndst.Address ??= new global::Test.Address();\ndst.Address.Zip = src.Other.Zip;\n}\n}")]
    public void GuardedAssignmentCreatesInsideCheck(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));
        var body = Body(generated);

        Assert.Empty(problems);
        Assert.Contains(expected, body, StringComparison.Ordinal);
        Assert.Equal(expected.Split('\n').Count(static l => l.Contains("??=", StringComparison.Ordinal)), body.Split('\n').Count(static l => l.Contains("??=", StringComparison.Ordinal)));
    }

    // The null check of the source path creates them inside, as before, and NullValue in both branches
    [Theory]
    [InlineData(
        "[MapProperty(\"Address.City\", \"Other.City\")]",
        "if (src.Other is not null)\n{\ndst.Address ??= new global::Test.Address();\ndst.Address.City = src.Other.City;\n}")]
    [InlineData(
        "[MapProperty(\"Address.City\", \"Other.City\", NullValue = \"none\")]",
        "if (src.Other is not null)\n{\ndst.Address ??= new global::Test.Address();\ndst.Address.City = src.Other.City;\n}\nelse\n{\ndst.Address ??= new global::Test.Address();\ndst.Address.City = \"none\";\n}")]
    public void SourcePathCheckCreatesAsBefore(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(expected, Body(generated), StringComparison.Ordinal);
    }

    // An assignment without a check going through the same member creates it up front as before, and the guarded one
    // does not create it again; one without a check alone does as before
    [Theory]
    [InlineData(
        "[MapProperty(\"Address.City\", nameof(Src.City), NullBehavior = NullBehavior.Skip)] [MapProperty(\"Address.Street\", nameof(Src.Street))]",
        "dst.Address ??= new global::Test.Address();\nif (src.City is not null)\n{\ndst.Address.City = src.City!;\n}\ndst.Address.Street = src.Street;")]
    [InlineData(
        "[MapProperty(\"Address.Street\", nameof(Src.Street))]",
        "dst.Address ??= new global::Test.Address();\ndst.Address.Street = src.Street;")]
    [InlineData(
        "[MapProperty(\"Address.City\", nameof(Src.City), NullValue = \"none\")]",
        "dst.Address ??= new global::Test.Address();\ndst.Address.City = src.City ?? \"none\";")]
    public void AssignmentWithoutCheckCreatesUpFront(string attributes, string expected)
    {
        var (generated, problems) = Build(Source(attributes));
        var body = Body(generated);

        Assert.Empty(problems);
        Assert.Contains(expected, body, StringComparison.Ordinal);
        Assert.Single(body.Split('\n'), static l => l.Contains("??=", StringComparison.Ordinal));
    }

    // A member created up front is not created again under the null check of a source path going through the same
    // member, in a void mapper and in a return-type mapper alike: nothing between the two can have replaced it
    [Theory]
    [InlineData(
        "public static partial void Map(Src src, Dst dst);",
        "dst.Address ??= new global::Test.Address();\ndst.Address.Street = src.Street;\nif (src.Other is not null)\n{\ndst.Address.City = src.Other.City;\n}")]
    [InlineData(
        "public static partial Dst Map(Src src);",
        "__d.Address ??= new global::Test.Address();\n__d.Address.Street = src.Street;\nif (src.Other is not null)\n{\n__d.Address.City = src.Other.City;\n}")]
    public void MemberCreatedUpFrontIsNotCreatedAgainUnderSourcePathCheck(string mapper, string expected)
    {
        var (generated, problems) = Build(Source("[MapProperty(\"Address.Street\", nameof(Src.Street))] [MapProperty(\"Address.City\", \"Other.City\")]", mapper));
        var body = Body(generated);

        Assert.Empty(problems);
        Assert.Contains(expected, body, StringComparison.Ordinal);
        Assert.Single(body.Split('\n'), static l => l.Contains("??=", StringComparison.Ordinal));
    }

    // A return-type mapper leaves the member null when nothing is assigned through it as well
    [Fact]
    public void ReturnMapperCreatesInsideCheck()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(\"Address.City\", nameof(Src.City), NullBehavior = NullBehavior.Skip)] [MapProperty(\"Area.Address.Street\", nameof(Src.Street))] [MapCondition(\"Area.Address.Street\", nameof(IsLong))]",
            "public static partial Dst Map(Src src);"));

        Assert.Empty(problems);
        Assert.Contains(
            "var __d = new global::Test.Dst();\nif (src.City is not null)\n{\n__d.Address ??= new global::Test.Address();\n__d.Address.City = src.City!;\n}\n" +
            "if (IsLong(src.Street))\n{\n__d.Area ??= new global::Test.Area();\n__d.Area.Address ??= new global::Test.Address();\n__d.Area.Address.Street = src.Street;\n}\nreturn __d;",
            Body(generated),
            StringComparison.Ordinal);
    }
}
