namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Smart.Mapper;

using SourceGenerateHelper.Testing;

// The pipeline models are immutable so that Roslyn can reuse cached results. These assert the
// property that immutability buys: an edit that does not touch a mapper must not invalidate it.
public sealed class IncrementalCacheTests
{
    private const string Source =
        """
        using Smart.Mapper;

        namespace Test;

        public sealed class Source
        {
            public int Id { get; set; }

            public string Name { get; set; } = default!;
        }

        public sealed class Destination
        {
            public int Id { get; set; }

            public string Name { get; set; } = default!;
        }

        public static partial class Mappers
        {
            [Mapper]
            public static partial Destination ToDestination(Source source);
        }
        """;

    [Fact]
    public void UnrelatedEditReusesTheCachedOutput()
    {
        var (driver, compilation) = GeneratorTestRunner
            .For<MapperGenerator>()
            .WithReference(typeof(MapperAttribute).Assembly)
            .WithTracking()
            .CreateDriver(Source);

        // First run populates the caches.
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        var first = driver.GetRunResult().Results.Single();

        // A class with no [Mapper] leaves every input of the mapper untouched.
        var unrelated = CSharpSyntaxTree.ParseText(
            "namespace Other { internal sealed class Unrelated { } }",
            new CSharpParseOptions(LanguageVersion.Preview),
            cancellationToken: TestContext.Current.CancellationToken);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(unrelated), TestContext.Current.CancellationToken);
        var second = driver.GetRunResult().Results.Single();

        // Same output, and the second run reported no step that had to recompute a value.
        Assert.Equal(
            first.GeneratedSources.Single().SourceText.ToString(),
            second.GeneratedSources.Single().SourceText.ToString());

        var reasons = second.TrackedOutputSteps
            .SelectMany(static x => x.Value)
            .SelectMany(static step => step.Outputs)
            .Select(static output => output.Reason)
            .ToList();

        Assert.NotEmpty(reasons);
        Assert.All(reasons, static reason => Assert.True(
            reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
            $"output step re-ran with reason '{reason}'"));
    }

    // Two independent runs over identical source must produce equal models, otherwise the models
    // would never compare equal across compilations and the cache above could never hit.
    [Fact]
    public void IdenticalSourceProducesEqualModels()
    {
        static GeneratorDriverRunResult Run()
        {
            var (driver, compilation) = GeneratorTestRunner
                .For<MapperGenerator>()
                .WithReference(typeof(MapperAttribute).Assembly)
                .CreateDriver(Source);
            return driver.RunGenerators(compilation).GetRunResult();
        }

        Assert.Equal(
            Run().Results.Single().GeneratedSources.Single().SourceText.ToString(),
            Run().Results.Single().GeneratedSources.Single().SourceText.ToString());
    }
}
