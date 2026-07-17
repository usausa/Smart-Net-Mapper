namespace Smart.Mapper.Generator;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Mapper.Generator.Helpers;
using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

internal static class MapperModelBuilder
{
    internal const string MapperAttributeName = "Smart.Mapper.MapperAttribute";
    internal const string MapPropertyAttributeName = "Smart.Mapper.MapPropertyAttribute";
    internal const string MapIgnoreAttributeName = "Smart.Mapper.MapIgnoreAttribute";
    internal const string MapConstantAttributeName = "Smart.Mapper.MapConstantAttribute";
    internal const string MapExpressionAttributeName = "Smart.Mapper.MapExpressionAttribute";
    internal const string BeforeMapAttributeName = "Smart.Mapper.BeforeMapAttribute";
    internal const string AfterMapAttributeName = "Smart.Mapper.AfterMapAttribute";
    internal const string MapConditionAttributeName = "Smart.Mapper.MapConditionAttribute";
    internal const string MapUsingAttributeName = "Smart.Mapper.MapUsingAttribute";
    internal const string MapFromAttributeName = "Smart.Mapper.MapFromAttribute";
    internal const string MapCollectionAttributeName = "Smart.Mapper.MapCollectionAttribute";
    internal const string MapNestedAttributeName = "Smart.Mapper.MapNestedAttribute";
    internal const string ValueConverterAttributeName = "Smart.Mapper.ValueConverterAttribute";
    internal const string CollectionConverterAttributeName = "Smart.Mapper.CollectionConverterAttribute";
    internal const string MapperProfileAttributeName = "Smart.Mapper.MapperProfileAttribute";

    internal const string DefaultValueConverterTypeName = "global::Smart.Mapper.DefaultValueConverter";
    internal const string DefaultCollectionConverterTypeName = "global::Smart.Mapper.DefaultCollectionConverter";

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

        var model = new MapperMethodModel
        {
            Namespace = ns,
            ClassName = containingType.GetClassName(),
            IsValueType = containingType.IsValueType,
            MethodAccessibility = symbol.DeclaredAccessibility,
            MethodName = symbol.Name
        };

        var sourceParam = symbol.Parameters[0];
        model.SourceTypeName = sourceParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        model.SourceParameterName = sourceParam.Name;
        model.IsSourceReadOnlyStruct = sourceParam.Type.IsValueType &&
                                       sourceParam.Type is INamedTypeSymbol { IsReadOnly: true };

        ITypeSymbol destinationType;
        int customParamStartIndex;

