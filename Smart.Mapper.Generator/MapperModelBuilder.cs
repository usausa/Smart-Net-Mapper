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

internal static class MapperModelBuilder
{
    // FullyQualifiedFormat leaves out the ? of nullable reference types. A type the generated code has
    // to spell exactly as the member declares it, such as a local function's return type, keeps it.
    private static readonly SymbolDisplayFormat NullableQualifiedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    internal static Result<MapperMethodModel> BuildModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not IMethodSymbol symbol)
        {
            return Results.Errors<MapperMethodModel>();
        }

        if (!symbol.IsStatic || !symbol.IsPartialDefinition)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodDefinition, syntax.Identifier.GetLocation(), symbol.Name));
        }

        if (symbol.Parameters.Length < 1)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.Identifier.GetLocation(), symbol.Name));
        }

        var containingType = symbol.ContainingType;
        var ns = String.IsNullOrEmpty(containingType.ContainingNamespace.Name)
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();

        var sourceParam = symbol.Parameters[0];

        ITypeSymbol destinationType;
        int customParamStartIndex;
        string destinationTypeName;
        string? destinationParameterName;
        bool returnsDestination;

        if (symbol.ReturnsVoid)
        {
            if (symbol.Parameters.Length < 2)
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.Identifier.GetLocation(), symbol.Name));
            }

            var destParam = symbol.Parameters[1];
            destinationTypeName = destParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            destinationParameterName = destParam.Name;
            returnsDestination = false;
            destinationType = destParam.Type;
            customParamStartIndex = 2;
        }
        else
        {
            destinationTypeName = symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            destinationParameterName = null;
            returnsDestination = true;
            destinationType = symbol.ReturnType;
            customParamStartIndex = 1;
        }

        // Names starting with __ are reserved for the locals and local functions of the generated code
        // (__d, __src, __expression0, ...), which a parameter spelled that way could collide with.
        for (var i = 0; i < symbol.Parameters.Length; i++)
        {
            var parameterName = symbol.Parameters[i].Name;
            if (parameterName.StartsWith("__", StringComparison.Ordinal))
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                    Diagnostics.ReservedParameterName,
                    syntax.ParameterList.Parameters[i].Identifier.GetLocation(),
                    symbol.Name,
                    parameterName));
            }
        }

        // The generated code reads every parameter and assigns the destination members. An out parameter
        // allows neither, and the members of a struct destination passed by readonly reference cannot be
        // assigned.
        for (var i = 0; i < symbol.Parameters.Length; i++)
        {
            var parameter = symbol.Parameters[i];
            var isReadOnlyStructDestination = symbol.ReturnsVoid && (i == 1) && parameter.Type.IsValueType &&
                                              (parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter);
            if ((parameter.RefKind == RefKind.Out) || isReadOnlyStructDestination)
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                    Diagnostics.UnsupportedParameterModifier,
                    syntax.ParameterList.Parameters[i].GetLocation(),
                    symbol.Name,
                    parameter.Name,
                    GetRefKindKeyword(parameter.RefKind)));
            }
        }

        var customParameters = new List<CustomParameterModel>();
        for (var i = customParamStartIndex; i < symbol.Parameters.Length; i++)
        {
            var param = symbol.Parameters[i];
            customParameters.Add(new CustomParameterModel(
                param.Name,
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                DeclaredTypeName: param.Type.ToDisplayString(NullableQualifiedFormat),
                Modifiers: GetParameterModifiers(syntax, i),
                RefKind: param.RefKind));
        }

        var duplicateType = customParameters
            .GroupBy(p => p.TypeName)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateType is not null)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                Diagnostics.DuplicateCustomParameterType,
                syntax.GetLocation(),
                symbol.Name,
                duplicateType.Key));
        }

        var model = new MapperMethodModel(
            Namespace: ns,
            ClassName: containingType.GetClassName(),
            IsValueType: containingType.IsValueType,
            MethodAccessibility: symbol.DeclaredAccessibility,
            MethodName: symbol.Name,
            SourceTypeName: sourceParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            SourceParameterName: sourceParam.Name,
            SourceParameterModifiers: GetParameterModifiers(syntax, 0),
            SourceRefKind: sourceParam.RefKind,
            SourceDeclaredTypeName: sourceParam.Type.ToDisplayString(NullableQualifiedFormat),
            IsSourceParameterNullable: IsNullableReference(sourceParam.Type),
            SourceNonNullableTypeName: GetNonNullableTypeName(sourceParam.Type),
            IsExtensionMethod: symbol.IsExtensionMethod,
            DestinationTypeName: destinationTypeName,
            DestinationParameterName: destinationParameterName,
            DestinationParameterModifiers: returnsDestination ? string.Empty : GetParameterModifiers(syntax, 1),
            DestinationRefKind: returnsDestination ? RefKind.None : symbol.Parameters[1].RefKind,
            DestinationDeclaredTypeName: destinationType.ToDisplayString(NullableQualifiedFormat),
            IsDestinationParameterNullable: !returnsDestination && IsNullableReference(destinationType),
            DestinationNonNullableTypeName: GetNonNullableTypeName(destinationType),
            DefaultReturnValue: destinationType.IsValueType || IsNullableReference(destinationType) ? "default" : "default!",
            ReturnsDestination: returnsDestination,
            CustomParameters: new EquatableArray<CustomParameterModel>(customParameters));

        model = ParseMappingAttributes(symbol, model);

        // Runs before the duplicate check so that two attributes naming the same member with
        // different casing are recognised as the duplicate they are.
        model = CanonicalizeTargetNames(model, destinationType);

        var duplicateTargetError = ValidateDuplicateTargets(model, syntax);
        if (duplicateTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(duplicateTargetError);
        }

        model = ParseConverterAttributes(symbol, model);

        var validationError = ValidateCallbackMethods(symbol, ref model, syntax);
        if (validationError is not null)
        {
            return Results.Error<MapperMethodModel>(validationError);
        }

        var sourceType = symbol.Parameters[0].Type;

        var explicitMappingError = ValidateExplicitPropertyMappings(ref model, sourceType, destinationType, syntax);
        if (explicitMappingError is not null)
        {
            return Results.Error<MapperMethodModel>(explicitMappingError);
        }

        model = BuildPropertyMappings(sourceType, destinationType, model);

        // Runs before the detection passes below so that mappings synthesized for constructor
        // parameters are analysed alongside the ones built from destination properties.
        var constructorError = BuildConstructorParameterMappings(ref model, destinationType, sourceType, syntax);
        if (constructorError is not null)
        {
            return Results.Error<MapperMethodModel>(constructorError);
        }

        var expressionTargetError = ValidateExpressionAssignedTargets(model, syntax);
        if (expressionTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(expressionTargetError);
        }

        model = model with
        {
            PropertyMappings = AnalyzeConversions(
                model.PropertyMappings,
                symbol,
                sourceType,
                destinationType,
                GetEffectiveConstructor(model, destinationType),
                model.MapConverterTypeName,
                model.MapConverterMethodName)
        };

        var converterError = ValidateConverterMethods(symbol, ref model, syntax);
        if (converterError is not null)
        {
            return Results.Error<MapperMethodModel>(converterError);
        }

        var valueConverterError = ValidateValueConverterMethods(symbol, context.SemanticModel.Compilation, ref model, syntax);
        if (valueConverterError is not null)
        {
            return Results.Error<MapperMethodModel>(valueConverterError);
        }

        var propertyConditionError = ValidatePropertyConditionMethods(symbol, ref model, syntax);
        if (propertyConditionError is not null)
        {
            return Results.Error<MapperMethodModel>(propertyConditionError);
        }

        model = BuildConstantMappings(destinationType, model);

        var mapUsingError = ValidateAndBuildMapUsingMappings(symbol, ref model, sourceType, destinationType, syntax);
        if (mapUsingError is not null)
        {
            return Results.Error<MapperMethodModel>(mapUsingError);
        }

        var mapFromError = ValidateAndBuildMapFromMappings(ref model, sourceType, destinationType, syntax);
        if (mapFromError is not null)
        {
            return Results.Error<MapperMethodModel>(mapFromError);
        }

        var mapCollectionError = ValidateAndBuildMapCollectionMappings(symbol, context.SemanticModel.Compilation, ref model, sourceType, destinationType, syntax);
        if (mapCollectionError is not null)
        {
            return Results.Error<MapperMethodModel>(mapCollectionError);
        }

        var mapNestedError = ValidateAndBuildMapNestedMappings(symbol, context.SemanticModel.Compilation, ref model, sourceType, destinationType, syntax);
        if (mapNestedError is not null)
        {
            return Results.Error<MapperMethodModel>(mapNestedError);
        }

        var warnings = new List<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)>();
        if (model.Strict)
        {
            warnings.AddRange(CollectStrictModeWarnings(model, destinationType));
        }

        warnings.AddRange(CollectMapExpressionReflectionWarnings(model));

        model = model with { Warnings = new(warnings) };

        var voidInitOnlyError = ValidateVoidMapperInitOnlyTargets(model, syntax);
        if (voidInitOnlyError is not null)
        {
            return Results.Error<MapperMethodModel>(voidInitOnlyError);
        }

        var requiredMemberError = ValidateRequiredMembers(model, destinationType, syntax);
        if (requiredMemberError is not null)
        {
            return Results.Error<MapperMethodModel>(requiredMemberError);
        }

        var cultureFormatError = ValidateCultureAndFormat(model, syntax);
        if (cultureFormatError is not null)
        {
            return Results.Error<MapperMethodModel>(cultureFormatError);
        }

        var typeConverterError = ValidateNoTypeConverterFallback(ref model, sourceType, syntax);
        if (typeConverterError is not null)
        {
            return Results.Error<MapperMethodModel>(typeConverterError);
        }

        return Results.Success(model);
    }

    // The modifiers of a declared parameter other than this (in, ref readonly, ref, scoped, params), in
    // their declared order. The implementation of a partial method has to repeat them.
    private static string GetParameterModifiers(MethodDeclarationSyntax syntax, int index) =>
        String.Join(" ", syntax.ParameterList.Parameters[index].Modifiers
            .Where(static m => !m.IsKind(SyntaxKind.ThisKeyword))
            .Select(static m => m.Text));

    private static string GetRefKindKeyword(RefKind refKind) => refKind switch
    {
        RefKind.Ref => "ref",
        RefKind.Out => "out",
        RefKind.In => "in",
        RefKind.RefReadOnlyParameter => "ref readonly",
        _ => string.Empty
    };

    // A reference type declared with ?. Types declared with nullable annotations disabled are oblivious
    // and do not count, so the output for them stays as it was.
    private static bool IsNullableReference(ITypeSymbol type) =>
        type.IsReferenceType && (type.NullableAnnotation == NullableAnnotation.Annotated);

    // The declared type without the ? of a nullable reference type, keeping the annotations inside it.
    private static string GetNonNullableTypeName(ITypeSymbol type) =>
        (IsNullableReference(type) ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : type)
            .ToDisplayString(NullableQualifiedFormat);

    // What the generated code passes to a parameter of a method it calls ([MapUsing], converter,
    // condition, BeforeMap / AfterMap, element mapper): a value such as a property read, a variable it may
    // only read (an in or ref readonly parameter of the mapper, an element of a read-only span, a foreach
    // variable, the culture field), or a variable it may write (another parameter, the instance it
    // creates, an element of an array or span).
    internal enum ArgumentKind
    {
        Value,
        ReadOnlyVariable,
        WritableVariable
    }

    private static ArgumentKind GetVariableKind(RefKind refKind) =>
        refKind is RefKind.In or RefKind.RefReadOnlyParameter ? ArgumentKind.ReadOnlyVariable : ArgumentKind.WritableVariable;

    // Whether a parameter can take an argument of that kind. By value and in take anything (a value goes
    // as a copy); ref readonly needs a variable (a value warns, CS9193), ref a writable one (CS1620 /
    // CS8329), and out none, as the generated code has nothing to receive the result with.
    internal static bool CanTakeArgument(RefKind refKind, ArgumentKind argument) => refKind switch
    {
        RefKind.None or RefKind.In => true,
        RefKind.RefReadOnlyParameter => argument != ArgumentKind.Value,
        RefKind.Ref => argument == ArgumentKind.WritableVariable,
        _ => false
    };

    private static bool TakesArgument(IParameterSymbol parameter, string typeName, ArgumentKind argument) =>
        (parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == typeName) &&
        CanTakeArgument(parameter.RefKind, argument);

    // Among matching overloads, one taking every argument by value is used, as the plain call always
    // chose it; otherwise the first one.
    private static IMethodSymbol PreferByValue(IMethodSymbol? current, IMethodSymbol candidate) =>
        (current is null) || (!TakesAllByValue(current) && TakesAllByValue(candidate)) ? candidate : current;

    private static bool TakesAllByValue(IMethodSymbol method) =>
        method.Parameters.All(static p => p.RefKind == RefKind.None);

    // Whether a call with that many arguments binds to the method as far as their number goes: the
    // parameters after them are optional.
    private static bool TakesArgumentCount(IMethodSymbol method, int count) =>
        (method.Parameters.Length >= count) && method.Parameters.Skip(count).All(static p => p.IsOptional || p.IsParams);

    private static EquatableArray<RefKind> GetParameterRefKinds(IMethodSymbol? method) =>
        method is null ? default : new EquatableArray<RefKind>(method.Parameters.Select(static p => p.RefKind).ToArray());

    private static DiagnosticInfo? ValidatePropertyConditionMethods(IMethodSymbol mapperMethod, ref MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        var resolved = new List<PropertyMappingModel>(model.PropertyMappings.Count);
        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.ConditionMethod))
            {
                resolved.Add(mapping);
                continue;
            }

            var conditionMethods = containingType.GetMembers(mapping.ConditionMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic && (m.ReturnType.SpecialType == SpecialType.System_Boolean))
                .ToList();

            var (matchResult, matchedMethod) = FindMatchingPropertyConditionMethod(conditionMethods, mapping, model);
            if (matchResult == ConverterMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidPropertyConditionSignature,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapping.ConditionMethod!,
                    mapping.TargetPath);
            }

            resolved.Add(mapping with
            {
                ConditionAcceptsCustomParameters = matchResult == ConverterMatchResult.MatchWithCustomParams,
                ConditionParameterRefKinds = GetParameterRefKinds(matchedMethod)
            });
        }

        model = model with { PropertyMappings = new(resolved) };
        return null;
    }

    private static (ConverterMatchResult Result, IMethodSymbol? Method) FindMatchingPropertyConditionMethod(List<IMethodSymbol> candidates, PropertyMappingModel mapping, MapperMethodModel model)
    {
        IMethodSymbol? withCustomParams = null;
        IMethodSymbol? withoutCustomParams = null;
        var customParams = model.CustomParameters;

        foreach (var method in candidates)
        {
            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 1 + customParams.Count) &&
                TakesValueAndCustomParameters(method, mapping.SourceType, customParams))
            {
                withCustomParams = PreferByValue(withCustomParams, method);
            }

            if ((method.Parameters.Length == 1) &&
                TakesArgument(method.Parameters[0], mapping.SourceType, ArgumentKind.Value))
            {
                withoutCustomParams = PreferByValue(withoutCustomParams, method);
            }
        }

        if (withCustomParams is not null)
        {
            return (ConverterMatchResult.MatchWithCustomParams, withCustomParams);
        }

        if (withoutCustomParams is not null)
        {
            return (ConverterMatchResult.MatchWithoutCustomParams, withoutCustomParams);
        }

        return (ConverterMatchResult.NoMatch, null);
    }

    // A converter or condition takes the property value, then the mapper's custom parameters. The
    // parameter count is checked by the caller.
    private static bool TakesValueAndCustomParameters(IMethodSymbol method, string valueType, EquatableArray<CustomParameterModel> customParams)
    {
        if (!TakesArgument(method.Parameters[0], valueType, ArgumentKind.Value))
        {
            return false;
        }

        for (var i = 0; i < customParams.Count; i++)
        {
            if (!TakesArgument(method.Parameters[i + 1], customParams[i].TypeName, GetVariableKind(customParams[i].RefKind)))
            {
                return false;
            }
        }

        return true;
    }

    private static DiagnosticInfo? ValidateConverterMethods(IMethodSymbol mapperMethod, ref MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        var resolved = new List<PropertyMappingModel>(model.PropertyMappings.Count);
        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.ConverterMethod))
            {
                resolved.Add(mapping);
                continue;
            }

            var converterMethods = containingType.GetMembers(mapping.ConverterMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var (matchResult, matchedMethod) = FindMatchingConverterMethod(converterMethods, mapping, model);
            if (matchResult == ConverterMatchResult.ReturnTypeMismatch)
            {
                var actualReturnType = converterMethods
                    .Where(m => (m.Parameters.Length >= 1) &&
                           (m.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == mapping.SourceType))
                    .Select(m => m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .FirstOrDefault() ?? "?";

                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterReturnType,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapping.ConverterMethod!,
                    mapping.TargetType,
                    actualReturnType);
            }

            if (matchResult == ConverterMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterSignature,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapping.ConverterMethod!,
                    mapping.TargetPath);
            }

            resolved.Add(mapping with
            {
                ConverterAcceptsCustomParameters = matchResult == ConverterMatchResult.MatchWithCustomParams,
                ConverterParameterRefKinds = GetParameterRefKinds(matchedMethod)
            });
        }

        model = model with { PropertyMappings = new(resolved) };
        return null;
    }

    internal enum ConverterMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams,
        ReturnTypeMismatch
    }

    internal static (ConverterMatchResult Result, IMethodSymbol? Method) FindMatchingConverterMethod(List<IMethodSymbol> candidates, PropertyMappingModel mapping, MapperMethodModel model)
    {
        IMethodSymbol? withCustomParams = null;
        IMethodSymbol? withoutCustomParams = null;
        var hasReturnTypeMismatch = false;
        var sourceType = mapping.SourceType;
        var targetType = mapping.TargetType;
        var customParams = model.CustomParameters;

        foreach (var method in candidates)
        {
            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var returnTypeMatches = returnType == targetType;

            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 1 + customParams.Count) &&
                TakesValueAndCustomParameters(method, sourceType, customParams))
            {
                if (returnTypeMatches)
                {
                    withCustomParams = PreferByValue(withCustomParams, method);
                }
                else
                {
                    hasReturnTypeMismatch = true;
                }
            }

            if ((method.Parameters.Length == 1) &&
                TakesArgument(method.Parameters[0], sourceType, ArgumentKind.Value))
            {
                if (returnTypeMatches)
                {
                    withoutCustomParams = PreferByValue(withoutCustomParams, method);
                }
                else
                {
                    hasReturnTypeMismatch = true;
                }
            }
        }

        if (withCustomParams is not null)
        {
            return (ConverterMatchResult.MatchWithCustomParams, withCustomParams);
        }

        if (withoutCustomParams is not null)
        {
            return (ConverterMatchResult.MatchWithoutCustomParams, withoutCustomParams);
        }

        if (hasReturnTypeMismatch)
        {
            return (ConverterMatchResult.ReturnTypeMismatch, null);
        }

        return (ConverterMatchResult.NoMatch, null);
    }

    internal static DiagnosticInfo? ValidateCallbackMethods(IMethodSymbol mapperMethod, ref MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        if (!String.IsNullOrEmpty(model.BeforeMapMethod))
        {
            var beforeMapMethods = containingType.GetMembers(model.BeforeMapMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var (matchResult, matchedMethod) = FindMatchingCallbackMethod(beforeMapMethods, model);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidBeforeMapSignature, syntax.GetLocation(), mapperMethod.Name, model.BeforeMapMethod!);
            }
            model = model with
            {
                BeforeMapAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams,
                BeforeMapParameterRefKinds = GetParameterRefKinds(matchedMethod)
            };
        }

        if (!String.IsNullOrEmpty(model.AfterMapMethod))
        {
            var afterMapMethods = containingType.GetMembers(model.AfterMapMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var (matchResult, matchedMethod) = FindMatchingCallbackMethod(afterMapMethods, model);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidAfterMapSignature, syntax.GetLocation(), mapperMethod.Name, model.AfterMapMethod!);
            }
            model = model with
            {
                AfterMapAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams,
                AfterMapParameterRefKinds = GetParameterRefKinds(matchedMethod)
            };
        }

        return null;
    }

    internal enum CallbackMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams
    }

    internal static (CallbackMatchResult Result, IMethodSymbol? Method) FindMatchingCallbackMethod(List<IMethodSymbol> candidates, MapperMethodModel model)
    {
        IMethodSymbol? withCustomParams = null;
        IMethodSymbol? withoutCustomParams = null;
        var customParams = model.CustomParameters;

        // The source and destination are variables: parameters of the mapper, or the instance a
        // return-type mapper builds, which the callback may write.
        var sourceKind = GetVariableKind(model.SourceRefKind);
        var destinationKind = model.ReturnsDestination ? ArgumentKind.WritableVariable : GetVariableKind(model.DestinationRefKind);

        bool TakesSourceAndDestination(IMethodSymbol method) =>
            TakesArgument(method.Parameters[0], model.SourceTypeName, sourceKind) &&
            TakesArgument(method.Parameters[1], model.DestinationTypeName, destinationKind);

        foreach (var method in candidates)
        {
            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 2 + customParams.Count) &&
                TakesSourceAndDestination(method))
            {
                var customParamsMatch = true;
                for (var i = 0; i < customParams.Count; i++)
                {
                    if (!TakesArgument(method.Parameters[i + 2], customParams[i].TypeName, GetVariableKind(customParams[i].RefKind)))
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (customParamsMatch)
                {
                    withCustomParams = PreferByValue(withCustomParams, method);
                }
            }

            if ((method.Parameters.Length == 2) && TakesSourceAndDestination(method))
            {
                withoutCustomParams = PreferByValue(withoutCustomParams, method);
            }
        }

        if (withCustomParams is not null)
        {
            return (CallbackMatchResult.MatchWithCustomParams, withCustomParams);
        }

        if (withoutCustomParams is not null)
        {
            return (CallbackMatchResult.MatchWithoutCustomParams, withoutCustomParams);
        }

        return (CallbackMatchResult.NoMatch, null);
    }

    internal static MapperMethodModel ParseMappingAttributes(IMethodSymbol symbol, MapperMethodModel model)
    {
        // Options come from named arguments scattered over several attributes, so they are
        // gathered into locals and applied together with the parsed collections at the end.
        var autoMapOption = model.AutoMap;
        var strictOption = model.Strict;
        var strictExplicitlySet = model.StrictExplicitlySet;
        var nameComparisonOption = model.NameComparison;
        var nameComparisonExplicitlySet = model.NameComparisonExplicitlySet;
        var cultureOption = model.Culture;
        var cultureExplicitlySet = model.CultureExplicitlySet;
        var dateTimeFormatOption = model.DateTimeFormat;
        var numberFormatOption = model.NumberFormat;
        var beforeMapMethod = model.BeforeMapMethod;
        var afterMapMethod = model.AfterMapMethod;

        var definitionOrder = 0;
        var propertyMappings = new List<PropertyMappingModel>();
        var ignoredProperties = new List<string>();
        var propertyConditions = new List<PropertyConditionModel>();
        var constantMappings = new List<ConstantMappingModel>();
        var expressionMappings = new List<ExpressionMappingModel>();
        var mapUsingMappings = new List<MapUsingModel>();
        var mapFromMappings = new List<MapFromModel>();
        var mapCollectionMappings = new List<MapCollectionModel>();
        var mapNestedMappings = new List<MapNestedModel>();

        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == Names.MapperAttribute)
            {
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if ((namedArg.Key == "AutoMap") && (namedArg.Value.Value is bool autoMap))
                    {
                        autoMapOption = autoMap;
                    }
                    else if ((namedArg.Key == "Strict") && (namedArg.Value.Value is bool strict))
                    {
                        strictOption = strict;
                        strictExplicitlySet = true;
                    }
                    else if ((namedArg.Key == "NameComparison") && (namedArg.Value.Value is int nc))
                    {
                        nameComparisonOption = nc;
                        nameComparisonExplicitlySet = true;
                    }
                    else if ((namedArg.Key == "Culture") && (namedArg.Value.Value is string culture))
                    {
                        cultureOption = culture;
                        cultureExplicitlySet = true;
                    }
                    else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string dtFmt))
                    {
                        dateTimeFormatOption = dtFmt;
                    }
                    else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string numFmt))
                    {
                        numberFormatOption = numFmt;
                    }
                }
            }
            else if ((attributeName == Names.MapPropertyAttribute) ||
                     ((attribute.AttributeClass?.IsGenericType == true) &&
                      (attribute.AttributeClass.OriginalDefinition.ToDisplayString() == Names.MapPropertyAttributeGeneric)))
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    string? sourceName = null;
                    string? converter = null;
                    var nullBehavior = NullBehaviorType.Default;
                    var order = 0;
                    string? nullValue = null;
                    string? propCulture = null;
                    string? propDateTimeFormat = null;
                    string? propNumberFormat = null;

                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        sourceName = attribute.ConstructorArguments[1].Value?.ToString();
                    }

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Converter") && (namedArg.Value.Value is string conv))
                        {
                            converter = conv;
                        }
                        else if ((namedArg.Key == "NullBehavior") && (namedArg.Value.Value is int nb))
                        {
                            nullBehavior = (NullBehaviorType)nb;
                        }
                        else if ((namedArg.Key == "Order") && (namedArg.Value.Value is int ord))
                        {
                            order = ord;
                        }
                        else if (namedArg.Key == "NullValue")
                        {
                            nullValue = FormatConstantValue(namedArg.Value.Value);
                        }
                        else if ((namedArg.Key == "Culture") && (namedArg.Value.Value is string pc))
                        {
                            propCulture = pc;
                        }
                        else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string pdf))
                        {
                            propDateTimeFormat = pdf;
                        }
                        else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string pnf))
                        {
                            propNumberFormat = pnf;
                        }
                    }

                    var mapping = new PropertyMappingModel(
                        TargetPath: targetName,
                        SourcePath: sourceName ?? targetName,
                        ConverterMethod: converter,
                        NullBehavior: nullBehavior,
                        Order: order,
                        DefinitionOrder: definitionOrder++,
                        HasExplicitMapping: true,
                        NullValue: nullValue,
                        EffectiveCulture: propCulture,
                        EffectiveDateTimeFormat: propDateTimeFormat,
                        EffectiveNumberFormat: propNumberFormat);

                    propertyMappings.Add(mapping);
                }
            }
            else if (attributeName == Names.MapIgnoreAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    ignoredProperties.Add(targetName);
                }
            }
            else if ((attributeName == Names.MapConstantAttribute) ||
                     ((attributeName is not null) && attributeName.StartsWith(Names.MapConstantAttributeGenericPrefix, StringComparison.Ordinal)))
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var value = attribute.ConstructorArguments[1].Value;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Order") && (namedArg.Value.Value is int ord))
                        {
                            order = ord;
                        }
                    }

                    var constantMapping = new ConstantMappingModel(
                        TargetName: targetName,
                        Value: FormatConstantValue(value),
                        Order: order,
                        DefinitionOrder: definitionOrder++);

                    constantMappings.Add(constantMapping);
                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == Names.MapExpressionAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var expression = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Order") && (namedArg.Value.Value is int ord))
                        {
                            order = ord;
                        }
                    }

                    expressionMappings.Add(new ExpressionMappingModel(
                        TargetName: targetName,
                        Expression: expression,
                        Order: order,
                        DefinitionOrder: definitionOrder++));

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == Names.BeforeMapAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    beforeMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
            else if (attributeName == Names.AfterMapAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    afterMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
            else if (attributeName == Names.MapConditionAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var conditionName = attribute.ConstructorArguments[1].Value?.ToString();
                    if (!String.IsNullOrEmpty(targetName) && (conditionName is not null))
                    {
                        propertyConditions.Add(new PropertyConditionModel { TargetName = targetName, ConditionMethod = conditionName });
                    }
                }
            }
            else if (attributeName == Names.MapUsingAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var methodName = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Order") && (namedArg.Value.Value is int ord))
                        {
                            order = ord;
                        }
                    }

                    mapUsingMappings.Add(new MapUsingModel(
                        TargetName: targetName,
                        Method: methodName,
                        Order: order,
                        DefinitionOrder: definitionOrder++));

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == Names.MapFromAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var member = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Order") && (namedArg.Value.Value is int ord))
                        {
                            order = ord;
                        }
                    }

                    mapFromMappings.Add(new MapFromModel(
                        TargetName: targetName,
                        Member: member,
                        Order: order,
                        DefinitionOrder: definitionOrder++));

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == Names.MapCollectionAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    string? sourceName = null;
                    var mapper = string.Empty;
                    string? converter = null;
                    var order = 0;
                    var inPlace = false;

                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        sourceName = attribute.ConstructorArguments[1].Value?.ToString();
                    }

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Mapper") && (namedArg.Value.Value is string m))
                        {
                            mapper = m;
                        }
                        else if ((namedArg.Key == "Converter") && (namedArg.Value.Value is string conv))
                        {
                            converter = conv;
                        }
                        else if ((namedArg.Key == "Order") && (namedArg.Value.Value is int ord))
                        {
                            order = ord;
                        }
                        else if ((namedArg.Key == "Strategy") && (namedArg.Value.Value is int strat))
                        {
                            inPlace = strat == 1;
                        }
                    }

                    mapCollectionMappings.Add(new MapCollectionModel(
                        TargetName: targetName,
                        SourceName: sourceName ?? targetName,
                        Mapper: mapper,
                        Converter: converter,
                        Order: order,
                        DefinitionOrder: definitionOrder++,
                        InPlace: inPlace));

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == Names.MapNestedAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    string? sourceName = null;
                    var mapper = string.Empty;
                    var order = 0;

                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        sourceName = attribute.ConstructorArguments[1].Value?.ToString();
                    }

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Mapper") && (namedArg.Value.Value is string m))
                        {
                            mapper = m;
                        }
                        else if ((namedArg.Key == "Order") && (namedArg.Value.Value is int ord))
                        {
                            order = ord;
                        }
                    }

                    mapNestedMappings.Add(new MapNestedModel(
                        TargetName: targetName,
                        SourceName: sourceName ?? targetName,
                        Mapper: mapper,
                        Order: order,
                        DefinitionOrder: definitionOrder++));

                    ignoredProperties.Add(targetName);
                }
            }
        }

        for (var i = 0; i < propertyMappings.Count; i++)
        {
            var condition = propertyConditions.FirstOrDefault(c => String.Equals(c.TargetName, propertyMappings[i].TargetPath, StringComparison.Ordinal));
            if (condition is not null)
            {
                propertyMappings[i] = propertyMappings[i] with { ConditionMethod = condition.ConditionMethod };
            }
        }

        return model with
        {
            AutoMap = autoMapOption,
            Strict = strictOption,
            StrictExplicitlySet = strictExplicitlySet,
            NameComparison = nameComparisonOption,
            NameComparisonExplicitlySet = nameComparisonExplicitlySet,
            Culture = cultureOption,
            CultureExplicitlySet = cultureExplicitlySet,
            DateTimeFormat = dateTimeFormatOption,
            NumberFormat = numberFormatOption,
            BeforeMapMethod = beforeMapMethod,
            AfterMapMethod = afterMapMethod,
            PropertyMappings = new(propertyMappings),
            IgnoredProperties = new(ignoredProperties),
            PropertyConditions = new(propertyConditions),
            ConstantMappings = new(constantMappings),
            ExpressionMappings = new(expressionMappings),
            MapUsingMappings = new(mapUsingMappings),
            MapFromMappings = new(mapFromMappings),
            MapCollectionMappings = new(mapCollectionMappings),
            MapNestedMappings = new(mapNestedMappings)
        };
    }

    internal static MapperMethodModel ParseConverterAttributes(IMethodSymbol symbol, MapperMethodModel model)
    {
        // The method attributes win over the containing type's, so the values are collected in
        // order into locals and applied once at the end.
        var mapConverterTypeName = model.MapConverterTypeName;
        var mapConverterMethodName = model.MapConverterMethodName;
        var collectionConverterTypeName = model.CollectionConverterTypeName;
        var strictOption = model.Strict;
        var nameComparisonOption = model.NameComparison;
        var cultureOption = model.Culture;
        var dateTimeFormatOption = model.DateTimeFormat;
        var numberFormatOption = model.NumberFormat;

        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == Names.ValueConverterAttribute)
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    mapConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Method") && (namedArg.Value.Value is string methodName))
                        {
                            mapConverterMethodName = methodName;
                        }
                    }
                }
            }
            else if (attributeName == Names.CollectionConverterAttribute)
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    collectionConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }

        var containingType = symbol.ContainingType;
        foreach (var attribute in containingType.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if ((attributeName == Names.ValueConverterAttribute) && (mapConverterTypeName is null))
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    mapConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Method") && (namedArg.Value.Value is string methodName))
                        {
                            mapConverterMethodName = methodName;
                        }
                    }
                }
            }
            else if ((attributeName == Names.CollectionConverterAttribute) && (collectionConverterTypeName is null))
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    collectionConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
            else if (attributeName == Names.MapperProfileAttribute)
            {
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if ((namedArg.Key == "Strict") && (namedArg.Value.Value is bool strict) && (!model.StrictExplicitlySet))
                    {
                        strictOption = strict;
                    }
                    else if ((namedArg.Key == "NameComparison") && (namedArg.Value.Value is int nc) && (!model.NameComparisonExplicitlySet))
                    {
                        nameComparisonOption = nc;
                    }
                    else if ((namedArg.Key == "Culture") && (namedArg.Value.Value is string profileCulture) && (!model.CultureExplicitlySet))
                    {
                        cultureOption = profileCulture;
                    }
                    else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string profileDtFmt) && (!model.CultureExplicitlySet))
                    {
                        dateTimeFormatOption = profileDtFmt;
                    }
                    else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string profileNumFmt) && (!model.CultureExplicitlySet))
                    {
                        numberFormatOption = profileNumFmt;
                    }
                }
            }
        }

        return model with
        {
            MapConverterTypeName = mapConverterTypeName,
            MapConverterMethodName = mapConverterMethodName,
            CollectionConverterTypeName = collectionConverterTypeName,
            Strict = strictOption,
            NameComparison = nameComparisonOption,
            Culture = cultureOption,
            DateTimeFormat = dateTimeFormatOption,
            NumberFormat = numberFormatOption
        };
    }

    internal static string? FormatConstantValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        return value switch
        {
            string s => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
            char c => $"'{c}'",
            bool b => b ? "true" : "false",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            uint ui => $"{ui}U",
            _ => value.ToString()
        };
    }

    internal static MapperMethodModel BuildConstantMappings(ITypeSymbol destinationType, MapperMethodModel model)
    {
        var destinationProperties = destinationType.GetAllPublicProperties();

        var constants = new ConstantMappingModel[model.ConstantMappings.Count];
        for (var i = 0; i < constants.Length; i++)
        {
            var constantMapping = model.ConstantMappings[i];
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == constantMapping.TargetName);
            constants[i] = destProp is not null
                ? constantMapping with
                {
                    TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    IsTargetInitOnly = destProp.SetMethod?.IsInitOnly == true,
                    IsTargetRequired = destProp.IsRequired
                }
                : constantMapping;
        }

        var expressions = new ExpressionMappingModel[model.ExpressionMappings.Count];
        for (var i = 0; i < expressions.Length; i++)
        {
            var expressionMapping = model.ExpressionMappings[i];
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == expressionMapping.TargetName);
            expressions[i] = destProp is not null
                ? expressionMapping with
                {
                    TargetType = destProp.Type.ToDisplayString(NullableQualifiedFormat),
                    IsTargetTypeOblivious = destProp.Type.NullableAnnotation == NullableAnnotation.None,
                    IsTargetInitOnly = destProp.SetMethod?.IsInitOnly == true,
                    IsTargetRequired = destProp.IsRequired
                }
                : expressionMapping;
        }

        return model with
        {
            ConstantMappings = [with(constants)],
            ExpressionMappings = [with(expressions)]
        };
    }

    internal static DiagnosticInfo? ValidateDuplicateTargets(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var targetMappings = new Dictionary<string, List<string>>();

        void AddTarget(string target, string attributeType)
        {
            if (!targetMappings.TryGetValue(target, out var list))
            {
                list = [];
                targetMappings[target] = list;
            }
            list.Add(attributeType);
        }

        foreach (var mapping in model.PropertyMappings.Where(m => m.HasExplicitMapping))
        {
            AddTarget(mapping.TargetPath, "MapProperty");
        }

        foreach (var mapping in model.ConstantMappings)
        {
            AddTarget(mapping.TargetName, "MapConstant");
        }

        foreach (var mapping in model.ExpressionMappings)
        {
            AddTarget(mapping.TargetName, "MapExpression");
        }

        foreach (var mapping in model.MapUsingMappings)
        {
            AddTarget(mapping.TargetName, "MapUsing");
        }

        foreach (var mapping in model.MapFromMappings)
        {
            AddTarget(mapping.TargetName, "MapFrom");
        }

        foreach (var mapping in model.MapCollectionMappings)
        {
            AddTarget(mapping.TargetName, "MapCollection");
        }

        foreach (var mapping in model.MapNestedMappings)
        {
            AddTarget(mapping.TargetName, "MapNested");
        }

        foreach (var kvp in targetMappings)
        {
            if (kvp.Value.Count > 1)
            {
                return new DiagnosticInfo(
                    Diagnostics.DuplicateTargetMapping,
                    syntax.GetLocation(),
                    model.MethodName,
                    kvp.Key,
                    String.Join(", ", kvp.Value));
            }
        }

        return null;
    }

    internal static DiagnosticInfo? ValidateAndBuildMapUsingMappings(
        IMethodSymbol mapperMethod,
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var destinationProperties = destinationType.GetAllPublicProperties();

        var resolved = new List<MapUsingModel>(model.MapUsingMappings.Count);
        foreach (var mapUsing in model.MapUsingMappings)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapUsing.TargetName);
            if (destProp is null)
            {
                resolved.Add(mapUsing);
                continue;
            }

            var targetTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var candidateMethods = containingType.GetMembers(mapUsing.Method)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingMapUsingMethod(candidateMethods, model, sourceType, destProp.Type);
            if (matchResult.Result == MapUsingMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapUsingSignature,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapUsing.Method,
                    mapUsing.TargetName);
            }

            if (matchResult.Result == MapUsingMatchResult.ReturnTypeMismatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapUsingReturnTypeMismatch,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapUsing.Method,
                    targetTypeName,
                    matchResult.ActualReturnType ?? "unknown");
            }

            resolved.Add(mapUsing with
            {
                TargetType = targetTypeName,
                IsTargetInitOnly = destProp.SetMethod?.IsInitOnly == true,
                IsTargetRequired = destProp.IsRequired,
                AcceptsCustomParameters = matchResult.Result == MapUsingMatchResult.MatchWithCustomParams,
                ParameterRefKinds = GetParameterRefKinds(matchResult.MatchedMethod),
                MethodReturnType = matchResult.MatchedMethod?.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty
            });
        }

        model = model with { MapUsingMappings = new(resolved) };
        return null;
    }

    internal enum MapUsingMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams,
        ReturnTypeMismatch
    }

    internal readonly struct MapUsingMatchInfo
    {
        public MapUsingMatchResult Result { get; init; }
        public IMethodSymbol? MatchedMethod { get; init; }
        public string? ActualReturnType { get; init; }
    }

    internal static MapUsingMatchInfo FindMatchingMapUsingMethod(
        List<IMethodSymbol> candidates,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol targetType)
    {
        IMethodSymbol? withCustomParams = null;
        IMethodSymbol? withoutCustomParams = null;
        string? mismatchedReturnType = null;

        var sourceTypeName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var customParams = model.CustomParameters;
        var sourceKind = GetVariableKind(model.SourceRefKind);

        foreach (var method in candidates)
        {
            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 1 + customParams.Count) &&
                TakesArgument(method.Parameters[0], sourceTypeName, sourceKind))
            {
                var customParamsMatch = true;
                for (var i = 0; i < customParams.Count; i++)
                {
                    if (!TakesArgument(method.Parameters[i + 1], customParams[i].TypeName, GetVariableKind(customParams[i].RefKind)))
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (customParamsMatch)
                {
                    var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if ((returnType == targetTypeName) || method.ReturnType.IsAssignableTo(targetType))
                    {
                        withCustomParams = PreferByValue(withCustomParams, method);
                    }
                    else
                    {
                        mismatchedReturnType = returnType;
                    }
                }
            }

            if ((method.Parameters.Length == 1) &&
                TakesArgument(method.Parameters[0], sourceTypeName, sourceKind))
            {
                var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if ((returnType == targetTypeName) || method.ReturnType.IsAssignableTo(targetType))
                {
                    withoutCustomParams = PreferByValue(withoutCustomParams, method);
                }
                else
                {
                    mismatchedReturnType = returnType;
                }
            }
        }

        if (withCustomParams is not null)
        {
            return new MapUsingMatchInfo { Result = MapUsingMatchResult.MatchWithCustomParams, MatchedMethod = withCustomParams };
        }

        if (withoutCustomParams is not null)
        {
            return new MapUsingMatchInfo { Result = MapUsingMatchResult.MatchWithoutCustomParams, MatchedMethod = withoutCustomParams };
        }

        if (mismatchedReturnType is not null)
        {
            return new MapUsingMatchInfo { Result = MapUsingMatchResult.ReturnTypeMismatch, ActualReturnType = mismatchedReturnType };
        }

        return new MapUsingMatchInfo { Result = MapUsingMatchResult.NoMatch };
    }

    internal static DiagnosticInfo? ValidateAndBuildMapFromMappings(
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var destinationProperties = destinationType.GetAllPublicProperties();

        var resolved = new List<MapFromModel>(model.MapFromMappings.Count);
        foreach (var mapFrom in model.MapFromMappings)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapFrom.TargetName);
            if (destProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapFromTargetProperty,
                    syntax.GetLocation(),
                    model.MethodName,
                    mapFrom.TargetName);
            }

            var targetTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var withTarget = mapFrom with
            {
                TargetType = targetTypeName,
                IsTargetInitOnly = destProp.SetMethod?.IsInitOnly == true,
                IsTargetRequired = destProp.IsRequired
            };

            var member = mapFrom.Member;
            var isMethodCall = !member.Contains('.');

            if (isMethodCall)
            {
                var sourceMethod = sourceType.GetMembers(member)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => !m.IsStatic && (m.Parameters.Length == 0));

                if (sourceMethod is not null)
                {
                    var returnType = sourceMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if ((returnType != targetTypeName) && !sourceMethod.ReturnType.IsAssignableTo(destProp.Type))
                    {
                        return new DiagnosticInfo(
                            Diagnostics.MapFromReturnTypeMismatch,
                            syntax.GetLocation(),
                            model.MethodName,
                            mapFrom.Member,
                            targetTypeName,
                            returnType);
                    }

                    resolved.Add(withTarget with { IsMethodCall = true, ReturnType = returnType });
                    continue;
                }
            }

            var (resolvedType, isValid) = PropertyPathHelper.ResolvePropertyPath(sourceType, member);
            if (isValid && (resolvedType is not null))
            {
                var returnType = resolvedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if ((returnType != targetTypeName) && !resolvedType.IsAssignableTo(destProp.Type))
                {
                    return new DiagnosticInfo(
                        Diagnostics.MapFromReturnTypeMismatch,
                        syntax.GetLocation(),
                        $"{targetTypeName}, {returnType}, {mapFrom.Member} -> {mapFrom.TargetName}");
                }

                resolved.Add(withTarget with { IsMethodCall = false, ReturnType = returnType });
                continue;
            }

            return new DiagnosticInfo(
                Diagnostics.InvalidMapFromMember,
                syntax.GetLocation(),
                model.MethodName,
                mapFrom.Member,
                mapFrom.TargetName);
        }

        model = model with { MapFromMappings = new(resolved) };
        return null;
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
        var destinationProperties = destinationType.GetAllPublicProperties();

        var resolvedCollections = new List<MapCollectionModel>(model.MapCollectionMappings.Count);
        foreach (var declared in model.MapCollectionMappings)
        {
            var sourceProp = PropertyPathHelper.ResolveProperty(sourceType, declared.SourceName, (StringComparison)model.NameComparison);
            if (sourceProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionSourceProperty,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    declared.SourceName);
            }

            // The name is emitted verbatim, so adopt the declared casing when NameComparison matched
            // a source property that the attribute spelled differently.
            var mapCollection = declared with { SourceName = sourceProp.Name };

            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapCollection.TargetName);
            if (destProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionTargetProperty,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapCollection.TargetName);
            }

            // The emitted loop runs after construction and assigns the target, so a target without a
            // setter the mapper class can call, or an init-only one, can never be assigned, and required
            // targets are left unset by the generator-constructed instance.
            if (!HasAssignableSetter(destProp, containingType, compilation) || (model.ReturnsDestination && destProp.IsRequired))
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedInitOnlyCollectionTarget,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapCollection.TargetName);
            }

            var sourceElementType = sourceProp.Type.GetCollectionOrMemoryElementType();
            if (sourceElementType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapCollectionSourceNotCollection,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapCollection.SourceName);
            }

            var targetElementType = destProp.Type.GetCollectionElementType();
            if (targetElementType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapCollectionTargetNotCollection,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapCollection.TargetName);
            }

            var sourceShape = DetermineSourceShape(sourceProp.Type);
            var targetCollectionMethod = DetermineCollectionMethod(destProp.Type);
            var useHelperPath = (model.CollectionConverterTypeName is not null) || mapCollection.HasCustomConverter();
            var usesConverter = useHelperPath && !mapCollection.InPlace;

            // Without a collection converter the generated code creates the target collection: the one the
            // loop builds, or for InPlace the one it creates when the target is null. A target that cannot
            // take it, such as a collection class of its own, is reported instead of failing in the
            // generated code.
            if (!usesConverter &&
                (GetCreatedCollectionType(mapCollection.InPlace, destProp.Type, targetElementType, compilation) is { } createdType) &&
                !compilation.ClassifyCommonConversion(createdType, destProp.Type).IsImplicit)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedCollectionTarget,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapCollection.TargetName,
                    createdType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            // The element mapper is called by the loop the generated code emits, or handed as a delegate to
            // the method of the collection converter on the helper path, which InPlace does not take.
            Func<IMethodSymbol, bool> canCallMapper;
            if (usesConverter)
            {
                // A converter method missing, or one that cannot take the source collection and the element
                // mapper, is reported as a converter signature mismatch instead of leaving the call to fail
                // in the generated code
                var converterType = FindConverterType(mapperMethod, Names.CollectionConverterAttribute, Names.DefaultCollectionConverter);
                var converterMethodName = mapCollection.HasCustomConverter() ? mapCollection.Converter! : targetCollectionMethod;
                var converterMethods = FindCollectionConverterMethods(converterType, converterMethodName, sourceProp.Type, destProp.Type, sourceElementType, targetElementType, compilation);
                if (converterMethods.Count == 0)
                {
                    return new DiagnosticInfo(
                        Diagnostics.InvalidConverterSignature,
                        syntax.GetLocation(),
                        mapperMethod.Name,
                        $"{converterType?.ToDisplayString() ?? Names.DefaultCollectionConverter}.{converterMethodName}",
                        mapCollection.TargetName);
                }

                canCallMapper = m => converterMethods.Any(c => IsDelegateFor(c.Parameters[1].Type, m));
            }
            else
            {
                // The loop passes the element, and creates the instance a void mapper fills with new T()
                var element = GetElementArgumentKind(sourceShape);
                var canCreateElement = CanCreateInstance(targetElementType, containingType, compilation);
                canCallMapper = m => TakesMapperArguments(m, element) && (!m.ReturnsVoid || canCreateElement);
            }

            var elementMapper = FindMapperMethod(containingType, mapCollection.Mapper!, sourceElementType, targetElementType, canCallMapper);
            if (elementMapper is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapCollectionMapperMethod,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapCollection.Mapper!,
                    mapCollection.TargetName);
            }

            resolvedCollections.Add(mapCollection with
            {
                SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                SourceElementType = sourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TargetElementType = targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsSourceNullable = sourceProp.Type.IsNullableType(),
                TargetIsArray = destProp.Type is IArrayTypeSymbol,
                TargetCollectionMethod = targetCollectionMethod,
                SourceShape = sourceShape,
                TargetShape = DetermineTargetShape(destProp.Type),
                UseHelperPath = useHelperPath,
                InPlaceFallbackTypeName = mapCollection.InPlace
                    ? DetermineInPlaceFallbackTypeName(destProp.Type, targetElementType)
                    : mapCollection.InPlaceFallbackTypeName,
                MapperReturnsValue = !elementMapper.ReturnsVoid,
                MapperParameterRefKinds = GetParameterRefKinds(elementMapper)
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
        var destinationProperties = destinationType.GetAllPublicProperties();

        var resolvedNested = new List<MapNestedModel>(model.MapNestedMappings.Count);
        foreach (var declared in model.MapNestedMappings)
        {
            var sourceProp = PropertyPathHelper.ResolveProperty(sourceType, declared.SourceName, (StringComparison)model.NameComparison);
            if (sourceProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionSourceProperty,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    declared.SourceName);
            }

            // The name is emitted verbatim, so adopt the declared casing when NameComparison matched
            // a source property that the attribute spelled differently.
            var mapNested = declared with { SourceName = sourceProp.Name };

            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapNested.TargetName);
            if (destProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionTargetProperty,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapNested.TargetName);
            }

            // Same restriction as MapCollection: the nested-map statements run after construction.
            if (!HasAssignableSetter(destProp, containingType, compilation) || (model.ReturnsDestination && destProp.IsRequired))
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedInitOnlyCollectionTarget,
                    syntax.GetLocation(),
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

            var targetUnderlyingType = destProp.Type;
            if ((destProp.Type.NullableAnnotation == NullableAnnotation.Annotated) &&
                (destProp.Type is INamedTypeSymbol namedDestType) &&
                (!namedDestType.IsValueType))
            {
                targetUnderlyingType = namedDestType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            // The nested mapper gets the property value, and a void one the instance the generated code
            // creates with new T()
            var canCreateTarget = CanCreateInstance(targetUnderlyingType, containingType, compilation);
            var nestedMapper = FindMapperMethod(
                containingType,
                mapNested.Mapper,
                sourceUnderlyingType,
                targetUnderlyingType,
                m => TakesMapperArguments(m, ArgumentKind.Value) && (!m.ReturnsVoid || canCreateTarget));
            if (nestedMapper is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapNestedMapperMethod,
                    syntax.GetLocation(),
                    mapperMethod.Name,
                    mapNested.Mapper,
                    mapNested.TargetName);
            }

            resolvedNested.Add(mapNested with
            {
                SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsSourceNullable = sourceProp.Type.IsNullableType(),
                MapperReturnsValue = !nestedMapper.ReturnsVoid,
                MapperParameterRefKinds = GetParameterRefKinds(nestedMapper)
            });
        }

        model = model with { MapNestedMappings = new(resolvedNested) };
        return null;
    }

    // The mapper of [MapCollection] / [MapNested]: a static method taking the source element and returning
    // the target one, or taking both and filling the target. Only one the call can pass its arguments to
    // is used. The first of those decides between the two shapes, as the first match did before, and an
    // overload of that shape taking every argument by value wins, as the plain call always chose it.
    internal static IMethodSymbol? FindMapperMethod(
        INamedTypeSymbol containingType,
        string methodName,
        ITypeSymbol sourceElementType,
        ITypeSymbol targetElementType,
        Func<IMethodSymbol, bool> canCall)
    {
        var sourceTypeName = sourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetTypeName = targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        IMethodSymbol? found = null;
        var methods = containingType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic);
        foreach (var method in methods)
        {
            var methodToCheck = method.PartialDefinitionPart ?? method;
            if (IsMapperShape(methodToCheck, sourceTypeName, targetTypeName) &&
                canCall(methodToCheck) &&
                ((found is null) || (found.ReturnsVoid == methodToCheck.ReturnsVoid)))
            {
                found = PreferByValue(found, methodToCheck);
            }
        }

        return found;
    }

    private static bool IsMapperShape(IMethodSymbol method, string sourceTypeName, string targetTypeName)
    {
        if (method.ReturnsVoid)
        {
            return (method.Parameters.Length == 2) &&
                   (method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceTypeName) &&
                   (method.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == targetTypeName);
        }

        return (method.Parameters.Length == 1) &&
               (method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceTypeName) &&
               (method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == targetTypeName);
    }

    // Whether a mapper takes the arguments of the call: the source of the given kind, and for a void
    // mapper the instance the generated code creates for it, a local it may write.
    private static bool TakesMapperArguments(IMethodSymbol method, ArgumentKind source) =>
        CanTakeArgument(method.Parameters[0].RefKind, source) &&
        method.Parameters.Skip(1).All(static p => CanTakeArgument(p.RefKind, ArgumentKind.WritableVariable));

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

        var constructor = named.InstanceConstructors.FirstOrDefault(c =>
            c.Parameters.All(static p => p.IsOptional || p.IsParams) && compilation.IsSymbolAccessibleWithin(c, within));
        return (named.TypeKind is TypeKind.Class or TypeKind.Struct) &&
               (constructor is not null) &&
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

    // Whether a statement after construction can assign the property: a setter that is not init-only and
    // that the mapper class can call.
    private static bool HasAssignableSetter(IPropertySymbol property, INamedTypeSymbol within, Compilation compilation) =>
        (property.SetMethod is { IsInitOnly: false } setter) && compilation.IsSymbolAccessibleWithin(setter, within);

    // The collection the generated code creates for a [MapCollection] target without a collection
    // converter: the one the loop builds for the target shape, or for InPlace the List<T> (HashSet<T> for a
    // set) it creates when the target is null.
    private static ITypeSymbol? GetCreatedCollectionType(bool inPlace, ITypeSymbol targetType, ITypeSymbol elementType, Compilation compilation)
    {
        if (inPlace)
        {
            var isSet = DetermineInPlaceFallbackTypeName(targetType, elementType)
                .StartsWith("global::System.Collections.Generic.HashSet<", StringComparison.Ordinal);
            return ConstructType(compilation, isSet ? "System.Collections.Generic.HashSet`1" : "System.Collections.Generic.List`1", elementType);
        }

        return DetermineTargetShape(targetType) switch
        {
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

            foreach (var iface in named.AllInterfaces)
            {
                var ifaceName = iface.ConstructedFrom.ToDisplayString();
                if (ifaceName is "System.Collections.Generic.IReadOnlyCollection<T>"
                              or "System.Collections.Generic.ICollection<T>")
                {
                    return CollectionSourceShape.ReadOnlyCollection;
                }
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
                _ => CollectionTargetShape.List
            };
        }

        return CollectionTargetShape.List;
    }

    internal static List<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)> CollectStrictModeWarnings(MapperMethodModel model, ITypeSymbol destinationType)
    {
        var warnings = new List<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)>();
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

        foreach (var destProp in destinationType.GetAllPublicProperties())
        {
            if (destProp.IsReadOnly)
            {
                continue;
            }

            if (!mappedTargets.Contains(destProp.Name))
            {
                warnings.Add((Diagnostics.UnmappedDestinationProperty, model.MethodName, destProp.Name));
            }
        }

        return warnings;
    }

    internal static DiagnosticInfo? ValidateCultureAndFormat(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        if (String.IsNullOrEmpty(model.Culture) && (!String.IsNullOrEmpty(model.DateTimeFormat) || !String.IsNullOrEmpty(model.NumberFormat)))
        {
            return new DiagnosticInfo(Diagnostics.FormatWithoutCulture, syntax.GetLocation(), model.MethodName, "(method)");
        }

        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.EffectiveCulture) &&
                (!String.IsNullOrEmpty(mapping.EffectiveDateTimeFormat) || !String.IsNullOrEmpty(mapping.EffectiveNumberFormat)))
            {
                return new DiagnosticInfo(Diagnostics.FormatWithoutCulture, syntax.GetLocation(), model.MethodName, mapping.TargetPath);
            }
        }

        return null;
    }

    internal static DiagnosticInfo? ValidateNoTypeConverterFallback(ref MapperMethodModel model, ITypeSymbol sourceType, MethodDeclarationSyntax syntax)
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
                    var sourceValueType = PropertyPathHelper.ResolvePropertySymbol(sourceType, mapping.SourcePath.Split('.'))?.Type.GetUnderlyingType();
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

            return new DiagnosticInfo(Diagnostics.TypeConverterFallbackNotAllowed, syntax.GetLocation(), model.MethodName, mapping.TargetPath);
        }

        model = model with { PropertyMappings = new(resolved) };
        return null;
    }

    internal static IEnumerable<(DiagnosticDescriptor Descriptor, string Arg0, string Arg1)> CollectMapExpressionReflectionWarnings(MapperMethodModel model)
    {
        foreach (var expression in model.ExpressionMappings)
        {
            foreach (var pattern in MapperSourceBuilder.ReflectionPatterns)
            {
                if (expression.Expression.IndexOf(pattern, StringComparison.Ordinal) >= 0)
                {
                    yield return (Diagnostics.MapExpressionReflectionNotAllowed, model.MethodName, expression.TargetName);
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

        var hasInitOnlyTarget =
            model.PropertyMappings.Any(static pm => pm.IsTargetInitOnly) ||
            model.ConstantMappings.Any(static cm => cm.IsTargetInitOnly) ||
            model.ExpressionMappings.Any(static em => em.IsTargetInitOnly) ||
            model.MapUsingMappings.Any(static mu => mu.IsTargetInitOnly) ||
            model.MapFromMappings.Any(static mf => mf.IsTargetInitOnly);

        return hasInitOnlyTarget
            ? new DiagnosticInfo(Diagnostics.InitOnlyDestinationRequiresReturnMapper, syntax.GetLocation(), model.MethodName, model.DestinationTypeName)
            : null;
    }

    internal static DiagnosticInfo? ValidateRequiredMembers(MapperMethodModel model, ITypeSymbol destinationType, MethodDeclarationSyntax syntax)
    {
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

        foreach (var destProp in destinationType.GetAllPublicProperties())
        {
            if (!destProp.IsRequired)
            {
                continue;
            }

            if (!mappedTargets.Contains(destProp.Name))
            {
                return new DiagnosticInfo(Diagnostics.UnmappedRequiredProperty, syntax.GetLocation(), model.MethodName, destProp.Name);
            }
        }

        return null;
    }

    internal static DiagnosticInfo? BuildConstructorParameterMappings(
        ref MapperMethodModel model,
        ITypeSymbol destinationType,
        ITypeSymbol sourceType,
        MethodDeclarationSyntax syntax)
    {
        if (destinationType is not INamedTypeSymbol namedDest)
        {
            return null;
        }

        var bestCtor = SelectBestConstructor(destinationType);

        if ((bestCtor is null) ||
            !WillUseConstructor(bestCtor, destinationType, (StringComparison)model.NameComparison, model.ReturnsDestination))
        {
            // Construction is parameterless. A return-mapper must still initialize init-only or
            // required members via an object initializer, since `new Dst()` cannot assign them
            // afterwards.
            if (model.ReturnsDestination &&
                destinationType.GetAllPublicProperties().Any(p => (p.SetMethod?.IsInitOnly == true) || p.IsRequired))
            {
                model = model with { UseConstructorMapping = true };
            }

            return null;
        }

        if (!model.ReturnsDestination)
        {
            return new DiagnosticInfo(
                Diagnostics.InitOnlyDestinationRequiresReturnMapper,
                syntax.GetLocation(),
                model.MethodName,
                namedDest.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        model = model with { UseConstructorMapping = true };

        // Mappings that supply a constructor argument are flagged in place; the flagged copies plus
        // any synthesized ones replace the collection at the end.
        var flagged = new List<PropertyMappingModel>(model.PropertyMappings);

        var nameComparison = (StringComparison)model.NameComparison;
        var sourceProperties = sourceType.GetAllPublicProperties();
        var ctorParams = new List<(string ParamName, string TargetPath)>();
        var synthesizedMappings = new List<PropertyMappingModel>();

        foreach (var param in bestCtor.Parameters)
        {
            // The constructor requires a value for every parameter, so an ignored member that a
            // parameter assigns cannot actually be skipped. Reject rather than silently map it.
            var ignoredName = model.IgnoredProperties.FirstOrDefault(name => MatchesConstructorParameter(param, name, nameComparison));
            if (ignoredName is not null)
            {
                return new DiagnosticInfo(
                    Diagnostics.IgnoredConstructorParameter,
                    syntax.GetLocation(),
                    model.MethodName,
                    ignoredName);
            }

            // Prefer the property mapping built for this member: it carries the converter, null
            // handling and culture/format metadata that the argument has to be emitted with. The
            // mapping is flagged rather than removed so it keeps flowing through the analysis passes.
            var index = flagged.FindIndex(pm => MatchesConstructorParameter(param, pm.TargetPath, nameComparison));
            if (index >= 0)
            {
                flagged[index] = flagged[index] with { IsConstructorParameter = true };
                ctorParams.Add((param.Name, flagged[index].TargetPath));
                continue;
            }

            // No destination property backs this parameter, so synthesize a mapping for it. An
            // explicit [MapProperty] may still name the parameter, in which case its source path and
            // options are used; otherwise the source is matched by name.
            var explicitMapping = model.ExplicitPropertyMappings.FirstOrDefault(pm => MatchesConstructorParameter(param, pm.TargetPath, nameComparison));

            string sourcePath;
            ITypeSymbol sourcePropertyType;
            if (explicitMapping is not null)
            {
                // ValidateExplicitPropertyMappings already resolved and canonicalized this path.
                sourcePath = explicitMapping.SourcePath;
                sourcePropertyType = PropertyPathHelper.ResolvePropertyType(sourceType, sourcePath)!;
            }
            else
            {
                var srcProp = sourceProperties.FirstOrDefault(p => String.Equals(p.Name, param.Name, nameComparison))
                           ?? sourceProperties.FirstOrDefault(p => String.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));
                if (srcProp is null)
                {
                    return new DiagnosticInfo(
                        Diagnostics.UnresolvedConstructorParameter,
                        syntax.GetLocation(),
                        model.MethodName,
                        param.Name,
                        destinationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                }

                sourcePath = srcProp.Name;
                sourcePropertyType = srcProp.Type;
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
                explicitMapping?.ConverterMethod,
                paramCondition?.ConditionMethod);
            synthesizedMappings.Add(synthesized with { IsConstructorParameter = true });
            ctorParams.Add((param.Name, param.Name));
        }

        model = model with
        {
            ConstructorParameters = new(ctorParams),
            PropertyMappings = [with([.. flagged, .. synthesizedMappings])]
        };

        return null;
    }

    // Selects the parameterized constructor that generated construction would call: the longest
    // explicitly declared one. Null when the type only has parameterless constructors.
    internal static IMethodSymbol? SelectBestConstructor(ITypeSymbol destinationType) =>
        destinationType is INamedTypeSymbol namedType
            ? namedType.InstanceConstructors
                .Where(static c => !c.IsImplicitlyDeclared && (c.Parameters.Length > 0))
                .OrderByDescending(static c => c.Parameters.Length)
                .FirstOrDefault()
            : null;

    // Whether generated construction will actually call bestCtor:
    //   - records always construct through their primary constructor;
    //   - a parameter without a settable matching property (matched the same way arguments bind,
    //     so the camelCase parameter / PascalCase property convention is honoured) forces the
    //     constructor, because it is the only way to assign that member;
    //   - a return mapper without a public parameterless constructor has no other way to construct.
    // For a void mapper the last clause does not apply - it never constructs - so the result reads
    // as "would construction be required", which is exactly what the SMP0302 check needs.
    internal static bool WillUseConstructor(IMethodSymbol bestCtor, ITypeSymbol destinationType, StringComparison nameComparison, bool returnsDestination)
    {
        if (bestCtor.ContainingType.IsRecord)
        {
            return true;
        }

        var allDestProps = destinationType.GetAllPublicProperties();
        var hasConstructorOnlyParams = bestCtor.Parameters.Any(p =>
        {
            var matchingProp = allDestProps.FirstOrDefault(prop => MatchesConstructorParameter(p, prop.Name, nameComparison));
            return (matchingProp?.SetMethod is null) || matchingProp.SetMethod.IsInitOnly;
        });
        if (hasConstructorOnlyParams)
        {
            return true;
        }

        return returnsDestination && !HasUsableParameterlessConstructor(bestCtor.ContainingType);
    }

    // Public keeps this conservative: implicit parameterless constructors are public, and when an
    // internal one is missed the generator simply keeps using the parameterized constructor, which
    // always compiles.
    private static bool HasUsableParameterlessConstructor(INamedTypeSymbol type) =>
        type.InstanceConstructors.Any(static c => (c.Parameters.Length == 0) && (c.DeclaredAccessibility == Accessibility.Public));

    // The constructor whose parameters generated construction will bind, or null when construction
    // is parameterless or never happens. Admission of get-only / parameter-only targets and their
    // consumption in BuildConstructorParameterMappings must agree on this single answer.
    internal static IMethodSymbol? GetEffectiveConstructor(MapperMethodModel model, ITypeSymbol destinationType)
    {
        var bestCtor = SelectBestConstructor(destinationType);
        return (bestCtor is not null) && WillUseConstructor(bestCtor, destinationType, (StringComparison)model.NameComparison, model.ReturnsDestination)
            ? bestCtor
            : null;
    }

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
                    syntax.GetLocation(),
                    model.MethodName,
                    mapping.TargetPath,
                    "MapCondition");
            }

            if (mapping.NullBehavior == NullBehaviorType.Skip)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnsupportedConstructorAssignedOption,
                    syntax.GetLocation(),
                    model.MethodName,
                    mapping.TargetPath,
                    "NullBehavior.Skip");
            }
        }

        return null;
    }

    // Canonicalizes every attribute target name to the destination member's declared name, so that the
    // mapper's NameComparison is honoured on the target side too. Auto-mapping already matched names
    // that way; without this, an explicit attribute would only accept the exact spelling.
    //
    // Doing it once here keeps every later stage matching ordinally against real member names, which
    // also means the emitted code carries the correct casing. Names that do not resolve are left
    // untouched so the existing "not found" diagnostics still fire.
    internal static MapperMethodModel CanonicalizeTargetNames(MapperMethodModel model, ITypeSymbol destinationType)
    {
        var nameComparison = (StringComparison)model.NameComparison;

        string Canonical(string targetName) =>
            (targetName.Contains('.')
                ? PropertyPathHelper.ResolveCanonicalPath(destinationType, targetName, nameComparison)
                : PropertyPathHelper.ResolveProperty(destinationType, targetName, nameComparison)?.Name)
            ?? targetName;

        // The nine loops below are the same shape but each one names a different model type and a
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

        if ((updatedMappings is null) && (updatedConditions is null) && (updatedConstants is null) &&
            (updatedExpressions is null) && (updatedMapUsings is null) && (updatedMapFroms is null) &&
            (updatedMapCollections is null) && (updatedMapNesteds is null) && (updatedIgnored is null))
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
            IgnoredProperties = updatedIgnored is null ? model.IgnoredProperties : new EquatableArray<string>(updatedIgnored)
        };
