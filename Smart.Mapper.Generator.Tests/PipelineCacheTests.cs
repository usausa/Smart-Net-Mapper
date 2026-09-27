namespace Smart.Mapper.Generator.Tests;

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
}
