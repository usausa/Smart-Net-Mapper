namespace Smart.Mapper.Generator;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Mapper.Generator.Helpers;
using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// The culture and the formats, the conversions of the values, the construction of the destination and the property
// mappings
internal static partial class MapperModelBuilder
{
    internal static DiagnosticInfo? ValidateCultureAndFormat(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        // A culture names the field the generated code gets it into, so it has to be a culture name
        // The culture of the method, of [Mapper] or [MapperProfile], is checked first, so that a mapping taking it
        // is not reported at its [MapProperty]
        var cultures = model.PropertyMappings.Select(static m => (Culture: m.EffectiveCulture, m.AttributeIndex))
            .Prepend((model.Culture, AttributeIndex: model.CultureAttributeIndex));
        foreach (var (culture, attributeIndex) in cultures)
        {
            if (!String.IsNullOrEmpty(culture) && !IsValidCultureName(culture!))
            {
                return new DiagnosticInfo(Diagnostics.InvalidCultureName, LocationOf(model, attributeIndex, syntax), model.MethodName, culture!);
            }
        }

        return null;
    }

    // The culture of the profile of the assembly that is not a culture name, reported once at the profile, with the
    // syntax tree it is in, where the diagnostic is located; the mappers go without it (ParseConverterAttributes)
    internal static (EquatableArray<DiagnosticInfo> Diagnostics, SyntaxTree? Tree) ValidateAssemblyProfile(Compilation compilation)
    {
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if ((attribute.AttributeClass is { Name: "MapperProfileAttribute" } attributeClass) &&
                (attributeClass.ToDisplayString() == Names.MapperProfileAttribute) &&
                (attribute.NamedArguments.FirstOrDefault(static a => a.Key == "Culture").Value.Value is string { Length: > 0 } culture) &&
                !IsValidCultureName(culture) &&
                (attribute.ApplicationSyntaxReference?.GetSyntax() is { } syntax))
            {
                return (new EquatableArray<DiagnosticInfo>([new DiagnosticInfo(Diagnostics.InvalidAssemblyCultureName, syntax.GetLocation(), culture)]), syntax.SyntaxTree);
            }
        }

