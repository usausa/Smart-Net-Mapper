namespace Smart.Mapper.Generator.Tests;

using System.Globalization;

// SMP0215 is reported for [MapCondition] and for NullBehavior.Skip alike on a target assigned through a constructor
// argument or an object initializer entry, and its message names both, where it used to name [MapCondition] only.
public class UnsupportedOptionMessageTests
{
    private static string Source(string attributes) =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public class Src { public string? Name { get; set; } }
        public record Dst(string Name);
        public static partial class M
        {
            [Mapper]
            {{attributes}}
            public static partial Dst Map(Src src);

            private static bool IsWanted(string? value) => true;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Name), NullBehavior = NullBehavior.Skip)]", "option=[NullBehavior.Skip]")]
    [InlineData("[MapCondition(nameof(Dst.Name), nameof(IsWanted))]", "option=[MapCondition]")]
    public void MessageNamesBothOptions(string attributes, string option)
    {
        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(Source(attributes)), static d => d.Id == "SMP0215");
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);

        Assert.StartsWith("[MapCondition] / NullBehavior.Skip requires a property assignment", message, StringComparison.Ordinal);
        Assert.Contains(option, message, StringComparison.Ordinal);
    }
}
