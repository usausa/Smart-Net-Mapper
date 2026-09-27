namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// In strict mode, a member a dotted target path writes into (Address for Address.City) is mapped through the path,
// which takes the place of its automatic mapping, of [MapProperty], [MapConstant], [MapExpression] and [MapUsing]
// alike, and a required one the object initializer creates for the path as well. It used to be reported as not
// mapped (SMP0501). A void mapper, which assigns the instance it is given and cannot assign an init-only property,
// does not report one either, as it does not report one only a constructor sets; a return-type mapper, which sets
// it in the object initializer, still does.
public class StrictPathAndInitOnlyTests
{
    private static string Source(string attributes, string signature, string types = "") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Address { public string? City { get; set; } public string? Street { get; set; } }
        public class Src { public int Id { get; set; } public string? City { get; set; } }
        public class Dst { public int Id { get; set; } public Address? Address { get; set; } }
        public class RequiredDst { public required int Id { get; init; } public required Address Address { get; init; } }
        {{types}}
        public static partial class M
        {
            [Mapper(Strict = true)]
            {{attributes}}
            {{signature}}

            private static string? GetCity(Src source) => source.City;
        }
        """;

    private static List<string> Unmapped(string source)
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);
        Assert.DoesNotContain(diagnostics, static d => d.Severity == DiagnosticSeverity.Error);

        return diagnostics
            .Where(static d => d.Id == "SMP0501")
            .Select(static d => d.GetMessage(CultureInfo.InvariantCulture))
            .Select(static m => m[(m.IndexOf("property=[", StringComparison.Ordinal) + 10)..].TrimEnd(']'))
            .OrderBy(static n => n, StringComparer.Ordinal)
            .ToList();
    }

    [Theory]
    [InlineData("[MapProperty(\"Address.City\", nameof(Src.City))]", "public static partial void Map(Src src, Dst dst);")]
    [InlineData("[MapProperty(\"Address.City\", nameof(Src.City))]", "public static partial Dst Map(Src src);")]
    [InlineData("[MapConstant(\"Address.City\", \"Tokyo\")]", "public static partial Dst Map(Src src);")]
    [InlineData("[MapExpression(\"Address.City\", \"src.City\")]", "public static partial Dst Map(Src src);")]
    [InlineData("[MapUsing(\"Address.City\", nameof(GetCity))]", "public static partial Dst Map(Src src);")]
    [InlineData("[MapProperty(\"Address.City\", nameof(Src.City))]", "public static partial RequiredDst Map(Src src);")]
    public void MemberDottedPathWritesIntoIsNotReported(string attributes, string signature)
    {
        Assert.Empty(Unmapped(Source(attributes, signature)));
    }

    // A member nothing maps is still reported
    [Fact]
    public void MemberNothingMapsIsReported()
    {
        Assert.Equal("Address", Assert.Single(Unmapped(Source(string.Empty, "public static partial Dst Map(Src src);"))));
    }

    // A void mapper cannot assign an init-only property, while a return-type mapper sets it in the object initializer
    [Theory]
    [InlineData("public static partial void Map(Src src, InitDst dst);", "")]
    [InlineData("public static partial InitDst Map(Src src);", "Name")]
    public void InitOnlyIsReportedByReturnMapperOnly(string signature, string expected)
    {
        var source = Source(string.Empty, signature, "public class InitDst { public int Id { get; set; } public string Name { get; init; } = \"\"; }");

        Assert.Equal(expected, String.Join(",", Unmapped(source)));
    }
}
