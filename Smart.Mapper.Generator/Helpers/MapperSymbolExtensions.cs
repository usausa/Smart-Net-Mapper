namespace Smart.Mapper.Generator.Helpers;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Mapper-domain Roslyn extension methods that are not candidates for SourceGenerateHelper promotion.
internal static class MapperSymbolExtensions
{
    // -------------------------------------------------------
    // Properties
    // -------------------------------------------------------

    // The accessors a read or an assignment of a property calls: its own, or else those of the property it
    // overrides, which a property overriding one accessor only inherits the other one of.
    public static IMethodSymbol? GetSetter(this IPropertySymbol property)
    {
        for (var current = property; current is not null; current = current.OverriddenProperty)
        {
            if (current.SetMethod is { } setter)
            {
                return setter;
            }
        }

        return null;
    }

    public static IMethodSymbol? GetGetter(this IPropertySymbol property)
    {
        for (var current = property; current is not null; current = current.OverriddenProperty)
        {
            if (current.GetMethod is { } getter)
            {
                return getter;
            }
        }

        return null;
    }

    // -------------------------------------------------------
    // Obsolete
    // -------------------------------------------------------

    // How C# reports a use of the member: the [Obsolete] of its original definition, as member lookup finds the
    // member an override overrides (one only an override has is reported where it is declared, CS0809, and not
    // where it is used).
    public static ObsoleteKind GetObsoleteKind(this ISymbol symbol)
    {
        var original = symbol switch
        {
            IPropertySymbol property => GetOriginalDefinition(property),
            IMethodSymbol method => GetOriginalDefinition(method),
            _ => symbol
        };
        foreach (var attribute in original.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute")
            {
                return (attribute.ConstructorArguments.Length == 2) && (attribute.ConstructorArguments[1].Value is true)
                    ? ObsoleteKind.Error
                    : ObsoleteKind.Warning;
            }
        }

        return ObsoleteKind.None;
    }

    // How C# reports a read or an assignment of the property: the [Obsolete] of the property, or of the accessor
    // called, whichever is reported more strictly.
    public static ObsoleteKind GetReadObsoleteKind(this IPropertySymbol property) =>
        Max(property.GetObsoleteKind(), property.GetGetter()?.GetObsoleteKind() ?? ObsoleteKind.None);

    public static ObsoleteKind GetWriteObsoleteKind(this IPropertySymbol property) =>
        Max(property.GetObsoleteKind(), property.GetSetter()?.GetObsoleteKind() ?? ObsoleteKind.None);

    private static ObsoleteKind Max(ObsoleteKind left, ObsoleteKind right) =>
        left > right ? left : right;

    private static IPropertySymbol GetOriginalDefinition(IPropertySymbol property)
    {
        var current = property;
        while (current.OverriddenProperty is { } overridden)
        {
            current = overridden;
        }

        return current;
    }

    private static IMethodSymbol GetOriginalDefinition(IMethodSymbol method)
    {
        var current = method;
        while (current.OverriddenMethod is { } overridden)
        {
            current = overridden;
        }

        return current;
    }

    // -------------------------------------------------------
    // Collections
    // -------------------------------------------------------

    // Returns the element type of a collection-like type, additionally recognizing
    // Memory<T> / ReadOnlyMemory<T>, which the shared GetCollectionElementType helper does not.
    public static ITypeSymbol? GetCollectionOrMemoryElementType(this ITypeSymbol type)
    {
        var elementType = type.GetEnumerableElementType();
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

    // Returns the element type of a collection, falling back to the IEnumerable<T> a type implements
    // through its base type or interfaces (such as class ItemList : List<Item>), which the shared
    // GetCollectionElementType helper does not look for. A string is not taken as a collection of chars.
    public static ITypeSymbol? GetEnumerableElementType(this ITypeSymbol type)
    {
        var elementType = type.GetCollectionElementType();
        if ((elementType is not null) || (type.SpecialType == SpecialType.System_String))
        {
            return elementType;
        }

        return type.AllInterfaces
            .FirstOrDefault(static i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)?
            .TypeArguments[0];
    }

    // -------------------------------------------------------
    // Type resolution
    // -------------------------------------------------------

    // The types found by name, per compilation, which the assembly of the mapper method stands for: every mapper looks
    // up the same few (the default converter classes, IParsable<T>) through the referenced assemblies one by one. The
    // table lets go of them with the compilation, and the dictionary takes lookups made at the same time, as the models
    // of several methods may be built in parallel.
    private static readonly ConditionalWeakTable<IAssemblySymbol, ConcurrentDictionary<string, ITypeSymbol?>> TypesByName = new();

    private static readonly ConditionalWeakTable<IAssemblySymbol, ConcurrentDictionary<string, INamedTypeSymbol?>> ReferencedTypesByName = new();

    // Resolves a type symbol from a fully-qualified name by searching
    // the method's containing assembly and all referenced assemblies.
    public static ITypeSymbol? FindTypeByFullyQualifiedName(this IMethodSymbol mapperMethod, string fullyQualifiedName) =>
        TypesByName.GetValue(mapperMethod.ContainingAssembly, static _ => new ConcurrentDictionary<string, ITypeSymbol?>(StringComparer.Ordinal))
            .GetOrAdd(fullyQualifiedName, name => LookupTypeByFullyQualifiedName(mapperMethod, name));

    // The type of the metadata name in the first referenced assembly defining it, in the order of the references.
    public static INamedTypeSymbol? FindReferencedType(this IMethodSymbol mapperMethod, string metadataName) =>
        ReferencedTypesByName.GetValue(mapperMethod.ContainingAssembly, static _ => new ConcurrentDictionary<string, INamedTypeSymbol?>(StringComparer.Ordinal))
            .GetOrAdd(metadataName, name => mapperMethod.ContainingModule.ReferencedAssemblySymbols
                .Select(reference => reference.GetTypeByMetadataName(name))
                .FirstOrDefault(static type => type is not null));

    private static ITypeSymbol? LookupTypeByFullyQualifiedName(IMethodSymbol mapperMethod, string fullyQualifiedName)
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
    // Both source-declared and target-declared operators are checked. One obsolete as an error is not used, as
    // the generated code could not call it (CS0619).
    public static bool HasUserDefinedConversion(ITypeSymbol sourceType, ITypeSymbol targetType, bool isImplicit)
    {
        var operatorName = isImplicit
            ? WellKnownMemberNames.ImplicitConversionName
            : WellKnownMemberNames.ExplicitConversionName;

        foreach (var declaringType in new[] { sourceType, targetType })
        {
            foreach (var member in declaringType.GetMembers(operatorName).OfType<IMethodSymbol>())
            {
                if ((member.MethodKind != MethodKind.Conversion) || !member.IsStatic || (member.GetObsoleteKind() == ObsoleteKind.Error))
                {
                    continue;
                }

                if ((member.Parameters.Length == 1) &&
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
