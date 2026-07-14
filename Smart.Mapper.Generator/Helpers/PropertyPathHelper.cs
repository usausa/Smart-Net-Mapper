namespace Smart.Mapper.Generator.Helpers;

using System.Linq;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Utilities for resolving property paths (dot-separated member access expressions)
// against Roslyn ITypeSymbol instances.
internal static class PropertyPathHelper
{
    // Walks the path (dot-separated property names) starting from rootType
    // and returns the resulting type and whether the path is valid.
    // Supports Length on arrays and Length/Count on named types.
    public static (ITypeSymbol? Type, bool IsValid) ResolvePropertyPath(ITypeSymbol rootType, string path)
    {
        var parts = path.Split('.');
        var currentType = rootType;

        foreach (var part in parts)
        {
            var properties = currentType.GetAllPublicProperties();
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

    // Resolves the IPropertySymbol at the end of a pre-split property path,
    // returning null when any segment cannot be found.
    public static IPropertySymbol? ResolvePropertySymbol(ITypeSymbol rootType, IEnumerable<string> parts)
    {
        var current = rootType;
        IPropertySymbol? prop = null;
        foreach (var part in parts)
        {
            prop = current.GetAllPublicProperties().FirstOrDefault(p => p.Name == part);
            if (prop is null)
            {
                return null;
            }
            current = prop.Type;
        }
        return prop;
    }

    // Returns the ITypeSymbol at the end of a dot-separated path
    // starting from type, or null when the path cannot be resolved.
    public static ITypeSymbol? ResolvePropertyType(ITypeSymbol type, string path)
    {
        var parts = path.Split('.');
        var currentType = type;

        foreach (var part in parts)
        {
            var prop = currentType.GetAllPublicProperties().FirstOrDefault(p => p.Name == part);
            if (prop is null)
            {
                return null;
            }
            currentType = prop.Type;
        }

        return currentType;
    }
}
