namespace Smart.Mapper.Generator.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Roslyn incremental generator をインメモリで実行し、診断結果を返すヘルパー。
/// </summary>
internal static class GeneratorTestHelper
{
    private static readonly Assembly SmartMapperAssembly =
        typeof(Smart.Mapper.MapperAttribute).Assembly;

    // ジェネレーターが依存する SourceGenerateHelper を事前ロードしておく。
    // テスト実行バイナリと同一ディレクトリに配置されているため、
    // Assembly.GetExecutingAssembly の CodeBase から解決する。
    private static readonly System.Lazy<bool> EnsureDeps = new(() =>
    {
        var dir = System.IO.Path.GetDirectoryName(typeof(GeneratorTestHelper).Assembly.Location)!;
        var helper = System.IO.Path.Combine(dir, "SourceGenerateHelper.dll");
        if (System.IO.File.Exists(helper))
        {
            Assembly.LoadFrom(helper);
        }
        return true;
    });

    /// <summary>
    /// 指定ソースコードに対してジェネレーターを実行し、SMP 診断を返す。
    /// </summary>
    public static IReadOnlyList<Diagnostic> GetDiagnostics(string source) =>
        RunGenerator(source)
            .Where(d => d.Id.StartsWith("SMP", System.StringComparison.Ordinal))
            .ToList();

    /// <summary>SMP 以外の診断も含め全診断を返す（デバッグ用）。</summary>
    public static IReadOnlyList<Diagnostic> GetDiagnosticsAll(string source) =>
        RunGenerator(source).ToList();

    private static IEnumerable<Diagnostic> RunGenerator(string source)
    {
        _ = EnsureDeps.Value;
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(SmartMapperAssembly.Location),
        }.Concat(GetRuntimeReferences());

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new Smart.Mapper.Generator.MapperGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var generatorDiagnostics);

        var driverResult = driver.GetRunResult();

        return driverResult.Results
            .SelectMany(r => r.Diagnostics)
            .Concat(generatorDiagnostics)
            .Concat(outputCompilation.GetDiagnostics());
    }

    private static IEnumerable<MetadataReference> GetRuntimeReferences()
    {
        var trustedAssemblies = System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trustedAssemblies is null)
        {
            yield break;
        }

        foreach (var path in trustedAssemblies.Split(System.IO.Path.PathSeparator))
        {
            if (!string.IsNullOrEmpty(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }
}
