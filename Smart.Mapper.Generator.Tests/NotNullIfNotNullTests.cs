namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// A return-type mapper whose source may be null (nullable, or declared with nullable annotations disabled) returns
// null for a null source only, so the implementation declares its nullable return type [NotNullIfNotNull] of the
// source parameter: a caller passing a source that is not null gets a result it can use without a warning. It is
// named by the name the parameter is declared with, and the defining declaration may have it as well.
public class NotNullIfNotNullTests
{
    private const string Attribute = "[return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(";

    // Any warning or error: a caller using the result as not null for a source not null warns without the attribute
    private static List<string> Problems(string source) =>
        GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || (d.Severity == DiagnosticSeverity.Warning))
            .Select(static d => d.Id + ": " + d.GetMessage(CultureInfo.InvariantCulture))
            .ToList();

    private static string Source(string declaration, string members = "") =>
        $$"""
        #nullable enable
        using System.Diagnostics.CodeAnalysis;
        using Smart.Mapper;
        namespace Test;
        public class Src { public int Value { get; set; } }
        public class Dst { public int Value { get; set; } }
        public struct DstStruct { public int Value { get; set; } }
        public static partial class M
        {
            [Mapper]
            {{declaration}}

            public static int Use(Src src) => {{members}};
        }
        """;

    [Theory]
    [InlineData("public static partial Dst? Map(Src? source);", "Map(src).Value", "\"source\"")]
    [InlineData("public static partial DstStruct? Map(Src? source);", "Map(src).Value.Value", "\"source\"")]
    [InlineData("public static partial Dst? Map(this Src? source);", "src.Map().Value", "\"source\"")]
    [InlineData("public static partial Dst? Map(Src? @class);", "Map(src).Value", "\"class\"")]
    [InlineData("[return: NotNullIfNotNull(nameof(source))] public static partial Dst? Map(Src? source);", "Map(src).Value", "\"source\"")]
    public void NullableResultIsNotNullForSourceNotNull(string declaration, string use, string name)
    {
        var source = Source(declaration, use);

        Assert.Empty(Problems(source));
        Assert.Contains(Attribute + name + ")]", GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }

    // A result that is not nullable, and a source that is not
    [Theory]
    [InlineData("public static partial Dst Map(Src? source);", "Map(src).Value")]
    [InlineData("public static partial Dst? Map(Src source);", "Map(src)!.Value")]
    [InlineData("public static partial void Map(Src? source, Dst destination);", "0")]
    public void AttributeIsLeftOutOtherwise(string declaration, string use)
    {
        var source = Source(declaration, use);

        Assert.Empty(Problems(source));
        Assert.DoesNotContain(Attribute, GeneratorTestHelper.GetGeneratedSource(source), StringComparison.Ordinal);
    }
}
