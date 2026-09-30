namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A NameComparison the generator cannot compare names with is reported (SMP0009) instead of stopping the generator, and of
// the types whose names differ only in case, whose generated files would collide, only the first is generated (SMP0010).
public class GeneratedNameTests
{
    private static List<string> GetProblems(string source) =>
        GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id)
            .ToList();

    [Theory]
    [InlineData("[Mapper(NameComparison = (StringComparison)99)]", "")]
    [InlineData("[Mapper]", "[MapperProfile(NameComparison = (StringComparison)99)]")]
    public void Smp0009UndefinedNameComparisonIsReported(string attribute, string profile)
    {
        var source = $$"""
            using System;
            using Smart.Mapper;
            namespace Test;
            public class Src { public int X { get; set; } }
            public class Dst { public int X { get; set; } }
            {{profile}}
            public static partial class M
            {
                {{attribute}}
                public static partial Dst Map(Src src);
            }
            """;

        var problems = GetProblems(source);

        Assert.Contains("SMP0009", problems);
        Assert.DoesNotContain("CS8785", problems);
        Assert.DoesNotContain("CS8795", problems);
    }

    [Fact]
    public void Smp0010CaseOnlyTypeNamesGenerateTheFirstOnly()
    {
        const string source = """
            using Smart.Mapper;
            namespace Test;
            public class Src { public int X { get; set; } }
            public class Dst { public int X { get; set; } }
            public static partial class Mappers
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            public static partial class mappers
            {
                [Mapper]
                public static partial Dst Map(Src src);
            }
            """;

        var problems = GetProblems(source);

        Assert.Contains("SMP0010", problems);
        Assert.DoesNotContain("CS8785", problems);
    }
}
