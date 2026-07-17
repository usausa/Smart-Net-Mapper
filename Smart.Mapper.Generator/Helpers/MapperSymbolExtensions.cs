namespace Smart.Mapper.Generator.Helpers;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Mapper-domain Roslyn extension methods that are not candidates for SourceGenerateHelper promotion.
internal static class MapperSymbolExtensions
{
    // -------------------------------------------------------
    // Collections
    // -------------------------------------------------------

    // Returns the element type of a collection-like type, additionally recognizing
    // Memory<T> / ReadOnlyMemory<T>, which the shared GetCollectionElementType helper does not.
    public static ITypeSymbol? GetCollectionOrMemoryElementType(this ITypeSymbol type)
    {
        var elementType = type.GetCollectionElementType();
        if (elementType is not null)
        {
            return elementType;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var constructedFrom = named.ConstructedFrom.ToDisplayString();
            if (constructedFrom is "System.Memory<T>" or "System.ReadOnlyMemory<T>")
            {
                return named.TypeArguments[0];
            }
        }

        return null;
    }

    // -------------------------------------------------------
    // Type resolution
    // -------------------------------------------------------

    // Resolves a type symbol from a fully-qualified name by searching
    // the method's containing assembly and all referenced assemblies.
    public static ITypeSymbol? FindTypeByFullyQualifiedName(this IMethodSymbol mapperMethod, string fullyQualifiedName)
    {
        var typeName = fullyQualifiedName.StartsWith("global::", StringComparison.Ordinal)
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

    // Returns true when a user-defined implicit (isImplicit=true) or explicit
    // conversion operator exists between sourceType and targetType.
    // Both source-declared and target-declared operators are checked.
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
