namespace Smart.Mapper.Generator.Helpers;

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

// NOTE: All methods in this file are candidates for promotion to the SourceGenerateHelper library.

/// <summary>
/// General-purpose Roslyn symbol extension methods not specific to the mapper domain.
/// </summary>
internal static class RoslynSymbolExtensions
{
    // -------------------------------------------------------
    // Type assignability
    // -------------------------------------------------------

    /// <summary>
    /// Returns true when <paramref name="sourceType"/> is assignable to <paramref name="targetType"/>,
    /// including identity, inheritance, and interface implementation checks.
    /// Nullable-annotated targets are treated as their non-nullable counterpart for the identity check.
    /// </summary>
    public static bool IsAssignableTo(this ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
        {
            return true;
        }

        if (targetType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            var nonNullableTarget = targetType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            if (SymbolEqualityComparer.Default.Equals(sourceType, nonNullableTarget))
            {
                return true;
            }
        }

        var current = sourceType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, targetType))
            {
                return true;
            }
            current = current.BaseType;
        }

        foreach (var iface in sourceType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, targetType))
            {
                return true;
            }
        }

        return false;
    }

    // -------------------------------------------------------
    // Nullability
    // -------------------------------------------------------

    /// <summary>
    /// Returns true when the type is nullable — either a nullable reference type annotation
    /// or a <c>Nullable&lt;T&gt;</c> value type.
    /// </summary>
    public static bool IsNullableSymbol(this ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the underlying non-nullable type: unwraps <c>Nullable&lt;T&gt;</c> value types
    /// and strips the nullable annotation from reference types.
    /// </summary>
    public static ITypeSymbol GetUnderlyingType(this ITypeSymbol type)
    {
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated &&
            type is INamedTypeSymbol refType)
        {
            return refType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        return type;
    }

    // -------------------------------------------------------
    // Numeric
    // -------------------------------------------------------

    /// <summary>
    /// Returns true when the type is one of the eight integer special types
    /// (sbyte, byte, short, ushort, int, uint, long, ulong).
    /// </summary>
    public static bool IsNumericType(this ITypeSymbol type) =>
        type.SpecialType is
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64;

    // -------------------------------------------------------
    // Collection element type
    // -------------------------------------------------------

    /// <summary>
    /// Returns the element type of a collection type (array or generic <c>IEnumerable&lt;T&gt;</c>),
    /// or <c>null</c> when the type is not a recognised collection.
    /// </summary>
    public static ITypeSymbol? GetCollectionElementType(this ITypeSymbol collectionType)
    {
        if (collectionType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        if (collectionType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType &&
                    iface.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                {
                    return iface.TypeArguments[0];
                }
            }

            if (namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }
        }

        return null;
    }

    // -------------------------------------------------------
    // Properties
    // -------------------------------------------------------

    /// <summary>
    /// Returns all public, non-static instance properties declared on <paramref name="type"/>
    /// and all of its base types, in declaration order (derived first).
    /// </summary>
    public static List<IPropertySymbol> GetAllPublicInstanceProperties(this ITypeSymbol type)
    {
        var properties = new List<IPropertySymbol>();
        var currentType = type;

        while (currentType is not null)
        {
            properties.AddRange(currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(static p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public));

            currentType = currentType.BaseType;
        }

        return properties;
    }

    // -------------------------------------------------------
    // Interface implementation checks
    // -------------------------------------------------------

    /// <summary>
    /// Returns true when <paramref name="typeSymbol"/> implements the open generic interface
    /// identified by <paramref name="genericInterfaceDefinition"/>.
    /// </summary>
    public static bool ImplementsGenericInterface(this ITypeSymbol typeSymbol, INamedTypeSymbol genericInterfaceDefinition) =>
        typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, genericInterfaceDefinition));

    /// <summary>
    /// Returns true when <paramref name="typeSymbol"/> implements an interface whose fully-qualified
    /// metadata name matches <paramref name="metadataName"/>.
    /// </summary>
    public static bool ImplementsInterfaceByName(this ITypeSymbol typeSymbol, string metadataName) =>
        typeSymbol.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == metadataName ||
            i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == $"global::{metadataName}" ||
            i.OriginalDefinition.MetadataName == metadataName.Split('.').Last());
}
