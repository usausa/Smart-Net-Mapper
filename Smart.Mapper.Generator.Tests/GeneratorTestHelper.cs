namespace Smart.Mapper.Generator.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Roslyn incremental generator をインメモリで実行し、診断結果を返すヘルパー
// Helper that runs the Roslyn incremental generator in-memory and returns the diagnostics.
internal static class GeneratorTestHelper
{
    private static readonly Assembly SmartMapperAssembly =
        typeof(MapperAttribute).Assembly;

    // ジェネレーターが依存する SourceGenerateHelper を事前ロードしておく。
    // Pre-loads the SourceGenerateHelper that the generator depends on.
    // テスト実行バイナリと同一ディレクトリに配置されているため、
    // Because it sits in the same directory as the test binary,
    // Assembly.GetExecutingAssembly の CodeBase から解決する。
    // it is resolved from the CodeBase of Assembly.GetExecutingAssembly.
    private static readonly Lazy<bool> EnsureDeps = new(() =>
    {
        var dir = Path.GetDirectoryName(typeof(GeneratorTestHelper).Assembly.Location)!;
        var helper = Path.Combine(dir, "SourceGenerateHelper.dll");
        if (File.Exists(helper))
        {
            Assembly.LoadFrom(helper);
        }
        return true;
    });

    // 指定ソースコードに対してジェネレーターを実行し、SMP 診断を返す。
    // Runs the generator against the given source code and returns the SMP diagnostics.
    public static IReadOnlyList<Diagnostic> GetDiagnostics(string source) =>
        RunGenerator(source).Diagnostics
            .Where(d => d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .ToList();

    // SMP 以外の診断も含め全診断を返す（デバッグ用）。
    // Returns all diagnostics, including non-SMP ones (for debugging).
    public static IReadOnlyList<Diagnostic> GetDiagnosticsAll(string source) =>
        RunGenerator(source).Diagnostics.ToList();

    // 最初の生成ファイルのソーステキストを返す。
    // Returns the source text of the first generated file.
    public static string GetGeneratedSource(string source) =>
        RunGenerator(source).GeneratedSource;

    private static (IEnumerable<Diagnostic> Diagnostics, string GeneratedSource) RunGenerator(string source)
    {
        _ = EnsureDeps.Value;
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(SmartMapperAssembly.Location)
        }.Concat(GetRuntimeReferences());

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MapperGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var generatorDiagnostics);

        var driverResult = driver.GetRunResult();

        var diagnostics = driverResult.Results
            .SelectMany(r => r.Diagnostics)
            .Concat(generatorDiagnostics)
            .Concat(outputCompilation.GetDiagnostics());

        var generatedSource = driverResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => s.SourceText.ToString())
            .FirstOrDefault() ?? string.Empty;

        return (diagnostics, generatedSource);
    }

    private static IEnumerable<MetadataReference> GetRuntimeReferences()
    {
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string trustedAssemblies)
        {
            yield break;
        }

        foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
        {
            if (!String.IsNullOrEmpty(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }
}