        if (symbol.ReturnsVoid)
        {
            if (symbol.Parameters.Length < 2)
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.Identifier.GetLocation(), symbol.Name));
            }

            var destParam = symbol.Parameters[1];
            model.DestinationTypeName = destParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            model.DestinationParameterName = destParam.Name;
            model.ReturnsDestination = false;
            destinationType = destParam.Type;
            customParamStartIndex = 2;
        }
        else
        {
            model.DestinationTypeName = symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            model.DestinationParameterName = null;
            model.ReturnsDestination = true;
            destinationType = symbol.ReturnType;
            customParamStartIndex = 1;
        }

        var customParameters = new List<CustomParameterModel>();
        for (var i = customParamStartIndex; i < symbol.Parameters.Length; i++)
        {
            var param = symbol.Parameters[i];
            customParameters.Add(new CustomParameterModel
            {
                Name = param.Name,
                TypeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            });
        }

        var duplicateType = customParameters
            .GroupBy(p => p.TypeName)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateType is not null)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                Diagnostics.DuplicateCustomParameterType,
                syntax.GetLocation(),
                $"{duplicateType.Key}, {symbol.Name}"));
        }

        model.CustomParameters = new EquatableArray<CustomParameterModel>([.. customParameters]);

        ParseMappingAttributes(symbol, model);

        var duplicateTargetError = ValidateDuplicateTargets(model, syntax);
        if (duplicateTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(duplicateTargetError);
        }

        ParseConverterAttributes(symbol, model);

        var validationError = ValidateCallbackMethods(symbol, model, syntax);
        if (validationError is not null)
        {
            return Results.Error<MapperMethodModel>(validationError);
        }

        var sourceType = symbol.Parameters[0].Type;

        BuildPropertyMappings(sourceType, destinationType, model);

        DetectSpecializedConverterMethods(model, symbol);

        DetectParsableMethods(model, symbol);

        DetectUserDefinedConversions(model, symbol, sourceType, destinationType);

        DetectFormattableMethod(model, symbol, sourceType);

        foreach (var mapping in model.PropertyMappings)
        {
            if (mapping.RequiresConversion && !mapping.IsEnumMapping() && !mapping.HasConverter() &&
                !mapping.HasSpecializedConverter() && !mapping.HasParsableMethod() &&
                !mapping.HasUserDefinedExplicit() && !mapping.UseFormattable)
            {
                var effectiveSource = mapping.SourceUnderlyingType is { Length: > 0 } s ? s : mapping.SourceType;
                var effectiveDest = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;
                if (TypeNameHelper.IsExplicitNumericConversion(effectiveSource, effectiveDest))
                {
                    mapping.RequiresExplicitNumericCast = true;
                }
            }
        }

        var converterError = ValidateConverterMethods(symbol, model, syntax);
        if (converterError is not null)
        {
            return Results.Error<MapperMethodModel>(converterError);
        }

        var propertyConditionError = ValidatePropertyConditionMethods(symbol, model, syntax);
        if (propertyConditionError is not null)
        {
            return Results.Error<MapperMethodModel>(propertyConditionError);
        }

        BuildConstantMappings(destinationType, model);

        var mapUsingError = ValidateAndBuildMapUsingMappings(symbol, model, sourceType, destinationType, syntax);
        if (mapUsingError is not null)
        {
            return Results.Error<MapperMethodModel>(mapUsingError);
        }

        var mapFromError = ValidateAndBuildMapFromMappings(model, sourceType, destinationType, syntax);
        if (mapFromError is not null)
        {
            return Results.Error<MapperMethodModel>(mapFromError);
        }

        var mapCollectionError = ValidateAndBuildMapCollectionMappings(symbol, model, sourceType, destinationType, syntax);
        if (mapCollectionError is not null)
        {
            return Results.Error<MapperMethodModel>(mapCollectionError);
        }

        var mapNestedError = ValidateAndBuildMapNestedMappings(symbol, model, sourceType, destinationType, syntax);
        if (mapNestedError is not null)
        {
            return Results.Error<MapperMethodModel>(mapNestedError);
        }

        var warnings = new List<(DiagnosticDescriptor Descriptor, string Arg)>();
        if (model.Strict)
        {
            warnings.AddRange(CollectStrictModeWarnings(model, destinationType));
        }

        warnings.AddRange(CollectMapExpressionReflectionWarnings(model));

        model.Warnings = new EquatableArray<(DiagnosticDescriptor Descriptor, string Arg)>([.. warnings]);

        var constructorError = BuildConstructorParameterMappings(model, destinationType, sourceType, syntax);
        if (constructorError is not null)
        {
            return Results.Error<MapperMethodModel>(constructorError);
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

        var typeConverterError = ValidateNoTypeConverterFallback(model, syntax);
        if (typeConverterError is not null)
        {
            return Results.Error<MapperMethodModel>(typeConverterError);
        }

        return Results.Success(model);
    }

    internal static DiagnosticInfo? ValidatePropertyConditionMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.ConditionMethod))
            {
                continue;
            }

            var conditionMethods = containingType.GetMembers(mapping.ConditionMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic && m.ReturnType.SpecialType == SpecialType.System_Boolean)
                .ToList();

            var matchResult = FindMatchingPropertyConditionMethod(conditionMethods, mapping, model);
            if (matchResult == ConverterMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidPropertyConditionSignature,
                    syntax.GetLocation(),
                    $"{mapping.ConditionMethod}, {mapping.SourcePath} -> {mapping.TargetPath}");
            }

            mapping.ConditionAcceptsCustomParameters = matchResult == ConverterMatchResult.MatchWithCustomParams;
        }

        return null;
    }

    internal static ConverterMatchResult FindMatchingPropertyConditionMethod(List<IMethodSymbol> candidates, PropertyMappingModel mapping, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;
        var sourceType = mapping.SourceType;
        var customParams = model.CustomParameters;

        foreach (var method in candidates)
        {
            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 1 + customParams.Count))
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                var customParamsMatch = true;
                for (var i = 0; i < customParams.Count; i++)
                {
                    if (method.Parameters[i + 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != customParams[i].TypeName)
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (sourceMatch && customParamsMatch)
                {
                    hasMatchWithCustomParams = true;
                }
            }

            if (method.Parameters.Length == 1)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                if (sourceMatch)
                {
                    hasMatchWithoutCustomParams = true;
                }
            }
        }

        if (hasMatchWithCustomParams)
        {
            return ConverterMatchResult.MatchWithCustomParams;
        }

        if (hasMatchWithoutCustomParams)
        {
            return ConverterMatchResult.MatchWithoutCustomParams;
        }

        return ConverterMatchResult.NoMatch;
    }

    internal static DiagnosticInfo? ValidateConverterMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.ConverterMethod))
            {
                continue;
            }

            var converterMethods = containingType.GetMembers(mapping.ConverterMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingConverterMethod(converterMethods, mapping, model);
            if (matchResult == ConverterMatchResult.ReturnTypeMismatch)
            {
                var actualReturnType = converterMethods
                    .Where(m => m.Parameters.Length >= 1 &&
                           m.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == mapping.SourceType)
                    .Select(m => m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    .FirstOrDefault() ?? "?";

                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterReturnType,
                    syntax.GetLocation(),
                    $"{mapping.ConverterMethod}, expected={mapping.TargetType}, actual={actualReturnType}, {mapping.SourcePath} -> {mapping.TargetPath}");
            }

            if (matchResult == ConverterMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterSignature,
                    syntax.GetLocation(),
                    $"{mapping.ConverterMethod}, {mapping.SourcePath} -> {mapping.TargetPath}");
            }

            mapping.ConverterAcceptsCustomParameters = matchResult == ConverterMatchResult.MatchWithCustomParams;
        }

        return null;
    }

    internal enum ConverterMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams,
        ReturnTypeMismatch
    }

    internal static ConverterMatchResult FindMatchingConverterMethod(List<IMethodSymbol> candidates, PropertyMappingModel mapping, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;
        var hasReturnTypeMismatch = false;
        var sourceType = mapping.SourceType;
        var targetType = mapping.TargetType;
        var customParams = model.CustomParameters;

        foreach (var method in candidates)
        {
            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var returnTypeMatches = returnType == targetType;

            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 1 + customParams.Count))
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                var customParamsMatch = true;
                for (var i = 0; i < customParams.Count; i++)
                {
                    if (method.Parameters[i + 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != customParams[i].TypeName)
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (sourceMatch && customParamsMatch)
                {
                    if (returnTypeMatches)
                    {
                        hasMatchWithCustomParams = true;
                    }
                    else
                    {
                        hasReturnTypeMismatch = true;
                    }
                }
            }

            if (method.Parameters.Length == 1)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                if (sourceMatch)
                {
                    if (returnTypeMatches)
                    {
                        hasMatchWithoutCustomParams = true;
                    }
                    else
                    {
                        hasReturnTypeMismatch = true;
                    }
                }
            }
        }

        if (hasMatchWithCustomParams)
        {
            return ConverterMatchResult.MatchWithCustomParams;
        }

        if (hasMatchWithoutCustomParams)
        {
            return ConverterMatchResult.MatchWithoutCustomParams;
        }

        if (hasReturnTypeMismatch)
        {
            return ConverterMatchResult.ReturnTypeMismatch;
        }

        return ConverterMatchResult.NoMatch;
    }

    internal static DiagnosticInfo? ValidateCallbackMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        if (!String.IsNullOrEmpty(model.BeforeMapMethod))
        {
            var beforeMapMethods = containingType.GetMembers(model.BeforeMapMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingCallbackMethod(beforeMapMethods, model);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidBeforeMapSignature, syntax.GetLocation(), $"{model.BeforeMapMethod!}, {mapperMethod.Name}");
            }
            model.BeforeMapAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams;
        }

        if (!String.IsNullOrEmpty(model.AfterMapMethod))
        {
            var afterMapMethods = containingType.GetMembers(model.AfterMapMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingCallbackMethod(afterMapMethods, model);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidAfterMapSignature, syntax.GetLocation(), $"{model.AfterMapMethod!}, {mapperMethod.Name}");
            }
            model.AfterMapAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams;
        }

        return null;
    }

    internal enum CallbackMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams
    }

    internal static CallbackMatchResult FindMatchingCallbackMethod(List<IMethodSymbol> candidates, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;
        var customParams = model.CustomParameters;

        foreach (var method in candidates)
        {
            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 2 + customParams.Count))
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.SourceTypeName;
                var destMatch = method.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.DestinationTypeName;

                var customParamsMatch = true;
                for (var i = 0; i < customParams.Count; i++)
                {
                    if (method.Parameters[i + 2].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != customParams[i].TypeName)
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (sourceMatch && destMatch && customParamsMatch)
                {
                    hasMatchWithCustomParams = true;
                }
            }

            if (method.Parameters.Length == 2)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.SourceTypeName;
                var destMatch = method.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.DestinationTypeName;

                if (sourceMatch && destMatch)
                {
                    hasMatchWithoutCustomParams = true;
                }
            }
        }

        if (hasMatchWithCustomParams)
        {
            return CallbackMatchResult.MatchWithCustomParams;
        }

        if (hasMatchWithoutCustomParams)
        {
            return CallbackMatchResult.MatchWithoutCustomParams;
        }

        return CallbackMatchResult.NoMatch;
    }

    internal static void ParseMappingAttributes(IMethodSymbol symbol, MapperMethodModel model)
    {
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

            if (attributeName == MapperAttributeName)
            {
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if ((namedArg.Key == "AutoMap") && (namedArg.Value.Value is bool autoMap))
                    {
                        model.AutoMap = autoMap;
                    }
                    else if ((namedArg.Key == "Strict") && (namedArg.Value.Value is bool strict))
                    {
                        model.Strict = strict;
                        model.StrictExplicitlySet = true;
                    }
                    else if ((namedArg.Key == "NameComparison") && (namedArg.Value.Value is int nc))
                    {
                        model.NameComparison = nc;
                        model.NameComparisonExplicitlySet = true;
                    }
                    else if ((namedArg.Key == "Culture") && (namedArg.Value.Value is string culture))
                    {
                        model.Culture = culture;
                        model.CultureExplicitlySet = true;
                    }
                    else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string dtFmt))
                    {
                        model.DateTimeFormat = dtFmt;
                    }
                    else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string numFmt))
                    {
                        model.NumberFormat = numFmt;
                    }
                }
            }
            else if ((attributeName == MapPropertyAttributeName) ||
                     (attribute.AttributeClass?.IsGenericType == true &&
                      attribute.AttributeClass.OriginalDefinition.ToDisplayString() == "Smart.Mapper.MapPropertyAttribute<T>"))
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    string? sourceName = null;
                    string? converter = null;
                    var nullBehavior = NullBehaviorType.Default;
                    var order = 0;
                    string? nullSubstitute = null;
                    string? propCulture = null;
                    string? propDateTimeFormat = null;
                    string? propNumberFormat = null;

                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        sourceName = attribute.ConstructorArguments[1].Value?.ToString();
                    }

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if ((namedArg.Key == "Source") && (namedArg.Value.Value is string src))
                        {
                            sourceName = src;
                        }
                        else if ((namedArg.Key == "Converter") && (namedArg.Value.Value is string conv))
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
                        else if (namedArg.Key == "NullSubstitute")
                        {
                            nullSubstitute = FormatConstantValue(namedArg.Value.Value);
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

                    var mapping = new PropertyMappingModel
                    {
                        TargetPath = targetName,
                        SourcePath = sourceName ?? targetName,
                        ConverterMethod = converter,
                        NullBehavior = nullBehavior,
                        Order = order,
                        DefinitionOrder = definitionOrder++,
                        HasExplicitMapping = true,
                        NullSubstitute = nullSubstitute,
                        EffectiveCulture = propCulture,
                        EffectiveDateTimeFormat = propDateTimeFormat,
                        EffectiveNumberFormat = propNumberFormat
                    };

                    propertyMappings.Add(mapping);
                }
            }
            else if (attributeName == MapIgnoreAttributeName)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    ignoredProperties.Add(targetName);
                }
            }
            else if ((attributeName == MapConstantAttributeName) ||
                     (attributeName is not null && attributeName.StartsWith("Smart.Mapper.MapConstantAttribute<", StringComparison.Ordinal)))
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var value = attribute.ConstructorArguments[1].Value;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                    }

                    var constantMapping = new ConstantMappingModel
                    {
                        TargetName = targetName,
                        Value = FormatConstantValue(value),
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    };

                    constantMappings.Add(constantMapping);
                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapExpressionAttributeName)
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var expression = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                    }

                    expressionMappings.Add(new ExpressionMappingModel
                    {
                        TargetName = targetName,
                        Expression = expression,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == BeforeMapAttributeName)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    model.BeforeMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
            else if (attributeName == AfterMapAttributeName)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    model.AfterMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
            else if (attributeName == MapConditionAttributeName)
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
            else if (attributeName == MapUsingAttributeName)
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var methodName = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                    }

                    mapUsingMappings.Add(new MapUsingModel
                    {
                        TargetName = targetName,
                        Method = methodName,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapFromAttributeName)
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var member = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                    }

                    mapFromMappings.Add(new MapFromModel
                    {
                        TargetName = targetName,
                        Member = member,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapCollectionAttributeName)
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
                        if (namedArg.Key == "Source" && namedArg.Value.Value is string src)
                        {
                            sourceName = src;
                        }
                        else if (namedArg.Key == "Mapper" && namedArg.Value.Value is string m)
                        {
                            mapper = m;
                        }
                        else if (namedArg.Key == "Converter" && namedArg.Value.Value is string conv)
                        {
                            converter = conv;
                        }
                        else if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                        else if (namedArg.Key == "Strategy" && namedArg.Value.Value is int strat)
                        {
                            inPlace = strat == 1;
                        }
                    }

                    mapCollectionMappings.Add(new MapCollectionModel
                    {
                        TargetName = targetName,
                        SourceName = sourceName ?? targetName,
                        Mapper = mapper,
                        Converter = converter,
                        Order = order,
                        DefinitionOrder = definitionOrder++,
                        InPlace = inPlace
                    });

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapNestedAttributeName)
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
                        if (namedArg.Key == "Source" && namedArg.Value.Value is string src)
                        {
                            sourceName = src;
                        }
                        else if (namedArg.Key == "Mapper" && namedArg.Value.Value is string m)
                        {
                            mapper = m;
                        }
                        else if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                    }

                    mapNestedMappings.Add(new MapNestedModel
                    {
                        TargetName = targetName,
                        SourceName = sourceName ?? targetName,
                        Mapper = mapper,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    ignoredProperties.Add(targetName);
                }
            }
        }

        foreach (var mapping in propertyMappings)
        {
            var condition = propertyConditions.FirstOrDefault(c => String.Equals(c.TargetName, mapping.TargetPath, StringComparison.Ordinal));
            if (condition is not null)
            {
                mapping.ConditionMethod = condition.ConditionMethod;
            }
        }

        model.PropertyMappings = new EquatableArray<PropertyMappingModel>([.. propertyMappings]);
        model.IgnoredProperties = new EquatableArray<string>([.. ignoredProperties]);
        model.PropertyConditions = new EquatableArray<PropertyConditionModel>([.. propertyConditions]);
        model.ConstantMappings = new EquatableArray<ConstantMappingModel>([.. constantMappings]);
        model.ExpressionMappings = new EquatableArray<ExpressionMappingModel>([.. expressionMappings]);
        model.MapUsingMappings = new EquatableArray<MapUsingModel>([.. mapUsingMappings]);
        model.MapFromMappings = new EquatableArray<MapFromModel>([.. mapFromMappings]);
        model.MapCollectionMappings = new EquatableArray<MapCollectionModel>([.. mapCollectionMappings]);
        model.MapNestedMappings = new EquatableArray<MapNestedModel>([.. mapNestedMappings]);
    }

    internal static void ParseConverterAttributes(IMethodSymbol symbol, MapperMethodModel model)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == ValueConverterAttributeName)
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    model.MapConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Method" && namedArg.Value.Value is string methodName)
                        {
                            model.MapConverterMethodName = methodName;
                        }
                    }
                }
            }
            else if (attributeName == CollectionConverterAttributeName)
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    model.CollectionConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }

        var containingType = symbol.ContainingType;
        foreach (var attribute in containingType.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if ((attributeName == ValueConverterAttributeName) && (model.MapConverterTypeName is null))
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    model.MapConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Method" && namedArg.Value.Value is string methodName)
                        {
                            model.MapConverterMethodName = methodName;
                        }
                    }
                }
            }
            else if ((attributeName == CollectionConverterAttributeName) && (model.CollectionConverterTypeName is null))
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    model.CollectionConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
            else if (attributeName == MapperProfileAttributeName)
            {
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if ((namedArg.Key == "Strict") && (namedArg.Value.Value is bool strict) && (!model.StrictExplicitlySet))
                    {
                        model.Strict = strict;
                    }
                    else if ((namedArg.Key == "NameComparison") && (namedArg.Value.Value is int nc) && (!model.NameComparisonExplicitlySet))
                    {
                        model.NameComparison = nc;
                    }
                    else if ((namedArg.Key == "Culture") && (namedArg.Value.Value is string profileCulture) && (!model.CultureExplicitlySet))
                    {
                        model.Culture = profileCulture;
                    }
                    else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string profileDtFmt) && (!model.CultureExplicitlySet))
                    {
                        model.DateTimeFormat = profileDtFmt;
                    }
                    else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string profileNumFmt) && (!model.CultureExplicitlySet))
                    {
                        model.NumberFormat = profileNumFmt;
                    }
                }
            }
        }
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

    internal static void BuildConstantMappings(ITypeSymbol destinationType, MapperMethodModel model)
    {
        var destinationProperties = destinationType.GetAllPublicProperties();

        foreach (var constantMapping in model.ConstantMappings)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == constantMapping.TargetName);
            if (destProp is not null)
            {
                constantMapping.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
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
                    $"{kvp.Key}: {String.Join(", ", kvp.Value)}");
            }
        }

        return null;
    }

    internal static DiagnosticInfo? ValidateAndBuildMapUsingMappings(
        IMethodSymbol mapperMethod,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var destinationProperties = destinationType.GetAllPublicProperties();

        foreach (var mapUsing in model.MapUsingMappings)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapUsing.TargetName);
            if (destProp is null)
            {
                continue;
            }

            mapUsing.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var candidateMethods = containingType.GetMembers(mapUsing.Method)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingMapUsingMethod(candidateMethods, model, sourceType, destProp.Type);
            if (matchResult.Result == MapUsingMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapFromSignature,
                    syntax.GetLocation(),
                    $"{mapUsing.Method}, {mapUsing.TargetName}");
            }

            if (matchResult.Result == MapUsingMatchResult.ReturnTypeMismatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapFromReturnTypeMismatch,
                    syntax.GetLocation(),
                    $"{mapUsing.TargetType}, {matchResult.ActualReturnType ?? "unknown"}, {mapUsing.Method} -> {mapUsing.TargetName}");
            }

            mapUsing.AcceptsCustomParameters = matchResult.Result == MapUsingMatchResult.MatchWithCustomParams;
            mapUsing.MethodReturnType = matchResult.MatchedMethod?.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
        }

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
        IMethodSymbol? matchedMethod = null;
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;
        string? mismatchedReturnType = null;

        var sourceTypeName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var customParams = model.CustomParameters;

        foreach (var method in candidates)
        {
            if ((customParams.Count > 0) &&
                (method.Parameters.Length == 1 + customParams.Count))
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceTypeName;

                var customParamsMatch = true;
                for (var i = 0; i < customParams.Count; i++)
                {
                    if (method.Parameters[i + 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != customParams[i].TypeName)
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (sourceMatch && customParamsMatch)
                {
                    var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if ((returnType == targetTypeName) || method.ReturnType.IsAssignableTo(targetType))
                    {
                        hasMatchWithCustomParams = true;
                        matchedMethod = method;
                    }
                    else
                    {
                        mismatchedReturnType = returnType;
                    }
                }
            }

            if (method.Parameters.Length == 1)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceTypeName;

                if (sourceMatch)
                {
                    var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if ((returnType == targetTypeName) || method.ReturnType.IsAssignableTo(targetType))
                    {
                        hasMatchWithoutCustomParams = true;
                        matchedMethod ??= method;
                    }
                    else
                    {
                        mismatchedReturnType = returnType;
                    }
                }
            }
        }

        if (hasMatchWithCustomParams)
        {
            return new MapUsingMatchInfo { Result = MapUsingMatchResult.MatchWithCustomParams, MatchedMethod = matchedMethod };
        }

        if (hasMatchWithoutCustomParams)
        {
            return new MapUsingMatchInfo { Result = MapUsingMatchResult.MatchWithoutCustomParams, MatchedMethod = matchedMethod };
        }

        if (mismatchedReturnType is not null)
        {
            return new MapUsingMatchInfo { Result = MapUsingMatchResult.ReturnTypeMismatch, ActualReturnType = mismatchedReturnType };
        }

        return new MapUsingMatchInfo { Result = MapUsingMatchResult.NoMatch };
    }

    internal static DiagnosticInfo? ValidateAndBuildMapFromMappings(
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var destinationProperties = destinationType.GetAllPublicProperties();

        foreach (var mapFrom in model.MapFromMappings)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapFrom.TargetName);
            if (destProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapFromTargetProperty,
                    syntax.GetLocation(),
                    mapFrom.TargetName);
            }

            mapFrom.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var member = mapFrom.Member;
            var isMethodCall = !member.Contains('.');

            if (isMethodCall)
            {
                var sourceMethod = sourceType.GetMembers(member)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => !m.IsStatic && m.Parameters.Length == 0);

                if (sourceMethod is not null)
                {
                    var returnType = sourceMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var targetTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if (returnType != targetTypeName && !sourceMethod.ReturnType.IsAssignableTo(destProp.Type))
                    {
                        return new DiagnosticInfo(
                            Diagnostics.MapFromMethodReturnTypeMismatch,
                            syntax.GetLocation(),
                            $"{targetTypeName}, {returnType}, {mapFrom.Member} -> {mapFrom.TargetName}");
                    }

                    mapFrom.IsMethodCall = true;
                    mapFrom.ReturnType = returnType;
                    continue;
                }
            }

            var (resolvedType, isValid) = PropertyPathHelper.ResolvePropertyPath(sourceType, member);
            if (isValid && (resolvedType is not null))
            {
                var returnType = resolvedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var targetTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (returnType != targetTypeName && !resolvedType.IsAssignableTo(destProp.Type))
                {
                    return new DiagnosticInfo(
                        Diagnostics.MapFromMethodReturnTypeMismatch,
                        syntax.GetLocation(),
                        $"{targetTypeName}, {returnType}, {mapFrom.Member} -> {mapFrom.TargetName}");
                }

                mapFrom.IsMethodCall = false;
                mapFrom.ReturnType = returnType;
                continue;
            }

            return new DiagnosticInfo(
                Diagnostics.InvalidMapFromMethodSignature,
                syntax.GetLocation(),
                $"{mapFrom.Member}, {mapFrom.TargetName}");
        }

        return null;
    }

    internal static DiagnosticInfo? ValidateAndBuildMapCollectionMappings(
        IMethodSymbol mapperMethod,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var sourceProperties = sourceType.GetAllPublicProperties();
        var destinationProperties = destinationType.GetAllPublicProperties();

        foreach (var mapCollection in model.MapCollectionMappings)
        {
            var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == mapCollection.SourceName);
            if (sourceProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionSourceProperty,
                    syntax.GetLocation(),
                    mapCollection.SourceName);
            }

            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapCollection.TargetName);
            if (destProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionTargetProperty,
                    syntax.GetLocation(),
                    mapCollection.TargetName);
            }

            var sourceElementType = sourceProp.Type.GetCollectionOrMemoryElementType();
            if (sourceElementType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapCollectionSourceNotCollection,
                    syntax.GetLocation(),
                    mapCollection.SourceName);
            }

            var targetElementType = destProp.Type.GetCollectionElementType();
            if (targetElementType is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.MapCollectionTargetNotCollection,
                    syntax.GetLocation(),
                    mapCollection.TargetName);
            }

            mapCollection.SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.SourceElementType = sourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.TargetElementType = targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.IsSourceNullable = sourceProp.Type.IsNullableType();
            mapCollection.TargetIsArray = destProp.Type is IArrayTypeSymbol;
            mapCollection.TargetCollectionMethod = DetermineCollectionMethod(destProp.Type);
            mapCollection.SourceShape = DetermineSourceShape(sourceProp.Type);
            mapCollection.TargetShape = DetermineTargetShape(destProp.Type);
            mapCollection.UseHelperPath = (model.CollectionConverterTypeName is not null) || mapCollection.HasCustomConverter();

            if (mapCollection.InPlace)
            {
                mapCollection.InPlaceFallbackTypeName = DetermineInPlaceFallbackTypeName(destProp.Type, targetElementType);
            }

            var mapperMethodResult = FindMapperMethod(containingType, mapCollection.Mapper!, sourceElementType, targetElementType);
            if (mapperMethodResult is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapCollectionMapperMethod,
                    syntax.GetLocation(),
                    $"{mapCollection.Mapper}, {mapCollection.SourceName} -> {mapCollection.TargetName}");
            }

            mapCollection.MapperReturnsValue = mapperMethodResult.Value;
        }

        return null;
    }

    internal static DiagnosticInfo? ValidateAndBuildMapNestedMappings(
        IMethodSymbol mapperMethod,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var sourceProperties = sourceType.GetAllPublicProperties();
        var destinationProperties = destinationType.GetAllPublicProperties();

        foreach (var mapNested in model.MapNestedMappings)
        {
            var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == mapNested.SourceName);
            if (sourceProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionSourceProperty,
                    syntax.GetLocation(),
                    mapNested.SourceName);
            }

            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapNested.TargetName);
            if (destProp is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.UnresolvedMapCollectionTargetProperty,
                    syntax.GetLocation(),
                    mapNested.TargetName);
            }

            mapNested.SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapNested.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapNested.IsSourceNullable = sourceProp.Type.IsNullableType();

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

            var mapperMethodResult = FindMapperMethod(containingType, mapNested.Mapper, sourceUnderlyingType, targetUnderlyingType);
            if (mapperMethodResult is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapNestedMapperMethod,
                    syntax.GetLocation(),
                    $"{mapNested.Mapper}, {mapNested.SourceName} -> {mapNested.TargetName}");
            }

            mapNested.MapperReturnsValue = mapperMethodResult.Value;
        }

        return null;
    }

    internal static bool? FindMapperMethod(INamedTypeSymbol containingType, string methodName, ITypeSymbol sourceElementType, ITypeSymbol targetElementType)
    {
        var sourceTypeName = sourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetTypeName = targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var methods = containingType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic);
        foreach (var method in methods)
        {
            var methodToCheck = method.PartialDefinitionPart ?? method;

            if ((methodToCheck.Parameters.Length == 1) &&
                (!methodToCheck.ReturnsVoid))
            {
                var paramType = methodToCheck.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var returnType = methodToCheck.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if ((paramType == sourceTypeName) && (returnType == targetTypeName))
                {
                    return true;
                }
            }

            if ((methodToCheck.Parameters.Length == 2) &&
                methodToCheck.ReturnsVoid)
            {
                var sourceParamType = methodToCheck.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var destParamType = methodToCheck.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if ((sourceParamType == sourceTypeName) && (destParamType == targetTypeName))
                {
                    return false;
                }
            }
        }

        return null;
    }

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

    internal static List<(DiagnosticDescriptor Descriptor, string Arg)> CollectStrictModeWarnings(MapperMethodModel model, ITypeSymbol destinationType)
    {
        var warnings = new List<(DiagnosticDescriptor Descriptor, string Arg)>();
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
                warnings.Add((Diagnostics.UnmappedDestinationProperty, destProp.Name));
            }
        }

        return warnings;
    }

    internal static DiagnosticInfo? ValidateCultureAndFormat(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        if (String.IsNullOrEmpty(model.Culture) && (!String.IsNullOrEmpty(model.DateTimeFormat) || !String.IsNullOrEmpty(model.NumberFormat)))
        {
            return new DiagnosticInfo(Diagnostics.FormatWithoutCulture, syntax.GetLocation(), model.MethodName);
        }

        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.EffectiveCulture) &&
                (!String.IsNullOrEmpty(mapping.EffectiveDateTimeFormat) || !String.IsNullOrEmpty(mapping.EffectiveNumberFormat)))
            {
                return new DiagnosticInfo(Diagnostics.FormatWithoutCulture, syntax.GetLocation(), $"{model.MethodName}.{mapping.TargetPath}");
            }
        }

        return null;
    }

    internal static DiagnosticInfo? ValidateNoTypeConverterFallback(MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        if (model.MapConverterTypeName is not null)
        {
            return null;
        }

        foreach (var mapping in model.PropertyMappings)
        {
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
                var lcTargetType = !String.IsNullOrEmpty(mapping.TargetUnderlyingType) ? mapping.TargetUnderlyingType : mapping.TargetType;
                if (TypeNameHelper.IsStringType(lcTargetType))
                {
                    var lcSourceType = !String.IsNullOrEmpty(mapping.SourceUnderlyingType) ? mapping.SourceUnderlyingType : mapping.SourceType;
                    if (!TypeNameHelper.IsBuiltInNumericOrDateType(lcSourceType))
                    {
                        mapping.UseFormattable = true;
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

            return new DiagnosticInfo(Diagnostics.TypeConverterFallbackNotAllowed, syntax.GetLocation(), mapping.TargetPath);
        }

        return null;
    }

    internal static IEnumerable<(DiagnosticDescriptor Descriptor, string Arg)> CollectMapExpressionReflectionWarnings(MapperMethodModel model)
    {
        foreach (var expression in model.ExpressionMappings)
        {
            foreach (var pattern in MapperSourceBuilder.ReflectionPatterns)
            {
                if (expression.Expression.IndexOf(pattern, StringComparison.Ordinal) >= 0)
                {
                    yield return (Diagnostics.MapExpressionReflectionNotAllowed, expression.TargetName);
                    break;
                }
            }
        }
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
                return new DiagnosticInfo(Diagnostics.UnmappedRequiredProperty, syntax.GetLocation(), destProp.Name);
            }
        }

        return null;
    }

    internal static DiagnosticInfo? BuildConstructorParameterMappings(
        MapperMethodModel model,
        ITypeSymbol destinationType,
        ITypeSymbol sourceType,
        MethodDeclarationSyntax syntax)
    {
        if (destinationType is not INamedTypeSymbol namedDest)
        {
            return null;
        }

        var bestCtor = namedDest.InstanceConstructors
            .Where(c => !c.IsImplicitlyDeclared && c.Parameters.Length > 0)
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (bestCtor is null)
        {
            // No parameterized constructor. A return-mapper must still initialize init-only or required
            // members via an object initializer, since `new Dst()` cannot assign them afterwards.
            if (model.ReturnsDestination &&
                destinationType.GetAllPublicProperties().Any(p => (p.SetMethod?.IsInitOnly == true) || p.IsRequired))
            {
                model.UseConstructorMapping = true;
            }

            return null;
        }

        var allDestProps = destinationType.GetAllPublicProperties();
        var isRecord = namedDest.IsRecord;

        var hasConstructorOnlyParams = bestCtor.Parameters.Any(p =>
        {
            var nc = (StringComparison)model.NameComparison;
            var matchingProp = allDestProps.FirstOrDefault(prop => String.Equals(prop.Name, p.Name, nc));
            return (matchingProp?.SetMethod is null) || matchingProp.SetMethod.IsInitOnly;
        });

        if ((!isRecord) && (!hasConstructorOnlyParams))
        {
            return null;
        }

        if (!model.ReturnsDestination)
        {
            return new DiagnosticInfo(
                Diagnostics.InitOnlyDestinationRequiresReturnMapper,
                syntax.GetLocation(),
                namedDest.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        model.UseConstructorMapping = true;

        var customMappings = model.PropertyMappings
            .Where(pm => !pm.TargetPath.Contains('.'))
            .ToDictionary(pm => pm.TargetPath, pm => pm.SourcePath, StringComparer.Ordinal);

        var nameComparison = (StringComparison)model.NameComparison;
        var sourceProperties = sourceType.GetAllPublicProperties();
        var ctorParams = new List<(string ParamName, string SourceExpression)>();

        foreach (var param in bestCtor.Parameters)
        {
            string sourceExpression;

            var pascalParamName = char.ToUpperInvariant(param.Name[0]) + param.Name.Substring(1);
            if (customMappings.TryGetValue(param.Name, out var customSource) ||
                customMappings.TryGetValue(pascalParamName, out customSource))
            {
                sourceExpression = $"{model.SourceParameterName}.{customSource}";
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
                        param.Name,
                        destinationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                }

                sourceExpression = $"{model.SourceParameterName}.{srcProp.Name}";
            }

            ctorParams.Add((param.Name, sourceExpression));
        }

        model.ConstructorParameters = new EquatableArray<(string ParamName, string SourceExpression)>([.. ctorParams]);

        model.PropertyMappings = new EquatableArray<PropertyMappingModel>([.. model.PropertyMappings
            .Where(pm => !bestCtor.Parameters.Any(p =>
                String.Equals(p.Name, pm.TargetPath, nameComparison)))]);

        return null;
    }

    internal static void BuildPropertyMappings(ITypeSymbol sourceType, ITypeSymbol destinationType, MapperMethodModel model)
    {
        var sourceProperties = sourceType.GetAllPublicProperties();
        var destinationProperties = destinationType.GetAllPublicProperties();

        var customMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        var nestedMappings = new List<PropertyMappingModel>();

        foreach (var mapping in model.PropertyMappings)
        {
            if (mapping.TargetPath.Contains('.') || mapping.SourcePath.Contains('.'))
            {
                ResolveNestedMapping(mapping, sourceType, destinationType);
                nestedMappings.Add(mapping);
            }
            else
            {
                customMappings[mapping.TargetPath] = mapping.SourcePath;
            }
        }

        var originalMappings = model.PropertyMappings.ToDictionary(m => m.TargetPath, m => m);

        var mappings = new List<PropertyMappingModel>();

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

            if (destProp.SetMethod is null)
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
                    var nameComparison = (StringComparison)model.NameComparison;
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
                var sourceTypeName = sourcePropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var destTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var isSourceNullable = sourcePropertyType.IsNullableType();
                var isTargetNullable = destProp.Type.IsNullableType();

                var sourceUnderlyingType = sourcePropertyType.GetUnderlyingType();
                var targetUnderlyingType = destProp.Type.GetUnderlyingType();
                var sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var targetUnderlyingTypeName = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                var order = 0;
                var definitionOrder = 0;
                var nullBehavior = NullBehaviorType.Default;
                var nullSubstitute = default(string?);
                string? propEffectiveCulture;
                string? propEffectiveDateTimeFormat;
                string? propEffectiveNumberFormat;
                if (originalMappings.TryGetValue(destProp.Name, out var origMapping))
                {
                    order = origMapping.Order;
                    definitionOrder = origMapping.DefinitionOrder;
                    nullBehavior = origMapping.NullBehavior;
                    nullSubstitute = origMapping.NullSubstitute;
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

                var mapping = new PropertyMappingModel
                {
                    SourcePath = sourcePropPath,
                    TargetPath = destProp.Name,
                    SourceType = sourceTypeName,
                    TargetType = destTypeName,
                    SourceUnderlyingType = sourceUnderlyingTypeName,
                    TargetUnderlyingType = targetUnderlyingTypeName,
                    RequiresConversion = requiresConversion,
                    IsSourceNullable = isSourceNullable,
                    IsTargetNullable = isTargetNullable,
                    ConverterMethod = converterMethod,
                    ConditionMethod = conditionMethod,
                    NullBehavior = nullBehavior,
                    NullSubstitute = nullSubstitute,
                    Order = order,
                    DefinitionOrder = definitionOrder,
                    IsTargetInitOnly = destProp.SetMethod?.IsInitOnly == true,
                    IsTargetRequired = destProp.IsRequired,
                    EffectiveCulture = propEffectiveCulture,
                    EffectiveDateTimeFormat = propEffectiveDateTimeFormat,
                    EffectiveNumberFormat = propEffectiveNumberFormat
                };

                DetectEnumMappingKind(mapping, sourceUnderlyingType, targetUnderlyingType);

                if (mapping.RequiresConversion && !mapping.IsEnumMapping() && !mapping.HasConverter())
                {
                    var srcUnderlying = sourceUnderlyingType.SpecialType == SpecialType.System_String
                        ? sourceUnderlyingType
                        : null;
                    if ((srcUnderlying is not null) && (mapping.EffectiveDateTimeFormat is null) && (mapping.EffectiveNumberFormat is null))
                    {
                        DetectParsableMethodFromSymbol(mapping, targetUnderlyingType);
                    }
                }

                mappings.Add(mapping);
            }
        }

        mappings.AddRange(nestedMappings);

        model.PropertyMappings = new EquatableArray<PropertyMappingModel>([.. mappings]);

        foreach (var mapping in model.PropertyMappings)
        {
            var condition = model.PropertyConditions.FirstOrDefault(c => String.Equals(c.TargetName, mapping.TargetPath, StringComparison.Ordinal));
            if (condition is not null)
            {
                mapping.ConditionMethod = condition.ConditionMethod;
            }
        }
    }

    internal static void DetectEnumMappingKind(PropertyMappingModel mapping, ITypeSymbol sourceUnderlying, ITypeSymbol targetUnderlying)
    {
        var sourceIsEnum = sourceUnderlying.TypeKind == TypeKind.Enum;
        var targetIsEnum = targetUnderlying.TypeKind == TypeKind.Enum;
        var sourceIsString = sourceUnderlying.SpecialType == SpecialType.System_String;
        var targetIsString = targetUnderlying.SpecialType == SpecialType.System_String;
        var sourceIsNumeric = sourceUnderlying.IsNumericType();
        var targetIsNumeric = targetUnderlying.IsNumericType();

        if (!sourceIsEnum && !targetIsEnum)
        {
            return;
        }

        if (sourceIsEnum && targetIsEnum)
        {
            mapping.EnumMappingKind = EnumMappingKind.EnumToEnum;
            mapping.RequiresConversion = true;

            mapping.SourceEnumMembers = new EquatableArray<string>([.. GetEnumMemberNamesDedupedByValue(sourceUnderlying)]);
            mapping.DestEnumMembers = new EquatableArray<string>(
                [.. targetUnderlying.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst).Select(f => f.Name)]);
        }
        else if (sourceIsEnum && targetIsNumeric)
        {
            mapping.EnumMappingKind = EnumMappingKind.EnumToNumeric;
            mapping.RequiresConversion = true;
        }
        else if (sourceIsNumeric && targetIsEnum)
        {
            mapping.EnumMappingKind = EnumMappingKind.NumericToEnum;
            mapping.RequiresConversion = true;
        }
        else if (sourceIsEnum && targetIsString)
        {
            mapping.EnumMappingKind = EnumMappingKind.EnumToString;
            mapping.RequiresConversion = true;

            mapping.SourceEnumMembers = new EquatableArray<string>([.. GetEnumMemberNamesDedupedByValue(sourceUnderlying)]);
        }
        else if (sourceIsString && targetIsEnum)
        {
            mapping.EnumMappingKind = EnumMappingKind.StringToEnum;
            mapping.RequiresConversion = true;

            mapping.DestEnumMembers = new EquatableArray<string>(
                [.. targetUnderlying.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst).Select(f => f.Name)]);
        }
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

    internal static void DetectParsableMethodFromSymbol(PropertyMappingModel mapping, ITypeSymbol targetType)
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
            mapping.ParseMethod = ParseMethodKind.SpanParsable;
        }
        else if (hasParsable)
        {
            mapping.ParseMethod = ParseMethodKind.Parsable;
        }
    }

    internal static void ResolveNestedMapping(PropertyMappingModel mapping, ITypeSymbol sourceType, ITypeSymbol destinationType)
    {
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
                    sourceSegments.Add(new NestedPathSegment
                    {
                        Path = String.Join(".", pathBuilder),
                        TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        IsNullable = isNullable
                    });
                    currentType = prop.Type;
                }
            }
            mapping.SourcePathSegments = new EquatableArray<NestedPathSegment>([.. sourceSegments]);

            var finalSourceProp = currentType.GetAllPublicProperties().FirstOrDefault(p => p.Name == sourceParts[sourceParts.Length - 1]);
            if (finalSourceProp is not null)
            {
                mapping.SourceType = finalSourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsSourceNullable = finalSourceProp.Type.IsNullableType();
                var sourceUnderlyingType = finalSourceProp.Type.GetUnderlyingType();
                mapping.SourceUnderlyingType = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            var sourceProp = sourceType.GetAllPublicProperties().FirstOrDefault(p => p.Name == mapping.SourcePath);
            if (sourceProp is not null)
            {
                mapping.SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsSourceNullable = sourceProp.Type.IsNullableType();
                var sourceUnderlyingType = sourceProp.Type.GetUnderlyingType();
                mapping.SourceUnderlyingType = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
                    targetSegments.Add(new NestedPathSegment
                    {
                        Path = String.Join(".", pathBuilder),
                        TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    });
                    currentTargetType = prop.Type;
                }
            }
            mapping.TargetPathSegments = new EquatableArray<NestedPathSegment>([.. targetSegments]);

            var finalProp = currentTargetType.GetAllPublicProperties().FirstOrDefault(p => p.Name == targetParts[targetParts.Length - 1]);
            if (finalProp is not null)
            {
                mapping.TargetType = finalProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsTargetNullable = finalProp.Type.IsNullableType();
                var targetUnderlyingType = finalProp.Type.GetUnderlyingType();
                mapping.TargetUnderlyingType = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            var destProp = destinationType.GetAllPublicProperties().FirstOrDefault(p => p.Name == mapping.TargetPath);
            if (destProp is not null)
            {
                mapping.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsTargetNullable = destProp.Type.IsNullableType();
                var targetUnderlyingType = destProp.Type.GetUnderlyingType();
                mapping.TargetUnderlyingType = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        if (!String.IsNullOrEmpty(mapping.SourceUnderlyingType) && !String.IsNullOrEmpty(mapping.TargetUnderlyingType))
        {
            var srcParts = mapping.SourcePath.Split('.');
            var dstParts = mapping.TargetPath.Split('.');
            var srcFinalProp = PropertyPathHelper.ResolvePropertySymbol(sourceType, srcParts);
            var dstFinalProp = PropertyPathHelper.ResolvePropertySymbol(destinationType, dstParts);
            var srcUnderlying = srcFinalProp?.Type.GetUnderlyingType();
            var dstUnderlying = dstFinalProp?.Type.GetUnderlyingType();
            var assignable = (srcUnderlying is not null) && (dstUnderlying is not null) && srcUnderlying.IsAssignableTo(dstUnderlying);
            mapping.RequiresConversion = (!assignable) &&
                TypeNameHelper.RequiresTypeConversion(mapping.SourceUnderlyingType, mapping.TargetUnderlyingType);
        }
        else if (!String.IsNullOrEmpty(mapping.SourceType) && !String.IsNullOrEmpty(mapping.TargetType))
        {
            mapping.RequiresConversion = TypeNameHelper.RequiresTypeConversion(mapping.SourceType, mapping.TargetType);
        }
    }

    internal static void DetectParsableMethods(MapperMethodModel model, IMethodSymbol mapperMethod)
    {
        if (model.MapConverterTypeName is not null)
        {
            foreach (var mapping in model.PropertyMappings)
            {
                mapping.ParseMethod = ParseMethodKind.None;
            }
            return;
        }

        INamedTypeSymbol? parsableSymbol = null;
        INamedTypeSymbol? spanParsableSymbol = null;

        foreach (var reference in mapperMethod.ContainingModule.ReferencedAssemblySymbols)
        {
            parsableSymbol ??= reference.GetTypeByMetadataName("System.IParsable`1");
            spanParsableSymbol ??= reference.GetTypeByMetadataName("System.ISpanParsable`1");
            if ((parsableSymbol is not null) && (spanParsableSymbol is not null))
            {
                break;
            }
        }

        if (parsableSymbol is null)
        {
            return;
        }

        foreach (var mapping in model.PropertyMappings)
        {
            if (!mapping.RequiresConversion)
            {
                continue;
            }

            if (mapping.IsEnumMapping() || mapping.HasSpecializedConverter() || mapping.HasConverter())
            {
                continue;
            }

            if ((mapping.EffectiveDateTimeFormat is not null) || (mapping.EffectiveNumberFormat is not null))
            {
                continue;
            }

            var sourceType = mapping.SourceUnderlyingType is { Length: > 0 } s ? s : mapping.SourceType;
            if (sourceType != "global::System.String")
            {
                continue;
            }

            var targetTypeName = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;
            var targetTypeSymbol = mapperMethod.FindTypeByFullyQualifiedName(targetTypeName);
            if (targetTypeSymbol is null)
            {
                continue;
            }

            if ((spanParsableSymbol is not null) && targetTypeSymbol.IsImplementGenericInterface(spanParsableSymbol))
            {
                mapping.ParseMethod = ParseMethodKind.SpanParsable;
            }
            else if ((spanParsableSymbol is null) && targetTypeSymbol.IsImplementsInterfaceByName("System.ISpanParsable`1"))
            {
                mapping.ParseMethod = ParseMethodKind.SpanParsable;
            }
            else if (targetTypeSymbol.IsImplementGenericInterface(parsableSymbol))
            {
                mapping.ParseMethod = ParseMethodKind.Parsable;
            }
            else if (targetTypeSymbol.IsImplementsInterfaceByName("System.IParsable`1"))
            {
                mapping.ParseMethod = ParseMethodKind.Parsable;
            }
        }
    }

    internal static void DetectUserDefinedConversions(MapperMethodModel model, IMethodSymbol mapperMethod, ITypeSymbol sourceType, ITypeSymbol destinationType)
    {
        if (model.MapConverterTypeName is not null)
        {
            return;
        }

        foreach (var mapping in model.PropertyMappings)
        {
            if (!mapping.RequiresConversion)
            {
                continue;
            }

            if (mapping.IsEnumMapping() || mapping.HasConverter())
            {
                continue;
            }

            var srcParts = mapping.SourcePath.Split('.');
            var dstParts = mapping.TargetPath.Split('.');
            var srcProp = PropertyPathHelper.ResolvePropertySymbol(sourceType, srcParts);
            var dstProp = PropertyPathHelper.ResolvePropertySymbol(destinationType, dstParts);

            var sourceTypeSymbol = srcProp?.Type.GetUnderlyingType();
            var targetTypeSymbol = dstProp?.Type.GetUnderlyingType();

            if ((sourceTypeSymbol is null) || (targetTypeSymbol is null))
            {
                var sourceTypeName = mapping.SourceUnderlyingType is { Length: > 0 } s ? s : mapping.SourceType;
                var targetTypeName = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;
                sourceTypeSymbol ??= mapperMethod.FindTypeByFullyQualifiedName(sourceTypeName);
                targetTypeSymbol ??= mapperMethod.FindTypeByFullyQualifiedName(targetTypeName);
            }

            if ((sourceTypeSymbol is null) || (targetTypeSymbol is null))
            {
                continue;
            }

            if (MapperSymbolExtensions.HasUserDefinedConversion(sourceTypeSymbol, targetTypeSymbol, isImplicit: true))
            {
                mapping.UserDefinedConversion = UserDefinedConversionKind.Implicit;
                if (!mapping.IsSourceNullable)
                {
                    mapping.RequiresConversion = false;
                }
            }
            else if (MapperSymbolExtensions.HasUserDefinedConversion(sourceTypeSymbol, targetTypeSymbol, isImplicit: false))
            {
                mapping.UserDefinedConversion = UserDefinedConversionKind.Explicit;
            }
        }
    }

    internal static void DetectFormattableMethod(MapperMethodModel model, IMethodSymbol mapperMethod, ITypeSymbol sourceType)
    {
        if (model.MapConverterTypeName is not null)
        {
            return;
        }

        INamedTypeSymbol? formattableSymbol = null;
        foreach (var reference in mapperMethod.ContainingModule.ReferencedAssemblySymbols)
        {
            formattableSymbol = reference.GetTypeByMetadataName("System.IFormattable");
            if (formattableSymbol is not null)
            {
                break;
            }
        }

        foreach (var mapping in model.PropertyMappings)
        {
            if (!mapping.RequiresConversion)
            {
                continue;
            }

            var targetType = mapping.TargetUnderlyingType is { Length: > 0 } t ? t : mapping.TargetType;
            if (!TypeNameHelper.IsStringType(targetType))
            {
                continue;
            }

            if (!mapping.HasCulture() && (mapping.EffectiveDateTimeFormat is null) && (mapping.EffectiveNumberFormat is null))
            {
                continue;
            }

            if (mapping.IsEnumMapping() || mapping.HasConverter() || mapping.HasSpecializedConverter() || mapping.HasParsableMethod())
            {
                continue;
            }

            if (mapping.HasUserDefinedExplicit())
            {
                continue;
            }

            var srcParts = mapping.SourcePath.Split('.');
            var srcProp = PropertyPathHelper.ResolvePropertySymbol(sourceType, srcParts);
            var sourceTypeSymbol = srcProp?.Type.GetUnderlyingType();

            if (sourceTypeSymbol is null)
            {
                var sourceTypeName = mapping.SourceUnderlyingType is { Length: > 0 } s ? s : mapping.SourceType;
                sourceTypeSymbol = mapperMethod.FindTypeByFullyQualifiedName(sourceTypeName);
            }

            if (sourceTypeSymbol is null)
            {
                continue;
            }

            var implementsFormattable = formattableSymbol is not null
                ? sourceTypeSymbol.AllInterfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i, formattableSymbol) ||
                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, formattableSymbol))
                : sourceTypeSymbol.IsImplementsInterfaceByName("System.IFormattable");

            if (!implementsFormattable)
            {
                implementsFormattable = sourceTypeSymbol.IsImplementsInterfaceByName("System.IFormattable");
            }

            if (implementsFormattable)
            {
                mapping.UseFormattable = true;
            }
        }
    }

    internal static void DetectSpecializedConverterMethods(MapperMethodModel model, IMethodSymbol mapperMethod)
    {
        var converterType = FindConverterType(mapperMethod, model.MapConverterTypeName ?? "Smart.Mapper.DefaultValueConverter");
        if (converterType is null)
        {
            return;
        }

        var methodPrefix = model.MapConverterMethodName;

        foreach (var mapping in model.PropertyMappings)
        {
            if (!mapping.RequiresConversion)
            {
                continue;
            }

            if (mapping.IsEnumMapping())
            {
                continue;
            }

            var sourceTypeForLookup = !String.IsNullOrEmpty(mapping.SourceUnderlyingType) ? mapping.SourceUnderlyingType : mapping.SourceType;
            var targetTypeForLookup = !String.IsNullOrEmpty(mapping.TargetUnderlyingType) ? mapping.TargetUnderlyingType : mapping.TargetType;

            var targetSimpleName = TypeNameHelper.GetSimpleTypeName(targetTypeForLookup);
            var specializedMethodName = $"{methodPrefix}To{targetSimpleName}";

            var specializedMethod = FindSpecializedMethod(
                converterType,
                specializedMethodName,
                sourceTypeForLookup,
                targetTypeForLookup);

            if (specializedMethod is not null)
            {
                mapping.SpecializedConverterMethod = specializedMethodName;
                mapping.ParseMethod = ParseMethodKind.None;
            }
        }
    }

    internal static ITypeSymbol? FindConverterType(IMethodSymbol mapperMethod, string converterTypeName)
    {
        var typeName = converterTypeName;
        if (typeName.StartsWith("global::", StringComparison.Ordinal))
        {
            typeName = typeName.Substring("global::".Length);
        }

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

    internal static IMethodSymbol? FindSpecializedMethod(
        ITypeSymbol converterType,
        string methodName,
        string sourceType,
        string targetType)
    {
        var methods = converterType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.Parameters.Length == 1)
            .ToList();

        foreach (var method in methods)
        {
            var paramType = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if ((paramType == sourceType) && (returnType == targetType))
            {
                return method;
            }
        }

        return null;
    }
}
