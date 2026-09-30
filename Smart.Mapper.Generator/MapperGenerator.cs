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

        // The syntax trees of the mapper methods, which the warnings are located in, so that a #pragma directive
        // suppresses them as it does a diagnostic of the compiler. A tree of a file not edited stays the same
        // instance, so this changes only with a file declaring a mapper.
        var treeProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Names.MapperAttribute,
                static (syntax, _) => IsMethodSyntax(syntax),
                static (context, _) => context.TargetNode.SyntaxTree)
            .Collect();

        context.RegisterSourceOutput(
            methodProvider.Combine(treeProvider),
            static (context, pair) => ReportDiagnostics(context, pair.Left, pair.Right));

        // The profile of the assembly is looked at once, so that a culture of it that is not a culture name is reported
        // once, at the profile, and not for every mapper taking it
        var assemblyProfileProvider = context.CompilationProvider
            .Select(static (compilation, _) => MapperModelBuilder.ValidateAssemblyProfile(compilation));
        context.RegisterSourceOutput(
            assemblyProfileProvider,
            static (context, profile) =>
            {
                ImmutableArray<SyntaxTree> trees = profile.Tree is null ? [] : [profile.Tree];
                foreach (var info in profile.Diagnostics)
                {
                    context.ReportDiagnostic(ToDiagnostic(info, trees));
                }
            });

        // The source does not depend on the locations the diagnostics are reported at (the warnings, and the
        // attributes), which change as the code around them moves
        var groups = methodProvider.SelectMany(static (methods, _) =>
        {
            var collisions = FindHintNameCollisions(methods);
            return methods.SelectValue()
                .Where(x => !collisions.ContainsKey(GetHintName(x.Namespace, x.ClassName)))
                .Select(static x => x with { Warnings = default, AttributeLocations = default })
                .GroupBy(static x => new { x.Namespace, x.ClassName })
                .Select(static g => new ClassMethodsModel(g.Key.Namespace, g.Key.ClassName, new EquatableArray<MapperMethodModel>(g)))
                .ToImmutableArray();
        });
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
    private static void ReportDiagnostics(SourceProductionContext context, ImmutableArray<Result<MapperMethodModel>> methods, ImmutableArray<SyntaxTree> trees)
    {
        var infos = methods.SelectError()
            .Concat(methods.SelectValue().SelectMany(static x => x.Warnings))
            .Concat(FindHintNameCollisions(methods).Values)
            .Distinct();
        foreach (var info in infos)
        {
            context.ReportDiagnostic(ToDiagnostic(info, trees));
        }
    }

    private static string GetHintName(string ns, string className) =>
        HintNameBuilder.Build(ns, className);

    private static Dictionary<string, DiagnosticInfo> FindHintNameCollisions(ImmutableArray<Result<MapperMethodModel>> methods)
    {
        var collisions = new Dictionary<string, DiagnosticInfo>(StringComparer.Ordinal);
        var firsts = new Dictionary<string, (string HintName, string TypeName)>(StringComparer.OrdinalIgnoreCase);
        foreach (var method in methods.SelectValue().OrderBy(static x => GetHintName(x.Namespace, x.ClassName), StringComparer.Ordinal))
        {
            var hintName = GetHintName(method.Namespace, method.ClassName);
            var typeName = String.IsNullOrEmpty(method.Namespace) ? method.ClassName : $"{method.Namespace}.{method.ClassName}";
            if (!firsts.TryGetValue(hintName, out var first))
            {
                firsts.Add(hintName, (hintName, typeName));
            }
            else if ((first.HintName != hintName) && !collisions.ContainsKey(hintName))
            {
                collisions.Add(hintName, new DiagnosticInfo(Diagnostics.HintNameCollision, (Location?)null, typeName, first.TypeName));
            }
        }

        return collisions;
    }

    // A diagnostic located in the syntax tree the model located it in: a location in a tree is what a #pragma
    // directive suppresses and an editor underlines, which one of the file path alone is not
    private static Diagnostic ToDiagnostic(DiagnosticInfo info, ImmutableArray<SyntaxTree> trees)
    {
        var location = info.Location is { } located
            ? trees.FirstOrDefault(t => t.FilePath == located.FilePath) is { } tree
                ? Location.Create(tree, located.TextSpan)
                : located.ToLocation()
            : Location.None;
        return Diagnostic.Create(info.Descriptor, location, info.Properties, [.. info.MessageArgs]);
    }

    private static void Execute(SourceProductionContext context, ClassMethodsModel group)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var builder = new SourceBuilder();
        MapperSourceBuilder.BuildSource(builder, group.Methods);

        context.AddSource(GetHintName(group.Namespace, group.ClassName), builder);
    }
}
