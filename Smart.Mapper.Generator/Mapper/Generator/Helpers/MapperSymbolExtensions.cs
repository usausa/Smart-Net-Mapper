namespace Smart.Mapper.Generator.Helpers;

using System.Linq;

using Microsoft.CodeAnalysis;

/// <summary>
/// Mapper-domain Roslyn extension methods that are not candidates for SourceGenerateHelper promotion.
/// </summary>
internal static class MapperSymbolExtensions
{
    // -------------------------------------------------------
    // Type resolution
    // -------------------------------------------------------

    /// <summary>
    /// Resolves a <see cref="INamedTypeSymbol"/> from a fully-qualified name by searching
    /// the containing assembly and all referenced assemblies of <paramref name="mapperMethod"/>.
    /// </summary>
    public static ITypeSymbol? FindTypeByFullyQualifiedName(this IMethodSymbol mapperMethod, string fullyQualifiedName)
    {
        var typeName = fullyQualifiedName.StartsWith("global::", System.StringComparison.Ordinal)
            ? fullyQualifiedName.Substring("global::".Length)
            : fullyQualifiedName;

        var type = mapperMethod.ContainingAssembly.GetTypeByMetadataName(typeName);
        if (type is not null)
        {
            return type;
        }

        foreach (var reference in mapperMethod.ContainingModule.ReferencedAssemblySymbols)
        {
            type = reference.GetTypeByMetadataName(typeName);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    // -------------------------------------------------------
    // User-defined conversion operators
    // -------------------------------------------------------

    /// <summary>
    /// Returns true when a user-defined implicit (<paramref name="isImplicit"/>=true) or explicit
    /// conversion operator exists between <paramref name="sourceType"/> and <paramref name="targetType"/>.
    /// Both source-declared and target-declared operators are checked.
    /// </summary>
    public static bool HasUserDefinedConversion(ITypeSymbol sourceType, ITypeSymbol targetType, bool isImplicit)
    {
        var operatorName = isImplicit
            ? WellKnownMemberNames.ImplicitConversionName
            : WellKnownMemberNames.ExplicitConversionName;

        foreach (var declaringType in new[] { sourceType, targetType })
        {
            foreach (var member in declaringType.GetMembers(operatorName).OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Conversion || !member.IsStatic)
                {
                    continue;
                }

                if (member.Parameters.Length == 1 &&
                    SymbolEqualityComparer.Default.Equals(member.Parameters[0].Type, sourceType) &&
                    SymbolEqualityComparer.Default.Equals(member.ReturnType, targetType))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
