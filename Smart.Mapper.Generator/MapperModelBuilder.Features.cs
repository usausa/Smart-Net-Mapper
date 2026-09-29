namespace Smart.Mapper.Generator;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using Smart.Mapper.Generator.Helpers;
using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// The explicit features ([MapConstant], [MapExpression], [MapUsing], [MapFrom], [MapCollection], [MapNested]), the
// mappers they call, the targets they write into and the values they give, and the warnings of strict mode
internal static partial class MapperModelBuilder
{
    // The targets of [MapConstant] and [MapExpression]: properties and fields, also at the end of a dotted path
    internal static MapperMethodModel BuildConstantMappings(ITypeSymbol destinationType, INamedTypeSymbol within, Compilation compilation, MapperMethodModel model)
    {
        var constants = new ConstantMappingModel[model.ConstantMappings.Count];
        for (var i = 0; i < constants.Length; i++)
        {
            var constantMapping = model.ConstantMappings[i];
            constants[i] = ResolveFeatureTarget(model, destinationType, constantMapping.TargetName, constantMapping.IsConstructorArgument, within, compilation) is { } target
                ? constantMapping with
                {
                    TargetPathSegments = target.Segments,
                    IsTargetInitOnly = target.IsInitOnly,
                    IsTargetRequired = target.IsRequired
                }
                : constantMapping;
        }

        var expressions = new ExpressionMappingModel[model.ExpressionMappings.Count];
        for (var i = 0; i < expressions.Length; i++)
        {
            var expressionMapping = model.ExpressionMappings[i];
            expressions[i] = ResolveFeatureTarget(model, destinationType, expressionMapping.TargetName, expressionMapping.IsConstructorArgument, within, compilation) is { } target
                ? expressionMapping with
                {
                    TargetType = target.Type.ToDisplayString(NullableQualifiedFormat),
                    IsTargetTypeOblivious = target.Type.NullableAnnotation == NullableAnnotation.None,
                    TargetPathSegments = target.Segments,
                    IsTargetInitOnly = target.IsInitOnly,
                    IsTargetRequired = target.IsRequired
                }
                : expressionMapping;
        }

        return model with
        {
            ConstantMappings = [with(constants)],
            ExpressionMappings = [with(expressions)]
        };
    }

    // [MapIgnore] and [MapCondition] name a target that has to exist, or they would do nothing without a word:
    // a property or field of the destination, a dotted path of them (for [MapCondition]; SMP0103 for
    // [MapIgnore]), or a parameter of the constructor a return mapper calls (or, for [MapIgnore], of one it
    // could call, which the parameter without a value keeps from being chosen). The names are canonical here
    // (CanonicalizeTargetNames), so they are matched as declared, under the mapper's name comparison. Not
    // found, it is reported as a target that is not found (SMP0102); a [MapCondition] on a member no property
    // mapping assigns is reported later (SMP0109).
    internal static DiagnosticInfo? ValidateIgnoredAndConditionTargets(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        var nameComparison = (StringComparison)model.NameComparison;
        var constructor = GetEffectiveConstructor(model, destinationType);

        // A [MapIgnore] naming a parameter of another constructor a return mapper can call has kept that one from
        // being chosen, the parameter having no value (SelectConstructor)
        var candidates = model.ReturnsDestination ? GetConstructorCandidates(destinationType, within, compilation) : [];

        bool Exists(string target, bool ignored) =>
            (FindTargetMember(destinationType, target, within, compilation) is not null) ||
            (!target.Contains('.') &&
             (IsConstructorParameterTarget(constructor, target, nameComparison) ||
              (ignored && candidates.Any(c => IsConstructorParameterTarget(c, target, nameComparison)))));

        var targets = model.IgnoreTargets.Select((t, i) => (Target: t, Ignored: true, Index: IgnoreAttributeIndex(model, i)))
            .Concat(model.PropertyConditions.Select(static c => (Target: c.TargetName, Ignored: false, Index: c.AttributeIndex)));
        foreach (var (target, ignored, index) in targets)
        {
            if (!Exists(target, ignored))
            {
                return new DiagnosticInfo(GetUnassignableTargetDescriptor(destinationType, target, within, compilation), LocationOf(model, index, syntax), model.MethodName, target);
            }
        }

        // [MapIgnore] keeps a member of the destination from the automatic mapping, which assigns the members
        // as a whole and never a member of a member, so a dotted target would do nothing: leaving out a part of
        // what a member is assigned cannot be done. One a dotted attribute maps as well was reported as naming
        // the same target (SMP0101).
        for (var i = 0; i < model.IgnoreTargets.Count; i++)
        {
            if (model.IgnoreTargets[i].Contains('.'))
            {
                return new DiagnosticInfo(Diagnostics.DottedIgnoreTarget, LocationOf(model, IgnoreAttributeIndex(model, i), syntax), model.MethodName, model.IgnoreTargets[i]);
            }
        }

        return null;
    }

    // A dotted path into a member the constructor of a return mapper assigns from an argument (a parameter
    // matching the member, as the arguments are bound) would write, after construction, into the object passed
    // to the constructor, which is the source's own when the argument copies it. It is reported instead,
    // whichever attribute the path is of (SMP0301). A void mapper never constructs, so it is not concerned.
    internal static DiagnosticInfo? ValidateConstructorAssignedPaths(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        var constructor = GetEffectiveConstructor(model, destinationType);
        if (constructor is null)
        {
            return null;
        }

        var nameComparison = (StringComparison)model.NameComparison;
        var targets = model.PropertyMappings.Select(static m => (Target: m.TargetPath, m.AttributeIndex))
            .Concat(model.ConstantMappings.Select(static c => (Target: c.TargetName, c.AttributeIndex)))
            .Concat(model.ExpressionMappings.Select(static e => (Target: e.TargetName, e.AttributeIndex)))
            .Concat(model.MapUsingMappings.Select(static u => (Target: u.TargetName, u.AttributeIndex)));
        foreach (var (target, attributeIndex) in targets)
        {
            var index = target.IndexOf('.');
            if (index < 0)
            {
                continue;
            }

            // A name no member has is left to be reported as not found
            var head = target.Substring(0, index);
            var parameter = constructor.Parameters.FirstOrDefault(p => MatchesConstructorParameter(p, head, nameComparison));
            if ((parameter is not null) && (FindTargetMember(destinationType, head, within, compilation) is not null))
            {
                return new DiagnosticInfo(
                    Diagnostics.ConstructorAssignedTargetPath,
                    LocationOf(model, attributeIndex, syntax),
                    model.MethodName,
                    target,
                    parameter.Name);
            }
        }

        return null;
    }

    // A [MapCondition] guards the property mapping of its target, the automatic one or a [MapProperty]. On a
    // target no property mapping assigns (nothing maps it, it is ignored, or another attribute assigns it,
    // which the condition does not guard), it would do nothing without a word, so it is reported (SMP0109).
    // The mappings of constructor arguments and initializer entries were reported before (SMP0306).
    internal static DiagnosticInfo? ValidateConditionTargetsMapped(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        foreach (var condition in model.PropertyConditions)
        {
            if (!model.PropertyMappings.Any(m => String.Equals(m.TargetPath, condition.TargetName, StringComparison.Ordinal)))
            {
                return new DiagnosticInfo(Diagnostics.UnguardedConditionTarget, LocationOf(model, condition.AttributeIndex, syntax), model.MethodName, condition.TargetName);
            }
        }

        return null;
    }

    internal static DiagnosticInfo? ValidateDuplicateTargets(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        // The attributes of each target, with the index of each: the diagnostic points to the second of those
        // that contradict one another, in the order they are declared
        var targetMappings = new Dictionary<string, List<(string Kind, int Index)>>();

        void AddTarget(string target, string attributeType, int index)
        {
            if (!targetMappings.TryGetValue(target, out var list))
            {
                list = [];
                targetMappings[target] = list;
            }
            list.Add((attributeType, index));
        }

        foreach (var mapping in model.PropertyMappings.Where(m => m.HasExplicitMapping))
        {
            AddTarget(mapping.TargetPath, "MapProperty", mapping.AttributeIndex);
        }

        foreach (var mapping in model.ConstantMappings)
        {
            AddTarget(mapping.TargetName, "MapConstant", mapping.AttributeIndex);
        }

        foreach (var mapping in model.ExpressionMappings)
        {
            AddTarget(mapping.TargetName, "MapExpression", mapping.AttributeIndex);
        }

        foreach (var mapping in model.MapUsingMappings)
        {
            AddTarget(mapping.TargetName, "MapUsing", mapping.AttributeIndex);
        }

        foreach (var mapping in model.MapFromMappings)
        {
            AddTarget(mapping.TargetName, "MapFrom", mapping.AttributeIndex);
        }

        foreach (var mapping in model.MapCollectionMappings)
        {
            AddTarget(mapping.TargetName, "MapCollection", mapping.AttributeIndex);
        }

        foreach (var mapping in model.MapNestedMappings)
        {
            AddTarget(mapping.TargetName, "MapNested", mapping.AttributeIndex);
        }

        foreach (var kvp in targetMappings)
        {
            if (kvp.Value.Count > 1)
            {
                return new DiagnosticInfo(
                    Diagnostics.DuplicateTargetMapping,
                    LocationOf(model, SecondDeclared(kvp.Value.Select(static x => x.Index)), syntax),
                    model.MethodName,
                    kvp.Key,
                    String.Join(", ", kvp.Value.Select(static x => x.Kind)));
            }
        }

        // [MapIgnore] and an attribute mapping the same target contradict each other, whichever the attribute
        // is. The [MapIgnore] of a member with dotted paths into it is not one of them: it only keeps the member
        // from being mapped as a whole, and the paths are applied.
        for (var i = 0; i < model.IgnoreTargets.Count; i++)
        {
            var target = model.IgnoreTargets[i];
            if (targetMappings.TryGetValue(target, out var kinds))
            {
                return new DiagnosticInfo(
                    Diagnostics.DuplicateTargetMapping,
                    LocationOf(model, SecondDeclared(kinds.Select(static x => x.Index).Append(IgnoreAttributeIndex(model, i))), syntax),
                    model.MethodName,
                    target,
                    "MapIgnore, " + String.Join(", ", kinds.Select(static x => x.Kind)));
            }
        }

        // A member mapped as a whole and a member of it mapped through a dotted path (Child and Child.Value)
        // cannot both apply: the whole would replace what the path wrote, or the path would write into the
        // object the whole came from, the source's own. So they are reported as mapping the same target.
        foreach (var kvp in targetMappings)
        {
            var path = kvp.Key;
            for (var index = path.IndexOf('.'); index >= 0; index = path.IndexOf('.', index + 1))
            {
                if (targetMappings.TryGetValue(path.Substring(0, index), out var whole))
                {
                    return new DiagnosticInfo(
                        Diagnostics.DuplicateTargetMapping,
                        LocationOf(model, SecondDeclared(whole.Concat(kvp.Value).Select(static x => x.Index)), syntax),
                        model.MethodName,
                        path.Substring(0, index),
                        String.Join(", ", whole.Select(static x => x.Kind).Concat(kvp.Value.Select(x => $"{x.Kind} ({path})"))));
                }
            }
        }

        return null;
    }

    // The second of the attributes in the order they are declared, the one found to contradict the first
    private static int SecondDeclared(IEnumerable<int> indexes) =>
        indexes.OrderBy(static i => i).Skip(1).DefaultIfEmpty(-1).First();

