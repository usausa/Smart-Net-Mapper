namespace Smart.Mapper.Generator.Helpers;

using System.Linq;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Utilities for resolving property paths (dot-separated member access expressions)
// against Roslyn ITypeSymbol instances.
internal static class PropertyPathHelper
{
    // The types a name is looked up in, in order: the type and its base types, or an interface and the
    // interfaces it extends (an interface has no base class), or the constraint types of a type parameter.
    public static IEnumerable<ITypeSymbol> GetLookupTypes(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return typeParameter.ConstraintTypes.SelectMany(GetLookupTypes);
        }

        if (type.TypeKind == TypeKind.Interface)
        {
            return [type, .. type.AllInterfaces];
        }

        var types = new List<ITypeSymbol>();
        for (var current = type; current is not null; current = current.BaseType)
        {
            types.Add(current);
        }

        return types;
    }

    // The member x.Name binds to in the generated code, which the mapper class (within) holds: the first one of
    // the name it can access, up the lookup types. It hides those of its name in the base types, whatever it
    // is, so a name whose member is not a property the mapping takes reaches none.
    public static ISymbol? ResolveMember(ITypeSymbol type, string name, INamedTypeSymbol within, Compilation compilation)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return ResolveInterfaceMember(type, name, within, compilation);
        }

        foreach (var candidateType in GetLookupTypes(type))
        {
            foreach (var member in candidateType.GetMembers(name))
            {
                if (compilation.IsSymbolAccessibleWithin(member, within, type))
                {
                    return member;
                }
            }
        }

        return null;
    }

    // The member of an interface x.Name binds to: of the members of the name in the interface and the ones it
    // extends, those not hidden by one declared in an interface extending theirs. Members of two interfaces that
    // do not hide one another make the name ambiguous (CS0229), so it binds to none.
    private static ISymbol? ResolveInterfaceMember(ITypeSymbol type, string name, INamedTypeSymbol within, Compilation compilation)
    {
        var members = GetLookupTypes(type)
            .SelectMany(t => t.GetMembers(name))
            .Where(m => compilation.IsSymbolAccessibleWithin(m, within, type))
            .ToList();
        var visible = members
            .Where(m => !members.Any(o => o.ContainingType.AllInterfaces.Contains(m.ContainingType, SymbolEqualityComparer.Default)))
            .ToList();
        return visible.Select(static m => m.ContainingType).Distinct(SymbolEqualityComparer.Default).Count() == 1 ? visible[0] : null;
    }

    // The public instance properties of a type as member access reaches them, the ones every lookup and
    // enumeration of the properties of a source or destination goes through: of each name, the member x.Name
    // binds to (ResolveMember), when that is a public instance property. A property overriding one of a base
    // type, or hiding it (new), stands for it; one hidden by a member the mapper class can access that is not a
    // public property (an internal one, a field, a method) is not reached. GetAllPublicProperties walks the
    // base-class chain but not base interfaces (an interface has no base class), so the members an interface
    // inherits, like IReadOnlyList<T>.Count declared on IReadOnlyCollection<T>, need the explicit
    // AllInterfaces sweep. Indexers are left out, as no name reaches them. Readable takes those of a source,
    // which the generated code reads: ones with a getter the mapper class can call on an instance of the type,
    // their own or inherited (a protected one only on a type deriving from the mapper class, CS1540), and not
    // obsolete as an error (CS0619). A type parameter has those of its constraint types.
    public static IReadOnlyList<IPropertySymbol> GetProperties(ITypeSymbol type, INamedTypeSymbol within, Compilation compilation, bool readable = false)
    {
        var properties = new List<IPropertySymbol>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (type is ITypeParameterSymbol)
        {
            foreach (var lookupType in GetLookupTypes(type))
            {
                AddProperties(properties, names, type, lookupType.GetAllPublicProperties(), within, compilation);
            }
        }
        else
        {
            AddProperties(properties, names, type, type.GetAllPublicProperties(), within, compilation);
            if (type.TypeKind == TypeKind.Interface)
            {
                foreach (var iface in type.AllInterfaces)
                {
                    AddProperties(properties, names, type, iface.GetAllPublicProperties(), within, compilation);
                }
            }
        }

        if (readable)
        {
            properties.RemoveAll(p =>
                (p.GetGetter() is not { } getter) || !compilation.IsSymbolAccessibleWithin(getter, within, type) ||
                (p.GetReadObsoleteKind() == ObsoleteKind.Error));
        }

        return properties;
    }

    private static void AddProperties(
        List<IPropertySymbol> properties,
        HashSet<string> names,
        ITypeSymbol type,
        IEnumerable<IPropertySymbol> candidates,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        foreach (var candidate in candidates)
        {
            if (!candidate.IsIndexer && names.Add(candidate.Name) &&
                (ResolveMember(type, candidate.Name, within, compilation) is IPropertySymbol { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public } property))
            {
                properties.Add(property);
            }
        }
    }

    // Single lookup primitive every resolver in this class goes through
    private static IPropertySymbol? FindPropertyCore(ITypeSymbol type, string name, StringComparison comparison, INamedTypeSymbol within, Compilation compilation, bool readable) =>
        GetProperties(type, within, compilation, readable).FirstOrDefault(p => String.Equals(p.Name, name, comparison));

    // Resolves a property by name, preferring an exact ordinal match before falling back to the
    // mapper's configured name comparison. Exact-first keeps the result deterministic when a type
    // exposes names differing only by case, and leaves the default (Ordinal) behavior unchanged.
    public static IPropertySymbol? ResolveProperty(ITypeSymbol type, string name, StringComparison comparison, INamedTypeSymbol within, Compilation compilation, bool readable = false)
    {
        var prop = FindPropertyCore(type, name, StringComparison.Ordinal, within, compilation, readable);
        if ((prop is null) && (comparison != StringComparison.Ordinal))
        {
            prop = FindPropertyCore(type, name, comparison, within, compilation, readable);
        }

        return prop;
    }

    // Walks the path (dot-separated property names) of a source starting from rootType
    // and returns the resulting type and whether the path is valid.
    // Supports Length on arrays and Length/Count on named types.
    public static (ITypeSymbol? Type, bool IsValid) ResolvePropertyPath(ITypeSymbol rootType, string path, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var currentType = rootType;

        foreach (var part in parts)
        {
            var prop = FindPropertyCore(currentType, part, StringComparison.Ordinal, within, compilation, readable: true);
            if (prop is not null)
            {
                currentType = prop.Type;
                continue;
            }

            if ((part == "Length") && (currentType is IArrayTypeSymbol))
            {
                return (currentType.ContainingAssembly?.GetTypeByMetadataName("System.Int32"), true);
            }

            if (((part == "Length") || (part == "Count")) && (currentType is INamedTypeSymbol namedType))
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
    public static IPropertySymbol? ResolvePropertySymbol(ITypeSymbol rootType, IEnumerable<string> parts, INamedTypeSymbol within, Compilation compilation, bool readable = false)
    {
        var current = rootType;
        IPropertySymbol? prop = null;
        foreach (var part in parts)
        {
            prop = FindPropertyCore(current, part, StringComparison.Ordinal, within, compilation, readable);
            if (prop is null)
            {
                return null;
            }
            current = prop.Type;
        }
        return prop;
    }

    // Walks a dot-separated path of a source and returns it rewritten with the declared property names, or
    // null when a segment cannot be resolved. The path is emitted into the generated source, so it has to
    // carry the real casing even when the attribute spelled a segment differently.
    public static string? ResolveCanonicalPath(ITypeSymbol type, string path, StringComparison comparison, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var resolved = new string[parts.Length];
        var currentType = type;

        for (var i = 0; i < parts.Length; i++)
        {
            var prop = ResolveProperty(currentType, parts[i], comparison, within, compilation, readable: true);
            if (prop is null)
            {
                return null;
            }

            resolved[i] = prop.Name;
            currentType = prop.Type;
        }

        return String.Join(".", resolved);
    }

    // Returns the ITypeSymbol at the end of a dot-separated path of a source
    // starting from type, or null when the path cannot be resolved.
    public static ITypeSymbol? ResolvePropertyType(ITypeSymbol type, string path, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var currentType = type;

        foreach (var part in parts)
        {
            var prop = FindPropertyCore(currentType, part, StringComparison.Ordinal, within, compilation, readable: true);
            if (prop is null)
            {
                return null;
            }
            currentType = prop.Type;
        }

        return currentType;
    }
}
