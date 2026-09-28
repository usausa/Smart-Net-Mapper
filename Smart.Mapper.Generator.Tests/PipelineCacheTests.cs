namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper.Testing;

public sealed class PipelineCacheTests
{
    private const string Source =
        """
        using Smart.Mapper;

        namespace Test;

        public sealed class Source
        {
            public int Id { get; set; }
        }

        public sealed class Destination
        {
            public int Id { get; set; }
        }

        public static partial class Mappers
        {
            [Mapper]
            public static partial Destination ToDestination(Source source);
        }
        """;

    private const string UnrelatedSource =
        """
        namespace Other;

        internal sealed class Unrelated;
        """;

    // Warnings located at the method (SMP0501) and at the attribute (SMP0403)
    private const string WarningSource =
        """
        using Smart.Mapper;

        namespace Test;

        public sealed class Source
        {
            public string Name { get; set; } = "";
        }

        public sealed class Destination
        {
            public string Name { get; set; } = "";

            public string Kind { get; set; } = "";

            public int Unmapped { get; set; }
        }

        public static partial class Mappers
        {
            [Mapper(Strict = true)]
            [MapExpression(nameof(Destination.Kind), "System.Type.GetType(source.Name)!.Name")]
            public static partial Destination ToDestination(Source source);
        }
        """;

    private const string AddedTargetSource =
        """
        using Smart.Mapper;

        namespace Test;

        public static partial class AddedMappers
        {
            [Mapper]
            public static partial Destination ToDestination(Source source);
        }
        """;

    // ------------------------------------------------------------
    // Cache
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsModelCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    // The model holding located warnings is equal for the same input, so an unrelated edit keeps it cached
    [Fact]
    public void UnrelatedEditKeepsModelWithWarningsCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(WarningSource, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
        Assert.Equal(2, result.SecondResult.Diagnostics.Count(static d => d.Id is "SMP0501" or "SMP0403"));
    }

    [Fact]
    public void TargetEditRebuildsModel()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, AddedTargetSource);

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }

    // The properties and the types the generator looks up are kept per compilation, so a compilation in which the
    // types have changed is looked at anew
    [Fact]
    public void EditedTypesAreLookedUpAgain()
    {
        // Arrange
        var (driver, compilation) = GeneratorTestHelper.CreateTrackingDriver(Source);
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var tree = compilation.SyntaxTrees.First();
        var edited = compilation.ReplaceSyntaxTree(
            tree,
            tree.WithChangedText(SourceText.From(Source.Replace("public int Id { get; set; }", "public int Id { get; set; }\n    public string Name { get; set; } = \"\";", StringComparison.Ordinal))));

        // Act
        driver = driver.RunGenerators(edited, TestContext.Current.CancellationToken);

        // Assert
        var generated = String.Join("\n", driver.GetRunResult().GeneratedTrees.Select(static t => t.ToString()));
        Assert.Contains("__d.Name = source.Name;", generated, StringComparison.Ordinal);
    }
}
