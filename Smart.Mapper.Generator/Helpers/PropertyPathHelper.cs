namespace Smart.Mapper.Generator.Helpers;

using System.Linq;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Utilities for resolving property paths (dot-separated member access expressions)
// against Roslyn ITypeSymbol instances.
internal static class PropertyPathHelper
{
    // Single lookup primitive every resolver in this class goes through. GetAllPublicProperties
    // walks the base-class chain but not base interfaces (an interface has no base class), so
    // members like IReadOnlyList<T>.Count - declared on IReadOnlyCollection<T> - need the explicit
    // AllInterfaces sweep.
    private static IPropertySymbol? FindPropertyCore(ITypeSymbol type, string name, StringComparison comparison)
    {
        var prop = type.GetAllPublicProperties().FirstOrDefault(p => String.Equals(p.Name, name, comparison));
        if (prop is not null)
        {
            return prop;
        }

        if (type.TypeKind == TypeKind.Interface)
        {
            foreach (var iface in type.AllInterfaces)
            {
                var ifaceProp = iface.GetAllPublicProperties().FirstOrDefault(p => String.Equals(p.Name, name, comparison));
                if (ifaceProp is not null)
                {
                    return ifaceProp;
                }
            }
        }

        return null;
    }

    // Resolves a property by name, preferring an exact ordinal match before falling back to the
    // mapper's configured name comparison. Exact-first keeps the result deterministic when a type
    // exposes names differing only by case, and leaves the default (Ordinal) behavior unchanged.
    public static IPropertySymbol? ResolveProperty(ITypeSymbol type, string name, StringComparison comparison)
    {
        var prop = FindPropertyCore(type, name, StringComparison.Ordinal);
        if ((prop is null) && (comparison != StringComparison.Ordinal))
        {
            prop = FindPropertyCore(type, name, comparison);
        }

        return prop;
    }

    // Walks the path (dot-separated property names) starting from rootType
    // and returns the resulting type and whether the path is valid.
    // Supports Length on arrays and Length/Count on named types.
    public static (ITypeSymbol? Type, bool IsValid) ResolvePropertyPath(ITypeSymbol rootType, string path)
    {
        var parts = path.Split('.');
        var currentType = rootType;

        foreach (var part in parts)
        {
            var prop = FindPropertyCore(currentType, part, StringComparison.Ordinal);
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
            prop = FindPropertyCore(current, part, StringComparison.Ordinal);
            if (prop is null)
            {
                return null;
            }
            current = prop.Type;
        }
        return prop;
    }

    // Walks a dot-separated path and returns it rewritten with the declared property names, or null
    // when a segment cannot be resolved. The path is emitted into the generated source, so it has to
    // carry the real casing even when the attribute spelled a segment differently.
    public static string? ResolveCanonicalPath(ITypeSymbol type, string path, StringComparison comparison)
    {
        var parts = path.Split('.');
        var resolved = new string[parts.Length];
        var currentType = type;

        for (var i = 0; i < parts.Length; i++)
        {
            var prop = ResolveProperty(currentType, parts[i], comparison);
            if (prop is null)
            {
                return null;
            }

            resolved[i] = prop.Name;
            currentType = prop.Type;
        }

        return String.Join(".", resolved);
    }

    // Returns the ITypeSymbol at the end of a dot-separated path
    // starting from type, or null when the path cannot be resolved.
    public static ITypeSymbol? ResolvePropertyType(ITypeSymbol type, string path)
    {
        var parts = path.Split('.');
        var currentType = type;

        foreach (var part in parts)
        {
            var prop = FindPropertyCore(currentType, part, StringComparison.Ordinal);
            if (prop is null)
            {
                return null;
            }
            currentType = prop.Type;
        }

        return currentType;
    }
}
