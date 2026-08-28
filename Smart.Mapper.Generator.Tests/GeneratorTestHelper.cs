namespace Smart.Mapper.Generator.Tests;

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using Smart.Mapper;

using SourceGenerateHelper.Testing;

internal static class GeneratorTestHelper
{
    private static GeneratorTestRunner Runner => GeneratorTestRunner
        .For<MapperGenerator>()
        .WithReference(typeof(MapperAttribute).Assembly)
        .WithDiagnosticPrefix("SMP");

    public static IReadOnlyList<Diagnostic> GetDiagnostics(string source) => Runner.GetDiagnostics(source);

    public static IReadOnlyList<Diagnostic> GetDiagnosticsAll(string source) => Runner.GetDiagnosticsAll(source);

    public static string GetGeneratedSource(string source) => Runner.GetGeneratedSource(source);

    public static GeneratorTestResult Run(string source) => Runner.VerifyCompiles(false).Run(source);

    public static IncrementalRunResult RunIncremental(string source, string addedSource) =>
        Runner.WithTracking().RunIncremental(source, addedSource);
}
