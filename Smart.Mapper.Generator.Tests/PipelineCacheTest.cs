namespace Smart.Mapper.Generator.Tests;

using SourceGenerateHelper.Testing;

public sealed class PipelineCacheTest
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

    [Fact]
    public void TargetEditRebuildsModel()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, AddedTargetSource);

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }
}