#pragma warning restore IDE0028
        // ReSharper restore UseCollectionExpression
    }

    internal static DiagnosticInfo? ValidateExplicitPropertyMappings(
        ref MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var nameComparison = (StringComparison)model.NameComparison;
        var destinationProperties = destinationType.GetAllPublicProperties();
        var effectiveConstructor = GetEffectiveConstructor(model, destinationType);

        var resolved = new List<PropertyMappingModel>(model.PropertyMappings.Count);
        foreach (var declared in model.PropertyMappings)
        {
            var canonicalSourcePath = PropertyPathHelper.ResolveCanonicalPath(sourceType, declared.SourcePath, nameComparison);
            if (canonicalSourcePath is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapPropertySourceProperty,
                    syntax.GetLocation(),
                    model.MethodName,
                    declared.TargetPath,
                    declared.SourcePath);
            }

            var mapping = declared with { SourcePath = canonicalSourcePath };
            resolved.Add(mapping);

            if (mapping.TargetPath.Contains('.'))
            {
                if (PropertyPathHelper.ResolvePropertyType(destinationType, mapping.TargetPath) is null)
                {
                    return new DiagnosticInfo(
                        Diagnostics.UnresolvedMapPropertyTargetProperty,
                        syntax.GetLocation(),
                        model.MethodName,
                        mapping.TargetPath);
                }

                continue;
            }

            // The target must be assignable: a settable property, or a parameter of the constructor
            // that construction will actually call. Matching mirrors the argument-binding loop, so
            // anything accepted here is guaranteed to be consumed rather than silently dropped.
            var targetProperty = destinationProperties.FirstOrDefault(p => String.Equals(p.Name, mapping.TargetPath, StringComparison.Ordinal));
            if ((targetProperty?.SetMethod is null) && !IsConstructorParameterTarget(effectiveConstructor, mapping.TargetPath, nameComparison))
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapPropertyTargetProperty,
                    syntax.GetLocation(),
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
        string? converterMethod,
        string? conditionMethod)
    {
        var sourceTypeName = sourcePropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var destTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isSourceNullable = sourcePropertyType.IsNullableType();
        var isTargetNullable = targetType.IsNullableType();

        var sourceUnderlyingType = sourcePropertyType.GetUnderlyingType();
        var targetUnderlyingType = targetType.GetUnderlyingType();
        var sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetUnderlyingTypeName = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var order = 0;
        var definitionOrder = 0;
        var nullBehavior = NullBehaviorType.Default;
        var nullValue = default(string?);
        string? propEffectiveCulture;
        string? propEffectiveDateTimeFormat;
        string? propEffectiveNumberFormat;
        if (originalMappings.TryGetValue(targetName, out var origMapping))
        {
            order = origMapping.Order;
            definitionOrder = origMapping.DefinitionOrder;
            nullBehavior = origMapping.NullBehavior;
            nullValue = origMapping.NullValue;
            propEffectiveCulture = origMapping.EffectiveCulture ?? model.Culture;
            propEffectiveDateTimeFormat = origMapping.EffectiveDateTimeFormat ?? model.DateTimeFormat;
            propEffectiveNumberFormat = origMapping.EffectiveNumberFormat ?? model.NumberFormat;
        }
        else
        {
            propEffectiveCulture = model.Culture;
            propEffectiveDateTimeFormat = model.DateTimeFormat;
            propEffectiveNumberFormat = model.NumberFormat;
        }

        var requiresConversion = TypeNameHelper.RequiresTypeConversion(sourceUnderlyingTypeName, targetUnderlyingTypeName)
                && (!sourceUnderlyingType.IsAssignableTo(targetUnderlyingType));

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
            Order: order,
            DefinitionOrder: definitionOrder,
            IsTargetInitOnly: isTargetInitOnly,
            IsTargetRequired: isTargetRequired,
            EffectiveCulture: propEffectiveCulture,
            EffectiveDateTimeFormat: propEffectiveDateTimeFormat,
            EffectiveNumberFormat: propEffectiveNumberFormat);

        mapping = DetectEnumMappingKind(mapping, sourceUnderlyingType, targetUnderlyingType);

        if (mapping.RequiresConversion && !mapping.IsEnumMapping() && !mapping.HasConverter())
        {
            var srcUnderlying = sourceUnderlyingType.SpecialType == SpecialType.System_String
                ? sourceUnderlyingType
                : null;
            if ((srcUnderlying is not null) && (mapping.EffectiveDateTimeFormat is null) && (mapping.EffectiveNumberFormat is null))
            {
                mapping = DetectParsableMethodFromSymbol(mapping, targetUnderlyingType);
            }
        }

        return mapping;
    }

    internal static MapperMethodModel BuildPropertyMappings(ITypeSymbol sourceType, ITypeSymbol destinationType, MapperMethodModel model)
    {
        var sourceProperties = sourceType.GetAllPublicProperties();
        var destinationProperties = destinationType.GetAllPublicProperties();

        var customMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        var nestedMappings = new List<PropertyMappingModel>();

        foreach (var declared in model.PropertyMappings)
        {
            if (declared.TargetPath.Contains('.') || declared.SourcePath.Contains('.'))
            {
                var mapping = ResolveNestedMapping(declared, sourceType, destinationType);

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
                            IsTargetInitOnly = nestedTargetProp.SetMethod?.IsInitOnly == true,
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

        var mappings = new List<PropertyMappingModel>();
        var effectiveConstructor = GetEffectiveConstructor(model, destinationType);
        var nameComparison = (StringComparison)model.NameComparison;

        foreach (var destProp in destinationProperties)
        {
            if (model.IgnoredProperties.Contains(destProp.Name))
            {
                continue;
            }

            if (nestedMappings.Any(m => m.TargetPath.StartsWith(destProp.Name + ".", StringComparison.Ordinal) || (m.TargetPath == destProp.Name)))
            {
                continue;
            }

            // A get-only property is still reachable when the constructor that construction will
            // call assigns it; that mapping is what carries the conversion metadata for the
            // argument. Gating on the same constructor and matching as the argument-binding loop is
            // what keeps this from admitting a mapping nothing consumes (which used to surface as an
            // assignment to a get-only property, CS0200).
            if ((destProp.SetMethod is null) && !IsConstructorParameterTarget(effectiveConstructor, destProp.Name, nameComparison))
            {
                continue;
            }

            string? sourcePropPath = null;
            ITypeSymbol? sourcePropertyType = null;
            string? converterMethod = null;
            string? conditionMethod = null;

            if (customMappings.TryGetValue(destProp.Name, out var customSourcePath))
            {
                sourcePropPath = customSourcePath;
                sourcePropertyType = PropertyPathHelper.ResolvePropertyType(sourceType, customSourcePath);

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
                    destProp.Type,
                    destProp.SetMethod?.IsInitOnly == true,
                    destProp.IsRequired,
                    sourcePropPath,
                    sourcePropertyType,
                    converterMethod,
                    conditionMethod));
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

        if (sourceIsEnum && targetIsEnum)
        {
            return mapping with
            {
                EnumMappingKind = EnumMappingKind.EnumToEnum,
                RequiresConversion = true,
                SourceEnumMembers = new(GetEnumMemberNamesDedupedByValue(sourceUnderlying)),
                DestEnumMembers = new(targetUnderlying.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst).Select(f => f.Name))
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
            return mapping with
            {
                EnumMappingKind = EnumMappingKind.EnumToString,
                RequiresConversion = true,
                SourceEnumMembers = new(GetEnumMemberNamesDedupedByValue(sourceUnderlying))
            };
        }

        if (sourceIsString && targetIsEnum)
        {
            return mapping with
            {
                EnumMappingKind = EnumMappingKind.StringToEnum,
                RequiresConversion = true,
                DestEnumMembers = new(targetUnderlying.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst).Select(f => f.Name))
            };
        }

        return mapping;
    }

    // Returns enum member names in declaration order, keeping only the first name per constant value.
    // Switch arms are emitted per member, and alias members (same value) would otherwise produce
    // duplicate case constants (CS8510).
    internal static List<string> GetEnumMemberNamesDedupedByValue(ITypeSymbol enumType)
    {
        var names = new List<string>();
        var seenValues = new HashSet<object>();
        foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (!field.IsConst || (field.ConstantValue is null))
            {
                continue;
            }

            if (seenValues.Add(field.ConstantValue))
            {
                names.Add(field.Name);
            }
        }

        return names;
    }

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

        if (hasSpanParsable)
        {
            return mapping with { ParseMethod = ParseMethodKind.SpanParsable };
        }

        if (hasParsable)
        {
            return mapping with { ParseMethod = ParseMethodKind.Parsable };
        }

        return mapping;
    }

    internal static PropertyMappingModel ResolveNestedMapping(PropertyMappingModel mapping, ITypeSymbol sourceType, ITypeSymbol destinationType)
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

                var prop = currentType.GetAllPublicProperties().FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    var isNullable = prop.Type.IsNullableType();
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

            var finalSourceProp = currentType.GetAllPublicProperties().FirstOrDefault(p => p.Name == sourceParts[sourceParts.Length - 1]);
            if (finalSourceProp is not null)
            {
                sourceTypeName = finalSourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isSourceNullable = finalSourceProp.Type.IsNullableType();
                var sourceUnderlyingType = finalSourceProp.Type.GetUnderlyingType();
                sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            var sourceProp = sourceType.GetAllPublicProperties().FirstOrDefault(p => p.Name == mapping.SourcePath);
            if (sourceProp is not null)
            {
                sourceTypeName = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isSourceNullable = sourceProp.Type.IsNullableType();
                var sourceUnderlyingType = sourceProp.Type.GetUnderlyingType();
                sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        var targetParts = mapping.TargetPath.Split('.');
        if (targetParts.Length > 1)
        {
            var currentTargetType = destinationType;
            var pathBuilder = new List<string>();

            var targetSegments = new List<NestedPathSegment>();
            for (var i = 0; i < targetParts.Length - 1; i++)
            {
                var part = targetParts[i];
                pathBuilder.Add(part);

                var prop = currentTargetType.GetAllPublicProperties().FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    targetSegments.Add(new NestedPathSegment(
                        String.Join(".", pathBuilder),
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        false));
                    currentTargetType = prop.Type;
                }
            }
#pragma warning disable IDE0028, IDE0306
            targetPathSegments = new(targetSegments);
#pragma warning restore IDE0028, IDE0306

            var finalProp = currentTargetType.GetAllPublicProperties().FirstOrDefault(p => p.Name == targetParts[targetParts.Length - 1]);
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
            var destProp = destinationType.GetAllPublicProperties().FirstOrDefault(p => p.Name == mapping.TargetPath);
            if (destProp is not null)
            {
                targetTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isTargetNullable = destProp.Type.IsNullableType();
                var targetUnderlyingType = destProp.Type.GetUnderlyingType();
                targetUnderlyingTypeName = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        if (!String.IsNullOrEmpty(sourceUnderlyingTypeName) && !String.IsNullOrEmpty(targetUnderlyingTypeName))
        {
            var srcParts = mapping.SourcePath.Split('.');
            var dstParts = mapping.TargetPath.Split('.');
            var srcFinalProp = PropertyPathHelper.ResolvePropertySymbol(sourceType, srcParts);
            var dstFinalProp = PropertyPathHelper.ResolvePropertySymbol(destinationType, dstParts);
            var srcUnderlying = srcFinalProp?.Type.GetUnderlyingType();
            var dstUnderlying = dstFinalProp?.Type.GetUnderlyingType();
            var assignable = (srcUnderlying is not null) && (dstUnderlying is not null) && srcUnderlying.IsAssignableTo(dstUnderlying);
            requiresConversion = (!assignable) &&
                TypeNameHelper.RequiresTypeConversion(sourceUnderlyingTypeName, targetUnderlyingTypeName);
        }
        else if (!String.IsNullOrEmpty(sourceTypeName) && !String.IsNullOrEmpty(targetTypeName))
        {
            requiresConversion = TypeNameHelper.RequiresTypeConversion(sourceTypeName, targetTypeName);
        }

        return mapping with
        {
            SourcePathSegments = sourcePathSegments,
            SourceType = sourceTypeName,
            IsSourceNullable = isSourceNullable,
            SourceUnderlyingType = sourceUnderlyingTypeName,
            TargetPathSegments = targetPathSegments,
            TargetType = targetTypeName,
            IsTargetNullable = isTargetNullable,
            TargetUnderlyingType = targetUnderlyingTypeName,
            RequiresConversion = requiresConversion
        };
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
        string? mapConverterTypeName,
        string mapConverterMethodName)
    {
        var hasMapConverter = mapConverterTypeName is not null;
        var converterType = FindConverterType(mapperMethod, Names.ValueConverterAttribute, Names.DefaultValueConverter);

        INamedTypeSymbol? parsableSymbol = null;
        INamedTypeSymbol? spanParsableSymbol = null;
        if (!hasMapConverter)
        {
            foreach (var reference in mapperMethod.ContainingModule.ReferencedAssemblySymbols)
            {
                parsableSymbol ??= reference.GetTypeByMetadataName("System.IParsable`1");
                spanParsableSymbol ??= reference.GetTypeByMetadataName("System.ISpanParsable`1");
                if ((parsableSymbol is not null) && (spanParsableSymbol is not null))
                {
                    break;
                }
            }
        }

        // The declared types of the members, from their symbols: the source property, and the target
        // property or the constructor parameter a constructor-only target stands for. The type names of
        // the model are looked up only as a last resort, which misses nested and generic types.
        ITypeSymbol? GetSourceType(PropertyMappingModel mapping, string typeName) =>
            PropertyPathHelper.ResolvePropertySymbol(sourceType, mapping.SourcePath.Split('.'))?.Type.GetUnderlyingType() ??
            mapperMethod.FindTypeByFullyQualifiedName(typeName);

        ITypeSymbol? GetTargetType(PropertyMappingModel mapping, string typeName) =>
            (PropertyPathHelper.ResolvePropertySymbol(destinationType, mapping.TargetPath.Split('.'))?.Type ??
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
            if ((converterType is not null) && requiresConversion && !isEnumMapping)
            {
                var specializedMethodName = $"{mapConverterMethodName}To{TypeNameHelper.GetSimpleTypeName(effectiveTarget)}";
                if (FindSpecializedMethod(converterType, specializedMethodName, effectiveSource, effectiveTarget) is not null)
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
                     (mapping.EffectiveDateTimeFormat is null) && (mapping.EffectiveNumberFormat is null) &&
                     (GetSourceType(mapping, effectiveSource)?.SpecialType == SpecialType.System_String))
            {
                var targetTypeSymbol = GetTargetType(mapping, effectiveTarget);
                if (targetTypeSymbol is not null)
                {
                    if ((spanParsableSymbol is not null)
                        ? targetTypeSymbol.IsImplementGenericInterface(spanParsableSymbol)
                        : targetTypeSymbol.IsImplementsInterfaceByName("System.ISpanParsable`1"))
                    {
                        parseMethod = ParseMethodKind.SpanParsable;
                    }
                    else if (targetTypeSymbol.IsImplementGenericInterface(parsableSymbol) ||
                             targetTypeSymbol.IsImplementsInterfaceByName("System.IParsable`1"))
                    {
                        parseMethod = ParseMethodKind.Parsable;
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
        var methods = converterType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && (m.Parameters.Length == 1))
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
        var converterType = FindConverterType(mapperMethod, Names.ValueConverterAttribute, Names.DefaultValueConverter);
        if (converterType is null)
        {
            return null;
        }

        var cultureInfoType = compilation.GetTypeByMetadataName("System.Globalization.CultureInfo");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        PropertyMappingModel[]? resolved = null;
        for (var i = 0; i < model.PropertyMappings.Count; i++)
        {
            var mapping = model.PropertyMappings[i];

            // A converter given to [MapProperty] takes over the conversion
            if (mapping.HasConverter())
            {
                continue;
            }

            var effectiveSource = mapping.SourceUnderlyingType is { Length: > 0 } s ? s : mapping.SourceType;
            var effectiveTarget = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;

            string? unusableMethod = null;
            if (mapping.HasSpecializedConverter() && mapping.HasCulture() && (cultureInfoType is not null))
            {
                var overload = FindCultureOverload(converterType, mapping.SpecializedConverterMethod!, effectiveSource, effectiveTarget, compilation, cultureInfoType, stringType);
                if (overload is null)
                {
                    unusableMethod = mapping.SpecializedConverterMethod;
                }
                else if ((overload.Parameters[1].RefKind is RefKind.In or RefKind.RefReadOnlyParameter) &&
                         (GetCultureArgumentKind(compilation, cultureInfoType, overload.Parameters[1].Type) == ArgumentKind.ReadOnlyVariable))
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
                    .Where(static m => m.IsStatic && (m.Arity == 2) && TakesArgumentCount(m, 1))
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
                    syntax.GetLocation(),
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
    // to and the format one a string converts to. The value and the format go as values. Null when no
    // overload can take the arguments.
    private static IMethodSymbol? FindCultureOverload(
        ITypeSymbol converterType,
        string methodName,
        string sourceType,
        string targetType,
        Compilation compilation,
        ITypeSymbol cultureInfoType,
        ITypeSymbol stringType)
    {
        IMethodSymbol? overload = null;
        var methods = converterType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(static m => m.IsStatic && TakesArgumentCount(m, 3));
        foreach (var method in methods)
        {
            var culture = GetCultureArgumentKind(compilation, cultureInfoType, method.Parameters[1].Type);
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

    // How the culture field, a static readonly CultureInfo, goes to a parameter: to a CultureInfo as
    // itself, a read-only variable (in / ref readonly get it by in), and to a type it converts to
    // (IFormatProvider, object) as the converted value, which goes as is. Null for any other type.
    private static ArgumentKind? GetCultureArgumentKind(Compilation compilation, ITypeSymbol cultureInfoType, ITypeSymbol parameterType)
    {
        var conversion = compilation.ClassifyCommonConversion(cultureInfoType, parameterType);
        if (conversion.IsIdentity)
        {
            return ArgumentKind.ReadOnlyVariable;
        }

        return conversion.IsImplicit ? ArgumentKind.Value : null;
    }

    // Whether value.ToString(format, provider) binds, as the IFormattable conversion calls it: a public
    // instance ToString(string, IFormatProvider) on the type or a base type, or on the interfaces an
    // interface type extends. An explicit implementation of IFormattable does not make the call bind.
    private static bool HasFormatToString(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return DeclaresFormatToString(type) || type.AllInterfaces.Any(DeclaresFormatToString);
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (DeclaresFormatToString(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool DeclaresFormatToString(ITypeSymbol type) =>
        type.GetMembers("ToString").OfType<IMethodSymbol>().Any(static m =>
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
