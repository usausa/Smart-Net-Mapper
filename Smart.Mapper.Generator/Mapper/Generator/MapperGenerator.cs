namespace Smart.Mapper.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

[Generator]
public sealed class MapperGenerator : IIncrementalGenerator
{
    private const string MapperAttributeName = "Smart.Mapper.MapperAttribute";
    private const string MapPropertyAttributeName = "Smart.Mapper.MapPropertyAttribute";
    private const string MapIgnoreAttributeName = "Smart.Mapper.MapIgnoreAttribute";
    private const string MapConstantAttributeName = "Smart.Mapper.MapConstantAttribute";
    private const string MapExpressionAttributeName = "Smart.Mapper.MapExpressionAttribute";
    private const string BeforeMapAttributeName = "Smart.Mapper.BeforeMapAttribute";
    private const string AfterMapAttributeName = "Smart.Mapper.AfterMapAttribute";
    private const string MapConditionAttributeName = "Smart.Mapper.MapConditionAttribute";
    private const string MapUsingAttributeName = "Smart.Mapper.MapUsingAttribute";
    private const string MapFromAttributeName = "Smart.Mapper.MapFromAttribute";
    private const string MapCollectionAttributeName = "Smart.Mapper.MapCollectionAttribute";
    private const string MapNestedAttributeName = "Smart.Mapper.MapNestedAttribute";
    private const string MapConverterAttributeName = "Smart.Mapper.MapConverterAttribute";
    private const string CollectionConverterAttributeName = "Smart.Mapper.CollectionConverterAttribute";

    private const string DefaultValueConverterTypeName = "global::Smart.Mapper.DefaultValueConverter";
    private const string DefaultCollectionConverterTypeName = "global::Smart.Mapper.DefaultCollectionConverter";

