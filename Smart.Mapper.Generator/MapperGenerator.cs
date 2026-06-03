namespace Smart.Mapper.Generator;

using System;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

[Generator]
public sealed class MapperGenerator : IIncrementalGenerator
{
    // ------------------------------------------------------------
    // Initialize / 初期化
    // ------------------------------------------------------------

    // [Mapper] 属性を持つメソッドを検出し、ソース生成パイプラインを登録する。
    // Discovers methods decorated with [Mapper] and registers the source generation pipeline.
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                MapperModelBuilder.MapperAttributeName,
                static (syntax, _) => IsMethodSyntax(syntax),
                static (context, _) => MapperModelBuilder.BuildModel(context))
            .Collect();

        context.RegisterImplementationSourceOutput(
            methodProvider,
            static (context, methods) => Execute(context, methods));
    }

    // ------------------------------------------------------------
    // Parser / 解析
    // ------------------------------------------------------------

    private static bool IsMethodSyntax(SyntaxNode syntax) =>
        syntax is MethodDeclarationSyntax;

    // ------------------------------------------------------------
    // Generator / コード生成
    // ------------------------------------------------------------

    // パーサーから受け取ったモデルをクラスごとにグループ化し、ソースファイルを生成する。診断も発行する。
    // Groups parsed mapper models by class, generates one source file per class, and reports diagnostics.
    private static void Execute(SourceProductionContext context, ImmutableArray<Result<MapperMethodModel>> methods)
    {
        foreach (var info in methods.SelectError())
        {
            context.ReportDiagnostic(info);
        }

        var builder = new SourceBuilder();
        foreach (var group in methods.SelectValue().GroupBy(static x => new { x.Namespace, x.ClassName }))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // Report strict-mode warnings
            foreach (var model in group)
            {
                foreach (var (descriptor, arg) in model.Warnings)
                {
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, arg));
                }
            }

            builder.Clear();
            MapperSourceBuilder.BuildSource(builder, group.ToList());

            var filename = MakeFilename(group.Key.Namespace, group.Key.ClassName);
            var source = builder.ToString();
            context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
        }
    }

    // ------------------------------------------------------------
    // Helper / ヘルパー
    // ------------------------------------------------------------

    // 名前空間とクラス名から生成ファイル名（.g.cs ファイル名）を生成する。
    // Generates the output file name (e.g., "MyNamespace_MyClass.g.cs") from namespace and class name.
    private static string MakeFilename(string ns, string className)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(className.Replace('<', '[').Replace('>', ']'));
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}
