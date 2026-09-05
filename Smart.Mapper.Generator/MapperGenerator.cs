namespace Smart.Mapper.Generator;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                .Select(static g => new ClassMethodsModel(g.Key.Namespace, g.Key.ClassName, new EquatableArray<MapperMethodModel>(g)))
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
        MapperSourceBuilder.BuildSource(builder, group.Methods);

        context.AddSource(HintNameBuilder.Build(group.Namespace, group.ClassName), builder);
    }
}
