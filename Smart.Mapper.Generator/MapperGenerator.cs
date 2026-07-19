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
                Names.MapperAttribute,
                static (syntax, _) => IsMethodSyntax(syntax),
                static (context, _) => MapperModelBuilder.BuildModel(context))
            .Collect();

        context.RegisterSourceOutput(
            methodProvider,
            static (context, methods) => ReportDiagnostics(context, methods));

        var groups = methodProvider.SelectMany(static (methods, _) =>
            methods.SelectValue()
                .GroupBy(static x => new { x.Namespace, x.ClassName })
                .Select(static g => new ClassMethodsModel(g.Key.Namespace, g.Key.ClassName, new EquatableArray<MapperMethodModel>(g.ToArray())))
                .ToImmutableArray());
        context.RegisterImplementationSourceOutput(
            groups,
            static (context, group) => Execute(context, group));
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
    private static void ReportDiagnostics(SourceProductionContext context, ImmutableArray<Result<MapperMethodModel>> methods)
    {
        foreach (var info in methods.SelectError())
        {
            context.ReportDiagnostic(info);
        }

        // Report strict-mode warnings
        foreach (var model in methods.SelectValue())
        {
            foreach (var (descriptor, arg0, arg1) in model.Warnings)
            {
                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, arg0, arg1));
            }
        }
    }

    private static void Execute(SourceProductionContext context, ClassMethodsModel group)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var builder = new SourceBuilder();
        MapperSourceBuilder.BuildSource(builder, group.Methods.ToList());

        var filename = MakeFilename(group.Namespace, group.ClassName);
        var source = builder.ToString();
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
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