    // ------------------------------------------------------------
    // Initialize
    // ------------------------------------------------------------

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                MapperAttributeName,
                static (syntax, _) => IsMethodSyntax(syntax),
                static (context, _) => GetMapperMethodModel(context))
            .Collect();

        context.RegisterImplementationSourceOutput(
            methodProvider,
            static (context, methods) => Execute(context, methods));
    }

    // ------------------------------------------------------------
    // Parser
    // ------------------------------------------------------------

    private static bool IsMethodSyntax(SyntaxNode syntax) =>
        syntax is MethodDeclarationSyntax;

    private static Result<MapperMethodModel> GetMapperMethodModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not IMethodSymbol symbol)
        {
            return Results.Error<MapperMethodModel>(null);
        }

        // Validate method definition
        if (!symbol.IsStatic || !symbol.IsPartialDefinition)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodDefinition, syntax.GetLocation(), symbol.Name));
        }

        // Validate parameters:
        // Pattern 1: void Map(Source source, Destination destination, ...customParams) - at least 2 parameters
        // Pattern 2: Destination Map(Source source, ...customParams) - at least 1 parameter with return type
        if (symbol.Parameters.Length < 1)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.GetLocation(), symbol.Name));
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

        // Get source type and parameter name (always first parameter)
        var sourceParam = symbol.Parameters[0];
        model.SourceTypeName = sourceParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        model.SourceParameterName = sourceParam.Name;

        ITypeSymbol destinationType;
        int customParamStartIndex;

        // Determine if void method with destination parameter or return type method
        if (symbol.ReturnsVoid)
        {
            // void Map(Source source, Destination destination, ...customParams)
            if (symbol.Parameters.Length < 2)
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.GetLocation(), symbol.Name));
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
            // Destination Map(Source source, ...customParams)
            model.DestinationTypeName = symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            model.DestinationParameterName = null;
            model.ReturnsDestination = true;
            destinationType = symbol.ReturnType;
            customParamStartIndex = 1;
        }

        // Extract custom parameters
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

        // Check for duplicate custom parameter types
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

        model.CustomParameters = customParameters;

        // Parse attributes for MapProperty, MapIgnore, MapConstant, BeforeMap, AfterMap, MapCondition
        ParseMappingAttributes(symbol, model);

        // Parse converter attributes (class level and method level)
        ParseConverterAttributes(symbol, model);

        // Validate BeforeMap/AfterMap signatures if specified
        var validationError = ValidateCallbackMethods(symbol, model, syntax);
        if (validationError is not null)
        {
            return Results.Error<MapperMethodModel>(validationError);
        }

        // Validate global condition method if specified
        var conditionError = ValidateConditionMethod(symbol, model, syntax);
        if (conditionError is not null)
        {
            return Results.Error<MapperMethodModel>(conditionError);
        }

        // Get source and destination properties
        var sourceType = symbol.Parameters[0].Type;

        BuildPropertyMappings(sourceType, destinationType, model);

        // Detect specialized converter methods for property mappings
        DetectSpecializedConverterMethods(model, symbol);

        // Validate Converter methods if specified
        var converterError = ValidateConverterMethods(symbol, model, syntax);
        if (converterError is not null)
        {
            return Results.Error<MapperMethodModel>(converterError);
        }

        // Validate Property Condition methods if specified
        var propertyConditionError = ValidatePropertyConditionMethods(symbol, model, syntax);
        if (propertyConditionError is not null)
        {
            return Results.Error<MapperMethodModel>(propertyConditionError);
        }

        // Build constant mappings with type information
        BuildConstantMappings(destinationType, model);

        // Validate and build MapUsing mappings
        var mapUsingError = ValidateAndBuildMapUsingMappings(symbol, model, sourceType, destinationType, syntax);
        if (mapUsingError is not null)
        {
            return Results.Error<MapperMethodModel>(mapUsingError);
        }

        // Validate and build MapFrom mappings
        var mapFromError = ValidateAndBuildMapFromMappings(symbol, model, sourceType, destinationType, syntax);
        if (mapFromError is not null)
        {
            return Results.Error<MapperMethodModel>(mapFromError);
        }

        // Validate and build MapCollection mappings
        var mapCollectionError = ValidateAndBuildMapCollectionMappings(symbol, model, sourceType, destinationType, syntax);
        if (mapCollectionError is not null)
        {
            return Results.Error<MapperMethodModel>(mapCollectionError);
        }

        // Validate and build MapNested mappings
        var mapNestedError = ValidateAndBuildMapNestedMappings(symbol, model, sourceType, destinationType, syntax);
        if (mapNestedError is not null)
        {
            return Results.Error<MapperMethodModel>(mapNestedError);
        }

        return Results.Success(model);
    }

    private static DiagnosticInfo? ValidateConditionMethod(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        if (string.IsNullOrEmpty(model.ConditionMethod))
        {
            return null;
        }

        var containingType = mapperMethod.ContainingType;
        var conditionMethods = containingType.GetMembers(model.ConditionMethod!)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.ReturnType.SpecialType == SpecialType.System_Boolean)
            .ToList();

        var matchResult = FindMatchingCallbackMethod(conditionMethods, model);
        if (matchResult == CallbackMatchResult.NoMatch)
        {
            return new DiagnosticInfo(
                Diagnostics.InvalidConditionSignature,
                syntax.GetLocation(),
                $"{model.ConditionMethod}, {mapperMethod.Name}");
        }

        model.ConditionAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams;
        return null;
    }

    private static DiagnosticInfo? ValidatePropertyConditionMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        foreach (var mapping in model.PropertyMappings)
        {
            if (string.IsNullOrEmpty(mapping.ConditionMethod))
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

    private static ConverterMatchResult FindMatchingPropertyConditionMethod(List<IMethodSymbol> candidates, PropertyMappingModel mapping, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;
        var sourceType = mapping.SourceType;

        foreach (var method in candidates)
        {
            // Check for match with custom parameters: (SourceType, customParams...)
            if (model.CustomParameters.Count > 0 &&
                method.Parameters.Length == 1 + model.CustomParameters.Count)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                var customParamsMatch = true;
                for (var i = 0; i < model.CustomParameters.Count; i++)
                {
                    if (method.Parameters[i + 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != model.CustomParameters[i].TypeName)
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

            // Check for match without custom parameters: (SourceType)
            if (method.Parameters.Length == 1)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                if (sourceMatch)
                {
                    hasMatchWithoutCustomParams = true;
                }
            }
        }

        // Prefer match with custom parameters
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

    private static DiagnosticInfo? ValidateConverterMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        foreach (var mapping in model.PropertyMappings)
        {
            if (string.IsNullOrEmpty(mapping.ConverterMethod))
            {
                continue;
            }

            var converterMethods = containingType.GetMembers(mapping.ConverterMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingConverterMethod(converterMethods, mapping, model);
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

    private enum ConverterMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams
    }

    private static ConverterMatchResult FindMatchingConverterMethod(List<IMethodSymbol> candidates, PropertyMappingModel mapping, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;
        var sourceType = mapping.SourceType;

        foreach (var method in candidates)
        {
            // Return type must match target type (or be assignable)
            //var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Check for match with custom parameters: (SourceType, customParams...)
            if (model.CustomParameters.Count > 0 &&
                method.Parameters.Length == 1 + model.CustomParameters.Count)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                var customParamsMatch = true;
                for (var i = 0; i < model.CustomParameters.Count; i++)
                {
                    if (method.Parameters[i + 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != model.CustomParameters[i].TypeName)
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

            // Check for match without custom parameters: (SourceType)
            if (method.Parameters.Length == 1)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                if (sourceMatch)
                {
                    hasMatchWithoutCustomParams = true;
                }
            }
        }

        // Prefer match with custom parameters
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

    private static DiagnosticInfo? ValidateCallbackMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        // Validate BeforeMap
        if (!string.IsNullOrEmpty(model.BeforeMapMethod))
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

        // Validate AfterMap
        if (!string.IsNullOrEmpty(model.AfterMapMethod))
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

    private enum CallbackMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams
    }

    private static CallbackMatchResult FindMatchingCallbackMethod(List<IMethodSymbol> candidates, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;

        foreach (var method in candidates)
        {
            // Check for match with custom parameters: (Source, Destination, customParams...)
            if (model.CustomParameters.Count > 0 &&
                method.Parameters.Length == 2 + model.CustomParameters.Count)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.SourceTypeName;
                var destMatch = method.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.DestinationTypeName;

                var customParamsMatch = true;
                for (var i = 0; i < model.CustomParameters.Count; i++)
                {
                    if (method.Parameters[i + 2].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != model.CustomParameters[i].TypeName)
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

            // Check for match without custom parameters: (Source, Destination)
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

        // Prefer match with custom parameters
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


    private static void ParseMappingAttributes(IMethodSymbol symbol, MapperMethodModel model)
    {
        var definitionOrder = 0;

        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == MapperAttributeName)
            {
                // Check for AutoMap named argument
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if (namedArg.Key == "AutoMap" && namedArg.Value.Value is bool autoMap)
                    {
                        model.AutoMap = autoMap;
                    }
                }
            }
            else if (attributeName == MapPropertyAttributeName)
            {
                // MapProperty(target) or MapProperty(target, source)
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    string? sourceName = null;
                    string? converter = null;
                    var nullBehavior = NullBehaviorType.Default;
                    var order = 0;

                    // Second constructor argument is source (optional)
                    if (attribute.ConstructorArguments.Length >= 2)
                    {
                        sourceName = attribute.ConstructorArguments[1].Value?.ToString();
                    }

                    // Check for named arguments
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Source" && namedArg.Value.Value is string src)
                        {
                            sourceName = src;
                        }
                        else if (namedArg.Key == "Converter" && namedArg.Value.Value is string conv)
                        {
                            converter = conv;
                        }
                        else if (namedArg.Key == "NullBehavior" && namedArg.Value.Value is int nb)
                        {
                            nullBehavior = (NullBehaviorType)nb;
                        }
                        else if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                    }

                    var mapping = new PropertyMappingModel
                    {
                        TargetName = targetName,
                        SourceName = sourceName ?? targetName,
                        ConverterMethod = converter,
                        NullBehavior = nullBehavior,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    };

                    model.PropertyMappings.Add(mapping);
                }
            }
            else if (attributeName == MapIgnoreAttributeName)
            {
                // MapIgnore(target)
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapConstantAttributeName ||
                     (attributeName is not null && attributeName.StartsWith("Smart.Mapper.MapConstantAttribute<", StringComparison.Ordinal)))
            {
                // MapConstant(target, value) or MapConstant<T>(target, value)
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

                    model.ConstantMappings.Add(constantMapping);
                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapExpressionAttributeName)
            {
                // MapExpression(target, expression)
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

                    model.ExpressionMappings.Add(new ExpressionMappingModel
                    {
                        TargetName = targetName,
                        Expression = expression,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == BeforeMapAttributeName)
            {
                // BeforeMap(method)
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    model.BeforeMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
            else if (attributeName == AfterMapAttributeName)
            {
                // AfterMap(method)
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    model.AfterMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
            else if (attributeName == MapConditionAttributeName)
            {
                // MapCondition(condition) or MapCondition(target, condition)
                if (attribute.ConstructorArguments.Length == 1)
                {
                    // Global condition
                    model.ConditionMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
                else if (attribute.ConstructorArguments.Length >= 2)
                {
                    // Property-level condition
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var conditionName = attribute.ConstructorArguments[1].Value?.ToString();
                    model.PropertyConditions[targetName] = conditionName;
                }
                else
                {
                    // Check named arguments for Target
                    string? target = null;
                    string? condition = null;
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Target" && namedArg.Value.Value is string t)
                        {
                            target = t;
                        }
                    }
                    if (attribute.ConstructorArguments.Length >= 1)
                    {
                        condition = attribute.ConstructorArguments[0].Value?.ToString();
                    }
                    if (target is not null && condition is not null)
                    {
                        model.PropertyConditions[target] = condition;
                    }
                    else if (condition is not null)
                    {
                        model.ConditionMethod = condition;
                    }
                }
            }
            else if (attributeName == MapUsingAttributeName)
            {
                // MapUsing(target, method)
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

                    model.MapUsingMappings.Add(new MapUsingModel
                    {
                        TargetName = targetName,
                        Method = methodName,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapFromAttributeName)
            {
                // MapFrom(target, from)
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var from = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    var order = 0;

                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                        {
                            order = ord;
                        }
                    }

                    model.MapFromMappings.Add(new MapFromModel
                    {
                        TargetName = targetName,
                        From = from,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapCollectionAttributeName)
            {
                // MapCollection(target) or MapCollection(target, source) with Mapper property
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

                    model.MapCollectionMappings.Add(new MapCollectionModel
                    {
                        TargetName = targetName,
                        SourceName = sourceName ?? targetName,
                        Mapper = mapper,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapNestedAttributeName)
            {
                // MapNested(target) or MapNested(target, source) with Mapper property
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

                    model.MapNestedMappings.Add(new MapNestedModel
                    {
                        TargetName = targetName,
                        SourceName = sourceName ?? targetName,
                        Mapper = mapper,
                        Order = order,
                        DefinitionOrder = definitionOrder++
                    });

                    model.IgnoredProperties.Add(targetName);
                }
            }
        }

        // Associate property conditions with mappings from MapProperty attributes
        foreach (var mapping in model.PropertyMappings)
        {
            if (model.PropertyConditions.TryGetValue(mapping.TargetPath, out var conditionMethod))
            {
                mapping.ConditionMethod = conditionMethod;
            }
        }
    }

    private static void ParseConverterAttributes(IMethodSymbol symbol, MapperMethodModel model)
    {
        // Check method level first (higher priority)
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == MapConverterAttributeName)
            {
                if (attribute.ConstructorArguments.Length >= 1 &&
                    attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType)
                {
                    model.MapConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    // Check for Method named argument
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
                if (attribute.ConstructorArguments.Length >= 1 &&
                    attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType)
                {
                    model.CollectionConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }

        // Check class level if not specified at method level
        var containingType = symbol.ContainingType;
        foreach (var attribute in containingType.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == MapConverterAttributeName && model.MapConverterTypeName is null)
            {
                if (attribute.ConstructorArguments.Length >= 1 &&
                    attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType)
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
            else if (attributeName == CollectionConverterAttributeName && model.CollectionConverterTypeName is null)
            {
                if (attribute.ConstructorArguments.Length >= 1 &&
                    attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType)
                {
                    model.CollectionConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }
    }

    private static string? FormatConstantValue(object? value)
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

    private static void BuildConstantMappings(ITypeSymbol destinationType, MapperMethodModel model)
    {
        var destinationProperties = GetAllProperties(destinationType);

        foreach (var constantMapping in model.ConstantMappings)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == constantMapping.TargetName);
            if (destProp is not null)
            {
                constantMapping.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
    }

    private static DiagnosticInfo? ValidateAndBuildMapUsingMappings(
        IMethodSymbol mapperMethod,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var destinationProperties = GetAllProperties(destinationType);

        foreach (var mapUsing in model.MapUsingMappings)
        {
            // Find target property type
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapUsing.TargetName);
            if (destProp is null)
            {
                continue; // Property not found, will be handled elsewhere
            }

            mapUsing.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Find the method in the containing type
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

    private enum MapUsingMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams,
        ReturnTypeMismatch
    }

    private readonly struct MapUsingMatchInfo
    {
        public MapUsingMatchResult Result { get; init; }
        public IMethodSymbol? MatchedMethod { get; init; }
        public string? ActualReturnType { get; init; }
    }

    private static MapUsingMatchInfo FindMatchingMapUsingMethod(
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

        foreach (var method in candidates)
        {
            // Check for match with custom parameters: (Source, customParams...)
            if (model.CustomParameters.Count > 0 &&
                method.Parameters.Length == 1 + model.CustomParameters.Count)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceTypeName;

                var customParamsMatch = true;
                for (var i = 0; i < model.CustomParameters.Count; i++)
                {
                    if (method.Parameters[i + 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != model.CustomParameters[i].TypeName)
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (sourceMatch && customParamsMatch)
                {
                    var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (returnType == targetTypeName || IsAssignableTo(method.ReturnType, targetType))
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

            // Check for match without custom parameters: (Source)
            if (method.Parameters.Length == 1)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceTypeName;

                if (sourceMatch)
                {
                    var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (returnType == targetTypeName || IsAssignableTo(method.ReturnType, targetType))
                    {
                        hasMatchWithoutCustomParams = true;
                        if (matchedMethod is null)
                        {
                            matchedMethod = method;
                        }
                    }
                    else
                    {
                        mismatchedReturnType = returnType;
                    }
                }
            }
        }

        // Prefer match with custom parameters
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

    private static bool IsAssignableTo(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        // Simple check for implicit conversion
        if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
        {
            return true;
        }

        // Check for nullable target
        if (targetType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            var nonNullableTarget = targetType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            if (SymbolEqualityComparer.Default.Equals(sourceType, nonNullableTarget))
            {
                return true;
            }
        }

        // Check inheritance
        var current = sourceType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, targetType))
            {
                return true;
            }
            current = current.BaseType;
        }

        // Check interfaces
        foreach (var iface in sourceType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, targetType))
            {
                return true;
            }
        }

        return false;
    }

    private static DiagnosticInfo? ValidateAndBuildMapFromMappings(
        IMethodSymbol mapperMethod,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var destinationProperties = GetAllProperties(destinationType);

        foreach (var mapFrom in model.MapFromMappings)
        {
            // Find target property type
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapFrom.TargetName);
            if (destProp is null)
            {
                continue; // Property not found, will be handled elsewhere
            }

            mapFrom.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Determine if it's a method call or property path
            // Method: GetCount, GetValue, etc. (no dots, or trailing "()")
            // Property path: Items.Length, Child.Name, etc. (with dots)
            var from = mapFrom.From;
            var isMethodCall = !from.Contains('.');

            // First try to find as method
            if (isMethodCall)
            {
                var sourceMethod = sourceType.GetMembers(from)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => !m.IsStatic && m.Parameters.Length == 0);

                if (sourceMethod is not null)
                {
                    // Check return type compatibility
                    var returnType = sourceMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var targetTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    if (returnType != targetTypeName && !IsAssignableTo(sourceMethod.ReturnType, destProp.Type))
                    {
                        return new DiagnosticInfo(
                            Diagnostics.MapFromMethodReturnTypeMismatch,
                            syntax.GetLocation(),
                            $"{targetTypeName}, {returnType}, {mapFrom.From} -> {mapFrom.TargetName}");
                    }

                    mapFrom.IsMethodCall = true;
                    mapFrom.ReturnType = returnType;
                    continue;
                }
            }

            // Try as property path
            var (resolvedType, isValid) = ResolvePropertyPath(sourceType, from);
            if (isValid && resolvedType is not null)
            {
                var returnType = resolvedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var targetTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (returnType != targetTypeName && !IsAssignableTo(resolvedType, destProp.Type))
                {
                    return new DiagnosticInfo(
                        Diagnostics.MapFromMethodReturnTypeMismatch,
                        syntax.GetLocation(),
                        $"{targetTypeName}, {returnType}, {mapFrom.From} -> {mapFrom.TargetName}");
                }

                mapFrom.IsMethodCall = false;
                mapFrom.ReturnType = returnType;
                continue;
            }

            // Neither method nor property path found
            return new DiagnosticInfo(
                Diagnostics.InvalidMapFromMethodSignature,
                syntax.GetLocation(),
                $"{mapFrom.From}, {mapFrom.TargetName}");
        }

        return null;
    }

    private static (ITypeSymbol? Type, bool IsValid) ResolvePropertyPath(ITypeSymbol rootType, string path)
    {
        var parts = path.Split('.');
        var currentType = rootType;

        foreach (var part in parts)
        {
            if (currentType is null)
            {
                return (null, false);
            }

            var properties = GetAllProperties(currentType);
            var prop = properties.FirstOrDefault(p => p.Name == part);
            if (prop is not null)
            {
                currentType = prop.Type;
                continue;
            }

            // Check for Length/Count on collections/arrays
            if (part == "Length" && currentType is IArrayTypeSymbol)
            {
                // Array.Length returns int
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

    private static DiagnosticInfo? ValidateAndBuildMapCollectionMappings(
        IMethodSymbol mapperMethod,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var sourceProperties = GetAllProperties(sourceType);
        var destinationProperties = GetAllProperties(destinationType);

        foreach (var mapCollection in model.MapCollectionMappings)
        {
            // Find source property
            var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == mapCollection.SourceName);
            if (sourceProp is null)
            {
                continue;
            }

            // Find target property
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapCollection.TargetName);
            if (destProp is null)
            {
                continue;
            }

            // Get element types
            var sourceElementType = GetCollectionElementType(sourceProp.Type);
            var targetElementType = GetCollectionElementType(destProp.Type);

            if (sourceElementType is null || targetElementType is null)
            {
                continue;
            }

            mapCollection.SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.SourceElementType = sourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.TargetElementType = targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapCollection.IsSourceNullable = IsNullableSymbol(sourceProp.Type);
            mapCollection.TargetIsArray = destProp.Type is IArrayTypeSymbol;

            // Find mapper method
            var mapperMethodResult = FindMapperMethod(containingType, mapCollection.Mapper, sourceElementType, targetElementType);
            if (mapperMethodResult is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapCollectionMapperMethod,
                    syntax.GetLocation(),
                    $"{mapCollection.Mapper}, {mapCollection.SourceName} -> {mapCollection.TargetName}");
            }

            mapCollection.MapperReturnsValue = mapperMethodResult.ReturnsValue;
        }

        return null;
    }

    private static DiagnosticInfo? ValidateAndBuildMapNestedMappings(
        IMethodSymbol mapperMethod,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var sourceProperties = GetAllProperties(sourceType);
        var destinationProperties = GetAllProperties(destinationType);

        foreach (var mapNested in model.MapNestedMappings)
        {
            // Find source property
            var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == mapNested.SourceName);
            if (sourceProp is null)
            {
                continue;
            }

            // Find target property
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == mapNested.TargetName);
            if (destProp is null)
            {
                continue;
            }

            mapNested.SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapNested.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            mapNested.IsSourceNullable = IsNullableSymbol(sourceProp.Type);

            // Get the underlying type for nullable reference types
            var sourceUnderlyingType = sourceProp.Type;
            if (sourceProp.Type.NullableAnnotation == NullableAnnotation.Annotated &&
                sourceProp.Type is INamedTypeSymbol namedType &&
                !namedType.IsValueType)
            {
                sourceUnderlyingType = namedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            var targetUnderlyingType = destProp.Type;
            if (destProp.Type.NullableAnnotation == NullableAnnotation.Annotated &&
                destProp.Type is INamedTypeSymbol namedDestType &&
                !namedDestType.IsValueType)
            {
                targetUnderlyingType = namedDestType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            // Find mapper method
            var mapperMethodResult = FindMapperMethod(containingType, mapNested.Mapper, sourceUnderlyingType, targetUnderlyingType);
            if (mapperMethodResult is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapNestedMapperMethod,
                    syntax.GetLocation(),
                    $"{mapNested.Mapper}, {mapNested.SourceName} -> {mapNested.TargetName}");
            }

            mapNested.MapperReturnsValue = mapperMethodResult.ReturnsValue;
        }

        return null;
    }

    private sealed class MapperMethodInfo
    {
        public bool ReturnsValue { get; set; }
    }

    private static MapperMethodInfo? FindMapperMethod(INamedTypeSymbol containingType, string methodName, ITypeSymbol sourceElementType, ITypeSymbol targetElementType)
    {
        var sourceTypeName = sourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetTypeName = targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var methods = containingType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic)
            .ToList();

        foreach (var method in methods)
        {
            // For partial methods, we need to check the definition
            var methodToCheck = method.PartialDefinitionPart ?? method;

            // Check for return value pattern: TargetType Method(SourceType source)
            if (methodToCheck.Parameters.Length == 1 &&
                !methodToCheck.ReturnsVoid)
            {
                var paramType = methodToCheck.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var returnType = methodToCheck.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (paramType == sourceTypeName && returnType == targetTypeName)
                {
                    return new MapperMethodInfo { ReturnsValue = true };
                }
            }

            // Check for void pattern: void Method(SourceType source, TargetType destination)
            if (methodToCheck.Parameters.Length == 2 &&
                methodToCheck.ReturnsVoid)
            {
                var sourceParamType = methodToCheck.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var destParamType = methodToCheck.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (sourceParamType == sourceTypeName && destParamType == targetTypeName)
                {
                    return new MapperMethodInfo { ReturnsValue = false };
                }
            }
        }

        return null;
    }

    private static ITypeSymbol? GetCollectionElementType(ITypeSymbol collectionType)
    {
        // Handle array types
        if (collectionType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        // Handle generic types like List<T>, IEnumerable<T>, ICollection<T>, etc.
        if (collectionType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            // Check if it implements IEnumerable<T>
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType &&
                    iface.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                {
                    return iface.TypeArguments[0];
                }
            }

            // Direct generic type like List<T>
            if (namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }
        }

        return null;
    }

    private static void BuildPropertyMappings(ITypeSymbol sourceType, ITypeSymbol destinationType, MapperMethodModel model)
    {
        var sourceProperties = GetAllProperties(sourceType);
        var destinationProperties = GetAllProperties(destinationType);

        // Separate custom mappings (with dot notation) from simple mappings
        var customMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        var nestedMappings = new List<PropertyMappingModel>();

        foreach (var mapping in model.PropertyMappings)
        {
            // Check if this is a nested mapping (contains dots)
            if (mapping.TargetPath.Contains('.') || mapping.SourcePath.Contains('.'))
            {
                // Resolve types for nested paths
                ResolveNestedMapping(mapping, sourceType, destinationType);
                nestedMappings.Add(mapping);
            }
            else
            {
                customMappings[mapping.TargetPath] = mapping.SourcePath;
            }
        }

        // Preserve original mappings with Converter info
        var originalMappings = model.PropertyMappings.ToDictionary(m => m.TargetPath, m => m);

        // Clear and rebuild property mappings
        var mappings = new List<PropertyMappingModel>();

        // Process simple (non-nested) destination properties
        foreach (var destProp in destinationProperties)
        {
            // Skip ignored properties
            if (model.IgnoredProperties.Contains(destProp.Name))
            {
                continue;
            }

            // Skip if there's a nested mapping for this target
            if (nestedMappings.Any(m => m.TargetPath.StartsWith(destProp.Name + ".", StringComparison.Ordinal) || m.TargetPath == destProp.Name))
            {
                continue;
            }

            // Skip read-only properties
            if (destProp.SetMethod is null)
            {
                continue;
            }

            string? sourcePropPath = null;
            ITypeSymbol? sourcePropertyType = null;
            string? converterMethod = null;
            string? conditionMethod = null;

            // Check for custom mapping first
            if (customMappings.TryGetValue(destProp.Name, out var customSourcePath))
            {
                sourcePropPath = customSourcePath;
                sourcePropertyType = ResolvePropertyType(sourceType, customSourcePath);

                // Preserve converter and condition info from original mapping
                if (originalMappings.TryGetValue(destProp.Name, out var originalMapping))
                {
                    converterMethod = originalMapping.ConverterMethod;
                    conditionMethod = originalMapping.ConditionMethod;
                }
            }
            else
            {
                // Try to find matching property by name (only if AutoMap is enabled)
                if (model.AutoMap)
                {
                    var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == destProp.Name);
                    if (sourceProp is not null)
                    {
                        sourcePropPath = sourceProp.Name;
                        sourcePropertyType = sourceProp.Type;

                        // Check for property condition from originalMappings
                        if (originalMappings.TryGetValue(destProp.Name, out var originalMapping))
                        {
                            conditionMethod = originalMapping.ConditionMethod;
                        }
                    }
                }
            }

            if (sourcePropPath is not null && sourcePropertyType is not null)
            {
                var sourceTypeName = sourcePropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var destTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var isSourceNullable = IsNullableSymbol(sourcePropertyType);
                var isTargetNullable = IsNullableSymbol(destProp.Type);

                // Get underlying types for nullable handling
                var sourceUnderlyingType = GetUnderlyingType(sourcePropertyType);
                var targetUnderlyingType = GetUnderlyingType(destProp.Type);
                var sourceUnderlyingTypeName = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var targetUnderlyingTypeName = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Get Order and DefinitionOrder from original mapping if exists
                var order = 0;
                var definitionOrder = 0;
                if (originalMappings.TryGetValue(destProp.Name, out var origMapping))
                {
                    order = origMapping.Order;
                    definitionOrder = origMapping.DefinitionOrder;
                }

                var mapping = new PropertyMappingModel
                {
                    SourcePath = sourcePropPath,
                    TargetPath = destProp.Name,
                    SourceType = sourceTypeName,
                    TargetType = destTypeName,
                    SourceUnderlyingType = sourceUnderlyingTypeName,
                    TargetUnderlyingType = targetUnderlyingTypeName,
                    RequiresConversion = RequiresTypeConversion(sourceUnderlyingTypeName, targetUnderlyingTypeName, isSourceNullable, isTargetNullable),
                    IsSourceNullable = isSourceNullable,
                    IsTargetNullable = isTargetNullable,
                    ConverterMethod = converterMethod,
                    ConditionMethod = conditionMethod,
                    Order = order,
                    DefinitionOrder = definitionOrder
                };

                mappings.Add(mapping);
            }
        }

        // Add nested mappings
        mappings.AddRange(nestedMappings);

        model.PropertyMappings = mappings;

        // Apply property conditions from MapPropertyCondition attributes
        foreach (var mapping in model.PropertyMappings)
        {
            if (model.PropertyConditions.TryGetValue(mapping.TargetPath, out var conditionMethod))
            {
                mapping.ConditionMethod = conditionMethod;
            }
        }
    }

    private static ITypeSymbol GetUnderlyingType(ITypeSymbol type)
    {
        // For Nullable<T> value types, return T
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        // For nullable reference types, return the underlying type
        if (type.NullableAnnotation == NullableAnnotation.Annotated &&
            type is INamedTypeSymbol refType)
        {
            return refType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        // Not nullable, return as-is
        return type;
    }

    private static bool IsNullableSymbol(ITypeSymbol type)
    {
        // Check for nullable reference type
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        // Check for Nullable<T> value type
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }


        return false;
    }

    private static void ResolveNestedMapping(PropertyMappingModel mapping, ITypeSymbol sourceType, ITypeSymbol destinationType)
    {
        // Resolve source path segments and check for nullable intermediate types
        var sourceParts = mapping.SourcePath.Split('.');
        if (sourceParts.Length > 1)
        {
            var currentType = sourceType;
            var pathBuilder = new List<string>();

            // Process all but the last segment
            for (var i = 0; i < sourceParts.Length - 1; i++)
            {
                var part = sourceParts[i];
                pathBuilder.Add(part);

                var prop = GetAllProperties(currentType).FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    var isNullable = IsNullableSymbol(prop.Type);
                    mapping.SourcePathSegments.Add(new NestedPathSegment
                    {
                        Path = string.Join(".", pathBuilder),
                        TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        IsNullable = isNullable
                    });
                    currentType = prop.Type;
                }
            }

            // Get the final property type
            var finalSourceProp = GetAllProperties(currentType).FirstOrDefault(p => p.Name == sourceParts[sourceParts.Length - 1]);
            if (finalSourceProp is not null)
            {
                mapping.SourceType = finalSourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsSourceNullable = IsNullableSymbol(finalSourceProp.Type);
                var sourceUnderlyingType = GetUnderlyingType(finalSourceProp.Type);
                mapping.SourceUnderlyingType = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            // Simple source path
            var sourceProp = GetAllProperties(sourceType).FirstOrDefault(p => p.Name == mapping.SourcePath);
            if (sourceProp is not null)
            {
                mapping.SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsSourceNullable = IsNullableSymbol(sourceProp.Type);
                var sourceUnderlyingType = GetUnderlyingType(sourceProp.Type);
                mapping.SourceUnderlyingType = sourceUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        // Resolve target type and build path segments for auto-instantiation
        var targetParts = mapping.TargetPath.Split('.');
        if (targetParts.Length > 1)
        {
            var currentTargetType = destinationType;
            var pathBuilder = new List<string>();

            // Process all but the last segment (which is the actual property to set)
            for (var i = 0; i < targetParts.Length - 1; i++)
            {
                var part = targetParts[i];
                pathBuilder.Add(part);

                var prop = GetAllProperties(currentTargetType).FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    mapping.TargetPathSegments.Add(new NestedPathSegment
                    {
                        Path = string.Join(".", pathBuilder),
                        TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    });
                    currentTargetType = prop.Type;
                }
            }

            // Get the final property type
            var finalProp = GetAllProperties(currentTargetType).FirstOrDefault(p => p.Name == targetParts[targetParts.Length - 1]);
            if (finalProp is not null)
            {
                mapping.TargetType = finalProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsTargetNullable = IsNullableSymbol(finalProp.Type);
                var targetUnderlyingType = GetUnderlyingType(finalProp.Type);
                mapping.TargetUnderlyingType = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            // Simple target, just get its type
            var destProp = GetAllProperties(destinationType).FirstOrDefault(p => p.Name == mapping.TargetPath);
            if (destProp is not null)
            {
                mapping.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsTargetNullable = IsNullableSymbol(destProp.Type);
                var targetUnderlyingType = GetUnderlyingType(destProp.Type);
                mapping.TargetUnderlyingType = targetUnderlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        // Determine if conversion is needed
        // Use underlying types for comparison (without nullable wrapper)
        if (!string.IsNullOrEmpty(mapping.SourceUnderlyingType) && !string.IsNullOrEmpty(mapping.TargetUnderlyingType))
        {
            mapping.RequiresConversion = RequiresTypeConversion(mapping.SourceUnderlyingType, mapping.TargetUnderlyingType, mapping.IsSourceNullable, mapping.IsTargetNullable);
        }
        else if (!string.IsNullOrEmpty(mapping.SourceType) && !string.IsNullOrEmpty(mapping.TargetType))
        {
            mapping.RequiresConversion = RequiresTypeConversion(mapping.SourceType, mapping.TargetType, mapping.IsSourceNullable, mapping.IsTargetNullable);
        }
    }

    private static bool RequiresTypeConversion(string sourceType, string targetType, bool isSourceNullable, bool isTargetNullable)
    {
        // Normalize type names
        var normalizedSource = NormalizeTypeName(sourceType);
        var normalizedTarget = NormalizeTypeName(targetType);

        // Same type - no conversion needed
        if (normalizedSource == normalizedTarget)
        {
            return false;
        }

        // Check for implicit numeric widening conversions (no explicit conversion needed)
        if (IsImplicitNumericConversion(normalizedSource, normalizedTarget))
        {
            return false;
        }

        // All other cases require conversion
        return true;
    }

    private static bool IsImplicitNumericConversion(string sourceType, string targetType)
    {
        // Implicit numeric conversions in C#
        // sbyte -> short, int, long, float, double, decimal
        // byte -> short, ushort, int, uint, long, ulong, float, double, decimal
        // short -> int, long, float, double, decimal
        // ushort -> int, uint, long, ulong, float, double, decimal
        // int -> long, float, double, decimal
        // uint -> long, ulong, float, double, decimal
        // long -> float, double, decimal
        // ulong -> float, double, decimal
        // float -> double
        // char -> ushort, int, uint, long, ulong, float, double, decimal

        return sourceType switch
        {
            "sbyte" or "SByte" => targetType is "short" or "Int16" or "int" or "Int32" or "long" or "Int64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "byte" or "Byte" => targetType is "short" or "Int16" or "ushort" or "UInt16"
                or "int" or "Int32" or "uint" or "UInt32" or "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "short" or "Int16" => targetType is "int" or "Int32" or "long" or "Int64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "ushort" or "UInt16" => targetType is "int" or "Int32" or "uint" or "UInt32"
                or "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "int" or "Int32" => targetType is "long" or "Int64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "uint" or "UInt32" => targetType is "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "long" or "Int64" => targetType is "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "ulong" or "UInt64" => targetType is "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "float" or "Single" => targetType is "double" or "Double",
            "char" or "Char" => targetType is "ushort" or "UInt16" or "int" or "Int32" or "uint" or "UInt32"
                or "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            _ => false
        };
    }

    private static string NormalizeTypeName(string typeName)
    {
        // Remove global:: prefix and System. prefix for comparison
        var normalized = typeName
            .Replace("global::", string.Empty)
            .Replace("System.", string.Empty);

        // Handle nullable types
        if (normalized.EndsWith("?", StringComparison.Ordinal))
        {
            normalized = normalized.TrimEnd('?');
        }
        if (normalized.StartsWith("Nullable<", StringComparison.Ordinal) && normalized.EndsWith(">", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(9, normalized.Length - 10);
        }

        return normalized;
    }

    private static ITypeSymbol? ResolvePropertyType(ITypeSymbol type, string path)
    {
        var parts = path.Split('.');
        var currentType = type;

        foreach (var part in parts)
        {
            var prop = GetAllProperties(currentType).FirstOrDefault(p => p.Name == part);
            if (prop is null)
            {
                return null;
            }
            currentType = prop.Type;
        }

        return currentType;
    }

    private static List<IPropertySymbol> GetAllProperties(ITypeSymbol type)
    {
        var properties = new List<IPropertySymbol>();
        var currentType = type;

        while (currentType is not null)
        {
            properties.AddRange(currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public));

            currentType = currentType.BaseType;
        }

        return properties;
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static void Execute(SourceProductionContext context, ImmutableArray<Result<MapperMethodModel>> methods)
    {
        foreach (var info in methods.SelectError())
        {
            context.ReportDiagnostic(info);
        }

        var builder = new SourceBuilder();
        foreach (var group in methods.SelectValue().GroupBy(static x => new { x.Namespace, x.ClassName }))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            builder.Clear();
            BuildSource(builder, group.ToList());

            var filename = MakeFilename(group.Key.Namespace, group.Key.ClassName);
            var source = builder.ToString();
            context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static void BuildSource(SourceBuilder builder, List<MapperMethodModel> methods)
    {
        var ns = methods[0].Namespace;
        var className = methods[0].ClassName;
        var isValueType = methods[0].IsValueType;

        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // namespace
        if (!String.IsNullOrEmpty(ns))
        {
            builder.Namespace(ns);
            builder.NewLine();
        }

        // class
        builder
            .Indent()
            .Append("partial ")
            .Append(isValueType ? "struct " : "class ")
            .Append(className)
            .NewLine();
        builder.BeginScope();

        var first = true;
        foreach (var method in methods)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                builder.NewLine();
            }

            BuildMethod(builder, method);
        }

        builder.EndScope();
    }

    private static void BuildMethod(SourceBuilder builder, MapperMethodModel method)
    {
        // Method signature
        builder.Indent().Append(method.MethodAccessibility.ToText()).Append(" static partial ");

        if (method.ReturnsDestination)
        {
            // Destination Map(Source source, ...customParams)
            builder.Append(method.DestinationTypeName).Append(" ");
            builder.Append(method.MethodName).Append("(");
            builder.Append(method.SourceTypeName).Append(" ").Append(method.SourceParameterName);

            // Add custom parameters
            foreach (var customParam in method.CustomParameters)
            {
                builder.Append(", ").Append(customParam.TypeName).Append(" ").Append(customParam.Name);
            }

            builder.Append(")").NewLine();
        }
        else
        {
            // void Map(Source source, Destination destination, ...customParams)
            builder.Append("void ");
            builder.Append(method.MethodName).Append("(");
            builder.Append(method.SourceTypeName).Append(" ").Append(method.SourceParameterName).Append(", ");
            builder.Append(method.DestinationTypeName).Append(" ").Append(method.DestinationParameterName!);

            // Add custom parameters
            foreach (var customParam in method.CustomParameters)
            {
                builder.Append(", ").Append(customParam.TypeName).Append(" ").Append(customParam.Name);
            }

            builder.Append(")").NewLine();
        }

        builder.BeginScope();

        var destVarName = method.ReturnsDestination ? "destination" : method.DestinationParameterName!;

        // Create destination if returning
        if (method.ReturnsDestination)
        {
            builder.Indent().Append("var ").Append(destVarName).Append(" = new ").Append(method.DestinationTypeName).Append("();").NewLine();
        }

        // Global condition check
        var hasGlobalCondition = !string.IsNullOrEmpty(method.ConditionMethod);
        if (hasGlobalCondition)
        {
            builder.Indent().Append("if (").Append(method.ConditionMethod!).Append("(").Append(method.SourceParameterName).Append(", ").Append(destVarName);
            if (method.ConditionAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }
            builder.Append("))").NewLine();
            builder.BeginScope();
        }

        // Call BeforeMap if specified
        if (!string.IsNullOrEmpty(method.BeforeMapMethod))
        {
            builder.Indent().Append(method.BeforeMapMethod!).Append("(").Append(method.SourceParameterName).Append(", ").Append(destVarName);
            if (method.BeforeMapAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }
            builder.Append(");").NewLine();
        }

        // Collect all nested paths that need auto-instantiation (excluding those with nullable source paths)
        var nestedPathsToInstantiate = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in method.PropertyMappings)
        {
            // Skip auto-instantiation if source has nullable path segments
            if (mapping.SourcePathSegments.Any(s => s.IsNullable))
            {
                continue;
            }

            foreach (var segment in mapping.TargetPathSegments)
            {
                if (!nestedPathsToInstantiate.ContainsKey(segment.Path))
                {
                    nestedPathsToInstantiate[segment.Path] = segment.TypeName;
                }
            }
        }

        // Generate auto-instantiation for nested paths (sorted by path length to ensure parent before child)
        foreach (var kvp in nestedPathsToInstantiate.OrderBy(x => x.Key.Count(c => c == '.')))
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(kvp.Key).Append(" ??= new ").Append(kvp.Value).Append("();").NewLine();
        }

        // Group mappings by whether they require null check (sorted by Order, then DefinitionOrder)
        var sortedMappings = method.PropertyMappings.OrderBy(m => m.Order).ThenBy(m => m.DefinitionOrder).ToList();
        var mappingsWithoutNullCheck = sortedMappings.Where(m => !m.RequiresNullCheck).ToList();
        var mappingsWithNullCheck = sortedMappings.Where(m => m.RequiresNullCheck).ToList();

        // Generate property mappings without null check
        foreach (var mapping in mappingsWithoutNullCheck)
        {
            BuildPropertyAssignment(builder, mapping, method.SourceParameterName, destVarName, method);
        }

        // Generate property mappings with null check (grouped by source null check condition)
        var groupedByNullCheck = mappingsWithNullCheck
            .GroupBy(m => GetNullCheckCondition(m, method.SourceParameterName))
            .Where(g => !string.IsNullOrEmpty(g.Key));

        foreach (var group in groupedByNullCheck)
        {
            builder.Indent().Append("if (").Append(group.Key).Append(")").NewLine();
            builder.BeginScope();

            // Generate auto-instantiation for these mappings' target paths
            var groupNestedPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var mapping in group)
            {
                foreach (var segment in mapping.TargetPathSegments)
                {
                    if (!groupNestedPaths.ContainsKey(segment.Path))
                    {
                        groupNestedPaths[segment.Path] = segment.TypeName;
                    }
                }
            }

            foreach (var kvp in groupNestedPaths.OrderBy(x => x.Key.Count(c => c == '.')))
            {
                builder.Indent();
                builder.Append(destVarName).Append(".").Append(kvp.Key).Append(" ??= new ").Append(kvp.Value).Append("();").NewLine();
            }

            foreach (var mapping in group)
            {
                BuildPropertyAssignment(builder, mapping, method.SourceParameterName, destVarName, method, nullChecked: true);
            }

            builder.EndScope();
        }

        // Generate constant mappings (sorted by Order, then DefinitionOrder)
        foreach (var constant in method.ConstantMappings.OrderBy(c => c.Order).ThenBy(c => c.DefinitionOrder))
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(constant.TargetName).Append(" = ");
            builder.Append(constant.Value ?? "null");
            builder.Append(";").NewLine();
        }

        // Generate expression mappings (sorted by Order, then DefinitionOrder)
        foreach (var expression in method.ExpressionMappings.OrderBy(e => e.Order).ThenBy(e => e.DefinitionOrder))
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(expression.TargetName).Append(" = ");
            builder.Append(expression.Expression);
            builder.Append(";").NewLine();
        }

        // Generate MapUsing mappings (call method in containing class, sorted by Order, then DefinitionOrder)
        foreach (var mapUsing in method.MapUsingMappings.OrderBy(m => m.Order).ThenBy(m => m.DefinitionOrder))
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(mapUsing.TargetName).Append(" = ");
            builder.Append(mapUsing.Method).Append("(").Append(method.SourceParameterName);
            if (mapUsing.AcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }
            builder.Append(");").NewLine();
        }

        // Generate MapFrom mappings (method call or property path on source, sorted by Order, then DefinitionOrder)
        foreach (var mapFrom in method.MapFromMappings.OrderBy(m => m.Order).ThenBy(m => m.DefinitionOrder))
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(mapFrom.TargetName).Append(" = ");
            builder.Append(method.SourceParameterName).Append(".");
            if (mapFrom.IsMethodCall)
            {
                builder.Append(mapFrom.From).Append("()");
            }
            else
            {
                builder.Append(mapFrom.From);
            }
            builder.Append(";").NewLine();
        }

        // Generate MapNested mappings (call mapper method for nested objects, sorted by Order, then DefinitionOrder)
        foreach (var mapNested in method.MapNestedMappings.OrderBy(m => m.Order).ThenBy(m => m.DefinitionOrder))
        {
            var sourceAccess = $"{method.SourceParameterName}.{mapNested.SourceName}";

            builder.Indent();
            builder.Append(destVarName).Append(".").Append(mapNested.TargetName).Append(" = ");

            if (mapNested.IsSourceNullable)
            {
                // source.Child is not null ? MapChild(source.Child) : default!
                builder.Append(sourceAccess).Append(" is not null ? ");
            }


            if (mapNested.MapperReturnsValue)
            {
                // Return value pattern: MapChild(source.Child)
                builder.Append(mapNested.Mapper).Append("(").Append(sourceAccess);
                if (mapNested.IsSourceNullable)
                {
                    builder.Append("!");
                }
                builder.Append(")");
            }
            else
            {
                // Void pattern: create new instance, call mapper, return instance
                // Using inline lambda for simplicity
                builder.Append("((global::System.Func<").Append(mapNested.TargetType).Append(">)(() => { var __nested = new ").Append(mapNested.TargetType).Append("(); ");
                builder.Append(mapNested.Mapper).Append("(").Append(sourceAccess);
                if (mapNested.IsSourceNullable)
                {
                    builder.Append("!");
                }
                builder.Append(", __nested); return __nested; }))()");
            }

            if (mapNested.IsSourceNullable)
            {
                builder.Append(" : default!");
            }

            builder.Append(";").NewLine();
        }

        // Generate MapCollection mappings using collection converter (sorted by Order, then DefinitionOrder)
        var collectionConverterTypeName = method.CollectionConverterTypeName ?? DefaultCollectionConverterTypeName;
        foreach (var mapCollection in method.MapCollectionMappings.OrderBy(m => m.Order).ThenBy(m => m.DefinitionOrder))
        {
            var sourceAccess = $"{method.SourceParameterName}.{mapCollection.SourceName}";

            builder.Indent();
            builder.Append(destVarName).Append(".").Append(mapCollection.TargetName).Append(" = ");

            // Use collection converter
            builder.Append(collectionConverterTypeName).Append(".");

            // Choose ToArray or ToList based on target type
            if (mapCollection.TargetIsArray)
            {
                builder.Append("ToArray");
            }
            else
            {
                builder.Append("ToList");
            }

            // Add type parameters
            builder.Append("<")
                   .Append(mapCollection.SourceElementType)
                   .Append(", ")
                   .Append(mapCollection.TargetElementType)
                   .Append(">(");

            // Source collection
            builder.Append(sourceAccess).Append(", ");

            // Mapper method reference
            builder.Append(mapCollection.Mapper);

            builder.Append(")!");  // null-forgiving for null handling

            builder.Append(";").NewLine();
        }

        // Call AfterMap if specified
        if (!string.IsNullOrEmpty(method.AfterMapMethod))
        {
            builder.Indent().Append(method.AfterMapMethod!).Append("(").Append(method.SourceParameterName).Append(", ").Append(destVarName);
            if (method.AfterMapAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }
            builder.Append(");").NewLine();
        }

        // Close global condition scope if present
        if (hasGlobalCondition)
        {
            builder.EndScope();
        }

        // Return destination if needed
        if (method.ReturnsDestination)
        {
            builder.Indent().Append("return ").Append(destVarName).Append(";").NewLine();
        }


        builder.EndScope();
    }

    private static void BuildPropertyAssignment(SourceBuilder builder, PropertyMappingModel mapping, string sourceParamName, string destVarName, MapperMethodModel method, bool nullChecked = false)
    {
        // Property-level condition check
        if (mapping.HasCondition)
        {
            var sourceAccessor = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);
            builder.Indent().Append("if (").Append(mapping.ConditionMethod!).Append("(").Append(sourceAccessor);
            if (mapping.ConditionAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }
            builder.Append("))").NewLine();
            builder.BeginScope();
        }

        // Handle nullable source with type conversion
        // When source is nullable (e.g., int?) and conversion is needed (e.g., int? -> string),
        // we need to check for null first, then convert the underlying value
        if (mapping.IsSourceNullable && mapping.RequiresConversion && !mapping.HasConverter)
        {
            BuildNullableSourceConversion(builder, mapping, sourceParamName, destVarName, method, nullChecked);
        }
        else
        {
            // Standard assignment
            BuildStandardAssignment(builder, mapping, sourceParamName, destVarName, method, nullChecked);
        }

        // Close property-level condition scope if present
        if (mapping.HasCondition)
        {
            builder.EndScope();
        }
    }

    private static void BuildStandardAssignment(SourceBuilder builder, PropertyMappingModel mapping, string sourceParamName, string destVarName, MapperMethodModel method, bool nullChecked)
    {
        builder.Indent();
        builder.Append(destVarName).Append(".").Append(mapping.TargetPath).Append(" = ");

        // Use custom converter if specified
        if (mapping.HasConverter)
        {
            var sourceAccessor = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);
            builder.Append(mapping.ConverterMethod!).Append("(").Append(sourceAccessor);

            // Add custom parameters if converter accepts them
            if (mapping.ConverterAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }

            builder.Append(")");
        }
        else if (mapping.RequiresConversion)
        {
            BuildTypeConversion(builder, mapping, sourceParamName, nullChecked, method);
        }
        else
        {
            var sourceAccessor = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);
            builder.Append(sourceAccessor);

            // For nullable to non-nullable terminal element, add null-forgiving operator
            if (mapping.RequiresNullCoalescing)
            {
                builder.Append("!");
            }
        }

        builder.Append(";").NewLine();
    }

    private static void BuildNullableSourceConversion(SourceBuilder builder, PropertyMappingModel mapping, string sourceParamName, string destVarName, MapperMethodModel method, bool nullChecked)
    {
        var sourceAccessor = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);

        // Check if source is a Nullable<T> value type (int?, etc.)
        var isNullableValueType = mapping.SourceType.Contains("?") ||
                                   mapping.SourceType.Contains("Nullable<");

        if (mapping.NullBehavior == NullBehaviorType.Skip)
        {
            // Skip when null
            builder.Indent().Append("if (").Append(sourceAccessor).Append(" is not null)").NewLine();
            builder.BeginScope();
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(mapping.TargetPath).Append(" = ");

            // For Nullable<T> value types, access .Value to get the underlying type
            if (isNullableValueType)
            {
                BuildTypeConversionWithValueAccess(builder, mapping, sourceAccessor, method);
            }
            else
            {
                BuildTypeConversion(builder, mapping, sourceParamName, nullChecked, method);
            }

            builder.Append(";").NewLine();
            builder.EndScope();
        }
        else
        {
            // Default behavior: convert if not null, otherwise use default
            if (mapping.IsTargetNullable)
            {
                // Target is nullable, use conditional expression
                builder.Indent();
                builder.Append(destVarName).Append(".").Append(mapping.TargetPath).Append(" = ");
                builder.Append(sourceAccessor).Append(" is not null ? ");

                if (isNullableValueType)
                {
                    BuildTypeConversionWithValueAccess(builder, mapping, sourceAccessor, method);
                }
                else
                {
                    BuildTypeConversion(builder, mapping, sourceParamName, nullChecked, method);
                }

                builder.Append(" : default;").NewLine();
            }
            else
            {
                // Target is non-nullable - need to handle null case
                // Generate: destination.Prop = source.NullableProp is not null
                //              ? Converter.ConvertToXxx(source.NullableProp.Value)
                //              : default!;
                builder.Indent();
                builder.Append(destVarName).Append(".").Append(mapping.TargetPath).Append(" = ");
                builder.Append(sourceAccessor).Append(" is not null ? ");

                if (isNullableValueType)
                {
                    BuildTypeConversionWithValueAccess(builder, mapping, sourceAccessor, method);
                }
                else
                {
                    BuildTypeConversion(builder, mapping, sourceParamName, nullChecked, method);
                }

                builder.Append(" : default!;").NewLine();
            }
        }
    }

    private static void BuildTypeConversionWithValueAccess(SourceBuilder builder, PropertyMappingModel mapping, string sourceAccessor, MapperMethodModel method)
    {
        var converterTypeName = method.MapConverterTypeName ?? DefaultValueConverterTypeName;

        if (mapping.HasSpecializedConverter)
        {
            // Use specialized converter method with .Value access
            builder.Append(converterTypeName)
                   .Append(".")
                   .Append(mapping.SpecializedConverterMethod!)
                   .Append("(")
                   .Append(sourceAccessor)
                   .Append(".Value)");
        }
        else
        {
            // Use generic converter with underlying types and .Value access
            var converterMethodName = method.MapConverterMethodName;
            var sourceTypeForConversion = !string.IsNullOrEmpty(mapping.SourceUnderlyingType) ? mapping.SourceUnderlyingType : mapping.SourceType;
            var targetTypeForConversion = !string.IsNullOrEmpty(mapping.TargetUnderlyingType) ? mapping.TargetUnderlyingType : mapping.TargetType;

            builder.Append(converterTypeName)
                   .Append(".").Append(converterMethodName).Append("<")
                   .Append(sourceTypeForConversion)
                   .Append(", ")
                   .Append(targetTypeForConversion)
                   .Append(">(")
                   .Append(sourceAccessor)
                   .Append(".Value)");
        }
    }

    private static string GetNullCheckCondition(PropertyMappingModel mapping, string sourceParamName)
    {
        var conditions = new List<string>();

        // Check for nullable source path segments (intermediate elements only)
        if (mapping.SourcePathSegments.Count > 0)
        {
            var pathBuilder = new StringBuilder();
            pathBuilder.Append(sourceParamName);

            foreach (var segment in mapping.SourcePathSegments)
            {
                pathBuilder.Append('.').Append(segment.Path.Split('.').Last());
                if (segment.IsNullable)
                {
                    conditions.Add($"{pathBuilder} is not null");
                }
            }
        }

        // Note: Terminal nullable to non-nullable is handled by RequiresNullCoalescing, not here

        return string.Join(" && ", conditions);
    }

    private static string BuildSourceAccessor(string sourcePath, string sourceParamName, bool nullChecked = false)
    {
        // For simple paths, just return the accessor
        if (!sourcePath.Contains('.'))
        {
            return $"{sourceParamName}.{sourcePath}";
        }

        var parts = sourcePath.Split('.');
        var result = new StringBuilder();
        result.Append(sourceParamName);

        for (var i = 0; i < parts.Length; i++)
        {
            result.Append('.');
            result.Append(parts[i]);

            // Add null-forgiving operator for intermediate segments only if not null checked
            if (i < parts.Length - 1 && !nullChecked)
            {
                result.Append('!');
            }
        }

        return result.ToString();
    }

    private static void BuildTypeConversion(SourceBuilder builder, PropertyMappingModel mapping, string sourceParamName, bool nullChecked, MapperMethodModel method)
    {
        var sourceExpr = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);
        var converterTypeName = method.MapConverterTypeName ?? DefaultValueConverterTypeName;

        // Check if specialized converter method should be used
        if (mapping.HasSpecializedConverter)
        {
            // Use specialized converter method: Converter.ConvertToInt32(source.Value)
            builder.Append(converterTypeName)
                   .Append(".")
                   .Append(mapping.SpecializedConverterMethod!)
                   .Append("(")
                   .Append(sourceExpr)
                   .Append(")");
        }
        else
        {
            // Use generic converter for type conversion
            // Use underlying types (without nullable wrapper) for the conversion
            // DefaultValueConverter.Convert<TSourceUnderlying, TDestUnderlying>(source.Value)
            var converterMethodName = method.MapConverterMethodName;
            var sourceTypeForConversion = !string.IsNullOrEmpty(mapping.SourceUnderlyingType) ? mapping.SourceUnderlyingType : mapping.SourceType;
            var targetTypeForConversion = !string.IsNullOrEmpty(mapping.TargetUnderlyingType) ? mapping.TargetUnderlyingType : mapping.TargetType;

            builder.Append(converterTypeName)
                   .Append(".").Append(converterMethodName).Append("<")
                   .Append(sourceTypeForConversion)
                   .Append(", ")
                   .Append(targetTypeForConversion)
                   .Append(">(")
                   .Append(sourceExpr)
                   .Append(")");
        }
    }

    private static void DetectSpecializedConverterMethods(MapperMethodModel model, IMethodSymbol mapperMethod)
    {
        // Get the converter type to check for specialized methods
        ITypeSymbol? converterType = null;

        if (model.MapConverterTypeName is not null)
        {
            // Custom converter - find the type
            converterType = FindConverterType(mapperMethod, model.MapConverterTypeName);
        }
        else
        {
            // DefaultValueConverter - find from compilation
            converterType = mapperMethod.ContainingAssembly
                .GetTypeByMetadataName("Smart.Mapper.DefaultValueConverter");
        }

        if (converterType is null)
        {
            return;
        }

        var methodPrefix = model.MapConverterMethodName; // e.g., "Convert"

        foreach (var mapping in model.PropertyMappings)
        {
            if (!mapping.RequiresConversion)
            {
                continue;
            }

            // Try to find specialized method using underlying types
            // e.g., for int? -> string, look for ConvertToString with int parameter
            var sourceTypeForLookup = !string.IsNullOrEmpty(mapping.SourceUnderlyingType) ? mapping.SourceUnderlyingType : mapping.SourceType;
            var targetTypeForLookup = !string.IsNullOrEmpty(mapping.TargetUnderlyingType) ? mapping.TargetUnderlyingType : mapping.TargetType;

            var targetSimpleName = GetSimpleTypeName(targetTypeForLookup);
            var specializedMethodName = $"{methodPrefix}To{targetSimpleName}";

            var specializedMethod = FindSpecializedMethod(
                converterType,
                specializedMethodName,
                sourceTypeForLookup,
                targetTypeForLookup);

            if (specializedMethod is not null)
            {
                mapping.SpecializedConverterMethod = specializedMethodName;
            }
        }
    }

    private static ITypeSymbol? FindConverterType(IMethodSymbol mapperMethod, string converterTypeName)
    {
        // Remove "global::" prefix if present
        var typeName = converterTypeName;
        if (typeName.StartsWith("global::", StringComparison.Ordinal))
        {
            typeName = typeName.Substring("global::".Length);
        }

        // Try to find in the containing assembly first
        var type = mapperMethod.ContainingAssembly.GetTypeByMetadataName(typeName);
        if (type is not null)
        {
            return type;
        }

        // Try to find in referenced assemblies
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

    private static IMethodSymbol? FindSpecializedMethod(
        ITypeSymbol converterType,
        string methodName,
        string sourceType,
        string targetType)
    {
        // Get the simple source type name for parameter matching
        var sourceSimpleName = GetSimpleTypeName(sourceType);

        var methods = converterType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.Parameters.Length == 1)
            .ToList();

        foreach (var method in methods)
        {
            var paramType = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Check if parameter type matches source type
            // and return type matches target type
            if (paramType == sourceType && returnType == targetType)
            {
                return method;
            }
        }

        return null;
    }

    private static string GetSimpleTypeName(string fullyQualifiedTypeName)
    {
        // Extract simple type name from fully qualified name
        // e.g., "global::System.Int32" -> "Int32", "int" -> "Int32", "string" -> "String"

        // Handle aliases
        var typeName = fullyQualifiedTypeName switch
        {
            "int" => "Int32",
            "global::System.Int32" => "Int32",
            "long" => "Int64",
            "global::System.Int64" => "Int64",
            "short" => "Int16",
            "global::System.Int16" => "Int16",
            "byte" => "Byte",
            "global::System.Byte" => "Byte",
            "sbyte" => "SByte",
            "global::System.SByte" => "SByte",
            "uint" => "UInt32",
            "global::System.UInt32" => "UInt32",
            "ulong" => "UInt64",
            "global::System.UInt64" => "UInt64",
            "ushort" => "UInt16",
            "global::System.UInt16" => "UInt16",
            "float" => "Single",
            "global::System.Single" => "Single",
            "double" => "Double",
            "global::System.Double" => "Double",
            "decimal" => "Decimal",
            "global::System.Decimal" => "Decimal",
            "bool" => "Boolean",
            "global::System.Boolean" => "Boolean",
            "string" => "String",
            "global::System.String" => "String",
            "global::System.DateTime" => "DateTime",
            "global::System.Guid" => "Guid",
            _ => ExtractLastSegment(fullyQualifiedTypeName)
        };

        return typeName;
    }

    private static string ExtractLastSegment(string fullyQualifiedTypeName)
    {
        // Remove "global::" prefix if present
        var name = fullyQualifiedTypeName;
        if (name.StartsWith("global::", StringComparison.Ordinal))
        {
            name = name.Substring("global::".Length);
        }

        // Get the last segment after the last dot
        var lastDot = name.LastIndexOf('.');
        if (lastDot >= 0)
        {
            return name.Substring(lastDot + 1);
        }

        return name;
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string MakeFilename(string ns, string className)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(className.Replace('<', '[').Replace('>', ']'));
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}