        return (default, null);
    }

    // The culture of [Mapper] is not used by a method taking a CultureInfo, which gives the culture instead. A nullable
    // one falls back to it for a null argument, so it is used then.
    internal static IEnumerable<DiagnosticInfo> CollectCultureParameterWarnings(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        if ((model.CultureParameterName is not null) && !model.IsCultureParameterNullable && model.CultureExplicitlySet)
        {
            yield return new DiagnosticInfo(
                Diagnostics.CultureOverriddenByParameter,
                LocationOf(model, model.CultureAttributeIndex, syntax),
                model.MethodName,
                model.Culture ?? string.Empty,
                model.CultureParameterName);
        }
    }

    // The culture a conversion goes with, as its name, whose field the generated code declares, and the argument the
    // conversion takes: the culture of [MapProperty] first, then the CultureInfo parameter, the culture of [Mapper] or
    // of a profile, and the current culture under DefaultCulture Current. A null CultureInfo gives the culture the
    // method takes without one. An empty culture name is the invariant culture's, which a conversion takes as it
    // takes no culture: without one, the converter's invariant one, or with the invariant culture when a format
    // applies to it.
    private static (string? Name, string? Argument) ResolveCulture(MapperMethodModel model, string? propertyCulture, bool formatApplies)
    {
        var invariant = formatApplies ? Names.InvariantCulture : null;
        if (propertyCulture is not null)
        {
            return propertyCulture.Length > 0 ? (propertyCulture, MapperSourceBuilder.GetCultureFieldName(propertyCulture)) : (null, invariant);
        }

        var name = String.IsNullOrEmpty(model.Culture) ? null : model.Culture;
        if (model.CultureParameterName is { } parameter)
        {
            return model.IsCultureParameterNullable ? (name, GetNullableCultureArgument(model, parameter)) : (null, parameter);
        }

        return (name, GetMethodCultureArgument(model) ?? invariant);
    }

    // The culture of the method as the argument of a conversion, its culture name or the current culture under
    // DefaultCulture Current; null for the invariant culture, an empty culture name as well
    private static string? GetMethodCultureArgument(MapperMethodModel model) =>
        model.Culture is { Length: > 0 } name ? MapperSourceBuilder.GetCultureFieldName(name) :
        (model.Culture is null) && model.UseCurrentCulture ? Names.CurrentCulture :
        null;

    // The culture a CultureInfo parameter that may be null gives: its value, or for null the culture of the method,
    // which is the invariant culture without one
    internal static string GetNullableCultureArgument(MapperMethodModel model, string parameter) =>
        "(" + parameter + " ?? " + (GetMethodCultureArgument(model) ?? Names.InvariantCulture) + ")";

    // The format a conversion between the types takes: the date and time format for a date or a time, the numeric one
    // otherwise, as the generated call passes it (MapperSourceBuilder.DetermineFormatArg)
    private static string? GetApplyingFormat(string? sourceType, string? targetType, string? dateTimeFormat, string? numberFormat) =>
        TypeNameHelper.IsDateTimeType(sourceType ?? string.Empty) || TypeNameHelper.IsDateTimeType(targetType ?? string.Empty) ? dateTimeFormat : numberFormat;

    // Whether a format decides how text is parsed into the target, which leaves the parse to the value converter: a
    // format applies to the parse of the numbers and the dates and times only. The Parse of IParsable<T> takes no
    // format, so a format of the method or a profile does not keep it from parsing the other types.
    private static bool HasParseFormat(PropertyMappingModel mapping)
    {
        var target = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;
        return TypeNameHelper.IsBuiltInNumericOrDateType(target) &&
               (GetApplyingFormat(mapping.SourceUnderlyingType, target, mapping.EffectiveDateTimeFormat, mapping.EffectiveNumberFormat) is not null);
    }

    // A culture name as CultureInfo takes one: a language of 1 to 8 ASCII letters, subtags of 1 to 8 ASCII
    // letters and digits after hyphens (zh-Hant-TW), and an alternate sort order after an underscore
    // (de-DE_phoneb). Whether the culture exists depends on the system the mapper runs on, so it is not
    // checked here.
    internal static bool IsValidCultureName(string name)
    {
        var parts = name.Split('_');
        if ((parts.Length > 2) || ((parts.Length == 2) && !IsSubtag(parts[1], lettersOnly: false)))
        {
            return false;
        }

        var subtags = parts[0].Split('-');
        return IsSubtag(subtags[0], lettersOnly: true) && subtags.Skip(1).All(static t => IsSubtag(t, lettersOnly: false));
    }

    private static bool IsSubtag(string text, bool lettersOnly) =>
        (text.Length is >= 1 and <= 8) &&
        text.All(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') || (!lettersOnly && c is >= '0' and <= '9'));

    internal static DiagnosticInfo? ValidateNoTypeConverterFallback(
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        if (model.MapConverterTypeName is not null)
        {
            return null;
        }

        // Seeded with every mapping so that the many `continue` paths cannot drop one; only the
        // entries this pass actually rewrites are replaced.
        var resolved = new List<PropertyMappingModel>(model.PropertyMappings);
        for (var i = 0; i < resolved.Count; i++)
        {
            var mapping = resolved[i];
            if (mapping.HasConverter())
            {
                continue;
            }

            if (!mapping.RequiresConversion)
            {
                continue;
            }

            if (mapping.IsEnumMapping())
            {
                continue;
            }

            if (mapping.HasSpecializedConverter())
            {
                continue;
            }

            if (mapping.HasParsableMethod())
            {
                continue;
            }

            if (mapping.RequiresExplicitNumericCast)
            {
                continue;
            }

            if (mapping.UserDefinedConversion == UserDefinedConversionKind.Implicit)
            {
                continue;
            }

            if (mapping.HasUserDefinedExplicit())
            {
                continue;
            }

            if (mapping.UseFormattable)
            {
                continue;
            }

            {
                // To a string, a value the conversions above did not claim is formatted with
                // ToString(format, provider). A type without that method goes on to the fallback,
                // reported below, rather than leaving the call to fail in the generated code.
                var lcTargetType = !String.IsNullOrEmpty(mapping.TargetUnderlyingType) ? mapping.TargetUnderlyingType : mapping.TargetType;
                if (TypeNameHelper.IsStringType(lcTargetType))
                {
                    var lcSourceType = !String.IsNullOrEmpty(mapping.SourceUnderlyingType) ? mapping.SourceUnderlyingType : mapping.SourceType;
                    var sourceValueType = PropertyPathHelper.ResolvePropertySymbol(sourceType, mapping.SourcePath.Split('.'), within, compilation, readable: true)?.Type.GetUnderlyingType();
                    if (!TypeNameHelper.IsBuiltInNumericOrDateType(lcSourceType) &&
                        ((sourceValueType is null) || HasFormatToString(sourceValueType)))
                    {
                        resolved[i] = mapping with { UseFormattable = true };
                        continue;
                    }
                }
            }

            var effectiveSource = !String.IsNullOrEmpty(mapping.SourceUnderlyingType) ? mapping.SourceUnderlyingType : mapping.SourceType;
            var effectiveDest = !String.IsNullOrEmpty(mapping.TargetUnderlyingType) ? mapping.TargetUnderlyingType : mapping.TargetType;

            if (effectiveSource == effectiveDest)
            {
                continue;
            }

            // A class, a struct or a collection going to one no conversion takes is most likely a nested member or a
            // collection without its [MapNested] / [MapCollection], which the message tells
            var descriptor =
                IsCompositeType(PropertyPathHelper.ResolvePropertySymbol(sourceType, mapping.SourcePath.Split('.'), within, compilation, readable: true)?.Type) &&
                IsCompositeType(GetMappingTargetType(mapping, model, destinationType, GetEffectiveConstructor(model, destinationType), within, compilation))
                    ? Diagnostics.UnmappedCompositeConversion
                    : Diagnostics.TypeConverterFallbackNotAllowed;
            return new DiagnosticInfo(descriptor, LocationOf(model, mapping.AttributeIndex, syntax), model.MethodName, mapping.TargetPath);
        }

        model = model with { PropertyMappings = new(resolved) };
        return null;
    }

    // A class, a struct, an interface or an array other than the types C# has keywords for (string, object, decimal,
    // ...), as its value or as the value of a nullable struct: a nested object or a collection, not a value the
    // conversions take.
    private static bool IsCompositeType(ITypeSymbol? type) =>
        (type?.GetUnderlyingType() is { SpecialType: SpecialType.None } underlying) &&
        (underlying.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Array);

    // Reported at the [MapExpression] attribute
    internal static IEnumerable<DiagnosticInfo> CollectMapExpressionReflectionWarnings(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        foreach (var expression in model.ExpressionMappings)
        {
            foreach (var pattern in MapperSourceBuilder.ReflectionPatterns)
            {
                if (expression.Expression.IndexOf(pattern, StringComparison.Ordinal) >= 0)
                {
                    yield return new DiagnosticInfo(Diagnostics.MapExpressionReflectionNotAllowed, LocationOf(model, expression.AttributeIndex, syntax), model.MethodName, expression.TargetName);
                    break;
                }
            }
        }
    }

    // A void mapper assigns properties on a caller-supplied instance, so init-only targets can never
    // be set. Reports SMP0302 when any mapping (automap or explicit feature) targets an init-only member.
    internal static DiagnosticInfo? ValidateVoidMapperInitOnlyTargets(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        if (model.ReturnsDestination)
        {
            return null;
        }

        // The first mapping to such a target, at its attribute or at the method for the automatic mapping
        var initOnlyTargets = model.PropertyMappings.Where(static pm => pm.IsTargetInitOnly).Select(static pm => pm.AttributeIndex)
            .Concat(model.ConstantMappings.Where(static cm => cm.IsTargetInitOnly).Select(static cm => cm.AttributeIndex))
            .Concat(model.ExpressionMappings.Where(static em => em.IsTargetInitOnly).Select(static em => em.AttributeIndex))
            .Concat(model.MapUsingMappings.Where(static mu => mu.IsTargetInitOnly).Select(static mu => mu.AttributeIndex))
            .Concat(model.MapFromMappings.Where(static mf => mf.IsTargetInitOnly).Select(static mf => mf.AttributeIndex))
            .ToList();

        return initOnlyTargets.Count > 0
            ? new DiagnosticInfo(Diagnostics.InitOnlyDestinationRequiresReturnMapper, LocationOf(model, initOnlyTargets[0], syntax), model.MethodName, model.DestinationTypeName)
            : null;
    }

    // The required members of the destination a return mapper creates have to be mapped, as they are set in
    // the object initializer; ignoring one would leave it unset (SMP0304, CS9035 otherwise). That is every one
    // of them, properties and fields, of any accessibility, and those of its base types. A constructor with
    // [SetsRequiredMembers] sets them itself, and a void mapper fills an instance that exists and never
    // constructs, so they do not concern either.
    //
    // A member the dotted paths of the attributes write into is set when the object initializer creates it:
    // a path only the initializer reaches creates it there with the member at its end (Item = new Child() {
    // Value = ... }), and otherwise the initializer creates it when its type can be (RequiredMemberCreations),
    // and the paths write into it after construction, as they do into any member. One of a type that cannot
    // be created is not set.
    internal static DiagnosticInfo? ValidateRequiredMembers(
        ref MapperMethodModel model,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        if (!model.ReturnsDestination || ConstructionSetsRequiredMembers(model, destinationType))
        {
            return null;
        }

        var mappedTargets = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pm in model.PropertyMappings)
        {
            mappedTargets.Add(pm.TargetPath);
        }

        foreach (var cm in model.ConstantMappings)
        {
            mappedTargets.Add(cm.TargetName);
        }

        foreach (var em in model.ExpressionMappings)
        {
            mappedTargets.Add(em.TargetName);
        }

        foreach (var mu in model.MapUsingMappings)
        {
            mappedTargets.Add(mu.TargetName);
        }

        foreach (var mf in model.MapFromMappings)
        {
            mappedTargets.Add(mf.TargetName);
        }

        foreach (var mc in model.MapCollectionMappings)
        {
            mappedTargets.Add(mc.TargetName);
        }

        foreach (var mn in model.MapNestedMappings)
        {
            mappedTargets.Add(mn.TargetName);
        }

        // The heads of the dotted target paths, and whether a path only the object initializer reaches
        // creates the head there
        var dottedHeads = new Dictionary<string, bool>(StringComparer.Ordinal);
        void AddDottedHead(string path, bool isInitializerRoute)
        {
            var index = path.IndexOf('.');
            if (index > 0)
            {
                var head = path.Substring(0, index);
                dottedHeads[head] = (dottedHeads.TryGetValue(head, out var created) && created) || isInitializerRoute;
            }
        }

        foreach (var pm in model.PropertyMappings)
        {
            AddDottedHead(pm.TargetPath, pm.IsTargetInitOnly);
        }

        foreach (var cm in model.ConstantMappings)
        {
            AddDottedHead(cm.TargetName, cm.IsTargetInitOnly);
        }

        foreach (var em in model.ExpressionMappings)
        {
            AddDottedHead(em.TargetName, em.IsTargetInitOnly);
        }

        foreach (var mu in model.MapUsingMappings)
        {
            AddDottedHead(mu.TargetName, mu.IsTargetInitOnly);
        }

        var constructor = GetEffectiveConstructor(model, destinationType);
        var arguments = model.ConstructorParameters;
        var nameComparison = (StringComparison)model.NameComparison;
        var creations = new List<NestedPathSegment>();
        foreach (var name in GetRequiredMemberNames(destinationType))
        {
            // A constructor argument assigns the member, and the object initializer would have to set it again,
            // replacing what the constructor made of the argument; [SetsRequiredMembers] lets the argument stand.
            // An optional parameter left out passes none.
            var parameter = constructor?.Parameters.FirstOrDefault(p =>
                MatchesConstructorParameter(p, name, nameComparison) && arguments.Any(a => a.ParamName == p.Name));
            if (parameter is not null)
            {
                return new DiagnosticInfo(Diagnostics.RequiredMemberConstructorArgument, syntax.GetLocation(), model.MethodName, name, parameter.Name);
            }

            if (mappedTargets.Contains(name))
            {
                continue;
            }

            if (dottedHeads.TryGetValue(name, out var createdByPath))
            {
                if (createdByPath)
                {
                    continue;
                }

                if ((FindTargetMember(destinationType, name, within, compilation) is { } member) &&
                    IsAssignableInInitializer(member, destinationType, within, compilation) &&
                    (GetMemberType(member) is { } type) &&
                    CanCreateInstance(type, within, compilation))
                {
                    creations.Add(new NestedPathSegment(name, GetCreatedTypeName(type), false));
                    continue;
                }
            }

            return model.IgnoreTargets.Contains(name)
                ? new DiagnosticInfo(Diagnostics.IgnoredConstructorParameter, LocationOf(model, IgnoreAttributeIndex(model, name), syntax), model.MethodName, name)
                : new DiagnosticInfo(Diagnostics.UnmappedRequiredProperty, syntax.GetLocation(), model.MethodName, name);
        }

        model = model with { RequiredMemberCreations = new(creations) };
        return null;
    }

    // Whether the constructor a return mapper calls has [SetsRequiredMembers], so that the required members
    // do not have to be set in the object initializer: the constructor taking the arguments, or else the one
    // new Dst() binds to, without parameters or with optional ones only.
    private static bool ConstructionSetsRequiredMembers(MapperMethodModel model, ITypeSymbol destinationType)
    {
        if (!model.ReturnsDestination || (destinationType is not INamedTypeSymbol named))
        {
            return false;
        }

        var constructor = GetEffectiveConstructor(model, destinationType) ??
                          named.InstanceConstructors.FirstOrDefault(static c => c.Parameters.Length == 0) ??
                          named.InstanceConstructors.FirstOrDefault(static c => c.Parameters.All(static p => p.IsOptional || p.IsParams));
        return (constructor is not null) && SetsRequiredMembers(constructor);
    }

    // The names of the required members of a type and its base types, properties and fields of any
    // accessibility: a required member is as visible as its type, so the generated code can assign it. One
    // overriding a required property is required as well, and is named once.
    private static IEnumerable<string> GetRequiredMemberNames(ITypeSymbol type)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if ((member is IPropertySymbol { IsRequired: true, IsStatic: false } or IFieldSymbol { IsRequired: true, IsStatic: false }) &&
                    names.Add(member.Name))
                {
                    yield return member.Name;
                }
            }
        }
    }

    internal static DiagnosticInfo? BuildConstructorParameterMappings(
        ref MapperMethodModel model,
        ITypeSymbol destinationType,
        ITypeSymbol sourceType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        // A void mapper never constructs: it fills the instance it is given, so no constructor applies to it
        if (!model.ReturnsDestination)
        {
            return null;
        }

        // A type parameter is created with new T(), which its constraints have to allow (CS0304 otherwise)
        if ((destinationType is ITypeParameterSymbol) && !CanCreateInstance(destinationType, within, compilation))
        {
            return new DiagnosticInfo(
                Diagnostics.UncreatableDestination,
                syntax.GetLocation(),
                model.MethodName,
                destinationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        if (destinationType is not INamedTypeSymbol namedType)
        {
            return null;
        }

        var constructor = GetEffectiveConstructor(model, destinationType);
        if (constructor is null)
        {
            // Construction is parameterless, which a type that is abstract or an interface, or without a
            // constructor the mapper class can call without arguments, cannot do (CS0144 / CS0122 otherwise)
            if (!CanConstructWithoutArguments(namedType, within, compilation))
            {
                return new DiagnosticInfo(
                    Diagnostics.UncreatableDestination,
                    syntax.GetLocation(),
                    model.MethodName,
                    destinationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            // It must still initialize init-only or required members via an object initializer, since
            // `new Dst()` cannot assign them afterwards, and the members of a dotted path only an initializer
            // reaches.
            if (PropertyPathHelper.GetProperties(destinationType, within, compilation).Any(static p => p.GetSetter()?.IsInitOnly == true) ||
                GetRequiredMemberNames(destinationType).Any() ||
                model.PropertyMappings.Any(static pm => pm.IsTargetInitOnly))
            {
                model = model with { UseConstructorMapping = true };
            }

            return null;
        }

        model = model with { UseConstructorMapping = true };

        // Mappings that supply a constructor argument are flagged in place; the flagged copies plus
        // any synthesized ones replace the collection at the end.
        var flagged = new List<PropertyMappingModel>(model.PropertyMappings);

        var nameComparison = (StringComparison)model.NameComparison;
        var sourceProperties = GetAutomaticSources(sourceType, within, compilation);
        var ctorParams = new List<(string ParamName, string TargetPath, ConstructorArgumentKind Kind, bool IsNamed)>();
        var synthesizedMappings = new List<PropertyMappingModel>();

        // An optional parameter without a value is left out to take its default, and the arguments after it are
        // passed by name, when the call binds to the constructor alone (as SelectConstructor found it)
        var unbound = model;
        var passed = constructor.Parameters
            .Select(p => HasArgumentValue(unbound, unbound.ExplicitPropertyMappings, p, sourceProperties, destinationType, within, compilation))
            .ToArray();
        var canLeaveOut = BindsAlone(constructor, passed, namedType, within, compilation);
        var named = false;
        foreach (var param in constructor.Parameters)
        {
            // A member [MapIgnore] leaves out that a parameter assigns cannot be skipped unless the parameter is
            // optional, which is left out then. The constructor is called only when no other one can be and the
            // type cannot be created without arguments (SelectConstructor), so it is rejected rather than
            // silently mapped.
            var ignoredName = model.IgnoreTargets.FirstOrDefault(name => MatchesConstructorParameter(param, name, nameComparison));
            if (ignoredName is not null)
            {
                if (canLeaveOut && IsOmittable(param))
                {
                    named = true;
                    continue;
                }

                return new DiagnosticInfo(
                    Diagnostics.IgnoredConstructorParameter,
                    LocationOf(model, IgnoreAttributeIndex(model, ignoredName), syntax),
                    model.MethodName,
                    ignoredName);
            }

            // A member another attribute assigns takes the value of the attribute as the argument
            if (FindConstructorArgumentAttribute(model, param, nameComparison, destinationType, within, compilation) is { } attribute)
            {
                model = MarkConstructorArgument(model, attribute.Kind, attribute.TargetName);
                ctorParams.Add((param.Name, attribute.TargetName, attribute.Kind, named));
                continue;
            }

            // Prefer the property mapping built for this member: it carries the converter, null
            // handling and culture/format metadata that the argument has to be emitted with. The
            // mapping is flagged rather than removed so it keeps flowing through the analysis passes.
            // An explicit [MapProperty] naming the parameter itself, where a member of another spelling
            // matches it as well, is what the argument takes: the mapping built for it (for a dotted
            // source), or else the one synthesized below, while the automatic mapping of the member is
            // dropped, as it would assign the member again after construction.
            var explicitMappings = model.ExplicitPropertyMappings;
            var explicitMapping = explicitMappings.FirstOrDefault(pm => MatchesConstructorParameter(param, pm.TargetPath, nameComparison));
            int index;
            if (explicitMapping is not null)
            {
                flagged.RemoveAll(pm =>
                    !pm.IsConstructorParameter &&
                    (pm.TargetPath != explicitMapping.TargetPath) &&
                    MatchesConstructorParameter(param, pm.TargetPath, nameComparison) &&
                    explicitMappings.All(e => e.TargetPath != pm.TargetPath));
                index = flagged.FindIndex(pm => pm.TargetPath == explicitMapping.TargetPath);
            }
            else
            {
                index = flagged.FindIndex(pm => MatchesConstructorParameter(param, pm.TargetPath, nameComparison));
            }

            if (index >= 0)
            {
                flagged[index] = flagged[index] with { IsConstructorParameter = true };
                ctorParams.Add((param.Name, flagged[index].TargetPath, ConstructorArgumentKind.Property, named));
                continue;
            }

            // No destination property backs this parameter, so synthesize a mapping for it. An
            // explicit [MapProperty] may still name the parameter, in which case its source path and
            // options are used; otherwise the source is matched by name.

            string sourcePath;
            ITypeSymbol sourcePropertyType;
            bool sourceReturnsMaybeNull;
            if (explicitMapping is not null)
            {
                // ValidateExplicitPropertyMappings already resolved and canonicalized this path.
                sourcePath = explicitMapping.SourcePath;
                var sourceProperty = PropertyPathHelper.ResolvePropertySymbol(sourceType, sourcePath.Split('.'), within, compilation, readable: true)!;
                sourcePropertyType = sourceProperty.Type;
                sourceReturnsMaybeNull = ReturnsMaybeNull(sourceProperty);
            }
            else
            {
                var srcProp = sourceProperties.FirstOrDefault(p => String.Equals(p.Name, param.Name, nameComparison))
                           ?? sourceProperties.FirstOrDefault(p => String.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));
                if (srcProp is null)
                {
                    if (canLeaveOut && IsOmittable(param))
                    {
                        named = true;
                        continue;
                    }

                    return new DiagnosticInfo(
                        Diagnostics.UnresolvedConstructorParameter,
                        syntax.GetLocation(),
                        model.MethodName,
                        param.Name,
                        destinationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                }

                sourcePath = srcProp.Name;
                sourcePropertyType = srcProp.Type;
                sourceReturnsMaybeNull = ReturnsMaybeNull(srcProp);
            }

            var options = new Dictionary<string, PropertyMappingModel>(StringComparer.Ordinal);
            if (explicitMapping is not null)
            {
                options[param.Name] = explicitMapping;
            }

            // Conditions for parameter-only targets never pass through BuildPropertyMappings, so the
            // attribute has to be looked up directly; carrying it on the mapping lets the shared
            // ValidateExpressionAssignedTargets pass reject it like any other constructor target.
            var paramCondition = model.PropertyConditions.FirstOrDefault(c => MatchesConstructorParameter(param, c.TargetName, nameComparison));

            var synthesized = CreatePropertyMapping(
                model,
                options,
                param.Name,
                param.Type,
                isTargetInitOnly: false,
                isTargetRequired: false,
                sourcePath,
                sourcePropertyType,
                sourceReturnsMaybeNull,
                explicitMapping?.ConverterMethod,
                paramCondition?.ConditionMethod,
                compilation);
            synthesizedMappings.Add(synthesized with { IsConstructorParameter = true });
            ctorParams.Add((param.Name, param.Name, ConstructorArgumentKind.Property, named));
        }

        model = model with
        {
            ConstructorParameters = new(ctorParams),
            PropertyMappings = [with([.. flagged, .. synthesizedMappings])]
        };

        return null;
    }

    // The attribute other than [MapProperty] whose value goes to a constructor argument: one naming the member
    // the parameter assigns, or, as [MapProperty] can, the parameter itself when no member of the destination
    // has its name, the name matched as the arguments are bound.
    private static (ConstructorArgumentKind Kind, string TargetName)? FindConstructorArgumentAttribute(
        MapperMethodModel model,
        IParameterSymbol param,
        StringComparison nameComparison,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        var targets = model.ConstantMappings.Select(static c => (Kind: ConstructorArgumentKind.Constant, Target: c.TargetName))
            .Concat(model.ExpressionMappings.Select(static e => (ConstructorArgumentKind.Expression, e.TargetName)))
            .Concat(model.MapUsingMappings.Select(static u => (ConstructorArgumentKind.MapUsing, u.TargetName)))
            .Concat(model.MapFromMappings.Select(static f => (ConstructorArgumentKind.MapFrom, f.TargetName)))
            .Concat(model.MapNestedMappings.Select(static n => (ConstructorArgumentKind.MapNested, n.TargetName)))
            .Concat(model.MapCollectionMappings.Select(static c => (ConstructorArgumentKind.MapCollection, c.TargetName)));
        foreach (var (kind, target) in targets)
        {
            if (!target.Contains('.') &&
                MatchesConstructorParameter(param, target, nameComparison) &&
                ((FindTargetMember(destinationType, target, within, compilation) is not null) ||
                 !HasParameterMember(param, nameComparison, destinationType, within, compilation)))
            {
                return (kind, target);
            }
        }

        return null;
    }

    // Whether a member of the destination has the name of a constructor parameter, as the arguments are bound:
    // the parameter name or its PascalCase form, under the mapper's comparison.
    private static bool HasParameterMember(
        IParameterSymbol param,
        StringComparison nameComparison,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        var pascalName = Char.ToUpperInvariant(param.Name[0]) + param.Name.Substring(1);
        return (FindTargetMember(destinationType, param.Name, within, compilation) is not null) ||
               (FindTargetMember(destinationType, pascalName, within, compilation) is not null) ||
               ((nameComparison != StringComparison.Ordinal) &&
                ((FindInstanceMemberIgnoringSpelling(destinationType, param.Name, nameComparison, within, compilation) is not null) ||
                 (FindInstanceMemberIgnoringSpelling(destinationType, pascalName, nameComparison, within, compilation) is not null)));
    }

    // The model with the attribute of the kind that assigns the target flagged as a constructor argument.
    private static MapperMethodModel MarkConstructorArgument(MapperMethodModel model, ConstructorArgumentKind kind, string target) => kind switch
    {
        ConstructorArgumentKind.Constant => model with
        {
            ConstantMappings = new(model.ConstantMappings.Select(c => c.TargetName == target ? c with { IsConstructorArgument = true } : c))
        },
        ConstructorArgumentKind.Expression => model with
        {
            ExpressionMappings = new(model.ExpressionMappings.Select(e => e.TargetName == target ? e with { IsConstructorArgument = true } : e))
        },
        ConstructorArgumentKind.MapUsing => model with
        {
            MapUsingMappings = new(model.MapUsingMappings.Select(u => u.TargetName == target ? u with { IsConstructorArgument = true } : u))
        },
        ConstructorArgumentKind.MapFrom => model with
        {
            MapFromMappings = new(model.MapFromMappings.Select(f => f.TargetName == target ? f with { IsConstructorArgument = true } : f))
        },
        ConstructorArgumentKind.MapNested => model with
        {
            MapNestedMappings = new(model.MapNestedMappings.Select(n => n.TargetName == target ? n with { IsConstructorArgument = true } : n))
        },
        ConstructorArgumentKind.MapCollection => model with
        {
            MapCollectionMappings = new(model.MapCollectionMappings.Select(c => c.TargetName == target ? c with { IsConstructorArgument = true } : c))
        },
        _ => model
    };

    // The constructors a return mapper can call with arguments, the longest first, in declaration order among
    // those of a length: explicitly declared ones the mapper class can call (a private or protected one it
    // cannot), taking every argument as a value. A ref, out or ref readonly parameter takes a variable
    // (CS1620, or CS9192 / CS9193 for ref readonly), while an in one takes a value as well. An abstract class
    // has none, whatever its constructors are declared as (CS0144).
    private static List<IMethodSymbol> GetConstructorCandidates(ITypeSymbol destinationType, INamedTypeSymbol within, Compilation compilation) =>
        destinationType is INamedTypeSymbol { IsAbstract: false, IsStatic: false } namedType
            ? namedType.InstanceConstructors
                .Where(c => !c.IsImplicitlyDeclared && (c.Parameters.Length > 0) && IsConstructorCallable(c, within, compilation) &&
                            c.Parameters.All(static p => p.RefKind is RefKind.None or RefKind.In))
                .OrderByDescending(static c => c.Parameters.Length)
                .ToList()
            : [];

    // Chooses the constructor a return mapper calls with arguments, null when it constructs without them (a
    // void mapper never constructs). The ones it can call are the candidates the mapping gives every argument, a
    // value (HasArgumentValue) or none for an optional parameter, which is left out when the call binds to the
    // candidate alone (BindsAlone). A candidate receiving the targets of the attributes only a constructor
    // receives is taken first, the one receiving the most of them, one not obsolete as a warning over one that is,
    // the longest, the first declared. Short of that, the ways to construct through a constructor obsolete as a
    // warning are taken only when no other is left, the longest candidate decides whether construction takes
    // arguments at all (WillUseConstructor), so a type that did not need them keeps constructing without them,
    // and the longest candidate the mapping fills is called, unless that one needs no arguments either, its
    // members assignable after construction. When no candidate can be given its arguments, the type is created
    // without them if it can be; if not, the longest is bound, which reports the parameter without a value
    // (SMP0305, SMP0304).
    private static IMethodSymbol? SelectConstructor(
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        if (!model.ReturnsDestination || (destinationType is not INamedTypeSymbol namedType))
        {
            return null;
        }

        var nameComparison = (StringComparison)model.NameComparison;
        var candidates = GetConstructorCandidates(destinationType, within, compilation);
        if (candidates.Count == 0)
        {
            return null;
        }

        // Before BuildPropertyMappings, the property mappings are the parsed [MapProperty] ones
        var sourceProperties = GetAutomaticSources(sourceType, within, compilation);
        var fillable = candidates
            .Where(c =>
            {
                var passed = c.Parameters
                    .Select(p => HasArgumentValue(model, model.PropertyMappings, p, sourceProperties, destinationType, within, compilation))
                    .ToArray();
                return c.Parameters.All(p => passed[p.Ordinal] || IsOmittable(p)) && BindsAlone(c, passed, namedType, within, compilation);
            })
            .ToList();

        // The targets of the attributes only a constructor receives go to the candidate receiving the most of them,
        // one obsolete as a warning (CS0618) only when no other receives as many; a target a setter receives does
        // not choose one
        var targets = GetConstructorOnlyTargets(model, destinationType, candidates, within, compilation);
        if (targets.Count > 0)
        {
            var (receiving, _) = fillable
                .Select(c => (Constructor: c, Count: targets.Count(target => IsConstructorParameterTarget(c, target, nameComparison))))
                .Where(static x => x.Count > 0)
                .OrderByDescending(static x => x.Count)
                .ThenBy(static x => IsObsoleteAsWarning(x.Constructor))
                .FirstOrDefault();
            if (receiving is not null)
            {
                return receiving;
            }
        }

        // A constructor obsolete as a warning is left for when no other way to construct is: a candidate the
        // mapping fills, or new T() through a constructor callable without arguments
        var avoidsObsolete = fillable.Any(static c => !IsObsoleteAsWarning(c)) ||
                             CanConstructWithoutArguments(namedType, within, compilation, allowObsolete: false);
        var usable = avoidsObsolete ? fillable.Where(static c => !IsObsoleteAsWarning(c)).ToList() : fillable;

        if (!WillUseConstructor(candidates[0], destinationType, nameComparison, !avoidsObsolete, within, compilation))
        {
            return null;
        }

        if (usable.Count > 0)
        {
            return WillUseConstructor(usable[0], destinationType, nameComparison, !avoidsObsolete, within, compilation) ? usable[0] : null;
        }

        return CanConstructWithoutArguments(namedType, within, compilation, allowObsolete: true) ? null : candidates[0];
    }

    // The targets of the attributes giving a value that only a constructor receives: a parameter name of a candidate
    // no member has, or a member the mapper class cannot assign after construction nor in an object initializer
    // (get-only, a setter it cannot call, a readonly field). Before BuildPropertyMappings, the property mappings are
    // the parsed [MapProperty] ones.
    private static List<string> GetConstructorOnlyTargets(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        List<IMethodSymbol> candidates,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        var nameComparison = (StringComparison)model.NameComparison;
        var targets = model.PropertyMappings.Select(static m => m.TargetPath)
            .Concat(model.ConstantMappings.Select(static c => c.TargetName))
            .Concat(model.ExpressionMappings.Select(static e => e.TargetName))
            .Concat(model.MapUsingMappings.Select(static u => u.TargetName))
            .Concat(model.MapFromMappings.Select(static f => f.TargetName))
            .Concat(model.MapNestedMappings.Select(static n => n.TargetName))
            .Concat(model.MapCollectionMappings.Select(static c => c.TargetName))
            .Where(static target => !target.Contains('.'))
            .Distinct(StringComparer.Ordinal);

        var result = new List<string>();
        foreach (var target in targets)
        {
            var constructorOnly = FindTargetMember(destinationType, target, within, compilation) switch
            {
                IPropertySymbol property => !CanAssignFromMapper(property, destinationType, within, compilation),
                IFieldSymbol field => field.IsReadOnly,
                _ => candidates.Any(c => IsConstructorParameterTarget(c, target, nameComparison))
            };
            if (constructorOnly)
            {
                result.Add(target);
            }
        }

        return result;
    }

    // The properties of the source the automatic mapping reads, and the arguments a parameter takes by its name:
    // those with a getter the mapper class can call, not obsolete (a property named by an attribute is read
    // unless obsolete as an error).
    private static List<IPropertySymbol> GetAutomaticSources(ITypeSymbol sourceType, INamedTypeSymbol within, Compilation compilation) =>
        PropertyPathHelper.GetProperties(sourceType, within, compilation, readable: true)
            .Where(static p => p.GetReadObsoleteKind() == ObsoleteKind.None)
            .ToList();

    // Whether the mapping gives the parameter a value, as BuildConstructorParameterMappings binds it: that of an
    // attribute naming the parameter or its member (FindConstructorArgumentAttribute), of a [MapProperty] naming
    // either, or of a source property of the name, matched as the automatic mapping of the member or the mapping
    // made for the parameter matches it. One [MapIgnore] names (or the member it assigns) has none.
    private static bool HasArgumentValue(
        MapperMethodModel model,
        EquatableArray<PropertyMappingModel> explicitMappings,
        IParameterSymbol param,
        IEnumerable<IPropertySymbol> sourceProperties,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        var nameComparison = (StringComparison)model.NameComparison;
        if (model.IgnoreTargets.Any(name => MatchesConstructorParameter(param, name, nameComparison)))
        {
            return false;
        }

        return (FindConstructorArgumentAttribute(model, param, nameComparison, destinationType, within, compilation) is not null) ||
               explicitMappings.Any(pm => MatchesConstructorParameter(param, pm.TargetPath, nameComparison)) ||
               sourceProperties.Any(p => MatchesConstructorParameter(param, p.Name, nameComparison) || String.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));
    }

    // Whether the call leaving out the parameters without a value (the arguments after the first passed by name)
    // binds to the constructor alone. Another constructor the mapper class can access that takes the same
    // arguments, of the same types, with its other parameters optional, would be preferred for needing fewer of
    // them, or make the call ambiguous (CS0121). Overload resolution weighs one obsolete as an error (CS0619 when
    // bound) or with a ref readonly parameter (CS9193) as well, while a ref or out parameter takes no value.
    private static bool BindsAlone(IMethodSymbol constructor, bool[] passed, INamedTypeSymbol type, INamedTypeSymbol within, Compilation compilation)
    {
        var firstLeftOut = Array.IndexOf(passed, false);
        if (firstLeftOut < 0)
        {
            return true;
        }

        foreach (var other in type.InstanceConstructors)
        {
            if (SymbolEqualityComparer.Default.Equals(other, constructor) ||
                !compilation.IsSymbolAccessibleWithin(other, within, other.ContainingType) ||
                other.Parameters.Any(static p => p.RefKind is RefKind.Ref or RefKind.Out))
            {
                continue;
            }

            var taken = new bool[other.Parameters.Length];
            var takesArguments = true;
            for (var i = 0; takesArguments && (i < passed.Length); i++)
            {
                if (!passed[i])
                {
                    continue;
                }

                var param = constructor.Parameters[i];
                var target = i < firstLeftOut
                    ? i < other.Parameters.Length ? other.Parameters[i] : null
                    : other.Parameters.FirstOrDefault(p => p.Name == param.Name);
                if ((target is null) || !SymbolEqualityComparer.Default.Equals(target.Type, param.Type))
                {
                    takesArguments = false;
                }
                else
                {
                    taken[target.Ordinal] = true;
                }
            }

            if (takesArguments && other.Parameters.All(p => taken[p.Ordinal] || IsOmittable(p)))
            {
                return false;
            }
        }

        return true;
    }

    // An optional parameter, or a params one, which the call can leave out.
    private static bool IsOmittable(IParameterSymbol param) =>
        param.IsOptional || param.IsParams;

    private static int GetConstructorIndex(ITypeSymbol destinationType, IMethodSymbol? constructor)
    {
        if ((constructor is not null) && (destinationType is INamedTypeSymbol namedType))
        {
            for (var i = 0; i < namedType.InstanceConstructors.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(namedType.InstanceConstructors[i], constructor))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    // Whether the construction of a return mapper calls the constructor (a void mapper never constructs):
    //   - records always construct through one;
    //   - a parameter without a matching property the mapper can assign after construction (none, a get-only
    //     or init-only one, or one with a setter the mapper class cannot call, such as a private one; matched
    //     the same way arguments bind, so the camelCase parameter / PascalCase property convention is
    //     honoured) forces the constructor, because it is the only way to assign that member;
    //   - a type without a public parameterless constructor has no other way to construct; one obsolete as a
    //     warning counts only when nothing else constructs (allowObsolete).
    private static bool WillUseConstructor(
        IMethodSymbol constructor,
        ITypeSymbol destinationType,
        StringComparison nameComparison,
        bool allowObsolete,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        if (constructor.ContainingType.IsRecord)
        {
            return true;
        }

        var allDestProps = PropertyPathHelper.GetProperties(destinationType, within, compilation);
        var hasConstructorOnlyParams = constructor.Parameters.Any(p =>
        {
            var matchingProp = allDestProps.FirstOrDefault(prop => MatchesConstructorParameter(p, prop.Name, nameComparison));
            return (matchingProp is null) || !HasAssignableSetter(matchingProp, destinationType, within, compilation);
        });
        if (hasConstructorOnlyParams)
        {
            return true;
        }

        return !HasUsableParameterlessConstructor(constructor.ContainingType, allowObsolete);
    }

    // Whether new T() can create the type from the mapper class: a class or struct that is not abstract nor
    // static, whose constructor new T() binds to the mapper class can call (a struct has one unless it declares
    // its own), or an enum. One obsolete as a warning counts when allowObsolete is set.
    private static bool CanConstructWithoutArguments(INamedTypeSymbol type, INamedTypeSymbol within, Compilation compilation, bool allowObsolete = true) =>
        (type.TypeKind == TypeKind.Enum) ||
        ((type.TypeKind is TypeKind.Class or TypeKind.Struct) && !type.IsAbstract && !type.IsStatic &&
         (FindConstructorWithoutArguments(type, within, compilation) is { } constructor) &&
         IsConstructorCallable(constructor, within, compilation) &&
         (allowObsolete || !IsObsoleteAsWarning(constructor)));

    // The constructor new T() binds to, of those the mapper class can access that take no argument (optional or
    // params parameters only): one without parameters, or else the only one; null when there are several, as the
    // call is ambiguous (CS0121). Whether it can be called (one obsolete as an error cannot) is left to the caller.
    private static IMethodSymbol? FindConstructorWithoutArguments(INamedTypeSymbol type, INamedTypeSymbol within, Compilation compilation)
    {
        var applicable = type.InstanceConstructors
            .Where(c => c.Parameters.All(static p => p.IsOptional || p.IsParams) && compilation.IsSymbolAccessibleWithin(c, within, c.ContainingType))
            .ToList();
        return applicable.FirstOrDefault(static c => c.Parameters.Length == 0) ?? (applicable.Count == 1 ? applicable[0] : null);
    }

    // Public keeps this conservative: implicit parameterless constructors are public, and when an
    // internal one is missed the generator simply keeps using the parameterized constructor, which
    // always compiles. One obsolete as an error cannot be called (CS0619), and one obsolete as a warning counts
    // when allowObsolete is set.
    private static bool HasUsableParameterlessConstructor(INamedTypeSymbol type, bool allowObsolete) =>
        type.InstanceConstructors.Any(c =>
            (c.Parameters.Length == 0) && (c.DeclaredAccessibility == Accessibility.Public) && !IsObsoleteAsError(c) &&
            (allowObsolete || !IsObsoleteAsWarning(c)));

    // The constructor whose parameters generated construction will bind, or null when construction
    // is parameterless or never happens (a void mapper): the one SelectConstructor chose. Admission of get-only /
    // parameter-only targets and their consumption in BuildConstructorParameterMappings must agree on this
    // single answer.
    internal static IMethodSymbol? GetEffectiveConstructor(MapperMethodModel model, ITypeSymbol destinationType) =>
        (model.ConstructorIndex >= 0) && (destinationType is INamedTypeSymbol namedType)
            ? namedType.InstanceConstructors[model.ConstructorIndex]
            : null;

    // Matches a target name against a constructor parameter exactly the way the consumption loop
    // binds arguments: the parameter name itself or its PascalCase form, under the mapper's
    // configured comparison. Admission and consumption sharing this is what guarantees an accepted
    // mapping is also emitted.
    internal static bool MatchesConstructorParameter(IParameterSymbol param, string targetName, StringComparison nameComparison)
    {
        var pascalParamName = Char.ToUpperInvariant(param.Name[0]) + param.Name.Substring(1);
        return String.Equals(targetName, param.Name, nameComparison) ||
               String.Equals(targetName, pascalParamName, nameComparison);
    }

    private static bool IsConstructorParameterTarget(IMethodSymbol? constructor, string targetName, StringComparison nameComparison) =>
        (constructor is not null) &&
        constructor.Parameters.Any(p => MatchesConstructorParameter(p, targetName, nameComparison));

    // Constructor arguments and object-initializer entries are emitted as single expressions, so
    // statement-only options cannot apply there: a [MapCondition] has no way to leave the member
    // unassigned, and NullBehavior.Skip has no previous value to keep. Rejecting them loudly beats
    // silently ignoring the attribute. Runs after BuildConstructorParameterMappings so that both
    // flagged and synthesized constructor mappings are covered.
    internal static DiagnosticInfo? ValidateExpressionAssignedTargets(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        foreach (var mapping in model.PropertyMappings)
        {
            var expressionAssigned = mapping.IsConstructorParameter ||
                (model.UseConstructorMapping && (mapping.IsTargetInitOnly || mapping.IsTargetRequired));
            if (!expressionAssigned)
            {
                continue;
            }

            if (mapping.ConditionMethod is not null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedConstructorAssignedOption,
                    LocationOf(model, ConditionAttributeIndex(model, mapping.TargetPath), syntax),
                    model.MethodName,
                    mapping.TargetPath,
                    "MapCondition");
            }

            if (mapping.NullBehavior == NullBehaviorType.Skip)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedConstructorAssignedOption,
                    LocationOf(model, mapping.AttributeIndex, syntax),
                    model.MethodName,
                    mapping.TargetPath,
                    "NullBehavior.Skip");
            }
        }

        return null;
    }

    // Canonicalizes every attribute target name to the destination member's declared name, so that the
    // mapper's NameComparison (of the method, or else of the profile) is honoured on the target side too.
    // Auto-mapping already matched names that way; without this, an explicit attribute would only accept
    // the exact spelling. A name is matched against the properties and then the fields, each segment of a
    // dotted path on its own, an exact match first (ResolveTargetMemberName).
    //
    // Doing it once here keeps every later stage matching ordinally against real member names, which
    // also means the emitted code carries the correct casing. Names that do not resolve are left
    // untouched so the existing "not found" diagnostics still fire.
    internal static MapperMethodModel CanonicalizeTargetNames(MapperMethodModel model, ITypeSymbol destinationType, INamedTypeSymbol within, Compilation compilation)
    {
        var nameComparison = (StringComparison)model.NameComparison;

        string Canonical(string targetName) =>
            ResolveCanonicalTargetPath(destinationType, targetName, nameComparison, within, compilation) ?? targetName;

        // The ten loops below are the same shape but each one names a different model type and a
        // different property, so there is nothing C# can factor out without a delegate per collection.
        // A delegate would be allocated on every call even when no name needs canonicalizing, which is
        // the common case, so the repetition is deliberate: this way a model that is already canonical
        // allocates nothing at all, and an array is only taken when an entry actually differs.

        PropertyMappingModel[]? updatedMappings = null;
        for (var i = 0; i < model.PropertyMappings.Count; i++)
        {
            var item = model.PropertyMappings[i];
            var canonical = Canonical(item.TargetPath);
            if (canonical == item.TargetPath)
            {
                continue;
            }

            updatedMappings ??= [.. model.PropertyMappings];
            updatedMappings[i] = item with { TargetPath = canonical };
        }

        PropertyConditionModel[]? updatedConditions = null;
        for (var i = 0; i < model.PropertyConditions.Count; i++)
        {
            var item = model.PropertyConditions[i];
            var canonical = Canonical(item.TargetName);
            if (canonical == item.TargetName)
            {
                continue;
            }

            updatedConditions ??= [.. model.PropertyConditions];
            updatedConditions[i] = item with { TargetName = canonical };
        }

        ConstantMappingModel[]? updatedConstants = null;
        for (var i = 0; i < model.ConstantMappings.Count; i++)
        {
            var item = model.ConstantMappings[i];
            var canonical = Canonical(item.TargetName);
            if (canonical == item.TargetName)
            {
                continue;
            }

            updatedConstants ??= [.. model.ConstantMappings];
            updatedConstants[i] = item with { TargetName = canonical };
        }

        ExpressionMappingModel[]? updatedExpressions = null;
        for (var i = 0; i < model.ExpressionMappings.Count; i++)
        {
            var item = model.ExpressionMappings[i];
            var canonical = Canonical(item.TargetName);
            if (canonical == item.TargetName)
            {
                continue;
            }

            updatedExpressions ??= [.. model.ExpressionMappings];
            updatedExpressions[i] = item with { TargetName = canonical };
        }

        MapUsingModel[]? updatedMapUsings = null;
        for (var i = 0; i < model.MapUsingMappings.Count; i++)
        {
            var item = model.MapUsingMappings[i];
            var canonical = Canonical(item.TargetName);
            if (canonical == item.TargetName)
            {
                continue;
            }

            updatedMapUsings ??= [.. model.MapUsingMappings];
            updatedMapUsings[i] = item with { TargetName = canonical };
        }

        MapFromModel[]? updatedMapFroms = null;
        for (var i = 0; i < model.MapFromMappings.Count; i++)
        {
            var item = model.MapFromMappings[i];
            var canonical = Canonical(item.TargetName);
            if (canonical == item.TargetName)
            {
                continue;
            }

            updatedMapFroms ??= [.. model.MapFromMappings];
            updatedMapFroms[i] = item with { TargetName = canonical };
        }

        MapCollectionModel[]? updatedMapCollections = null;
        for (var i = 0; i < model.MapCollectionMappings.Count; i++)
        {
            var item = model.MapCollectionMappings[i];
            var canonical = Canonical(item.TargetName);
            if (canonical == item.TargetName)
            {
                continue;
            }

            updatedMapCollections ??= [.. model.MapCollectionMappings];
            updatedMapCollections[i] = item with { TargetName = canonical };
        }

        MapNestedModel[]? updatedMapNesteds = null;
        for (var i = 0; i < model.MapNestedMappings.Count; i++)
        {
            var item = model.MapNestedMappings[i];
            var canonical = Canonical(item.TargetName);
            if (canonical == item.TargetName)
            {
                continue;
            }

            updatedMapNesteds ??= [.. model.MapNestedMappings];
            updatedMapNesteds[i] = item with { TargetName = canonical };
        }

        string[]? updatedIgnored = null;
        for (var i = 0; i < model.IgnoredProperties.Count; i++)
        {
            var item = model.IgnoredProperties[i];
            var canonical = Canonical(item);
            if (canonical == item)
            {
                continue;
            }

            updatedIgnored ??= [.. model.IgnoredProperties];
            updatedIgnored[i] = canonical;
        }

        string[]? updatedIgnoreTargets = null;
        for (var i = 0; i < model.IgnoreTargets.Count; i++)
        {
            var item = model.IgnoreTargets[i];
            var canonical = Canonical(item);
            if (canonical == item)
            {
                continue;
            }

            updatedIgnoreTargets ??= [.. model.IgnoreTargets];
            updatedIgnoreTargets[i] = canonical;
        }

        if ((updatedMappings is null) && (updatedConditions is null) && (updatedConstants is null) &&
            (updatedExpressions is null) && (updatedMapUsings is null) && (updatedMapFroms is null) &&
            (updatedMapCollections is null) && (updatedMapNesteds is null) && (updatedIgnored is null) &&
            (updatedIgnoreTargets is null))
        {
            return model;
        }

        // ReSharper disable UseCollectionExpression
#pragma warning disable IDE0028
        return model with
        {
            PropertyMappings = updatedMappings is null ? model.PropertyMappings : new EquatableArray<PropertyMappingModel>(updatedMappings),
            PropertyConditions = updatedConditions is null ? model.PropertyConditions : new EquatableArray<PropertyConditionModel>(updatedConditions),
            ConstantMappings = updatedConstants is null ? model.ConstantMappings : new EquatableArray<ConstantMappingModel>(updatedConstants),
            ExpressionMappings = updatedExpressions is null ? model.ExpressionMappings : new EquatableArray<ExpressionMappingModel>(updatedExpressions),
            MapUsingMappings = updatedMapUsings is null ? model.MapUsingMappings : new EquatableArray<MapUsingModel>(updatedMapUsings),
            MapFromMappings = updatedMapFroms is null ? model.MapFromMappings : new EquatableArray<MapFromModel>(updatedMapFroms),
            MapCollectionMappings = updatedMapCollections is null ? model.MapCollectionMappings : new EquatableArray<MapCollectionModel>(updatedMapCollections),
            MapNestedMappings = updatedMapNesteds is null ? model.MapNestedMappings : new EquatableArray<MapNestedModel>(updatedMapNesteds),
            IgnoredProperties = updatedIgnored is null ? model.IgnoredProperties : new EquatableArray<string>(updatedIgnored),
            IgnoreTargets = updatedIgnoreTargets is null ? model.IgnoreTargets : new EquatableArray<string>(updatedIgnoreTargets)
        };
#pragma warning restore IDE0028
        // ReSharper restore UseCollectionExpression
    }

    internal static DiagnosticInfo? ValidateExplicitPropertyMappings(
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        var nameComparison = (StringComparison)model.NameComparison;
        var destinationProperties = PropertyPathHelper.GetProperties(destinationType, within, compilation);
        var effectiveConstructor = GetEffectiveConstructor(model, destinationType);

        var resolved = new List<PropertyMappingModel>(model.PropertyMappings.Count);
        foreach (var declared in model.PropertyMappings)
        {
            var canonicalSourcePath = PropertyPathHelper.ResolveCanonicalPath(sourceType, declared.SourcePath, nameComparison, within, compilation);
            if (canonicalSourcePath is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapPropertySourceProperty,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    model.MethodName,
                    declared.TargetPath,
                    declared.SourcePath);
            }

            var mapping = declared with { SourcePath = canonicalSourcePath };
            resolved.Add(mapping);

            if (mapping.TargetPath.Contains('.'))
            {
                var pathError = ValidateTargetPath(model, destinationType, mapping.TargetPath, mapping.AttributeIndex, within, compilation, publicPropertiesOnly: true, syntax);
                if (pathError is not null)
                {
                    return pathError;
                }

                continue;
            }

            // The target must be assignable: a property with a setter the mapper class can call, or a
            // parameter of the constructor that construction will actually call. Matching mirrors the
            // argument-binding loop, so anything accepted here is guaranteed to be consumed rather than
            // silently dropped.
            var targetProperty = destinationProperties.FirstOrDefault(p => String.Equals(p.Name, mapping.TargetPath, StringComparison.Ordinal));
            if (((targetProperty is null) || !CanAssignFromMapper(targetProperty, destinationType, within, compilation)) &&
                !IsConstructorParameterTarget(effectiveConstructor, mapping.TargetPath, nameComparison))
            {
                // A member only the constructor of a return mapper assigns (a get-only property or a
                // parameter it takes) is out of reach of a void mapper, which never constructs
                if (!model.ReturnsDestination &&
                    IsConstructorParameterTarget(GetConstructorCandidates(destinationType, within, compilation).FirstOrDefault(), mapping.TargetPath, nameComparison))
                {
                    return new DiagnosticInfo(
                        Diagnostics.InitOnlyDestinationRequiresReturnMapper,
                        LocationOf(model, mapping.AttributeIndex, syntax),
                        model.MethodName,
                        model.DestinationTypeName);
                }

                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapPropertyTargetProperty,
                    LocationOf(model, mapping.AttributeIndex, syntax),
                    model.MethodName,
                    mapping.TargetPath);
            }
        }

        var mappings = new EquatableArray<PropertyMappingModel>(resolved);
        model = model with { PropertyMappings = mappings, ExplicitPropertyMappings = mappings };

        return null;
    }

    // Builds a fully analysed mapping for one target member. Constructor parameters that have no
    // backing destination property synthesize their mapping through here as well, so an argument is
    // described exactly like an assignment and picks up the same conversion analysis.
    private static PropertyMappingModel CreatePropertyMapping(
        MapperMethodModel model,
        Dictionary<string, PropertyMappingModel> originalMappings,
        string targetName,
        ITypeSymbol targetType,
        bool isTargetInitOnly,
        bool isTargetRequired,
        string sourcePath,
        ITypeSymbol sourcePropertyType,
        bool sourceReturnsMaybeNull,
        string? converterMethod,
        string? conditionMethod,
        Compilation compilation)
    {
        var sourceTypeName = sourcePropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var destTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isSourceNullable = MayBeNull(sourcePropertyType) || sourceReturnsMaybeNull;
        var isTargetNullable = targetType.IsNullableType();

        var sourceUnderlyingType = sourcePropertyType.GetUnderlyingType();
        var targetUnderlyingType = targetType.GetUnderlyingType();
        var sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetUnderlyingTypeName = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var order = 0;
        var definitionOrder = 0;
        var nullBehavior = NullBehaviorType.Default;
        var nullValue = default(string?);
        var nullValueUnsupported = false;
        var nullValueHasNullElement = false;
        string? propertyCulture = null;
        string? propEffectiveDateTimeFormat;
        string? propEffectiveNumberFormat;
        if (originalMappings.TryGetValue(targetName, out var origMapping))
        {
            order = origMapping.Order;
            definitionOrder = origMapping.DefinitionOrder;
            nullBehavior = origMapping.NullBehavior;
            nullValue = origMapping.NullValue;
            nullValueUnsupported = origMapping.IsNullValueUnsupported;
            nullValueHasNullElement = origMapping.NullValueHasNullElement;
            propertyCulture = origMapping.EffectiveCulture;
            propEffectiveDateTimeFormat = origMapping.EffectiveDateTimeFormat ?? model.DateTimeFormat;
            propEffectiveNumberFormat = origMapping.EffectiveNumberFormat ?? model.NumberFormat;
        }
        else
        {
            propEffectiveDateTimeFormat = model.DateTimeFormat;
            propEffectiveNumberFormat = model.NumberFormat;
        }

        var (propEffectiveCulture, cultureArgument) = ResolveCulture(
            model,
            propertyCulture,
            GetApplyingFormat(sourceUnderlyingTypeName, targetUnderlyingTypeName, propEffectiveDateTimeFormat, propEffectiveNumberFormat) is not null);

        // A value the target takes by an implicit reference conversion, through variance as well
        // (IReadOnlyList<Circle> to IReadOnlyList<Shape>), is assigned as it is
        var requiresConversion = TypeNameHelper.RequiresTypeConversion(sourceUnderlyingTypeName, targetUnderlyingTypeName)
                && (!sourceUnderlyingType.IsAssignableTo(targetUnderlyingType))
                && !IsImplicitReferenceConversion(sourceUnderlyingType, targetUnderlyingType, compilation);

        var mapping = new PropertyMappingModel(
            SourcePath: sourcePath,
            TargetPath: targetName,
            SourceType: sourceTypeName,
            TargetType: destTypeName,
            SourceUnderlyingType: sourceUnderlyingTypeName,
            TargetUnderlyingType: targetUnderlyingTypeName,
            RequiresConversion: requiresConversion,
            IsSourceNullable: isSourceNullable,
            IsTargetNullable: isTargetNullable,
            ConverterMethod: converterMethod,
            ConditionMethod: conditionMethod,
            NullBehavior: nullBehavior,
            NullValue: nullValue,
            IsNullValueUnsupported: nullValueUnsupported,
            NullValueHasNullElement: nullValueHasNullElement,
            Order: order,
            DefinitionOrder: definitionOrder,
            IsTargetInitOnly: isTargetInitOnly,
            IsTargetRequired: isTargetRequired,
            EffectiveCulture: propEffectiveCulture,
            EffectiveDateTimeFormat: propEffectiveDateTimeFormat,
            EffectiveNumberFormat: propEffectiveNumberFormat,
            CultureArgument: cultureArgument,
            AttributeIndex: origMapping?.AttributeIndex ?? -1);

        mapping = DetectEnumMappingKind(mapping, sourceUnderlyingType, targetUnderlyingType);

        if (mapping.RequiresConversion && !mapping.IsEnumMapping() && !mapping.HasConverter())
        {
            var srcUnderlying = sourceUnderlyingType.SpecialType == SpecialType.System_String
                ? sourceUnderlyingType
                : null;
            if ((srcUnderlying is not null) && !HasParseFormat(mapping))
            {
                mapping = DetectParsableMethodFromSymbol(mapping, targetUnderlyingType);
            }
        }

        return mapping;
    }

    internal static MapperMethodModel BuildPropertyMappings(
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MapperMethodModel model)
    {
        var sourceProperties = GetAutomaticSources(sourceType, within, compilation);
        var destinationProperties = PropertyPathHelper.GetProperties(destinationType, within, compilation);

        var customMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        var nestedMappings = new List<PropertyMappingModel>();

        // The value of a member the constructor that construction calls assigns from an argument goes to that
        // argument, so it is converted to the type of the parameter, which need not be the member's
        var effectiveConstructor = GetEffectiveConstructor(model, destinationType);
        var nameComparison = (StringComparison)model.NameComparison;
        IParameterSymbol? FindParameter(string target) =>
            effectiveConstructor?.Parameters.FirstOrDefault(p => MatchesConstructorParameter(p, target, nameComparison));

        foreach (var declared in model.PropertyMappings)
        {
            if (declared.TargetPath.Contains('.') || declared.SourcePath.Contains('.'))
            {
                // The culture and the formats of the method or the profile apply to a path as they do to a member,
                // the culture once the path gives the types it converts
                var parameter = declared.TargetPath.Contains('.') ? null : FindParameter(declared.TargetPath);
                var withDefaults = (model.DateTimeFormat is null) && (model.NumberFormat is null)
                    ? declared
                    : declared with
                    {
                        EffectiveDateTimeFormat = declared.EffectiveDateTimeFormat ?? model.DateTimeFormat,
                        EffectiveNumberFormat = declared.EffectiveNumberFormat ?? model.NumberFormat
                    };
                var mapping = ResolveNestedMapping(withDefaults, sourceType, destinationType, within, compilation, parameter?.Type);
                var (cultureName, cultureArgument) = ResolveCulture(
                    model,
                    declared.EffectiveCulture,
                    GetApplyingFormat(mapping.SourceUnderlyingType, mapping.TargetUnderlyingType, mapping.EffectiveDateTimeFormat, mapping.EffectiveNumberFormat) is not null);
                mapping = mapping with { EffectiveCulture = cultureName, CultureArgument = cultureArgument };

                // A dotted source path still lands on a plain destination member, which may be
                // init-only or required. Without these flags the emitters treat it as an ordinary
                // assignment and produce code that cannot compile (CS8852).
                if (!mapping.TargetPath.Contains('.'))
                {
                    var nestedTargetProp = destinationProperties.FirstOrDefault(p => String.Equals(p.Name, mapping.TargetPath, StringComparison.Ordinal));
                    if (nestedTargetProp is not null)
                    {
                        mapping = mapping with
                        {
                            IsTargetInitOnly = nestedTargetProp.GetSetter()?.IsInitOnly == true,
                            IsTargetRequired = nestedTargetProp.IsRequired
                        };
                    }
                }

                nestedMappings.Add(mapping);
            }
            else
            {
                customMappings[declared.TargetPath] = declared.SourcePath;
            }
        }

        var originalMappings = model.PropertyMappings.ToDictionary(m => m.TargetPath, m => m);

        // A member the dotted path of another attribute goes into is not mapped as a whole either, as with a
        // dotted [MapProperty]: the path writes into the member the destination holds or creates, where writing
        // after the whole was copied would change the source's own object, and an object initializer would
        // have assigned the member twice
        var dottedFeatureHeads = new HashSet<string>(
            model.ConstantMappings.Select(static c => c.TargetName)
                .Concat(model.ExpressionMappings.Select(static e => e.TargetName))
                .Concat(model.MapUsingMappings.Select(static u => u.TargetName))
                .Where(static t => t.Contains('.'))
                .Select(static t => t.Substring(0, t.IndexOf('.'))),
            StringComparer.Ordinal);

        var mappings = new List<PropertyMappingModel>();

        foreach (var destProp in destinationProperties)
        {
            if (model.IgnoredProperties.Contains(destProp.Name))
            {
                continue;
            }

            if (nestedMappings.Any(m => m.TargetPath.StartsWith(destProp.Name + ".", StringComparison.Ordinal) || (m.TargetPath == destProp.Name)) ||
                dottedFeatureHeads.Contains(destProp.Name))
            {
                continue;
            }

            // A property without a setter the mapper class can call (get-only, or a private setter) is
            // still reachable when the constructor that construction will call assigns it; that mapping
            // is what carries the conversion metadata for the argument. Gating on the same constructor
            // and matching as the argument-binding loop is what keeps this from admitting a mapping
            // nothing consumes (which used to surface as an assignment the mapper cannot make, CS0200 /
            // CS0272).
            if (!CanAssignFromMapper(destProp, destinationType, within, compilation) && !IsConstructorParameterTarget(effectiveConstructor, destProp.Name, nameComparison))
            {
                continue;
            }

            string? sourcePropPath = null;
            ITypeSymbol? sourcePropertyType = null;
            var sourceReturnsMaybeNull = false;
            string? converterMethod = null;
            string? conditionMethod = null;

            // The automatic mapping leaves an obsolete property out (CS0618 / CS0619 otherwise), while one a
            // [MapProperty] names is mapped
            var isNamed = customMappings.ContainsKey(destProp.Name);
            if (!isNamed && (destProp.GetWriteObsoleteKind() != ObsoleteKind.None))
            {
                continue;
            }

            if (customMappings.TryGetValue(destProp.Name, out var customSourcePath))
            {
                var customProperty = PropertyPathHelper.ResolvePropertySymbol(sourceType, customSourcePath.Split('.'), within, compilation, readable: true);
                sourcePropPath = customSourcePath;
                sourcePropertyType = customProperty?.Type;
                sourceReturnsMaybeNull = ReturnsMaybeNull(customProperty);

                if (originalMappings.TryGetValue(destProp.Name, out var originalMapping))
                {
                    converterMethod = originalMapping.ConverterMethod;
                    conditionMethod = originalMapping.ConditionMethod;
                }
            }
            else
            {
                if (model.AutoMap)
                {
                    var sourceProp = sourceProperties.FirstOrDefault(p => String.Equals(p.Name, destProp.Name, nameComparison));
                    if (sourceProp is not null)
                    {
                        sourcePropPath = sourceProp.Name;
                        sourcePropertyType = sourceProp.Type;
                        sourceReturnsMaybeNull = ReturnsMaybeNull(sourceProp);

                        if (originalMappings.TryGetValue(destProp.Name, out var originalMapping))
                        {
                            conditionMethod = originalMapping.ConditionMethod;
                        }
                    }
                }
            }

            if ((sourcePropPath is not null) && (sourcePropertyType is not null))
            {
                mappings.Add(CreatePropertyMapping(
                    model,
                    originalMappings,
                    destProp.Name,
                    FindParameter(destProp.Name)?.Type ?? destProp.Type,
                    destProp.GetSetter()?.IsInitOnly == true,
                    destProp.IsRequired,
                    sourcePropPath,
                    sourcePropertyType,
                    sourceReturnsMaybeNull,
                    converterMethod,
                    conditionMethod,
                    compilation));
            }
        }

        mappings.AddRange(nestedMappings);

        for (var i = 0; i < mappings.Count; i++)
        {
            var condition = model.PropertyConditions.FirstOrDefault(c => String.Equals(c.TargetName, mappings[i].TargetPath, StringComparison.Ordinal));
            if (condition is not null)
            {
                mappings[i] = mappings[i] with { ConditionMethod = condition.ConditionMethod };
            }
        }

        return model with { PropertyMappings = new(mappings) };
    }

    internal static PropertyMappingModel DetectEnumMappingKind(PropertyMappingModel mapping, ITypeSymbol sourceUnderlying, ITypeSymbol targetUnderlying)
    {
        var sourceIsEnum = sourceUnderlying.TypeKind == TypeKind.Enum;
        var targetIsEnum = targetUnderlying.TypeKind == TypeKind.Enum;
        var sourceIsString = sourceUnderlying.SpecialType == SpecialType.System_String;
        var targetIsString = targetUnderlying.SpecialType == SpecialType.System_String;
        var sourceIsNumeric = sourceUnderlying.IsNumericType();
        var targetIsNumeric = targetUnderlying.IsNumericType();

        if (!sourceIsEnum && !targetIsEnum)
        {
            return mapping;
        }

        // The same enum on both sides is copied as it is, which keeps a value no member has, such as a combination
        // of flags
        if (SymbolEqualityComparer.Default.Equals(sourceUnderlying, targetUnderlying))
        {
            return mapping;
        }

        if (sourceIsEnum && targetIsEnum)
        {
            var sourceMembers = GetEnumMembersDedupedByValue(sourceUnderlying);
            var destMembers = GetEnumMembers(targetUnderlying);
            return mapping with
            {
                EnumMappingKind = EnumMappingKind.EnumToEnum,
                RequiresConversion = true,
                SourceEnumMembers = new(sourceMembers.Select(static f => f.Name).ToArray()),
                SourceEnumCastValues = new(sourceMembers.Select(GetEnumCastValue).ToArray()),
                DestEnumMembers = new(destMembers.Select(static f => f.Name).ToArray()),
                DestEnumCastValues = new(destMembers.Select(GetEnumCastValue).ToArray())
            };
        }

        if (sourceIsEnum && targetIsNumeric)
        {
            return mapping with { EnumMappingKind = EnumMappingKind.EnumToNumeric, RequiresConversion = true };
        }

        if (sourceIsNumeric && targetIsEnum)
        {
            return mapping with { EnumMappingKind = EnumMappingKind.NumericToEnum, RequiresConversion = true };
        }

        if (sourceIsEnum && targetIsString)
        {
            var sourceMembers = GetEnumMembersDedupedByValue(sourceUnderlying);
            return mapping with
            {
                EnumMappingKind = EnumMappingKind.EnumToString,
                RequiresConversion = true,
                SourceEnumMembers = new(sourceMembers.Select(static f => f.Name).ToArray()),
                SourceEnumCastValues = new(sourceMembers.Select(GetEnumCastValue).ToArray())
            };
        }

        if (sourceIsString && targetIsEnum)
        {
            var destMembers = GetEnumMembers(targetUnderlying);
            return mapping with
            {
                EnumMappingKind = EnumMappingKind.StringToEnum,
                RequiresConversion = true,
                DestEnumMembers = new(destMembers.Select(static f => f.Name).ToArray()),
                DestEnumCastValues = new(destMembers.Select(GetEnumCastValue).ToArray())
            };
        }

        return mapping;
    }

    // Returns the enum members in declaration order.
    internal static List<IFieldSymbol> GetEnumMembers(ITypeSymbol enumType) =>
        enumType.GetMembers().OfType<IFieldSymbol>().Where(static f => f.IsConst).ToList();

    // Returns enum members in declaration order, keeping only the first per constant value.
    // Switch arms are emitted per member, and alias members (same value) would otherwise produce
    // duplicate case constants (CS8510).
    internal static List<IFieldSymbol> GetEnumMembersDedupedByValue(ITypeSymbol enumType)
    {
        var members = new List<IFieldSymbol>();
        var seenValues = new HashSet<object>();
        foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (!field.IsConst || (field.ConstantValue is null))
            {
                continue;
            }

            if (seenValues.Add(field.ConstantValue))
            {
                members.Add(field);
            }
        }

        return members;
    }

    // A member marked [Obsolete] is written as a cast of its number, as naming it warns (CS0618) or fails
    // (CS0619); the matching by name stays as it is. Empty for a member written by its name.
    private static string GetEnumCastValue(IFieldSymbol field) =>
        field.GetObsoleteKind() == ObsoleteKind.None ? string.Empty : ConstantExpressionHelper.FormatEnumNumber(field.ConstantValue) ?? string.Empty;

    internal static PropertyMappingModel DetectParsableMethodFromSymbol(PropertyMappingModel mapping, ITypeSymbol targetType)
    {
        const string spanParsableMetadataName = "ISpanParsable`1";
        const string parsableMetadataName = "IParsable`1";

        var hasSpanParsable = false;
        var hasParsable = false;

        foreach (var iface in targetType.AllInterfaces)
        {
            var meta = iface.OriginalDefinition.MetadataName;
            if (meta == spanParsableMetadataName)
            {
                hasSpanParsable = true;
                break;
            }

            if (meta == parsableMetadataName)
            {
                hasParsable = true;
            }
        }

        if (hasSpanParsable && CanCallParse(targetType, span: true))
        {
            return mapping with { ParseMethod = ParseMethodKind.SpanParsable };
        }

        if ((hasParsable || hasSpanParsable) && CanCallParse(targetType, span: false))
        {
            return mapping with { ParseMethod = ParseMethodKind.Parsable };
        }

        return mapping;
    }

    // Whether T.Parse(text, provider), as the parse conversion calls it, binds to a method not obsolete as an error
    // (CS0619): the public static Parse of the type taking the text, a ReadOnlySpan<char> for the span one or else a
    // string, and an IFormatProvider. One not found (implemented explicitly) is left to the call as before.
    private static bool CanCallParse(ITypeSymbol targetType, bool span)
    {
        var method = targetType.GetMembers("Parse").OfType<IMethodSymbol>().FirstOrDefault(m =>
            m.IsStatic && (m.DeclaredAccessibility == Accessibility.Public) && (m.Parameters.Length == 2) &&
            (span
                ? m.Parameters[0].Type is INamedTypeSymbol { IsGenericType: true } named &&
                  (named.ConstructedFrom.ToDisplayString() == "System.ReadOnlySpan<T>") &&
                  (named.TypeArguments[0].SpecialType == SpecialType.System_Char)
                : m.Parameters[0].Type.SpecialType == SpecialType.System_String) &&
            (m.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.IFormatProvider"));
        return method?.GetObsoleteKind() != ObsoleteKind.Error;
    }

    // TargetTypeOverride is the type of the constructor parameter a target without a dot goes to, which the
    // value is converted to instead of the type of the member.
    internal static PropertyMappingModel ResolveNestedMapping(
        PropertyMappingModel mapping,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        ITypeSymbol? targetTypeOverride = null)
    {
        // Resolution walks the source path, then the target path, and finally decides whether a
        // conversion is needed from the resolved types. The steps feed each other, so they are
        // accumulated into locals and applied to the mapping in one step at the end.
        var sourcePathSegments = mapping.SourcePathSegments;
        var sourceTypeName = mapping.SourceType;
        var isSourceNullable = mapping.IsSourceNullable;
        var sourceUnderlyingTypeName = mapping.SourceUnderlyingType;
        var targetPathSegments = mapping.TargetPathSegments;
        var targetTypeName = mapping.TargetType;
        var isTargetNullable = mapping.IsTargetNullable;
        var targetUnderlyingTypeName = mapping.TargetUnderlyingType;
        var requiresConversion = mapping.RequiresConversion;

        var sourceParts = mapping.SourcePath.Split('.');
        if (sourceParts.Length > 1)
        {
            var currentType = sourceType;
            var pathBuilder = new List<string>();

            var sourceSegments = new List<NestedPathSegment>();
            for (var i = 0; i < sourceParts.Length - 1; i++)
            {
                var part = sourceParts[i];
                pathBuilder.Add(part);

                var prop = PropertyPathHelper.GetProperties(currentType, within, compilation, readable: true).FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    var isNullable = MayBeNullMember(prop);
                    sourceSegments.Add(new NestedPathSegment(
                        String.Join(".", pathBuilder),
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        isNullable));
                    currentType = prop.Type;
                }
            }
#pragma warning disable IDE0028, IDE0306
            sourcePathSegments = new(sourceSegments);
#pragma warning restore IDE0028, IDE0306

            var finalSourceProp = PropertyPathHelper.GetProperties(currentType, within, compilation, readable: true).FirstOrDefault(p => p.Name == sourceParts[sourceParts.Length - 1]);
            if (finalSourceProp is not null)
            {
                sourceTypeName = finalSourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isSourceNullable = MayBeNullMember(finalSourceProp);
                var sourceUnderlyingType = finalSourceProp.Type.GetUnderlyingType();
                sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            var sourceProp = PropertyPathHelper.GetProperties(sourceType, within, compilation, readable: true).FirstOrDefault(p => p.Name == mapping.SourcePath);
            if (sourceProp is not null)
            {
                sourceTypeName = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isSourceNullable = MayBeNullMember(sourceProp);
                var sourceUnderlyingType = sourceProp.Type.GetUnderlyingType();
                sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        var isTargetInitOnly = mapping.IsTargetInitOnly;
        var targetParts = mapping.TargetPath.Split('.');
        if (targetParts.Length > 1)
        {
            // How the generated code reaches through the intermediate members, and whether only an object
            // initializer can assign the member at the end (ValidateExplicitPropertyMappings has reported a
            // path it cannot assign)
            var currentTargetType = destinationType;
            var targetMembers = ResolveTargetMembers(destinationType, mapping.TargetPath, within, compilation, publicPropertiesOnly: true);
            if (targetMembers is not null)
            {
                var (route, segments) = AnalyzeTargetPath(destinationType, targetMembers, within, compilation);
                targetPathSegments = segments;
                isTargetInitOnly = route == TargetRoute.Initializer;
                currentTargetType = GetMemberType(targetMembers[targetMembers.Count - 2])!;
            }

            var finalProp = PropertyPathHelper.GetProperties(currentTargetType, within, compilation).FirstOrDefault(p => p.Name == targetParts[targetParts.Length - 1]);
            if (finalProp is not null)
            {
                targetTypeName = finalProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isTargetNullable = finalProp.Type.IsNullableType();
                var targetUnderlyingType = finalProp.Type.GetUnderlyingType();
                targetUnderlyingTypeName = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            var targetMemberType = targetTypeOverride ??
                                   PropertyPathHelper.GetProperties(destinationType, within, compilation).FirstOrDefault(p => p.Name == mapping.TargetPath)?.Type;
            if (targetMemberType is not null)
            {
                targetTypeName = targetMemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isTargetNullable = targetMemberType.IsNullableType();
                var targetUnderlyingType = targetMemberType.GetUnderlyingType();
                targetUnderlyingTypeName = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        ITypeSymbol? srcUnderlying = null;
        ITypeSymbol? dstUnderlying = null;
        if (!String.IsNullOrEmpty(sourceUnderlyingTypeName) && !String.IsNullOrEmpty(targetUnderlyingTypeName))
        {
            var srcParts = mapping.SourcePath.Split('.');
            var dstParts = mapping.TargetPath.Split('.');
            var srcFinalProp = PropertyPathHelper.ResolvePropertySymbol(sourceType, srcParts, within, compilation, readable: true);
            var dstFinalProp = PropertyPathHelper.ResolvePropertySymbol(destinationType, dstParts, within, compilation);
            srcUnderlying = srcFinalProp?.Type.GetUnderlyingType();
            dstUnderlying = (targetTypeOverride ?? dstFinalProp?.Type)?.GetUnderlyingType();
            var assignable = (srcUnderlying is not null) && (dstUnderlying is not null) &&
                             (srcUnderlying.IsAssignableTo(dstUnderlying) || IsImplicitReferenceConversion(srcUnderlying, dstUnderlying, compilation));
            requiresConversion = (!assignable) &&
                TypeNameHelper.RequiresTypeConversion(sourceUnderlyingTypeName, targetUnderlyingTypeName);
        }
        else if (!String.IsNullOrEmpty(sourceTypeName) && !String.IsNullOrEmpty(targetTypeName))
        {
            requiresConversion = TypeNameHelper.RequiresTypeConversion(sourceTypeName, targetTypeName);
        }

        var resolved = mapping with
        {
            SourcePathSegments = sourcePathSegments,
            SourceType = sourceTypeName,
            IsSourceNullable = isSourceNullable,
            SourceUnderlyingType = sourceUnderlyingTypeName,
            TargetPathSegments = targetPathSegments,
            TargetType = targetTypeName,
            IsTargetNullable = isTargetNullable,
            TargetUnderlyingType = targetUnderlyingTypeName,
            IsTargetInitOnly = isTargetInitOnly,
            RequiresConversion = requiresConversion
        };

        // An enum at either end converts as it does for a member the path does not go through (by member name, by
        // number, from and to text), which the conversion analysis takes from the mapping as it does for those
        return (srcUnderlying is not null) && (dstUnderlying is not null)
            ? DetectEnumMappingKind(resolved, srcUnderlying, dstUnderlying)
            : resolved;
    }

    // Conversion analysis for the property mappings. Each step depends on what the previous ones
    // decided, so they all run against locals for a single mapping and the rewritten mapping is
    // built once, at the end of the iteration. Symbol lookups that do not vary per mapping are
    // resolved before the loop, and the array is only created once a mapping actually changes.
    internal static EquatableArray<PropertyMappingModel> AnalyzeConversions(
        EquatableArray<PropertyMappingModel> mappings,
        IMethodSymbol mapperMethod,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        IMethodSymbol? constructor,
        EquatableArray<(string ParamName, string TargetPath, ConstructorArgumentKind Kind, bool IsNamed)> constructorArguments,
        string? mapConverterTypeName,
        string mapConverterMethodName,
        Compilation compilation)
    {
        var within = mapperMethod.ContainingType;
        var hasMapConverter = mapConverterTypeName is not null;

        // The converter class is looked up for a mapping that needs a conversion only
        ITypeSymbol? converterType = null;
        var converterTypeFound = false;
        ITypeSymbol? GetConverterType()
        {
            if (!converterTypeFound)
            {
                converterType = FindConverterType(mapperMethod, Names.ValueConverterAttribute, Names.DefaultValueConverter);
                converterTypeFound = true;
            }

            return converterType;
        }

        // The first referenced assembly defining each, looked up once per compilation
        var parsableSymbol = hasMapConverter ? null : mapperMethod.FindReferencedType("System.IParsable`1");
        var spanParsableSymbol = hasMapConverter ? null : mapperMethod.FindReferencedType("System.ISpanParsable`1");

        // The declared types of the members, from their symbols: the source property, and the target
        // property or the constructor parameter a constructor-only target stands for, the parameter for a
        // constructor argument. The type names of the model are looked up only as a last resort, which misses
        // nested and generic types.
        ITypeSymbol? GetSourceType(PropertyMappingModel mapping, string typeName) =>
            PropertyPathHelper.ResolvePropertySymbol(sourceType, mapping.SourcePath.Split('.'), within, compilation, readable: true)?.Type.GetUnderlyingType() ??
            mapperMethod.FindTypeByFullyQualifiedName(typeName);

        ITypeSymbol? GetTargetType(PropertyMappingModel mapping, string typeName) =>
            ((mapping.IsConstructorParameter ? FindArgumentParameter(constructor, constructorArguments, mapping.TargetPath)?.Type : null) ??
             PropertyPathHelper.ResolvePropertySymbol(destinationType, mapping.TargetPath.Split('.'), within, compilation)?.Type ??
             constructor?.Parameters.FirstOrDefault(p => p.Name == mapping.TargetPath)?.Type)?.GetUnderlyingType() ??
            mapperMethod.FindTypeByFullyQualifiedName(typeName);

        PropertyMappingModel[]? analyzed = null;

        for (var i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];

            var specializedConverterMethod = mapping.SpecializedConverterMethod;
            var parseMethod = mapping.ParseMethod;
            var userDefinedConversion = mapping.UserDefinedConversion;
            var requiresConversion = mapping.RequiresConversion;
            var useFormattable = mapping.UseFormattable;
            var requiresExplicitNumericCast = mapping.RequiresExplicitNumericCast;

            var isEnumMapping = mapping.IsEnumMapping();
            var hasConverter = mapping.HasConverter();
            var effectiveSource = mapping.SourceUnderlyingType is { Length: > 0 } s ? s : mapping.SourceType;
            var effectiveTarget = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;

            // Specialized converter method
            if (requiresConversion && !isEnumMapping && (GetConverterType() is { } valueConverterType))
            {
                var specializedMethodName = $"{mapConverterMethodName}To{TypeNameHelper.GetSimpleTypeName(effectiveTarget)}";
                if (FindSpecializedMethod(valueConverterType, specializedMethodName, effectiveSource, effectiveTarget) is not null)
                {
                    specializedConverterMethod = specializedMethodName;
                    parseMethod = ParseMethodKind.None;
                }
            }

            var hasSpecializedConverter = !String.IsNullOrEmpty(specializedConverterMethod);

            // IParsable / ISpanParsable. A user supplied converter takes over the whole conversion,
            // so parsing is never emitted for it.
            if (hasMapConverter)
            {
                parseMethod = ParseMethodKind.None;
            }
            else if ((parsableSymbol is not null) &&
                     requiresConversion && !isEnumMapping && !hasSpecializedConverter && !hasConverter &&
                     !HasParseFormat(mapping) &&
                     (GetSourceType(mapping, effectiveSource)?.SpecialType == SpecialType.System_String))
            {
                var targetTypeSymbol = GetTargetType(mapping, effectiveTarget);
                if (targetTypeSymbol is not null)
                {
                    // A Parse obsolete as an error is not called: the other one, or else another conversion
                    var isSpanParsable = (spanParsableSymbol is not null)
                        ? targetTypeSymbol.IsImplementGenericInterface(spanParsableSymbol)
                        : targetTypeSymbol.IsImplementsInterfaceByName("System.ISpanParsable`1");
                    var isParsable = isSpanParsable ||
                                     targetTypeSymbol.IsImplementGenericInterface(parsableSymbol) ||
                                     targetTypeSymbol.IsImplementsInterfaceByName("System.IParsable`1");
                    if (isSpanParsable && CanCallParse(targetTypeSymbol, span: true))
                    {
                        parseMethod = ParseMethodKind.SpanParsable;
                    }
                    else if (isParsable && CanCallParse(targetTypeSymbol, span: false))
                    {
                        parseMethod = ParseMethodKind.Parsable;
                    }
                    else if (isParsable)
                    {
                        parseMethod = ParseMethodKind.None;
                    }
                }
            }

            var hasParsableMethod = parseMethod != ParseMethodKind.None;

            // User defined conversion operator
            if (!hasMapConverter && requiresConversion && !isEnumMapping && !hasConverter)
            {
                var sourceTypeSymbol = GetSourceType(mapping, effectiveSource);
                var targetTypeSymbol = GetTargetType(mapping, effectiveTarget);

                if ((sourceTypeSymbol is not null) && (targetTypeSymbol is not null))
                {
                    if (MapperSymbolExtensions.HasUserDefinedConversion(sourceTypeSymbol, targetTypeSymbol, isImplicit: true))
                    {
                        userDefinedConversion = UserDefinedConversionKind.Implicit;
                        requiresConversion = mapping.IsSourceNullable && requiresConversion;
                    }
                    else if (MapperSymbolExtensions.HasUserDefinedConversion(sourceTypeSymbol, targetTypeSymbol, isImplicit: false))
                    {
                        userDefinedConversion = UserDefinedConversionKind.Explicit;
                    }
                }
            }

            var hasUserDefinedExplicit = userDefinedConversion == UserDefinedConversionKind.Explicit;

            // IFormattable, only meaningful when a culture or a format was specified
            if (!hasMapConverter && requiresConversion && !isEnumMapping && !hasConverter &&
                !hasSpecializedConverter && !hasParsableMethod && !hasUserDefinedExplicit &&
                TypeNameHelper.IsStringType(effectiveTarget) &&
                (mapping.HasCulture() || (mapping.EffectiveDateTimeFormat is not null) || (mapping.EffectiveNumberFormat is not null)))
            {
                var sourceTypeSymbol = GetSourceType(mapping, effectiveSource);
                if (sourceTypeSymbol is not null)
                {
                    useFormattable = HasFormatToString(sourceTypeSymbol);
                }
            }

            // Explicit numeric cast, the fallback when nothing above claimed the conversion
            if (requiresConversion && !isEnumMapping && !hasConverter &&
                !hasSpecializedConverter && !hasParsableMethod && !hasUserDefinedExplicit && !useFormattable &&
                TypeNameHelper.IsExplicitNumericConversion(effectiveSource, effectiveTarget))
            {
                requiresExplicitNumericCast = true;
            }

            if ((specializedConverterMethod == mapping.SpecializedConverterMethod) &&
                (parseMethod == mapping.ParseMethod) &&
                (userDefinedConversion == mapping.UserDefinedConversion) &&
                (requiresConversion == mapping.RequiresConversion) &&
                (useFormattable == mapping.UseFormattable) &&
                (requiresExplicitNumericCast == mapping.RequiresExplicitNumericCast))
            {
                continue;
            }

            analyzed ??= [.. mappings];
            analyzed[i] = mapping with
            {
                SpecializedConverterMethod = specializedConverterMethod,
                ParseMethod = parseMethod,
                UserDefinedConversion = userDefinedConversion,
                RequiresConversion = requiresConversion,
                UseFormattable = useFormattable,
                RequiresExplicitNumericCast = requiresExplicitNumericCast
            };
        }

        // ReSharper disable UseCollectionExpression
#pragma warning disable IDE0028
        return analyzed is null ? mappings : new EquatableArray<PropertyMappingModel>(analyzed);
#pragma warning restore IDE0028
        // ReSharper restore UseCollectionExpression
    }

    // The converter class of a mapper: the type a [ValueConverter] / [CollectionConverter] names with
    // typeof, on the mapper method or else on its containing type (the order ParseConverterAttributes
    // follows), and the default class of the library without one. The typeof argument is taken as is, so
    // nested and generic classes are found as well, which a lookup by the displayed name missed.
    internal static ITypeSymbol? FindConverterType(IMethodSymbol mapperMethod, string attributeName, string defaultTypeName) =>
        GetAttributeTypeArgument(mapperMethod.GetAttributes(), attributeName) ??
        GetAttributeTypeArgument(mapperMethod.ContainingType.GetAttributes(), attributeName) ??
        mapperMethod.FindTypeByFullyQualifiedName(defaultTypeName);

    private static INamedTypeSymbol? GetAttributeTypeArgument(IEnumerable<AttributeData> attributes, string attributeName) =>
        attributes
            .Where(a => (a.AttributeClass?.ToDisplayString() == attributeName) && (a.ConstructorArguments.Length >= 1))
            .Select(static a => a.ConstructorArguments[0].Value as INamedTypeSymbol)
            .FirstOrDefault(static t => t is not null);

    internal static IMethodSymbol? FindSpecializedMethod(
        ITypeSymbol converterType,
        string methodName,
        string sourceType,
        string targetType)
    {
        // One obsolete as an error is not used, as the generated code could not call it (CS0619)
        var methods = converterType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(static m => m.IsStatic && (m.Parameters.Length == 1) && (m.GetObsoleteKind() != ObsoleteKind.Error))
            .ToList();

        // A method whose parameter cannot take the property value (ref, ref readonly, out) is still
        // returned when it is the only one, so that ValidateValueConverterMethods can report it.
        IMethodSymbol? unusable = null;
        foreach (var method in methods)
        {
            var paramType = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if ((paramType == sourceType) && (returnType == targetType))
            {
                if (CanTakeArgument(method.Parameters[0].RefKind, ArgumentKind.Value))
                {
                    return method;
                }

                unusable ??= method;
            }
        }

        return unusable;
    }

    // Methods of a [ValueConverter] class the generated code calls with the property value, which a ref,
    // ref readonly or out parameter cannot take: the specialized method a conversion found (with a
    // culture, its overload taking the culture and the format), and the generic one a user converter falls
    // back to. One that is missing, or that cannot take the arguments, is reported as a converter
    // signature mismatch instead of leaving the call to fail in the generated code.
    internal static DiagnosticInfo? ValidateValueConverterMethods(IMethodSymbol mapperMethod, Compilation compilation, ref MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        // The converter class is looked up once a mapping calls a method of it, which most mappers do not
        ITypeSymbol? converterType = null;
        var cultureInfoType = compilation.GetTypeByMetadataName("System.Globalization.CultureInfo");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        PropertyMappingModel[]? resolved = null;
        for (var i = 0; i < model.PropertyMappings.Count; i++)
        {
            var mapping = model.PropertyMappings[i];

            // A converter given to [MapProperty] takes over the conversion, and a mapping calling no method of the
            // converter class has nothing to check
            if (mapping.HasConverter() ||
                (!mapping.HasSpecializedConverter() && ((model.MapConverterTypeName is null) || !UsesGenericConversion(mapping))))
            {
                continue;
            }

            converterType ??= FindConverterType(mapperMethod, Names.ValueConverterAttribute, Names.DefaultValueConverter);
            if (converterType is null)
            {
                return null;
            }

            var effectiveSource = mapping.SourceUnderlyingType is { Length: > 0 } s ? s : mapping.SourceType;
            var effectiveTarget = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;

            string? unusableMethod = null;
            if (mapping.HasSpecializedConverter() && mapping.HasCulture() && (cultureInfoType is not null))
            {
                var cultureArgument = IsCultureArgumentVariable(model, mapping) ? ArgumentKind.ReadOnlyVariable : ArgumentKind.Value;
                var overload = FindCultureOverload(converterType, mapping.SpecializedConverterMethod!, effectiveSource, effectiveTarget, compilation, cultureInfoType, stringType, cultureArgument);
                if (overload is null)
                {
                    unusableMethod = mapping.SpecializedConverterMethod;
                }
                else if ((overload.Parameters[1].RefKind is RefKind.In or RefKind.RefReadOnlyParameter) &&
                         (GetCultureArgumentKind(compilation, cultureInfoType, overload.Parameters[1].Type, cultureArgument) == ArgumentKind.ReadOnlyVariable))
                {
                    resolved ??= [.. model.PropertyMappings];
                    resolved[i] = mapping with { CultureArgumentModifier = "in " };
                }
            }
            else if (mapping.HasSpecializedConverter())
            {
                var method = FindSpecializedMethod(converterType, mapping.SpecializedConverterMethod!, effectiveSource, effectiveTarget);
                if ((method is not null) && !CanTakeArgument(method.Parameters[0].RefKind, ArgumentKind.Value))
                {
                    unusableMethod = method.Name;
                }
            }
            else if ((model.MapConverterTypeName is not null) && UsesGenericConversion(mapping))
            {
                var candidates = converterType.GetMembers(model.MapConverterMethodName)
                    .OfType<IMethodSymbol>()
                    .Where(static m => m.IsStatic && (m.Arity == 2) && TakesArgumentCount(m, 1) && (m.GetObsoleteKind() != ObsoleteKind.Error))
                    .ToList();
                if (!candidates.Any(static m => CanTakeArgument(m.Parameters[0].RefKind, ArgumentKind.Value)))
                {
                    unusableMethod = model.MapConverterMethodName;
                }
            }

            if (unusableMethod is not null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterSignature,
                    LocationOf(model, model.ValueConverterAttributeIndex >= 0 ? model.ValueConverterAttributeIndex : mapping.AttributeIndex, syntax),
                    mapperMethod.Name,
                    $"{converterType.ToDisplayString()}.{unusableMethod}",
                    mapping.TargetPath);
            }
        }

        if (resolved is not null)
        {
            model = model with { PropertyMappings = new(resolved) };
        }

        return null;
    }

    // The overload of a specialized method a culture calls, (value, culture, format) returning the target
    // type, the value typed as the one-parameter method takes it, the culture a type a CultureInfo converts
    // to and the format one a string converts to. The value and the format go as values, and the culture as
    // cultureArgument tells. Null when no overload can take the arguments.
    private static IMethodSymbol? FindCultureOverload(
        ITypeSymbol converterType,
        string methodName,
        string sourceType,
        string targetType,
        Compilation compilation,
        ITypeSymbol cultureInfoType,
        ITypeSymbol stringType,
        ArgumentKind cultureArgument)
    {
        IMethodSymbol? overload = null;
        var methods = converterType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(static m => m.IsStatic && TakesArgumentCount(m, 3) && (m.GetObsoleteKind() != ObsoleteKind.Error));
        foreach (var method in methods)
        {
            var culture = GetCultureArgumentKind(compilation, cultureInfoType, method.Parameters[1].Type, cultureArgument);
            if ((method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType) &&
                (method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == targetType) &&
                (culture is not null) &&
                compilation.ClassifyCommonConversion(stringType, method.Parameters[2].Type).IsImplicit &&
                CanTakeArgument(method.Parameters[0].RefKind, ArgumentKind.Value) &&
                CanTakeArgument(method.Parameters[1].RefKind, culture.Value) &&
                CanTakeArgument(method.Parameters[2].RefKind, ArgumentKind.Value))
            {
                overload = PreferByValue(overload, method);
            }
        }

        return overload;
    }

    // How the culture goes to a parameter: to a CultureInfo as itself, the field of a culture name or the CultureInfo
    // parameter as a read-only variable (in / ref readonly get it by in), the current or the invariant culture as a
    // value, and to a type it converts to (IFormatProvider, object) as the converted value, which goes as is. Null for
    // any other type.
    private static ArgumentKind? GetCultureArgumentKind(Compilation compilation, ITypeSymbol cultureInfoType, ITypeSymbol parameterType, ArgumentKind cultureArgument)
    {
        var conversion = compilation.ClassifyCommonConversion(cultureInfoType, parameterType);
        if (conversion.IsIdentity)
        {
            return cultureArgument;
        }

        return conversion.IsImplicit ? ArgumentKind.Value : null;
    }

    // Whether the culture the conversion goes with is a variable: the field of a culture name, or the CultureInfo
    // parameter, which the generated code passes on and never writes
    private static bool IsCultureArgumentVariable(MapperMethodModel model, PropertyMappingModel mapping) =>
        (mapping.CultureArgument is { } argument) &&
        ((argument == model.CultureParameterName) ||
         ((mapping.EffectiveCulture is { } name) && (argument == MapperSourceBuilder.GetCultureFieldName(name))));

    // Whether value.ToString(format, provider) binds, as the IFormattable conversion calls it: a public
    // instance ToString(string, IFormatProvider) on the type or a base type, or on the interfaces an
    // interface type extends. An explicit implementation of IFormattable does not make the call bind. The call
    // binds to the one of the most derived type, which cannot be called when obsolete as an error (CS0619).
    private static bool HasFormatToString(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return (FindFormatToString(type) ?? type.AllInterfaces.Select(FindFormatToString).FirstOrDefault(static m => m is not null)) is { } method &&
                   (method.GetObsoleteKind() != ObsoleteKind.Error);
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (FindFormatToString(current) is { } method)
            {
                return method.GetObsoleteKind() != ObsoleteKind.Error;
            }
        }

        return false;
    }

    private static IMethodSymbol? FindFormatToString(ITypeSymbol type) =>
        type.GetMembers("ToString").OfType<IMethodSymbol>().FirstOrDefault(static m =>
            !m.IsStatic &&
            (m.DeclaredAccessibility == Accessibility.Public) &&
            (m.Parameters.Length == 2) &&
            (m.Parameters[0].Type.SpecialType == SpecialType.System_String) &&
            (m.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.IFormatProvider"));

    // Whether the conversion ends in the generic Convert<TSource, TDestination> of the converter class,
    // after the specialized, parse, cast, user-defined and IFormattable conversions all passed.
    private static bool UsesGenericConversion(PropertyMappingModel mapping) =>
        mapping.RequiresConversion && !mapping.HasConverter() && !mapping.IsEnumMapping() &&
        !mapping.HasSpecializedConverter() && !mapping.HasParsableMethod() && !mapping.RequiresExplicitNumericCast &&
        (mapping.UserDefinedConversion == UserDefinedConversionKind.None) && !mapping.UseFormattable;
}
