namespace Smart.Mapper.Generator.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

/// <summary>
/// Utilities for resolving property paths (dot-separated member access expressions)
/// against Roslyn <see cref="ITypeSymbol"/> instances.
/// </summary>
internal static class PropertyPathHelper
{
    /// <summary>
    /// Walks <paramref name="path"/> (dot-separated property names) starting from
    /// <paramref name="rootType"/> and returns the resulting type and whether the path is valid.
    /// Supports <c>Length</c> on arrays and <c>Length</c>/<c>Count</c> on named types.
    /// </summary>
    public static (ITypeSymbol? Type, bool IsValid) ResolvePropertyPath(ITypeSymbol rootType, string path)
    {
        var parts = path.Split('.');
        var currentType = rootType;

        foreach (var part in parts)
        {
            var properties = currentType.GetAllPublicInstanceProperties();
            var prop = properties.FirstOrDefault(p => p.Name == part);
            if (prop is not null)
            {
                currentType = prop.Type;
                continue;
            }

            if (part == "Length" && currentType is IArrayTypeSymbol)
            {
                return (currentType.ContainingAssembly?.GetTypeByMetadataName("System.Int32"), true);
            }

            if ((part == "Length" || part == "Count") && currentType is INamedTypeSymbol namedType)
            {
                var member = namedType.GetMembers(part).FirstOrDefault();
                if (member is IPropertySymbol propSymbol)
                {
                    currentType = propSymbol.Type;
                    continue;
                }
            }

            return (null, false);
        }

        return (currentType, true);
    }

    /// <summary>
    /// Resolves the <see cref="IPropertySymbol"/> at the end of a pre-split property path,
    /// returning <c>null</c> when any segment cannot be found.
    /// </summary>
    public static IPropertySymbol? ResolvePropertySymbol(ITypeSymbol rootType, string[] parts)
    {
        var current = rootType;
        IPropertySymbol? prop = null;
        foreach (var part in parts)
        {
            prop = current.GetAllPublicInstanceProperties().FirstOrDefault(p => p.Name == part);
            if (prop is null)
            {
                return null;
            }
            current = prop.Type;
        }
        return prop;
    }

    /// <summary>
    /// Returns the <see cref="ITypeSymbol"/> at the end of a dot-separated <paramref name="path"/>
    /// starting from <paramref name="type"/>, or <c>null</c> when the path cannot be resolved.
    /// </summary>
    public static ITypeSymbol? ResolvePropertyType(ITypeSymbol type, string path)
    {
        var parts = path.Split('.');
        var currentType = type;

        foreach (var part in parts)
        {
            var prop = currentType.GetAllPublicInstanceProperties().FirstOrDefault(p => p.Name == part);
            if (prop is null)
            {
                return null;
            }
            currentType = prop.Type;
        }

        return currentType;
    }
}
