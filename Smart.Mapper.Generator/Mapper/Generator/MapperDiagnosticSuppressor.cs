namespace Smart.Mapper.Generator;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

// Suppresses CS8618 and CS8602 warnings on partial methods decorated with [Mapper].
// The Source Generator guarantees that all properties are assigned in the generated implementation,
// so these nullable warnings are false positives.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class MapperDiagnosticSuppressor : DiagnosticSuppressor
{
    // ReSharper disable InconsistentNaming
    // CS8618: Non-nullable field/property must contain a non-null value when exiting constructor.
    private static readonly SuppressionDescriptor SuppressCS8618 = new(
        id: "SPR0001",
        suppressedDiagnosticId: "CS8618",
        justification: "Smart.Mapper Source Generator assigns all non-nullable properties in the generated implementation.");

    // CS8602: Dereference of a possibly null reference.
    private static readonly SuppressionDescriptor SuppressCS8602 = new(
        id: "SPR0002",
        suppressedDiagnosticId: "CS8602",
        justification: "Smart.Mapper Source Generator guarantees non-null access in the generated implementation.");
    // ReSharper restore InconsistentNaming

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        [SuppressCS8618, SuppressCS8602];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Id == "CS8618" || diagnostic.Id == "CS8602")
            {
                TrySuppress(context, diagnostic);
            }
        }
    }

    private static void TrySuppress(SuppressionAnalysisContext context, Diagnostic diagnostic)
    {
        var location = diagnostic.Location;
        if (location == Location.None)
        {
            return;
        }

        var root = location.SourceTree?.GetRoot(context.CancellationToken);
        if (root is null)
        {
            return;
        }

        var node = root.FindNode(location.SourceSpan, getInnermostNodeForTie: true);

        // Walk up to find a method declaration
        var current = node;
        while (current is not null)
        {
            if (current is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDecl)
            {
                var model = context.GetSemanticModel(location.SourceTree!);
                var symbol = model.GetDeclaredSymbol(methodDecl, context.CancellationToken);
                if (symbol is IMethodSymbol method && IsMapperMethod(method))
                {
                    var descriptor = diagnostic.Id == "CS8618" ? SuppressCS8618 : SuppressCS8602;
                    context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
                }

                return;
            }

            current = current.Parent;
        }
    }

    private static bool IsMapperMethod(IMethodSymbol method)
    {
        if (!method.IsStatic || !method.IsPartialDefinition)
        {
            return false;
        }

        foreach (var attr in method.GetAttributes())
        {
            var name = attr.AttributeClass?.ToDisplayString();
            if (name == "Smart.Mapper.MapperAttribute")
            {
                return true;
            }
        }

        return false;
    }
}