    // The method of a [MapUsing] is matched against the type of its target, a property or field, also at the
    // end of a dotted path: it returns a type the assignment converts to it implicitly (int to long or int?, a
    // class to a base class or an interface). A target that is not found is left to ValidateAssignedTargets.
    internal static DiagnosticInfo? ValidateAndBuildMapUsingMappings(
        IMethodSymbol mapperMethod,
        Compilation compilation,
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var customTypes = GetCustomParameterTypes(mapperMethod, model);

        var resolved = new List<MapUsingModel>(model.MapUsingMappings.Count);
        foreach (var mapUsing in model.MapUsingMappings)
        {
            if (ResolveFeatureTarget(model, destinationType, mapUsing.TargetName, mapUsing.IsConstructorArgument, containingType, compilation) is not { } target)
            {
                resolved.Add(mapUsing);
                continue;
            }

            var targetTypeName = target.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (InstanceMethodOfStaticMapper(model, containingType, mapUsing.Method, mapUsing.AttributeIndex, compilation, syntax) is { } instanceError)
            {
                return instanceError;
            }

            // The method takes the source, as its own type or as a base class or an interface it converts to, and
            // returns the type of the target, or a type converting to it implicitly
            var match = MatchValueMethod(
                LookupMethods(containingType, mapUsing.Method, model.IsInstance, compilation),
                new ValueCall(sourceType, sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), GetVariableKind(model.SourceRefKind), customTypes),
                model.CustomParameters,
                m => (m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == targetTypeName) ||
                     IsImplicitlyConvertible(m.ReturnType, target.Type, compilation),
                compilation);
            if (match.Result == ConverterMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapUsingSignature,
                    LocationOf(model, mapUsing.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapUsing.Method,
                    mapUsing.TargetName);
            }

            if (match.Result == ConverterMatchResult.ReturnTypeMismatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapUsingReturnTypeMismatch,
                    LocationOf(model, mapUsing.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapUsing.Method,
                    targetTypeName,
                    match.Mismatched!.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            // The method gets the source, which is not null past the null check of the mapper
            var matchedMethod = match.Method!;
            resolved.Add(mapUsing with
            {
                TargetPathSegments = target.Segments,
                IsTargetInitOnly = target.IsInitOnly,
                IsTargetRequired = target.IsRequired,
                CustomArguments = match.CustomArguments,
                ParameterRefKinds = GetParameterRefKinds(matchedMethod),
                ForgivesNull = (IsNullableReference(matchedMethod.ReturnType) || ReturnsMaybeNull(matchedMethod)) && !target.Type.IsNullableType() &&
                               !ReturnsNotNullForValue(matchedMethod, compilation)
            });
        }

        model = model with { MapUsingMappings = new(resolved) };
        return null;
    }

    internal static DiagnosticInfo? ValidateAndBuildMapFromMappings(
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        var destinationProperties = PropertyPathHelper.GetProperties(destinationType, within, compilation);
        var constructor = GetEffectiveConstructor(model, destinationType);

        var resolved = new List<MapFromModel>(model.MapFromMappings.Count);
        foreach (var mapFrom in model.MapFromMappings)
        {
            // The target property, or for a value that goes to a constructor argument, the parameter
            var destProp = mapFrom.IsConstructorArgument ? null : destinationProperties.FirstOrDefault(p => p.Name == mapFrom.TargetName);
            var targetMemberType = mapFrom.IsConstructorArgument
                ? FindArgumentParameter(constructor, model.ConstructorParameters, mapFrom.TargetName)?.Type
                : destProp?.Type;
            if (targetMemberType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapFromTargetProperty,
                    LocationOf(model, mapFrom.AttributeIndex, syntax),
                    model.MethodName,
                    mapFrom.TargetName);
            }

            var targetTypeName = targetMemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var withTarget = mapFrom with
            {
                IsTargetInitOnly = destProp?.GetSetter()?.IsInitOnly == true,
                IsTargetRequired = destProp?.IsRequired == true
            };

            // The member of the source is matched under the mapper's name comparison, an exact match first,
            // and written as declared
            var nameComparison = (StringComparison)model.NameComparison;
            var member = mapFrom.Member;
            var isMethodCall = !member.Contains('.');

            if (isMethodCall)
            {
                var sourceMethod = FindSourceMethod(sourceType, member, nameComparison, within, compilation);
                if (sourceMethod is not null)
                {
                    var returnType = sourceMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if ((returnType != targetTypeName) && !IsImplicitlyConvertible(sourceMethod.ReturnType, targetMemberType, compilation))
                    {
                        return new DiagnosticInfo(
                            Diagnostics.MapFromReturnTypeMismatch,
                            LocationOf(model, mapFrom.AttributeIndex, syntax),
                            model.MethodName,
                            mapFrom.Member,
                            targetTypeName,
                            returnType);
                    }

                    resolved.Add(withTarget with
                    {
                        IsMethodCall = true,
                        Member = sourceMethod.Name,
                        ForgivesNull = (IsNullableReference(sourceMethod.ReturnType) || ReturnsMaybeNull(sourceMethod)) && !targetMemberType.IsNullableType(),
                        IsTargetNullable = targetMemberType.IsNullableType()
                    });
                    continue;
                }
            }

            member = CanonicalizeSourcePath(sourceType, member, nameComparison, within, compilation);
            var (resolvedType, isValid) = PropertyPathHelper.ResolvePropertyPath(sourceType, member, within, compilation);
            if (isValid && (resolvedType is not null))
            {
                var returnType = resolvedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if ((returnType != targetTypeName) && !IsImplicitlyConvertible(resolvedType, targetMemberType, compilation))
                {
                    return new DiagnosticInfo(
                        Diagnostics.MapFromReturnTypeMismatch,
                        LocationOf(model, mapFrom.AttributeIndex, syntax),
                        model.MethodName,
                        mapFrom.Member,
                        targetTypeName,
                        returnType);
                }

                var finalProperty = PropertyPathHelper.ResolvePropertySymbol(sourceType, member.Split('.'), within, compilation, readable: true);
                resolved.Add(withTarget with
                {
                    IsMethodCall = false,
                    Member = member,
                    NullCheckedPaths = GetNullableIntermediatePaths(sourceType, member, within, compilation),
                    ForgivesNull = (IsNullableReference(resolvedType) || ReturnsMaybeNull(finalProperty)) && !targetMemberType.IsNullableType(),
                    IsTargetNullable = targetMemberType.IsNullableType()
                });
                continue;
            }

            return new DiagnosticInfo(
                Diagnostics.InvalidMapFromMember,
                LocationOf(model, mapFrom.AttributeIndex, syntax),
                model.MethodName,
                mapFrom.Member,
                mapFrom.TargetName);
        }

        model = model with { MapFromMappings = new(resolved) };
        return null;
    }

    // The intermediate members of a source property path that may be null, as the paths up to them (Customer and
    // Customer.Address for Customer.Address.City), under whose null check the value is read.
    private static EquatableArray<string> GetNullableIntermediatePaths(ITypeSymbol sourceType, string path, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var paths = new List<string>();
        var type = sourceType;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var property = PropertyPathHelper.ResolveProperty(type, parts[i], StringComparison.Ordinal, within, compilation, readable: true);
            if (property is null)
            {
                break;
            }

            if (MayBeNullMember(property))
            {
                paths.Add(String.Join(".", parts, 0, i + 1));
            }

            type = property.Type;
        }

        return new EquatableArray<string>(paths.ToArray());
    }

    // Whether a value of the type goes to a member of the other as the generated assignment passes it, C# converting
    // it implicitly: the same type, or an implicit reference, boxing, nullable or numeric conversion, or a
    // user-defined implicit operator the generated code can call (not obsolete as an error, CS0619).
    private static bool IsImplicitlyConvertible(ITypeSymbol source, ITypeSymbol destination, Compilation compilation)
    {
        var conversion = compilation.ClassifyCommonConversion(source, destination);
        return conversion.IsImplicit &&
               (!conversion.IsUserDefined || ((conversion.MethodSymbol is { } method) && (method.GetObsoleteKind() != ObsoleteKind.Error)));
    }

    // The method [MapFrom] calls on the source, found the way the call src.Name() binds it: a parameterless
    // instance method the mapper class can call, up the base types (or through the interfaces an interface
    // extends), the one of the most derived type first, so that a method hiding one of a base type (new) wins.
    // An exact match anywhere wins over one under the name comparison. A generic method is not one, as the
    // call gives nothing to infer its type arguments from (CS0411).
    private static IMethodSymbol? FindSourceMethod(ITypeSymbol sourceType, string name, StringComparison comparison, INamedTypeSymbol within, Compilation compilation)
    {
        bool IsCallable(ISymbol member) =>
            member is IMethodSymbol { IsStatic: false, IsGenericMethod: false, Parameters.Length: 0 } && compilation.IsSymbolAccessibleWithin(member, within, sourceType);

        var types = PropertyPathHelper.GetLookupTypes(sourceType).ToList();
        var method = types.SelectMany(t => t.GetMembers(name)).FirstOrDefault(IsCallable);
        if ((method is null) && (comparison != StringComparison.Ordinal))
        {
            method = types.SelectMany(static t => t.GetMembers()).FirstOrDefault(m => IsCallable(m) && String.Equals(m.Name, name, comparison));
        }

        // The call binds to that one, so one obsolete as an error is not callable at all (CS0619)
        return method?.GetObsoleteKind() == ObsoleteKind.Error ? null : (IMethodSymbol?)method;
    }

    // A property path of the source with the names of the properties it resolves to under the comparison,
    // segment by segment; the rest from a segment that is not a property (such as Length) is left as written. A
    // member of the struct a nullable struct holds is read through its Value, as ResolveCanonicalPath takes it.
    private static string CanonicalizeSourcePath(ITypeSymbol sourceType, string path, StringComparison comparison, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var resolved = new List<string>(parts.Length);
        var type = sourceType;
        var i = 0;
        for (; i < parts.Length; i++)
        {
            var property = PropertyPathHelper.ResolveProperty(type, parts[i], comparison, within, compilation, readable: true);
            if ((property is null) && (PropertyPathHelper.ResolveThroughValue(type, parts[i], comparison, within, compilation) is { } member))
            {
                resolved.Add(PropertyPathHelper.NullableValueName);
                property = member;
            }

            if (property is null)
            {
                break;
            }

            resolved.Add(property.Name);
            type = property.Type;
        }

        resolved.AddRange(parts.Skip(i));
        return String.Join(".", resolved);
    }

    internal static DiagnosticInfo? ValidateAndBuildMapCollectionMappings(
        IMethodSymbol mapperMethod,
        Compilation compilation,
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var customTypes = GetCustomParameterTypes(mapperMethod, model);
        var destinationProperties = PropertyPathHelper.GetProperties(destinationType, containingType, compilation);
        var constructor = GetEffectiveConstructor(model, destinationType);

        var resolvedCollections = new List<MapCollectionModel>(model.MapCollectionMappings.Count);
        foreach (var declared in model.MapCollectionMappings)
        {
            var sourceProp = PropertyPathHelper.ResolveProperty(sourceType, declared.SourceName, (StringComparison)model.NameComparison, containingType, compilation, readable: true);
            if (sourceProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionSourceProperty,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    declared.SourceName);
            }

            // The name is emitted verbatim, so adopt the declared casing when NameComparison matched
            // a source property that the attribute spelled differently.
            var mapCollection = declared with { SourceName = sourceProp.Name };

            // The target property, or for a collection that goes to a constructor argument, the parameter
            var destProp = mapCollection.IsConstructorArgument ? null : destinationProperties.FirstOrDefault(p => p.Name == mapCollection.TargetName);
            var targetMemberType = mapCollection.IsConstructorArgument
                ? FindArgumentParameter(constructor, model.ConstructorParameters, mapCollection.TargetName)?.Type
                : destProp?.Type;
            if (targetMemberType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionTargetProperty,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapCollection.TargetName);
            }

            // The emitted loop runs after construction and assigns the target through a setter the mapper class can
            // call (InPlace refills the instance it holds instead). A return mapper makes the collection of an
            // init-only member, or of a required one its constructor does not set ([SetsRequiredMembers]), before
            // construction and sets it in the object initializer, as it passes the one of a member a constructor
            // parameter assigns to the constructor. InPlace has no instance to refill before construction, and a
            // void mapper cannot assign an init-only member.
            var hasAssignableSetter = (destProp is not null) && HasAssignableSetter(destProp, destinationType, containingType, compilation);
            var requiredAtConstruction = model.ReturnsDestination && (destProp?.IsRequired == true) && !ConstructionSetsRequiredMembers(model, destinationType);
            var inInitializer = !mapCollection.IsConstructorArgument && !mapCollection.InPlace &&
                                IsInitializerTarget(model, destProp, hasAssignableSetter, requiredAtConstruction, destinationType, containingType, compilation);
            if (mapCollection.InPlace && (mapCollection.IsConstructorArgument || requiredAtConstruction))
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedInPlaceCollectionTarget,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapCollection.TargetName);
            }

            if (!mapCollection.IsConstructorArgument && !mapCollection.InPlace && !hasAssignableSetter && !inInitializer)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedInitOnlyCollectionTarget,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapCollection.TargetName);
            }

            var sourceElementType = sourceProp.Type.GetCollectionOrMemoryElementType();
            if (sourceElementType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapCollectionSourceNotCollection,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapCollection.SourceName);
            }

            var targetElementType = targetMemberType.GetEnumerableElementType();
            if (targetElementType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapCollectionTargetNotCollection,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapCollection.TargetName);
            }

            var sourceShape = DetermineSourceShape(sourceProp.Type);
            var targetShape = DetermineTargetShape(targetMemberType);
            var targetCollectionMethod = DetermineCollectionMethod(targetMemberType);
            var useHelperPath = (model.CollectionConverterTypeName is not null) || mapCollection.HasCustomConverter();
            var usesConverter = useHelperPath && !mapCollection.InPlace;

            // Without a collection converter the generated code creates the target collection, and a target
            // that cannot take it is reported instead of failing in the generated code
            ITypeSymbol? createdType = null;
            if (mapCollection.InPlace)
            {
                // InPlace clears and refills the target through ICollection<T>, which its declared type has
                // to implement without being a collection that is read-only by design
                if (!IsRefillableCollection(targetMemberType, targetElementType))
                {
                    return new DiagnosticInfo(
                        Diagnostics.UnsupportedInPlaceCollectionTarget,
                        LocationOf(model, declared.AttributeIndex, syntax),
                        mapperMethod.Name,
                        mapCollection.TargetName);
                }

                // A null target gets a new instance when the mapper can assign it; otherwise it stays null
                if (hasAssignableSetter)
                {
                    createdType = GetInPlaceFallbackType(targetMemberType, targetElementType, containingType, compilation);
                }
            }
            else if (!usesConverter)
            {
                // The loop builds a List<T> for a type the shapes do not know; a collection class of its own
                // that cannot take one is created with its own constructor instead
                if ((targetShape == CollectionTargetShape.List) &&
                    (ConstructType(compilation, "System.Collections.Generic.List`1", targetElementType) is { } listType) &&
                    !compilation.ClassifyCommonConversion(listType, targetMemberType).IsImplicit &&
                    IsCreatableCollectionClass(targetMemberType, targetElementType, containingType, compilation))
                {
                    targetShape = CollectionTargetShape.Custom;
                }

                createdType = GetCreatedCollectionType(targetShape, targetMemberType, targetElementType, compilation);
            }

            if ((createdType is not null) && !compilation.ClassifyCommonConversion(createdType, targetMemberType).IsImplicit)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedCollectionTarget,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapCollection.TargetName,
                    createdType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            // The element mapper is called by the loop the generated code emits, or handed as a delegate to
            // the method of the collection converter on the helper path, which InPlace does not take. Only the
            // loop can give the value of a nullable struct element to a mapper taking the struct.
            Func<IMethodSymbol, bool, bool> canCallMapper;
            if (usesConverter)
            {
                // A converter method missing, or one that cannot take the source collection and the element
                // mapper, is reported as a converter signature mismatch instead of leaving the call to fail
                // in the generated code
                var converterType = FindConverterType(mapperMethod, Names.CollectionConverterAttribute, Names.DefaultCollectionConverter);
                var converterMethodName = mapCollection.HasCustomConverter() ? mapCollection.Converter! : targetCollectionMethod;
                var converterMethods = FindCollectionConverterMethods(converterType, converterMethodName, sourceProp.Type, targetMemberType, sourceElementType, targetElementType, compilation);
                if (converterMethods.Count == 0)
                {
                    return new DiagnosticInfo(
                        Diagnostics.InvalidConverterSignature,
                        LocationOf(model, declared.AttributeIndex, syntax),
                        mapperMethod.Name,
                        $"{converterType?.ToDisplayString() ?? Names.DefaultCollectionConverter}.{converterMethodName}",
                        mapCollection.TargetName);
                }

                // An instance method of a ref struct makes no delegate, which would box the instance
                canCallMapper = (m, _) => (m.IsStatic || !m.ContainingType.IsRefLikeType) &&
                                          converterMethods.Any(c => IsDelegateFor(c.Parameters[1].Type, m));
            }
            else
            {
                // The loop passes the element, or the value of a nullable struct one, and creates the instance a
                // void mapper fills with new T()
                var element = GetElementArgumentKind(sourceShape);
                var canCreateElement = CanCreateInstance(targetElementType, containingType, compilation);
                canCallMapper = (m, unwraps) => TakesMapperArguments(m, unwraps ? ArgumentKind.Value : element) && (!m.ReturnsVoid || canCreateElement);
            }

            if (!String.IsNullOrEmpty(mapCollection.Mapper) &&
                (InstanceMethodOfStaticMapper(model, containingType, mapCollection.Mapper!, declared.AttributeIndex, compilation, syntax) is { } instanceError))
            {
                return instanceError;
            }

            var elementMatch = FindMapperMethod(
                containingType,
                mapCollection.Mapper!,
                sourceElementType,
                targetElementType,
                compilation,
                usesConverter ? null : GetElementArgumentKind(sourceShape),
                canCallMapper,
                model.IsInstance,
                model.CustomParameters,
                customTypes);
            if (elementMatch is not { } matchedElementMapper)
            {
                return String.IsNullOrEmpty(mapCollection.Mapper)
                    ? new DiagnosticInfo(
                        Diagnostics.MapCollectionMapperNotSpecified,
                        LocationOf(model, declared.AttributeIndex, syntax),
                        mapperMethod.Name,
                        mapCollection.TargetName)
                    : new DiagnosticInfo(
                        Diagnostics.InvalidMapCollectionMapperMethod,
                        LocationOf(model, declared.AttributeIndex, syntax),
                        mapperMethod.Name,
                        mapCollection.Mapper!,
                        mapCollection.TargetName);
            }

            var elementMapper = matchedElementMapper.Method;

            // A reference element that may be null (nullable, or declared with nullable annotations disabled) goes to a
            // mapper whose parameter does not take null only when it has a value, as the value of a nullable struct one
            // does, and a null one gives default, as a null source of [MapNested] does. The loop can pass it so to a
            // parameter taking a value; a collection converter takes the mapper as a delegate.
            var unwrapsReference = !usesConverter && MayBeNullReference(sourceElementType) &&
                                   (elementMapper.Parameters[0].RefKind is RefKind.None or RefKind.In) &&
                                   !TakesNull(elementMapper.Parameters[0], elementMapper.Parameters[0].Type);

            // The mapper gets an element that is not null: one of a type that is not nullable, or one it is called
            // for only when it has a value
            var unwrapsElement = matchedElementMapper.UnwrapsSource || unwrapsReference;
            var getsElementValue = unwrapsElement || !MayBeNull(sourceElementType);
            resolvedCollections.Add(mapCollection with
            {
                IsInitializerEntry = inInitializer,
                SourceElementTypeArgument = sourceElementType.ToDisplayString(NullableQualifiedFormat),
                TargetType = GetCreatedTypeName(targetMemberType),
                TargetElementType = GetCreatedTypeName(targetElementType),
                TargetElementTypeArgument = targetElementType.ToDisplayString(NullableQualifiedFormat),
                IsSourceNullable = MayBeNullMember(sourceProp),
                TargetCollectionMethod = targetCollectionMethod,
                SourceShape = sourceShape,
                TargetShape = targetShape,
                UseHelperPath = useHelperPath,
                InPlaceFallbackTypeName = mapCollection.InPlace
                    ? (createdType is null ? null : GetCreatedTypeName(createdType))
                    : mapCollection.InPlaceFallbackTypeName,
                CreatedTypeName = !mapCollection.InPlace && (targetShape == CollectionTargetShape.Dictionary) && (createdType is not null)
                    ? GetCreatedTypeName(createdType)
                    : null,
                MapperReturnsValue = !elementMapper.ReturnsVoid,
                ForgivesMapperResult = ReturnsNullableInto(elementMapper, targetElementType) && !(getsElementValue && ReturnsNotNullForValue(elementMapper, compilation)),
                UnwrapsSource = unwrapsElement,
                NullResult = GetNullResult(elementMapper, targetElementType),
                MapperParameterRefKinds = GetParameterRefKinds(elementMapper),
                MapperCustomArguments = GetCustomArgumentText(elementMapper, matchedElementMapper.CustomArguments, model.CustomParameters),
                PassesNonNullCulture = matchedElementMapper.CustomArguments.Any(static i => i < 0)
            });
        }

        model = model with { MapCollectionMappings = new(resolvedCollections) };
        return null;
    }

    internal static DiagnosticInfo? ValidateAndBuildMapNestedMappings(
        IMethodSymbol mapperMethod,
        Compilation compilation,
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var customTypes = GetCustomParameterTypes(mapperMethod, model);
        var destinationProperties = PropertyPathHelper.GetProperties(destinationType, containingType, compilation);
        var constructor = GetEffectiveConstructor(model, destinationType);

        var resolvedNested = new List<MapNestedModel>(model.MapNestedMappings.Count);
        foreach (var declared in model.MapNestedMappings)
        {
            var sourceProp = PropertyPathHelper.ResolveProperty(sourceType, declared.SourceName, (StringComparison)model.NameComparison, containingType, compilation, readable: true);
            if (sourceProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionSourceProperty,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    declared.SourceName);
            }

            // The name is emitted verbatim, so adopt the declared casing when NameComparison matched
            // a source property that the attribute spelled differently.
            var mapNested = declared with { SourceName = sourceProp.Name };

            // The target property, or for a value that goes to a constructor argument, the parameter
            var destProp = mapNested.IsConstructorArgument ? null : destinationProperties.FirstOrDefault(p => p.Name == mapNested.TargetName);
            var targetMemberType = mapNested.IsConstructorArgument
                ? FindArgumentParameter(constructor, model.ConstructorParameters, mapNested.TargetName)?.Type
                : destProp?.Type;
            if (targetMemberType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionTargetProperty,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapNested.TargetName);
            }

            // Same rules as MapCollection: the nested-map statements run after construction, or for a member a
            // constructor parameter assigns, before it, the value passed to the constructor, and a return mapper
            // makes the value of an init-only or required member before construction for the object initializer.
            var hasAssignableSetter = (destProp is not null) && HasAssignableSetter(destProp, destinationType, containingType, compilation);
            var requiredAtConstruction = model.ReturnsDestination && (destProp?.IsRequired == true) && !ConstructionSetsRequiredMembers(model, destinationType);
            var inInitializer = IsInitializerTarget(model, destProp, hasAssignableSetter, requiredAtConstruction, destinationType, containingType, compilation);
            if ((destProp is not null) && !hasAssignableSetter && !inInitializer)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedInitOnlyCollectionTarget,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapNested.TargetName);
            }

            var sourceUnderlyingType = sourceProp.Type;
            if ((sourceProp.Type.NullableAnnotation == NullableAnnotation.Annotated) &&
                (sourceProp.Type is INamedTypeSymbol namedType) &&
                (!namedType.IsValueType))
            {
                sourceUnderlyingType = namedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            var targetUnderlyingType = targetMemberType;
            if ((targetMemberType.NullableAnnotation == NullableAnnotation.Annotated) &&
                (targetMemberType is INamedTypeSymbol namedDestType) &&
                (!namedDestType.IsValueType))
            {
                targetUnderlyingType = namedDestType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            // The nested mapper gets the property value, or the value of a nullable struct, and a void one the
            // instance the generated code creates with new T()
            var canCreateTarget = CanCreateInstance(targetUnderlyingType, containingType, compilation);
            if (!String.IsNullOrEmpty(mapNested.Mapper) &&
                (InstanceMethodOfStaticMapper(model, containingType, mapNested.Mapper, declared.AttributeIndex, compilation, syntax) is { } instanceError))
            {
                return instanceError;
            }

            var nestedMatch = FindMapperMethod(
                containingType,
                mapNested.Mapper,
                sourceUnderlyingType,
                targetUnderlyingType,
                compilation,
                ArgumentKind.Value,
                (m, _) => TakesMapperArguments(m, ArgumentKind.Value) && (!m.ReturnsVoid || canCreateTarget),
                model.IsInstance,
                model.CustomParameters,
                customTypes);
            if (nestedMatch is not { } matchedNestedMapper)
            {
                return String.IsNullOrEmpty(mapNested.Mapper)
                    ? new DiagnosticInfo(
                        Diagnostics.MapNestedMapperNotSpecified,
                        LocationOf(model, declared.AttributeIndex, syntax),
                        mapperMethod.Name,
                        mapNested.TargetName)
                    : new DiagnosticInfo(
                        Diagnostics.InvalidMapNestedMapperMethod,
                        LocationOf(model, declared.AttributeIndex, syntax),
                        mapperMethod.Name,
                        mapNested.Mapper,
                        mapNested.TargetName);
            }

            // The mapper gets a value that is not null unless it takes null and the source may be null: the source is
            // not of a nullable type, or it is called for a value only
            var nestedMapper = matchedNestedMapper.Method;
            var mapperTakesNull = !matchedNestedMapper.UnwrapsSource && TakesNull(nestedMapper.Parameters[0], nestedMapper.Parameters[0].Type);
            var getsValue = !MayBeNullMember(sourceProp) || !mapperTakesNull;

            resolvedNested.Add(mapNested with
            {
                IsInitializerEntry = inInitializer,
                TargetType = GetCreatedTypeName(targetMemberType),
                ArgumentLocalType = ReturnsNullable(nestedMapper) && (targetMemberType.NullableAnnotation == NullableAnnotation.Annotated)
                    ? targetMemberType.ToDisplayString(NullableQualifiedFormat)
                    : targetMemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsSourceNullable = MayBeNullMember(sourceProp),
                MapperTakesNull = mapperTakesNull,
                MapperReturnsValue = !nestedMapper.ReturnsVoid,
                ForgivesMapperResult = ReturnsNullableInto(nestedMapper, targetMemberType) && !(getsValue && ReturnsNotNullForValue(nestedMapper, compilation)),
                UnwrapsSource = matchedNestedMapper.UnwrapsSource,
                NullResult = GetNullResult(nestedMapper, targetMemberType),
                MapperParameterRefKinds = GetParameterRefKinds(nestedMapper),
                MapperCustomArguments = GetCustomArgumentText(nestedMapper, matchedNestedMapper.CustomArguments, model.CustomParameters),
                PassesNonNullCulture = matchedNestedMapper.CustomArguments.Any(static i => i < 0)
            });
        }

        model = model with { MapNestedMappings = new(resolvedNested) };
        return null;
    }

    // Whether a return mapper sets the target of [MapNested] / [MapCollection] in its object initializer: an init-only
    // member, or a required one the constructor called does not set, with a setter or an init accessor the mapper class
    // can call.
    private static bool IsInitializerTarget(
        MapperMethodModel model,
        IPropertySymbol? destProp,
        bool hasAssignableSetter,
        bool requiredAtConstruction,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation) =>
        model.ReturnsDestination && (destProp is not null) && (requiredAtConstruction || !hasAssignableSetter) &&
        CanAssignFromMapper(destProp, destinationType, within, compilation);

    // The name of a type the generated code creates, or declares the local of a constructor argument as: with the
    // nullable annotations of its type arguments (List<Item?>), which the target has to get for its type (CS8619
    // otherwise), and without its own, which new cannot take (CS8628), a type parameter's (T?) included. A nullable
    // struct is a type of its own.
    private static string GetCreatedTypeName(ITypeSymbol type) =>
        ((type.NullableAnnotation == NullableAnnotation.Annotated) && !type.IsValueType ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : type)
        .ToDisplayString(NullableQualifiedFormat);

    // Whether a mapper returns a nullable reference into a target not annotated as one. The generated code takes
    // its result with !, as it takes a null source member as default!, so that the assignment does not warn
    // (CS8601): a mapper declared to take null, as in Map(Src? source), returns null only for a null source.
    private static bool ReturnsNullableInto(IMethodSymbol mapper, ITypeSymbol target) =>
        ReturnsNullable(mapper) && (target.NullableAnnotation != NullableAnnotation.Annotated);

    // What the target gets for a null source: default!, or, for a nullable struct target a mapper returning the
    // struct it holds fills, default of the target type, null, as the conditional would take the type of the
    // result and give its default.
    private static string GetNullResult(IMethodSymbol mapper, ITypeSymbol target) =>
        !mapper.ReturnsVoid && (GetNullableValueType(target) is { } valueType) && IsSameType(mapper.ReturnType, valueType)
            ? "default(" + target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")"
            : "default!";

    // A mapper returning a nullable reference, or one whose return [MaybeNull] says may be null
    private static bool ReturnsNullable(IMethodSymbol mapper) =>
        !mapper.ReturnsVoid && mapper.ReturnType.IsReferenceType &&
        ((mapper.ReturnType.NullableAnnotation == NullableAnnotation.Annotated) || ReturnsMaybeNull(mapper));

    // Whether a method returning a nullable reference returns one that is not null for a first argument that is not
    // null: [return: NotNullIfNotNull] names its first parameter, or it is a [Mapper] the generated code puts the
    // attribute on (ReturnNotNullIfNotNull), which the compilation the generator reads does not have yet. The compiler
    // takes the result of such a call so, so the generated code takes it without ! and Strict mode does not report it.
    private static bool ReturnsNotNullForValue(IMethodSymbol method, Compilation compilation)
    {
        if (method.ReturnsVoid || (method.Parameters.Length == 0))
        {
            return false;
        }

        var parameterName = method.Parameters[0].Name;
        if (method.GetReturnTypeAttributes().Any(a =>
                (a.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute") &&
                (a.ConstructorArguments.Length == 1) &&
                (a.ConstructorArguments[0].Value is string name) &&
                (name == parameterName)))
        {
            return true;
        }

        return method.IsPartialDefinition &&
               method.GetAttributes().Any(static a => a.AttributeClass?.ToDisplayString() == Names.MapperAttribute) &&
               MayBeNullReference(method.Parameters[0].Type) &&
               method.ReturnType.IsNullableType() &&
               CanApplyNotNullIfNotNull(compilation, method.ContainingType);
    }

    // The mapper of [MapCollection] / [MapNested], whether the value of a nullable struct source goes to it, and the
    // custom parameters of the mapper it takes after the source (and the instance of a void one).
    internal readonly record struct MapperMatch(IMethodSymbol Method, bool UnwrapsSource, EquatableArray<int> CustomArguments);

    // The mapper of [MapCollection] / [MapNested]: a static method taking the source element and returning
    // the target one, or taking both and filling the target. The source goes to a parameter of its type, or of
    // one it converts to by an implicit reference conversion when taken by value, or, for a nullable struct the
    // generated code calls the mapper with (sourceArgument, how it passes the source; null for a mapper handed to a
    // collection converter as a delegate), the value it holds to one of the struct; the result goes to a target
    // of its type, or of one it converts to by an implicit reference conversion or as the value of a nullable
    // struct; and the instance created for a void mapper goes to a parameter of its type, or of one it converts
    // to by an implicit reference conversion when taken by value. Only one the call can pass its arguments to is
    // used, the closest ones first (the types themselves over conversions, conversions over the value of a
    // nullable struct). Of those, the first decides between the two shapes, as the first match did before; the
    // call binds to the one whose parameter types each convert to those of the others, and none binds when there
    // is no such one (CS0121); of the same types, one taking every argument by value wins, as the plain call always
    // chose it. The call the generated code makes binds among all the methods the name is looked up as, as C# binds it
    // (BindCall), which may be another one: one matched of the same shape is used instead, and none otherwise (a
    // more specific one returning another type, a generic one, one with optional parameters, one obsolete as an
    // error), as the call would bind to it. After the source (and the instance of a void one) it takes the custom
    // parameters of the mapper it declares (MapCustomArguments), one taking more of them going first, and none of
    // several taking as many but other ones (TakeDifferentCustomParameters), which a mapper handed to a collection
    // converter as a delegate cannot take.
    internal static MapperMatch? FindMapperMethod(
        INamedTypeSymbol containingType,
        string methodName,
        ITypeSymbol sourceElementType,
        ITypeSymbol targetElementType,
        Compilation compilation,
        ArgumentKind? sourceArgument,
        Func<IMethodSymbol, bool, bool> canCall,
        bool instance,
        EquatableArray<CustomParameterModel> customParams,
        IReadOnlyList<ITypeSymbol> customTypes)
    {
        var matches = new List<(IMethodSymbol Method, int Score, bool Unwraps, int[] Custom)>();
        var methods = LookupMethods(containingType, methodName, instance, compilation);
        foreach (var method in methods.Where(IsCallableByName))
        {
            if (MatchesMapperShape(method, sourceElementType, targetElementType, compilation, sourceArgument is not null, out var score, out var unwraps) &&
                (MapCustomArguments(method, method.ReturnsVoid ? 2 : 1, customParams, customTypes, compilation) is { } custom) &&
                ((sourceArgument is not null) || (custom.Length == 0)) &&
                canCall(method, unwraps))
            {
                matches.Add((method, score, unwraps, custom));
            }
        }

        if (matches.Count == 0)
        {
            return null;
        }

        var mostCustom = matches.Max(static m => m.Custom.Length);
        var preferred = matches.Where(m => m.Custom.Length == mostCustom).ToList();
        if ((preferred.Count > 1) && TakeDifferentCustomParameters(preferred.Select(static m => m.Custom).ToList()))
        {
            return null;
        }

        var closestScore = preferred.Min(static m => m.Score);
        var closest = preferred.Where(m => m.Score == closestScore).ToList();
        var returnsVoid = closest[0].Method.ReturnsVoid;
        closest.RemoveAll(m => m.Method.ReturnsVoid != returnsVoid);

        var bound = closest.Where(m => closest.All(o => IsAtLeastAsSpecific(m.Method, o.Method, compilation))).ToList();
        if (bound.Count == 0)
        {
            return null;
        }

        var chosen = bound[0];
        foreach (var candidate in bound)
        {
            if (TakesAllByValue(candidate.Method))
            {
                chosen = candidate;
                break;
            }
        }

        if (sourceArgument is { } argument)
        {
            var called = BindCall(methods, GetMapperCallArguments(chosen.Method, chosen.Unwraps, sourceElementType, targetElementType, argument, chosen.Custom, customTypes), compilation);
            if (!SymbolEqualityComparer.Default.Equals(called, chosen.Method))
            {
                var other = matches.FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(m.Method, called) &&
                                                        (m.Method.ReturnsVoid == chosen.Method.ReturnsVoid) && (m.Unwraps == chosen.Unwraps));
                if ((other.Method is null) ||
                    !SymbolEqualityComparer.Default.Equals(
                        BindCall(methods, GetMapperCallArguments(other.Method, other.Unwraps, sourceElementType, targetElementType, argument, other.Custom, customTypes), compilation),
                        other.Method))
                {
                    return null;
                }

                chosen = other;
            }
        }

        return new MapperMatch(chosen.Method, chosen.Unwraps, new EquatableArray<int>(chosen.Custom));
    }

    // The custom parameters a mapper of [MapCollection] / [MapNested] takes after the source (and the instance of a void
    // one), as the arguments the generated code appends to its call, each with the modifier of the parameter taking it,
    // or the culture not null (MapCustomArguments)
    private static string GetCustomArgumentText(IMethodSymbol mapper, EquatableArray<int> customArguments, EquatableArray<CustomParameterModel> customParams)
    {
        var first = mapper.ReturnsVoid ? 2 : 1;
        return String.Concat(Enumerable.Range(0, customArguments.Count).Select(i =>
            ", " +
            (customArguments[i] < 0
                ? customParams[~customArguments[i]].NonNullArgument
                : GetArgumentModifierKind(mapper.Parameters[first + i].RefKind) switch
                  {
                      RefKind.Ref => "ref ",
                      RefKind.In => "in ",
                      _ => string.Empty
                  } +
                  customParams[customArguments[i]].Name)));
    }

    // What the call of the mapper of [MapCollection] / [MapNested] passes: the source, or the value a nullable struct
    // holds, as a value, or as a variable with the modifier of the parameter taking it, for a void mapper the instance
    // created for it, with the modifier of its parameter, and the custom parameters it takes.
    private static List<CallArgument> GetMapperCallArguments(
        IMethodSymbol mapper,
        bool unwraps,
        ITypeSymbol sourceType,
        ITypeSymbol createdType,
        ArgumentKind sourceArgument,
        int[] custom,
        IReadOnlyList<ITypeSymbol> customTypes)
    {
        var arguments = new List<CallArgument>
        {
            new(
                unwraps ? GetNullableValueType(sourceType)! : sourceType,
                unwraps || (sourceArgument == ArgumentKind.Value) ? RefKind.None : GetArgumentModifierKind(mapper.Parameters[0].RefKind))
        };
        if (mapper.ReturnsVoid)
        {
            arguments.Add(new CallArgument(createdType, GetArgumentModifierKind(mapper.Parameters[1].RefKind)));
        }

        var first = arguments.Count;
        for (var i = 0; i < custom.Length; i++)
        {
            arguments.Add(CustomCallArgument(custom[i], customTypes, mapper.Parameters[first + i]));
        }

        return arguments;
    }

    // Whether the call binds to the method rather than to the other: each parameter taking the source (and the
    // instance) of the same type, or of one converting to the other's. The custom parameters after them are of the
    // types of the mapper's.
    private static bool IsAtLeastAsSpecific(IMethodSymbol method, IMethodSymbol other, Compilation compilation) =>
        method.Parameters.Take(method.ReturnsVoid ? 2 : 1)
            .Zip(other.Parameters, (p, q) => IsSameType(p.Type, q.Type) || compilation.ClassifyCommonConversion(p.Type, q.Type).IsImplicit)
            .All(static x => x);

    // Whether the method has the shape of a mapper for the source and the target, and how close the match is:
    // 0 for the types themselves, more for each conversion (see FindMapperMethod).
    private static bool MatchesMapperShape(
        IMethodSymbol method,
        ITypeSymbol sourceType,
        ITypeSymbol targetType,
        Compilation compilation,
        bool allowUnwrap,
        out int score,
        out bool unwraps)
    {
        score = 0;
        unwraps = false;
        if ((method.Parameters.Length < (method.ReturnsVoid ? 2 : 1)) ||
            !MatchesMapperSource(method.Parameters[0], sourceType, compilation, allowUnwrap, out var sourceScore, out unwraps))
        {
            return false;
        }

        if (method.ReturnsVoid)
        {
            if (!MatchesMapperInstance(method.Parameters[1], targetType, compilation, out var instanceScore))
            {
                return false;
            }

            score = sourceScore + instanceScore;
            return true;
        }

        if (!MatchesMapperResult(method.ReturnType, targetType, compilation, out var resultScore))
        {
            return false;
        }

        score = sourceScore + resultScore;
        return true;
    }

    private static bool MatchesMapperSource(IParameterSymbol parameter, ITypeSymbol sourceType, Compilation compilation, bool allowUnwrap, out int score, out bool unwraps)
    {
        unwraps = false;
        if (IsSameType(sourceType, parameter.Type))
        {
            score = 0;
            return true;
        }

        if ((parameter.RefKind == RefKind.None) && IsImplicitReferenceConversion(sourceType, parameter.Type, compilation))
        {
            score = 1;
            return true;
        }

        // The value goes as a value, which a parameter taking it by value or in takes
        unwraps = allowUnwrap && (parameter.RefKind is RefKind.None or RefKind.In) &&
                  (GetNullableValueType(sourceType) is { } valueType) && IsSameType(valueType, parameter.Type);
        score = 2;
        return unwraps;
    }

    private static bool MatchesMapperResult(ITypeSymbol resultType, ITypeSymbol targetType, Compilation compilation, out int score)
    {
        score = IsSameType(resultType, targetType) ? 0 : 1;
        return (score == 0) ||
               IsImplicitReferenceConversion(resultType, targetType, compilation) ||
               ((GetNullableValueType(targetType) is { } valueType) && IsSameType(resultType, valueType));
    }

    private static bool MatchesMapperInstance(IParameterSymbol parameter, ITypeSymbol createdType, Compilation compilation, out int score)
    {
        score = IsSameType(createdType, parameter.Type) ? 0 : 1;
        return (score == 0) || ((parameter.RefKind == RefKind.None) && IsImplicitReferenceConversion(createdType, parameter.Type, compilation));
    }

    // The same type, the nullable annotations of references aside
    private static bool IsSameType(ITypeSymbol left, ITypeSymbol right) =>
        left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static bool IsImplicitReferenceConversion(ITypeSymbol source, ITypeSymbol destination, Compilation compilation)
    {
        var conversion = compilation.ClassifyCommonConversion(source, destination);
        return conversion.IsImplicit && conversion.IsReference;
    }

    // The struct a nullable struct holds, or null for another type
    private static ITypeSymbol? GetNullableValueType(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : null;

    // A method of the mapper class the generated code calls by name, of those the name is looked up as: a converter, a
    // condition, a [MapUsing] method, a callback, or the mapper of [MapCollection] / [MapNested]. A generic one is not,
    // as the call passes no type arguments, and the arguments of the shapes matched never let them be inferred
    // (CS0411).
    // One obsolete as an error cannot be called (CS0619), so it does not match; one obsolete as a warning is
    // called, as the attribute names it.
    private static bool IsCallableByName(IMethodSymbol method) =>
        !method.IsGenericMethod && (method.GetObsoleteKind() != ObsoleteKind.Error);

    // Whether a mapper takes the arguments of the call: the source of the given kind, and for a void
    // mapper the instance the generated code creates for it, a local it may write.
    private static bool TakesMapperArguments(IMethodSymbol method, ArgumentKind source) =>
        CanTakeArgument(method.Parameters[0].RefKind, source) &&
        method.Parameters.Skip(1).Take(method.ReturnsVoid ? 1 : 0).All(static p => CanTakeArgument(p.RefKind, ArgumentKind.WritableVariable));

    // What the loop over a source collection passes as the element: an element of an array or of a span
    // over an array, List<T> or Memory<T>, which it may write; one of a read-only span (ImmutableArray<T>,
    // ReadOnlyMemory<T>) or a foreach variable, which it may only read; and the value an IList<T> /
    // IReadOnlyList<T> indexer returns.
    internal static ArgumentKind GetElementArgumentKind(CollectionSourceShape shape) => shape switch
    {
        CollectionSourceShape.Array or CollectionSourceShape.List or CollectionSourceShape.Memory => ArgumentKind.WritableVariable,
        CollectionSourceShape.IndexedList => ArgumentKind.Value,
        _ => ArgumentKind.ReadOnlyVariable
    };

    // The methods of a collection converter the helper path can call as Method<TSourceElement,
    // TTargetElement>(source, mapper), constructed with the element types: static, taking both arguments as
    // values, the first one the source collection, meeting the constraints of their type parameters, and
    // returning something the target property takes.
    private static List<IMethodSymbol> FindCollectionConverterMethods(
        ITypeSymbol? converterType,
        string methodName,
        ITypeSymbol sourceCollectionType,
        ITypeSymbol targetCollectionType,
        ITypeSymbol sourceElementType,
        ITypeSymbol targetElementType,
        Compilation compilation)
    {
        if (converterType is null)
        {
            return [];
        }

        var typeArguments = new[] { sourceElementType, targetElementType };
        return converterType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic &&
                        (m.GetObsoleteKind() != ObsoleteKind.Error) &&
                        (m.Arity == 2) &&
                        TakesArgumentCount(m, 2) &&
                        m.Parameters.Take(2).All(static p => CanTakeArgument(p.RefKind, ArgumentKind.Value)) &&
                        SatisfiesConstraints(m, typeArguments, compilation))
            .Select(m => m.Construct(typeArguments))
            .Where(m => compilation.ClassifyCommonConversion(sourceCollectionType, m.Parameters[0].Type).IsImplicit &&
                        compilation.ClassifyCommonConversion(m.ReturnType, targetCollectionType).IsImplicit)
            .ToList();
    }

    // Whether the type arguments meet the constraints of the method's type parameters: new(), class,
    // struct, and the constraint types that do not refer to the type parameters themselves.
    private static bool SatisfiesConstraints(IMethodSymbol method, ITypeSymbol[] typeArguments, Compilation compilation)
    {
        for (var i = 0; i < method.TypeParameters.Length; i++)
        {
            var parameter = method.TypeParameters[i];
            var argument = typeArguments[i];
            if ((parameter.HasConstructorConstraint && !SatisfiesNewConstraint(argument)) ||
                (parameter.HasReferenceTypeConstraint && !argument.IsReferenceType) ||
                (parameter.HasValueTypeConstraint && (!argument.IsValueType || (argument.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T))) ||
                parameter.ConstraintTypes.Any(c => !RefersToTypeParameter(c) && !IsConstraintConversion(compilation.ClassifyCommonConversion(argument, c))))
            {
                return false;
            }
        }

        return true;
    }

    private static bool RefersToTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol array => RefersToTypeParameter(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(RefersToTypeParameter),
        _ => false
    };

    // A constraint type takes an identity, reference or boxing conversion.
    private static bool IsConstraintConversion(CommonConversion conversion) =>
        conversion.IsImplicit && !conversion.IsUserDefined && !conversion.IsNumeric && !conversion.IsNullable;

    // new() takes an enum, or a struct or class that is not abstract with a public constructor without
    // parameters, and not one with required members that constructor leaves unset (CS9040).
    private static bool SatisfiesNewConstraint(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return typeParameter.HasConstructorConstraint || typeParameter.HasValueTypeConstraint;
        }

        if ((type is not INamedTypeSymbol named) || named.IsAbstract)
        {
            return false;
        }

        if (named.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        var constructor = named.InstanceConstructors.FirstOrDefault(static c => (c.Parameters.Length == 0) && (c.DeclaredAccessibility == Accessibility.Public));
        return (named.TypeKind is TypeKind.Class or TypeKind.Struct) &&
               (constructor is not null) &&
               (!HasRequiredMembers(named) || SetsRequiredMembers(constructor));
    }

    // Whether the generated code can write new T() for the instance a void mapper fills: an enum, or a
    // struct or class that is not abstract with a constructor callable without arguments from the mapper
    // class, and not one with required members that constructor leaves unset (CS9035).
    private static bool CanCreateInstance(ITypeSymbol type, INamedTypeSymbol within, Compilation compilation)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return typeParameter.HasConstructorConstraint || typeParameter.HasValueTypeConstraint;
        }

        if ((type is not INamedTypeSymbol named) || named.IsAbstract || named.IsStatic)
        {
            return false;
        }

        if (named.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        var constructor = FindConstructorWithoutArguments(named, within, compilation);
        return (named.TypeKind is TypeKind.Class or TypeKind.Struct) &&
               (constructor is not null) && IsConstructorCallable(constructor, within, compilation) &&
               (!HasRequiredMembers(named) || SetsRequiredMembers(constructor));
    }

    private static bool HasRequiredMembers(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetMembers().Any(static m => m is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true }))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SetsRequiredMembers(IMethodSymbol constructor) =>
        constructor.GetAttributes().Any(static a => a.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute");

    // Whether the mapper class can assign the property of an instance of the receiver type, in a statement or
    // an object initializer: a setter or an init accessor it can call on that instance (a protected one only on
    // a type deriving from the mapper class, CS1540), not obsolete as an error (CS0619).
    private static bool CanAssignFromMapper(IPropertySymbol property, ITypeSymbol receiverType, INamedTypeSymbol within, Compilation compilation) =>
        (property.GetSetter() is { } setter) && compilation.IsSymbolAccessibleWithin(setter, within, receiverType) &&
        (property.GetWriteObsoleteKind() != ObsoleteKind.Error);

    // Whether new T(...) can call the constructor from the mapper class. The instance it creates is the one the
    // constructor is reached through, so a protected one is not callable from a class deriving from the type
    // (CS0122), as a base(...) call is the only way to it. One obsolete as an error is not callable either
    // (CS0619).
    private static bool IsConstructorCallable(IMethodSymbol constructor, INamedTypeSymbol within, Compilation compilation) =>
        compilation.IsSymbolAccessibleWithin(constructor, within, constructor.ContainingType) && !IsObsoleteAsError(constructor);

    private static bool IsObsoleteAsError(ISymbol? symbol) =>
        symbol?.GetObsoleteKind() == ObsoleteKind.Error;

    // Obsolete as a warning (CS0618), which a constructor is called under only when nothing else constructs
    private static bool IsObsoleteAsWarning(ISymbol symbol) =>
        symbol.GetObsoleteKind() == ObsoleteKind.Warning;

    // A [MapConstant] value, and the NullValue of a mapping wherever the generated code writes it, have to be
    // written as an expression (SMP0215 for one that cannot be, such as a file-local type) that converts to
    // the type it is assigned to the way the compiler converts it (an int constant to a long or a byte, null
    // to a reference), and that puts null only where the target takes it. One that does not is reported
    // instead of failing or warning in the generated code (CS0029 / CS0019 / CS0266 / CS8625 / CS8601 /
    // CS8619).
    internal static DiagnosticInfo? ValidateConstantValues(
        MapperMethodModel model,
        SemanticModel semanticModel,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        MethodDeclarationSyntax syntax)
    {
        var position = syntax.SpanStart;
        var constructor = GetEffectiveConstructor(model, destinationType);
        foreach (var constant in model.ConstantMappings)
        {
            if (constant.Value is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedConstantValue,
                    LocationOf(model, constant.AttributeIndex, syntax),
                    model.MethodName,
                    constant.TargetName);
            }

            var target = constant.IsConstructorArgument
                ? FindArgumentParameter(constructor, model.ConstructorParameters, constant.TargetName)
                : FindTargetMember(destinationType, constant.TargetName, within, semanticModel.Compilation);
            if ((target is not null) &&
                (!ConvertsImplicitly(semanticModel, position, constant.Value, GetMemberType(target)!) ||
                 !PutsNullWhereTaken(constant.Value, constant.HasNullElement, target)))
            {
                return new DiagnosticInfo(
                    Diagnostics.UnassignableConstantValue,
                    LocationOf(model, constant.AttributeIndex, syntax),
                    model.MethodName,
                    constant.TargetName,
                    constant.Value);
            }
        }

        foreach (var mapping in model.PropertyMappings)
        {
            if (!mapping.HasNullValue() && !mapping.IsNullValueUnsupported)
            {
                continue;
            }

            var target = (mapping.IsConstructorParameter ? FindArgumentParameter(constructor, model.ConstructorParameters, mapping.TargetPath) : null) ??
                         (ISymbol?)PropertyPathHelper.ResolvePropertySymbol(destinationType, mapping.TargetPath.Split('.'), within, semanticModel.Compilation) ??
                         constructor?.Parameters.FirstOrDefault(p => p.Name == mapping.TargetPath);
            var (fallback, coalesce) = GetNullValueUses(mapping, model);
            if ((target is null) || (!fallback && !coalesce))
            {
                continue;
            }

            if (mapping.IsNullValueUnsupported)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedConstantValue,
                    LocationOf(model, mapping.AttributeIndex, syntax),
                    model.MethodName,
                    mapping.TargetPath);
            }

            var targetType = GetMemberType(target)!;
            if ((fallback && !ConvertsImplicitly(semanticModel, position, mapping.NullValue!, targetType)) ||
                (coalesce && !ConvertsImplicitly(semanticModel, position, $"default({mapping.SourceType}) ?? {mapping.NullValue}", targetType)) ||
                !PutsNullWhereTaken(mapping.NullValue!, mapping.NullValueHasNullElement, target))
            {
                return new DiagnosticInfo(
                    Diagnostics.UnassignableConstantValue,
                    LocationOf(model, mapping.AttributeIndex, syntax),
                    model.MethodName,
                    mapping.TargetPath,
                    mapping.NullValue!);
            }
        }

        return null;
    }

    // Where the generated code writes the NullValue of a mapping: as the fallback of a conditional (a
    // nullable source that is converted or given to a converter method, or a source path with a nullable
    // intermediate member, which a statement guarded by a condition leaves out), and after ?? on a nullable
    // source copied as it is. NullBehavior.Skip in a statement leaves it out, keeping the target as it is.
    private static (bool Fallback, bool Coalesce) GetNullValueUses(PropertyMappingModel mapping, MapperMethodModel model)
    {
        var inExpression = mapping.IsConstructorParameter ||
            (model.UseConstructorMapping && (mapping.IsTargetInitOnly || mapping.IsTargetRequired));
        var written = inExpression || (mapping.NullBehavior != NullBehaviorType.Skip);
        var nullableSource = mapping.IsSourceNullable && written;
        var converted = mapping.RequiresConversion || mapping.HasConverter();
        return (
            (nullableSource && converted) || (written && mapping.RequiresNullCheck() && (inExpression || !mapping.HasCondition())),
            nullableSource && !converted);
    }

    private static bool ConvertsImplicitly(SemanticModel semanticModel, int position, string expression, ITypeSymbol type) =>
        semanticModel.ClassifyConversion(position, SyntaxFactory.ParseExpression(expression), type).IsImplicit;

    // Whether the value puts null only where the target takes it: null itself into a member that takes null,
    // and the null elements of an array into elements that do.
    private static bool PutsNullWhereTaken(string value, bool hasNullElement, ISymbol target)
    {
        var type = GetMemberType(target)!;
        return ((value != "null") || TakesNull(target, type)) && (!hasNullElement || TakesNullElements(type));
    }

    // A member takes null without a nullable warning unless its type is a reference annotated as not null
    // without [AllowNull], or it has [DisallowNull]. A type declared with nullable annotations disabled
    // takes it, and a value type is left to the conversion check.
    private static bool TakesNull(ISymbol target, ITypeSymbol type)
    {
        if (HasAttribute(target, "System.Diagnostics.CodeAnalysis.DisallowNullAttribute"))
        {
            return false;
        }

        return !type.IsReferenceType || (type.NullableAnnotation != NullableAnnotation.NotAnnotated) ||
               HasAttribute(target, "System.Diagnostics.CodeAnalysis.AllowNullAttribute");
    }

    // An array holding null goes to an array or a sequence whose elements take null, or to a type without
    // elements, such as object.
    private static bool TakesNullElements(ITypeSymbol type)
    {
        var elementType = type is IArrayTypeSymbol array ? array.ElementType : type.GetEnumerableElementType();
        return (elementType is null) || !elementType.IsReferenceType || (elementType.NullableAnnotation != NullableAnnotation.NotAnnotated);
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeName);

    private static ITypeSymbol? GetMemberType(ISymbol? member) => member switch
    {
        IPropertySymbol property => property.Type,
        IFieldSymbol field => field.Type,
        IParameterSymbol parameter => parameter.Type,
        _ => null
    };

    // The members [MapConstant], [MapExpression], [MapUsing] and [MapFrom] assign have to exist and be
    // assignable from the mapper class: a property with a setter it can call, or a field that is not readonly,
    // reached along a dotted path the way ValidateTargetPath checks. One that is not found (a misspelled name,
    // a static member) or cannot be assigned (get-only, a setter it cannot call, a readonly field) is reported
    // instead of failing in the generated code (CS1061 / CS0200 / CS0272 / CS0191 / CS0176 / CS8852).
    internal static DiagnosticInfo? ValidateAssignedTargets(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        // A member the constructor assigns from the value of the attribute is not assigned by the mapper
        var targets = model.ConstantMappings.Where(static c => !c.IsConstructorArgument).Select(static c => (Target: c.TargetName, c.AttributeIndex))
            .Concat(model.ExpressionMappings.Where(static e => !e.IsConstructorArgument).Select(static e => (Target: e.TargetName, e.AttributeIndex)))
            .Concat(model.MapUsingMappings.Where(static u => !u.IsConstructorArgument).Select(static u => (Target: u.TargetName, u.AttributeIndex)))
            .Concat(model.MapFromMappings.Where(static f => !f.IsConstructorArgument).Select(static f => (Target: f.TargetName, f.AttributeIndex)));
        foreach (var (target, attributeIndex) in targets)
        {
            if (target.Contains('.'))
            {
                var pathError = ValidateTargetPath(model, destinationType, target, attributeIndex, within, compilation, publicPropertiesOnly: false, syntax);
                if (pathError is not null)
                {
                    return pathError;
                }

                continue;
            }

            if (!(FindTargetMember(destinationType, target, within, compilation) is { } member && IsAssignableInInitializer(member, destinationType, within, compilation)))
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapPropertyTargetProperty,
                    LocationOf(model, attributeIndex, syntax),
                    model.MethodName,
                    target);
            }
        }

        return null;
    }

    // How a feature mapping reaches its target, as the overload below does, or for a value that goes to a
    // constructor argument, the parameter, whose type the value is converted to.
    private static (ISymbol Member, ITypeSymbol Type, bool IsInitOnly, bool IsRequired, EquatableArray<NestedPathSegment> Segments)? ResolveFeatureTarget(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        string target,
        bool isConstructorArgument,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        if (!isConstructorArgument)
        {
            return ResolveFeatureTarget(destinationType, target, within, compilation);
        }

        return FindArgumentParameter(GetEffectiveConstructor(model, destinationType), model.ConstructorParameters, target) is { } parameter
            ? (parameter, parameter.Type, false, false, default)
            : null;
    }

    // The parameter of the constructor that takes the value of the target, as ConstructorParameters binds them.
    private static IParameterSymbol? FindArgumentParameter(
        IMethodSymbol? constructor,
        EquatableArray<(string ParamName, string TargetPath, ConstructorArgumentKind Kind, bool IsNamed)> arguments,
        string target)
    {
        if (constructor is null)
        {
            return null;
        }

        foreach (var (paramName, targetPath, _, _) in arguments)
        {
            if (targetPath == target)
            {
                return constructor.Parameters.FirstOrDefault(p => p.Name == paramName);
            }
        }

        return null;
    }

    // How a feature mapping reaches its target: the member at the end, its type, whether it is assigned in the
    // object initializer (an init-only or required member, or a dotted path only the initializer reaches), and
    // the intermediate members of a dotted path. Null when it is not found (ValidateAssignedTargets reports
    // it).
    private static (ISymbol Member, ITypeSymbol Type, bool IsInitOnly, bool IsRequired, EquatableArray<NestedPathSegment> Segments)? ResolveFeatureTarget(
        ITypeSymbol destinationType,
        string target,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        var members = ResolveTargetMembers(destinationType, target, within, compilation, publicPropertiesOnly: false);
        if (members is null)
        {
            return null;
        }

        var member = members[members.Count - 1];
        var type = GetMemberType(member)!;
        if (members.Count > 1)
        {
            var (route, segments) = AnalyzeTargetPath(destinationType, members, within, compilation);
            return (member, type, route == TargetRoute.Initializer, false, segments);
        }

        return member switch
        {
            IPropertySymbol property => (member, type, property.GetSetter()?.IsInitOnly == true, property.IsRequired, default),
            IFieldSymbol field => (member, type, false, field.IsRequired, default),
            _ => null
        };
    }

    // The member the generated code assigns as __d.<path> for [MapConstant], [MapExpression] and [MapUsing].
    private static ISymbol? FindTargetMember(ITypeSymbol destinationType, string path, INamedTypeSymbol within, Compilation compilation) =>
        ResolveTargetMembers(destinationType, path, within, compilation, publicPropertiesOnly: false)?.LastOrDefault();

    // The members along a target path the generated code assigns as __d.<path>, null when one is not found. The
    // path of [MapProperty] goes through the public properties, as the automatic mapping matches them. The
    // target of [MapConstant], [MapExpression] and [MapUsing] is bound the way the compiler binds each name:
    // the first member of that name the mapper class can access up the base types (or through the base
    // interfaces of an interface), which has to be an instance property or field.
    private static List<ISymbol>? ResolveTargetMembers(
        ITypeSymbol destinationType,
        string path,
        INamedTypeSymbol within,
        Compilation compilation,
        bool publicPropertiesOnly)
    {
        var members = new List<ISymbol>();
        var type = destinationType;
        foreach (var name in path.Split('.'))
        {
            var member = publicPropertiesOnly
                ? PropertyPathHelper.ResolveProperty(type, name, StringComparison.Ordinal, within, compilation)
                : FindInstanceMember(type, name, within, compilation);
            var memberType = GetMemberType(member);
            if (memberType is null)
            {
                return null;
            }

            members.Add(member!);
            type = memberType;
        }

        return members;
    }

    // How the generated code assigns a target: by a statement after construction, only in the object
    // initializer of a return mapper, or not at all.
    internal enum TargetRoute
    {
        Unassignable,
        Statement,
        Initializer
    }

    // Analyzes the members along a dotted target path: how the generated code reaches through each
    // intermediate one (TargetSegmentAccess), and how it assigns the one at the end. A statement after
    // construction reaches every intermediate member and assigns the one at the end. Failing that, an object
    // initializer can create the intermediate members with the member at the end in them (Child = new Inner()
    // { Value = ... }), when each of them can be assigned there and created, and the member at the end can be
    // assigned there: an init-only one, or one reached through a struct property only the initializer
    // assigns. A nested object initializer (Child = { Value = ... }) is not used, as an init accessor cannot
    // be called there (CS8852) and a null member would throw.
    private static (TargetRoute Route, EquatableArray<NestedPathSegment> Segments) AnalyzeTargetPath(
        ITypeSymbol destinationType,
        List<ISymbol> members,
        INamedTypeSymbol within,
        Compilation compilation)
    {
        var segments = new NestedPathSegment[members.Count - 1];
        var reachable = true;
        var initializable = true;
        var path = String.Empty;
        var receiverType = destinationType;
        for (var i = 0; i < segments.Length; i++)
        {
            var member = members[i];
            var type = GetMemberType(member)!;
            path = i == 0 ? member.Name : path + "." + member.Name;

            var access = GetSegmentAccess(member, receiverType, within, compilation);
            reachable &= access is not null;
            initializable &= IsAssignableInInitializer(member, receiverType, within, compilation) && CanCreateInstance(type, within, compilation);
            segments[i] = new NestedPathSegment(
                path,
                GetCreatedTypeName(type),
                false,
                access ?? TargetSegmentAccess.Existing);
            receiverType = type;
        }

        var target = members[members.Count - 1];
        var route = reachable && IsAssignableAfterConstruction(target, receiverType, within, compilation)
            ? TargetRoute.Statement
            : initializable && IsAssignableInInitializer(target, receiverType, within, compilation)
                ? TargetRoute.Initializer
                : TargetRoute.Unassignable;
        return (route, new EquatableArray<NestedPathSegment>(segments));
    }

    // How the generated code reaches through an intermediate member of a target path, or null when it cannot:
    // a property whose getter it cannot call, or a struct it cannot write back, a property without a setter
    // it can call after construction (get-only, init-only, or a readonly struct's) or a readonly field. A
    // class the mapper can assign and create is created when null; one it cannot assign or create is written
    // into when it holds an instance. A struct property is copied, written into and assigned back, and a
    // struct field is a variable written into directly.
    private static TargetSegmentAccess? GetSegmentAccess(ISymbol member, ITypeSymbol receiverType, INamedTypeSymbol within, Compilation compilation)
    {
        if ((member is IPropertySymbol property) &&
            ((property.GetGetter() is not { } getter) || !compilation.IsSymbolAccessibleWithin(getter, within, receiverType) ||
             (property.GetReadObsoleteKind() == ObsoleteKind.Error)))
        {
            return null;
        }

        if ((member is IFieldSymbol field) && (field.GetObsoleteKind() == ObsoleteKind.Error))
        {
            return null;
        }

        var type = GetMemberType(member)!;
        var assignable = IsAssignableAfterConstruction(member, receiverType, within, compilation);
        if (type.IsValueType)
        {
            return !assignable ? null : member is IFieldSymbol ? TargetSegmentAccess.Direct : TargetSegmentAccess.Copy;
        }

        return assignable && CanCreateInstance(type, within, compilation) ? TargetSegmentAccess.Create : TargetSegmentAccess.Existing;
    }

    // Whether a statement after construction can assign the member: a setter that is not init-only and that
    // the mapper class can call, or a field that is not readonly.
    private static bool IsAssignableAfterConstruction(ISymbol member, ITypeSymbol receiverType, INamedTypeSymbol within, Compilation compilation) => member switch
    {
        IPropertySymbol property => HasAssignableSetter(property, receiverType, within, compilation),
        IFieldSymbol field => IsAssignableField(field),
        _ => false
    };

    // Whether an object initializer can assign the member: a setter or an init accessor the mapper class can
    // call, or a field that is not readonly.
    private static bool IsAssignableInInitializer(ISymbol member, ITypeSymbol receiverType, INamedTypeSymbol within, Compilation compilation) => member switch
    {
        IPropertySymbol property => CanAssignFromMapper(property, receiverType, within, compilation),
        IFieldSymbol field => IsAssignableField(field),
        _ => false
    };

    // A field that is not readonly nor a constant, and not obsolete as an error (CS0619)
    private static bool IsAssignableField(IFieldSymbol field) =>
        !field.IsReadOnly && !field.IsConst && (field.GetObsoleteKind() != ObsoleteKind.Error);

    // A dotted target path the generated code cannot assign: one it cannot reach or assign at all (SMP0102), or
    // one only an object initializer reaches, in a void mapper, which never constructs (SMP0302).
    private static DiagnosticInfo? ValidateTargetPath(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        string path,
        int attributeIndex,
        INamedTypeSymbol within,
        Compilation compilation,
        bool publicPropertiesOnly,
        MethodDeclarationSyntax syntax)
    {
        var members = ResolveTargetMembers(destinationType, path, within, compilation, publicPropertiesOnly);
        var route = members is null ? TargetRoute.Unassignable : AnalyzeTargetPath(destinationType, members, within, compilation).Route;
        if (route == TargetRoute.Unassignable)
        {
            return new DiagnosticInfo(GetUnassignableTargetDescriptor(destinationType, path, within, compilation), LocationOf(model, attributeIndex, syntax), model.MethodName, path);
        }

        if ((route == TargetRoute.Initializer) && !model.ReturnsDestination)
        {
            return new DiagnosticInfo(Diagnostics.InitOnlyDestinationRequiresReturnMapper, LocationOf(model, attributeIndex, syntax), model.MethodName, model.DestinationTypeName);
        }

        return null;
    }

    // A target that is not found or cannot be assigned (SMP0102), told apart when it is a dotted path going through a
    // nullable struct (Location.Lat for a GeoPoint? Location): the path would write into the struct it holds, a copy
    // read through Value that no setter takes back, so a dotted target does not go through one, as a dotted source does.
    private static DiagnosticDescriptor GetUnassignableTargetDescriptor(ITypeSymbol destinationType, string path, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var type = destinationType;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (GetMemberType(FindInstanceMember(type, parts[i], within, compilation)) is not { } memberType)
            {
                break;
            }

            if (memberType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
            {
                return Diagnostics.NullableStructTargetPath;
            }

            type = memberType;
        }

        return Diagnostics.UnresolvedMapPropertyTargetProperty;
    }

    // The member x.Name binds to (PropertyPathHelper.ResolveMember), when it is an instance property or field.
    private static ISymbol? FindInstanceMember(ITypeSymbol type, string name, INamedTypeSymbol within, Compilation compilation) =>
        PropertyPathHelper.ResolveMember(type, name, within, compilation) is { IsStatic: false } member &&
        (member is IPropertySymbol { IsIndexer: false } or IFieldSymbol)
            ? member
            : null;

    // The first property or field the mapper class can access whose name matches under the comparison, up the
    // base types, as a property is looked up with it (the first match wins when several do).
    private static ISymbol? FindInstanceMemberIgnoringSpelling(ITypeSymbol type, string name, StringComparison comparison, INamedTypeSymbol within, Compilation compilation)
    {
        foreach (var candidateType in PropertyPathHelper.GetLookupTypes(type))
        {
            foreach (var member in candidateType.GetMembers())
            {
                if ((member is IPropertySymbol { IsIndexer: false } or IFieldSymbol) && !member.IsStatic &&
                    String.Equals(member.Name, name, comparison) && compilation.IsSymbolAccessibleWithin(member, within, type))
                {
                    return member;
                }
            }
        }

        return null;
    }

    // A target name written in an attribute as the declared names of the members it resolves to, a dotted
    // path segment by segment, or null when one is not found. Each segment is matched exactly first, as a
    // public property and then as a property or field the mapper class can access, and only then under the
    // comparison, in the same order, so that the default (Ordinal) behaves as an exact match.
    private static string? ResolveCanonicalTargetPath(ITypeSymbol destinationType, string path, StringComparison comparison, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var type = destinationType;
        for (var i = 0; i < parts.Length; i++)
        {
            var member = (ISymbol?)PropertyPathHelper.ResolveProperty(type, parts[i], StringComparison.Ordinal, within, compilation) ??
                         FindInstanceMember(type, parts[i], within, compilation);
            if ((member is null) && (comparison != StringComparison.Ordinal))
            {
                member = (ISymbol?)PropertyPathHelper.ResolveProperty(type, parts[i], comparison, within, compilation) ??
                         FindInstanceMemberIgnoringSpelling(type, parts[i], comparison, within, compilation);
            }

            var memberType = GetMemberType(member);
            if (memberType is null)
            {
                return null;
            }

            parts[i] = member!.Name;
            type = memberType;
        }

        return String.Join(".", parts);
    }

    // Whether a statement after construction can assign the property of an instance of the receiver type: a
    // setter that is not init-only and that the mapper class can call on that instance, not obsolete as an error.
    private static bool HasAssignableSetter(IPropertySymbol property, ITypeSymbol receiverType, INamedTypeSymbol within, Compilation compilation) =>
        (property.GetSetter() is { IsInitOnly: false } setter) && compilation.IsSymbolAccessibleWithin(setter, within, receiverType) &&
        (property.GetWriteObsoleteKind() != ObsoleteKind.Error);

    // The collection the loop builds for a [MapCollection] target of the shape, without a collection
    // converter.
    private static ITypeSymbol? GetCreatedCollectionType(CollectionTargetShape shape, ITypeSymbol targetType, ITypeSymbol elementType, Compilation compilation)
    {
        return shape switch
        {
            CollectionTargetShape.Custom => targetType,
            CollectionTargetShape.Dictionary => compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")?.Construct([.. ((INamedTypeSymbol)targetType).TypeArguments]),
            CollectionTargetShape.Array => compilation.CreateArrayTypeSymbol(elementType),
            CollectionTargetShape.ImmutableArray => ConstructType(compilation, "System.Collections.Immutable.ImmutableArray`1", elementType),
            CollectionTargetShape.ImmutableList => ConstructType(compilation, "System.Collections.Immutable.ImmutableList`1", elementType),
            CollectionTargetShape.HashSet => ConstructType(compilation, "System.Collections.Generic.HashSet`1", elementType),
            CollectionTargetShape.ImmutableHashSet => ConstructType(compilation, "System.Collections.Immutable.ImmutableHashSet`1", elementType),
            CollectionTargetShape.FrozenSet => ConstructType(compilation, "System.Collections.Frozen.FrozenSet`1", elementType),
            _ => ConstructType(compilation, "System.Collections.Generic.List`1", elementType)
        };
    }

    private static ITypeSymbol? ConstructType(Compilation compilation, string metadataName, ITypeSymbol typeArgument) =>
        compilation.GetTypeByMetadataName(metadataName)?.Construct(typeArgument);

    // The instance InPlace creates when the target it can assign is null: one of the target's own type
    // when that is a class the mapper can create (List<T>, HashSet<T>, ObservableCollection<T>, ...), or a
    // List<T> for an interface (HashSet<T> for a set, and Dictionary<TKey, TValue> for IDictionary<TKey, TValue>,
    // as the loop builds for one without InPlace).
    private static ITypeSymbol? GetInPlaceFallbackType(ITypeSymbol targetType, ITypeSymbol elementType, INamedTypeSymbol within, Compilation compilation)
    {
        if ((targetType.TypeKind != TypeKind.Interface) && CanCreateInstance(targetType, within, compilation))
        {
            return targetType;
        }

        if (DetermineTargetShape(targetType) == CollectionTargetShape.Dictionary)
        {
            return GetCreatedCollectionType(CollectionTargetShape.Dictionary, targetType, elementType, compilation);
        }

        var isSet = DetermineInPlaceFallbackTypeName(targetType, elementType)
            .StartsWith("global::System.Collections.Generic.HashSet<", StringComparison.Ordinal);
        return ConstructType(compilation, isSet ? "System.Collections.Generic.HashSet`1" : "System.Collections.Generic.List`1", elementType);
    }

    // Whether InPlace can clear and refill a target of the declared type through ICollection<T>: the type
    // implements it, and is not a collection that is read-only by design (an array, an immutable or frozen
    // collection, ReadOnlyCollection<T>), whose Clear would throw.
    private static bool IsRefillableCollection(ITypeSymbol type, ITypeSymbol elementType) =>
        (type is not IArrayTypeSymbol) && !IsReadOnlyCollectionType(type) && ImplementsCollectionOf(type, elementType);

    // Whether the loop can build a target of a collection class of its own: a class it can create that
    // takes the elements through ICollection<T> and is not read-only by design.
    private static bool IsCreatableCollectionClass(ITypeSymbol type, ITypeSymbol elementType, INamedTypeSymbol within, Compilation compilation) =>
        (type.TypeKind == TypeKind.Class) &&
        !IsReadOnlyCollectionType(type) &&
        ImplementsCollectionOf(type, elementType) &&
        CanCreateInstance(type, within, compilation);

    private static bool ImplementsCollectionOf(ITypeSymbol type, ITypeSymbol elementType)
    {
        return IsCollectionOf(type) || type.AllInterfaces.Any(IsCollectionOf);

        bool IsCollectionOf(ITypeSymbol candidate) =>
            (candidate is INamedTypeSymbol { IsGenericType: true } named) &&
            (named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_ICollection_T) &&
            SymbolEqualityComparer.Default.Equals(named.TypeArguments[0], elementType);
    }

    private static bool IsReadOnlyCollectionType(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var ns = current.ContainingNamespace?.ToDisplayString();
            var name = (current as INamedTypeSymbol)?.ConstructedFrom.ToDisplayString();
            if ((ns is "System.Collections.Immutable" or "System.Collections.Frozen") ||
                (name is "System.Collections.ObjectModel.ReadOnlyCollection<T>" or
                         "System.Collections.ObjectModel.ReadOnlyObservableCollection<T>" or
                         "System.Collections.ObjectModel.ReadOnlySet<T>"))
            {
                return true;
            }
        }

        return false;
    }

    // Whether a method converts to the delegate type: a method group does so only when the method takes
    // each parameter the way the delegate's Invoke does and returns likewise.
    private static bool IsDelegateFor(ITypeSymbol type, IMethodSymbol method) =>
        (type is INamedTypeSymbol { DelegateInvokeMethod: { } invoke }) &&
        (invoke.ReturnsVoid == method.ReturnsVoid) &&
        invoke.Parameters.Select(static p => p.RefKind).SequenceEqual(method.Parameters.Select(static p => p.RefKind));

    internal static string DetermineCollectionMethod(ITypeSymbol targetType)
    {
        if (targetType is IArrayTypeSymbol)
        {
            return "ToArray";
        }

        if (targetType is INamedTypeSymbol named)
        {
            var fullName = named.ConstructedFrom.ToDisplayString();
            return fullName switch
            {
                "System.Collections.Immutable.ImmutableArray<T>" => "ToImmutableArray",
                "System.Collections.Immutable.IImmutableList<T>" => "ToImmutableList",
                "System.Collections.Immutable.ImmutableList<T>" => "ToImmutableList",
                "System.Collections.Immutable.IImmutableSet<T>" => "ToImmutableHashSet",
                "System.Collections.Immutable.ImmutableHashSet<T>" => "ToImmutableHashSet",
                "System.Collections.Frozen.FrozenSet<T>" => "ToFrozenSet",
                "System.Collections.Generic.HashSet<T>" => "ToHashSet",
                "System.Collections.Generic.ISet<T>" => "ToHashSet",
                "System.Collections.Generic.IReadOnlySet<T>" => "ToHashSet",
                _ => "ToList"
            };
        }

        return "ToList";
    }

    internal static string DetermineInPlaceFallbackTypeName(ITypeSymbol targetType, ITypeSymbol elementType)
    {
        var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (targetType is INamedTypeSymbol named)
        {
            var fullName = named.ConstructedFrom.ToDisplayString();
            return fullName switch
            {
                "System.Collections.Generic.ICollection<T>" or
                "System.Collections.Generic.IList<T>" or
                "System.Collections.Generic.List<T>" => $"global::System.Collections.Generic.List<{elementTypeName}>",
                "System.Collections.Generic.HashSet<T>" or
                "System.Collections.Generic.ISet<T>" => $"global::System.Collections.Generic.HashSet<{elementTypeName}>",
                _ => $"global::System.Collections.Generic.List<{elementTypeName}>"
            };
        }

        return $"global::System.Collections.Generic.List<{elementTypeName}>";
    }

    internal static CollectionSourceShape DetermineSourceShape(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return CollectionSourceShape.Array;
        }

        if ((type is INamedTypeSymbol named) && named.IsGenericType)
        {
            var fullName = named.ConstructedFrom.ToDisplayString();
            switch (fullName)
            {
                case "System.Collections.Generic.List<T>":
                    return CollectionSourceShape.List;
                case "System.Collections.Immutable.ImmutableArray<T>":
                    return CollectionSourceShape.ImmutableArray;
                case "System.ReadOnlyMemory<T>":
                    return CollectionSourceShape.ReadOnlyMemory;
                case "System.Memory<T>":
                    return CollectionSourceShape.Memory;
                case "System.Collections.Generic.IList<T>":
                case "System.Collections.Generic.IReadOnlyList<T>":
                    // Indexer iteration: one interface call per element and no enumerator allocation,
                    // versus two calls per element plus an enumerator through IEnumerable<T>.
                    return CollectionSourceShape.IndexedList;
                case "System.Collections.Generic.IReadOnlyCollection<T>":
                case "System.Collections.Generic.ICollection<T>":
                    // An interface type does not appear in its own AllInterfaces, so properties
                    // declared as these interfaces need an explicit match to get Count-based presizing.
                    return CollectionSourceShape.ReadOnlyCollection;
            }
        }

        // Other types, including collection classes of their own such as class ItemList : List<Item>,
        // are presized from their count when they implement ICollection<T> / IReadOnlyCollection<T>
        foreach (var iface in type.AllInterfaces)
        {
            var ifaceName = iface.ConstructedFrom.ToDisplayString();
            if (ifaceName is "System.Collections.Generic.IReadOnlyCollection<T>"
                          or "System.Collections.Generic.ICollection<T>")
            {
                return CollectionSourceShape.ReadOnlyCollection;
            }
        }

        return CollectionSourceShape.Enumerable;
    }

    internal static CollectionTargetShape DetermineTargetShape(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
        {
            return CollectionTargetShape.Array;
        }

        if ((type is INamedTypeSymbol named) && named.IsGenericType)
        {
            var fullName = named.ConstructedFrom.ToDisplayString();
            return fullName switch
            {
                "System.Collections.Immutable.ImmutableArray<T>" => CollectionTargetShape.ImmutableArray,
                "System.Collections.Immutable.ImmutableList<T>" or
                "System.Collections.Immutable.IImmutableList<T>" => CollectionTargetShape.ImmutableList,
                "System.Collections.Generic.HashSet<T>" or
                "System.Collections.Generic.ISet<T>" or
                "System.Collections.Generic.IReadOnlySet<T>" => CollectionTargetShape.HashSet,
                "System.Collections.Immutable.ImmutableHashSet<T>" or
                "System.Collections.Immutable.IImmutableSet<T>" => CollectionTargetShape.ImmutableHashSet,
                "System.Collections.Frozen.FrozenSet<T>" => CollectionTargetShape.FrozenSet,
                "System.Collections.Generic.IDictionary<TKey, TValue>" or
                "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>" => CollectionTargetShape.Dictionary,
                _ => CollectionTargetShape.List
            };
        }

        return CollectionTargetShape.List;
    }

    // Reported at the mapper method, as the errors about its destination are
    internal static List<DiagnosticInfo> CollectStrictModeWarnings(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        var warnings = new List<DiagnosticInfo>();
        var mappedTargets = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pm in model.PropertyMappings)
        {
            mappedTargets.Add(pm.TargetPath);
        }

        foreach (var name in model.IgnoredProperties)
        {
            mappedTargets.Add(name);
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

        // A member a dotted target path writes into (Child for Child.Value) is mapped through the path, which takes
        // the place of its automatic mapping
        foreach (var target in mappedTargets.Where(static t => t.Contains('.')).ToList())
        {
            mappedTargets.Add(target.Substring(0, target.IndexOf('.')));
        }

        // The members the constructor called sets from the arguments passed, which a parameter's own mapping
        // (named after it) may supply
        var nameComparison = (StringComparison)model.NameComparison;
        var constructor = GetEffectiveConstructor(model, destinationType);
        var setByConstructor = constructor is null
            ? []
            : model.ConstructorParameters
                .Select(a => constructor.Parameters.First(p => p.Name == a.ParamName))
                .ToList();
        var candidates = model.ReturnsDestination ? GetConstructorCandidates(destinationType, within, compilation) : [];

        foreach (var destProp in PropertyPathHelper.GetProperties(destinationType, within, compilation))
        {
            // The automatic mapping leaves an obsolete property out, as it is to be left alone
            if (setByConstructor.Any(p => MatchesConstructorParameter(p, destProp.Name, nameComparison)) ||
                (destProp.GetWriteObsoleteKind() != ObsoleteKind.None))
            {
                continue;
            }

            // A property the mapper cannot assign (get-only, or a setter it cannot call) is mapped only through a
            // constructor: it is left out unless a constructor a return mapper can call takes it, which the
            // construction chosen did not use for it. A void mapper assigns the instance it is given, so an
            // init-only property is out of its reach as well
            var assignable = model.ReturnsDestination
                ? CanAssignFromMapper(destProp, destinationType, within, compilation)
                : HasAssignableSetter(destProp, destinationType, within, compilation);
            if (!assignable && !candidates.Any(c => IsConstructorParameterTarget(c, destProp.Name, nameComparison)))
            {
                continue;
            }

            if (!mappedTargets.Contains(destProp.Name))
            {
                warnings.Add(new DiagnosticInfo(Diagnostics.UnmappedDestinationProperty, syntax.GetLocation(), model.MethodName, destProp.Name));
            }
        }

        return warnings;
    }

    // Strict mode: the mappings giving a value that may be null to a target that does not take null, which gets null or
    // default for it without the mapping saying what it is to get (SMP0502). A value may be null as declared: a source
    // member of a nullable type, and one read through a member of one where the value is made as an expression (a
    // constructor argument, an object initializer entry), a statement leaving the target as it is then; a nullable
    // reference the method of [MapFrom] or [MapUsing], a converter or the mapper of [MapNested] / [MapCollection]
    // returns, which the generated code takes with !; and a source of [MapNested] / [MapCollection] or an element of a
    // nullable type a mapper not taking null is not called for. A mapping with NullValue, NullBehavior.Skip or a
    // [MapCondition] says what the target gets. A reference declared with nullable annotations disabled says nothing
    // about null, so it is not taken as one here, as a model written without them would warn everywhere. Reported at
    // the attribute of the mapping, or at the method for the automatic mapping. A return mapper whose source is
    // declared nullable returns default for a null source, which a return type not taking null gets as well (the
    // target (return), reported at the method).
    private static List<DiagnosticInfo> CollectNullableValueWarnings(
        MapperMethodModel model,
        IMethodSymbol mapperMethod,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        INamedTypeSymbol within,
        Compilation compilation,
        MethodDeclarationSyntax syntax)
    {
        var warnings = new List<DiagnosticInfo>();

        void Report(int attributeIndex, string target) =>
            warnings.Add(new DiagnosticInfo(Diagnostics.NullableValueToNonNullableTarget, LocationOf(model, attributeIndex, syntax), model.MethodName, target));

        if (model.ReturnsDestination && IsNullableReference(sourceType) && RejectsNullReturn(mapperMethod))
        {
            Report(-1, "(return)");
        }

        // The target does not take null: a struct, or a reference annotated as not null without [AllowNull], or one with
        // [DisallowNull]
        bool Rejects(string target, bool isConstructorArgument) =>
            ResolveFeatureTarget(model, destinationType, target, isConstructorArgument, within, compilation) is { } resolved &&
            RejectsNullValue(resolved.Member, resolved.Type);

        // Whether a source member path is of a nullable type as declared, at its end and before it
        (bool Value, bool Intermediate) GetDeclaredNullability(string path)
        {
            var parts = path.Split('.');
            var type = sourceType;
            var intermediate = false;
            for (var i = 0; i < parts.Length; i++)
            {
                if (PropertyPathHelper.ResolveProperty(type, parts[i], StringComparison.Ordinal, within, compilation, readable: true) is not { } property)
                {
                    break;
                }

                if (i == parts.Length - 1)
                {
                    return (property.Type.IsNullableType() || ReturnsMaybeNull(property), intermediate);
                }

                intermediate |= property.Type.IsNullableType() || ReturnsMaybeNull(property);
                type = property.Type;
            }

            return (false, intermediate);
        }

        bool IsInitializerEntry(bool isTargetInitOnly, bool isTargetRequired) =>
            model.UseConstructorMapping && (isTargetInitOnly || isTargetRequired);

        foreach (var mapping in model.PropertyMappings)
        {
            if (mapping.HasNullValue() || (mapping.NullBehavior == NullBehaviorType.Skip) || mapping.HasCondition() ||
                !Rejects(mapping.TargetPath, mapping.IsConstructorParameter))
            {
                continue;
            }

            var (value, intermediate) = GetDeclaredNullability(mapping.SourcePath);
            var isExpression = mapping.IsConstructorParameter || IsInitializerEntry(mapping.IsTargetInitOnly, mapping.IsTargetRequired);
            var mayBeNull = mapping.HasConverter()
                ? mapping.ConverterForgivesNull || (value && mapping.ConverterRejectsNull && isExpression)
                : value;
            if (mayBeNull || (intermediate && isExpression))
            {
                Report(mapping.AttributeIndex, mapping.TargetPath);
            }
        }

        foreach (var mapUsing in model.MapUsingMappings)
        {
            if (mapUsing.ForgivesNull && Rejects(mapUsing.TargetName, mapUsing.IsConstructorArgument))
            {
                Report(mapUsing.AttributeIndex, mapUsing.TargetName);
            }
        }

        foreach (var mapFrom in model.MapFromMappings)
        {
            var isExpression = mapFrom.IsConstructorArgument || IsInitializerEntry(mapFrom.IsTargetInitOnly, mapFrom.IsTargetRequired);
            if ((mapFrom.ForgivesNull || (!mapFrom.IsMethodCall && isExpression && GetDeclaredNullability(mapFrom.Member).Intermediate)) &&
                Rejects(mapFrom.TargetName, mapFrom.IsConstructorArgument))
            {
                Report(mapFrom.AttributeIndex, mapFrom.TargetName);
            }
        }

        foreach (var mapNested in model.MapNestedMappings)
        {
            if ((mapNested.ForgivesMapperResult || (!mapNested.MapperTakesNull && GetDeclaredNullability(mapNested.SourceName).Value)) &&
                Rejects(mapNested.TargetName, mapNested.IsConstructorArgument))
            {
                Report(mapNested.AttributeIndex, mapNested.TargetName);
            }
        }

        foreach (var mapCollection in model.MapCollectionMappings)
        {
            if (ResolveFeatureTarget(model, destinationType, mapCollection.TargetName, mapCollection.IsConstructorArgument, within, compilation) is not { } target ||
                (PropertyPathHelper.ResolveProperty(sourceType, mapCollection.SourceName, StringComparison.Ordinal, within, compilation, readable: true) is not { } sourceProperty))
            {
                continue;
            }

            // The collection, which InPlace leaves as it is for a null source, and its elements
            var nullCollection = !mapCollection.InPlace && (sourceProperty.Type.IsNullableType() || ReturnsMaybeNull(sourceProperty)) &&
                                 RejectsNullValue(target.Member, target.Type);
            var targetElementType = target.Type.GetEnumerableElementType();
            var elementRejectsNull = (targetElementType is not null) && (targetElementType.IsValueType
                ? !targetElementType.IsNullableType()
                : targetElementType.IsReferenceType && (targetElementType.NullableAnnotation == NullableAnnotation.NotAnnotated));
            var nullElement = elementRejectsNull &&
                              (mapCollection.ForgivesMapperResult ||
                               (mapCollection.UnwrapsSource && (sourceProperty.Type.GetCollectionOrMemoryElementType()?.IsNullableType() == true)));
            if (nullCollection || nullElement)
            {
                Report(mapCollection.AttributeIndex, mapCollection.TargetName);
            }
        }

        return warnings;
    }

    // The return value of a method does not take null: a struct, or a reference annotated as not null without
    // [return: MaybeNull]. A type parameter, which may be either, does not count.
    private static bool RejectsNullReturn(IMethodSymbol method) =>
        method.ReturnType.IsValueType
            ? !method.ReturnType.IsNullableType()
            : method.ReturnType.IsReferenceType &&
              (method.ReturnType.NullableAnnotation == NullableAnnotation.NotAnnotated) &&
              !method.GetReturnTypeAttributes().Any(static a => a.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.MaybeNullAttribute");

    // A member or a parameter that does not take null: a struct, or a reference annotated as not null without
    // [AllowNull], or one with [DisallowNull]. A type parameter, which may be either, does not count.
    private static bool RejectsNullValue(ISymbol member, ITypeSymbol type) =>
        type.IsValueType ? !type.IsNullableType() : type.IsReferenceType && !TakesNull(member, type);

    // Strict mode: the members of a source enum a mapping to another enum by name finds no member of the same name for
    // in the target enum, whose values give the target default, or null for a nullable one (SMP0503). The values of a
    // [Flags] enum combining members are not known before they come, so only the members are looked at. A converter
    // given to the mapping takes over the conversion. Reported at the attribute of the mapping, or at the method for
    // the automatic mapping.
    private static List<DiagnosticInfo> CollectUnmatchedEnumMemberWarnings(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var warnings = new List<DiagnosticInfo>();
        foreach (var mapping in model.PropertyMappings.Where(static m => (m.EnumMappingKind == EnumMappingKind.EnumToEnum) && !m.HasConverter()))
        {
            var unmatched = mapping.SourceEnumMembers
                .Where(member => !mapping.DestEnumMembers.Contains(member, StringComparer.Ordinal))
                .ToList();
            if (unmatched.Count > 0)
            {
                warnings.Add(new DiagnosticInfo(
                    Diagnostics.UnmatchedEnumMember,
                    LocationOf(model, mapping.AttributeIndex, syntax),
                    model.MethodName,
                    mapping.TargetPath,
                    String.Join(", ", unmatched)));
            }
        }

        return warnings;
    }
}
