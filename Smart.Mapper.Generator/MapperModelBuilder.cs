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

    // The namespace as its names, without the @ of a keyword: it names the generated file, which cannot have
    // one, and the generated code escapes the names itself
    private static readonly SymbolDisplayFormat NamespaceNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    internal static Result<MapperMethodModel> BuildModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not IMethodSymbol symbol)
        {
            return Results.Errors<MapperMethodModel>();
        }

        // The generated code declares the containing types again, outermost first, so each of them has to be
        // partial, or that declaration would not compile (CS0260)
        var typeChain = GetContainingTypes(symbol.ContainingType);
        if (!symbol.IsStatic || !symbol.IsPartialDefinition || !typeChain.All(IsPartialType))
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
            : containingType.ContainingNamespace.ToDisplayString(NamespaceNameFormat);

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
            destinationParameterName = IdentifierHelper.Escape(destParam.Name);
            returnsDestination = false;
            destinationType = destParam.Type;
            customParamStartIndex = 2;
        }
        else
        {
            // A nullable struct is returned as the struct it holds, which the mapper creates and fills, as new T?()
            // makes null
            destinationType = symbol.ReturnType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableReturn
                ? nullableReturn.TypeArguments[0]
                : symbol.ReturnType;
            destinationTypeName = destinationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            destinationParameterName = null;
            returnsDestination = true;
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
        // allows neither, and a struct destination has to come by ref: the members of one passed by readonly
        // reference cannot be assigned, and one passed by value is a copy, whose members the caller never sees.
        for (var i = 0; i < symbol.Parameters.Length; i++)
        {
            var parameter = symbol.Parameters[i];
            var isUnwritableStructDestination = symbol.ReturnsVoid && (i == 1) && parameter.Type.IsValueType &&
                                                (parameter.RefKind is RefKind.None or RefKind.In or RefKind.RefReadOnlyParameter);
            if ((parameter.RefKind == RefKind.Out) || isUnwritableStructDestination)
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                    Diagnostics.UnsupportedParameterModifier,
                    syntax.ParameterList.Parameters[i].GetLocation(),
                    symbol.Name,
                    parameter.Name,
                    GetRefKindKeyword(parameter.RefKind)));
            }
        }

        // A nullable struct has none of the members of the struct it holds (only HasValue and Value), so from such
        // a source nothing would be mapped
        if (sourceParam.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                Diagnostics.NullableValueTypeSource,
                syntax.ParameterList.Parameters[0].GetLocation(),
                symbol.Name,
                sourceParam.Name,
                sourceParam.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        var customParameters = new List<CustomParameterModel>();
        for (var i = customParamStartIndex; i < symbol.Parameters.Length; i++)
        {
            var param = symbol.Parameters[i];
            customParameters.Add(new CustomParameterModel(
                IdentifierHelper.Escape(param.Name),
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

        // The destination as the method declares it: the return type, a nullable struct as it is, or the type of the
        // destination parameter
        var declaredDestinationType = returnsDestination ? symbol.ReturnType : destinationType;
        var model = new MapperMethodModel(
            Namespace: ns,
            ClassName: String.Join(".", typeChain.Select(GetTypeName)),
            TypeDeclarations: new EquatableArray<string>(typeChain.Select(static t => t.GetDeclarationKeyword() + " " + GetTypeDeclarationName(t)).ToArray()),
            MethodAccessibility: symbol.DeclaredAccessibility,
            MethodName: symbol.Name,
            TypeParameterList: GetTypeParameterList(symbol),
            ConstraintClauses: GetConstraintClauses(symbol),
            SourceTypeName: sourceParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            SourceParameterName: IdentifierHelper.Escape(sourceParam.Name),
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
            DestinationDeclaredTypeName: declaredDestinationType.ToDisplayString(NullableQualifiedFormat),
            IsDestinationParameterNullable: !returnsDestination && IsNullableReference(destinationType),
            DestinationNonNullableTypeName: GetNonNullableTypeName(destinationType),
            DefaultReturnValue: declaredDestinationType.IsValueType || IsNullableReference(declaredDestinationType) ? "default" : "default!",
            ReturnsDestination: returnsDestination,
            CustomParameters: new EquatableArray<CustomParameterModel>(customParameters));

        model = ParseMappingAttributes(symbol, model);

        // The profile gives the name comparison when the method does not, so it is read before the names
        // written in the attributes are resolved under that comparison
        model = ParseConverterAttributes(symbol, model);

        // Runs before the duplicate check so that two attributes naming the same member with
        // different casing are recognised as the duplicate they are.
        model = CanonicalizeTargetNames(model, destinationType, containingType, context.SemanticModel.Compilation);

        // The constructor a return mapper calls goes by the values the mapping has for its parameters, so it is
        // chosen here, from the attributes as written and the source, before any stage reads it
        var sourceType = symbol.Parameters[0].Type;
        var compilation = context.SemanticModel.Compilation;
        model = model with
        {
            ConstructorIndex = GetConstructorIndex(destinationType, SelectConstructor(model, sourceType, destinationType, containingType, compilation))
        };

        var duplicateTargetError = ValidateDuplicateTargets(model, syntax);
        if (duplicateTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(duplicateTargetError);
        }

        var ignoredOrConditionTargetError = ValidateIgnoredAndConditionTargets(model, destinationType, containingType, context.SemanticModel.Compilation, syntax);
        if (ignoredOrConditionTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(ignoredOrConditionTargetError);
        }

        var constructorAssignedPathError = ValidateConstructorAssignedPaths(model, destinationType, containingType, context.SemanticModel.Compilation, syntax);
        if (constructorAssignedPathError is not null)
        {
            return Results.Error<MapperMethodModel>(constructorAssignedPathError);
        }

        var validationError = ValidateCallbackMethods(symbol, sourceType, destinationType, compilation, ref model, syntax);
        if (validationError is not null)
        {
            return Results.Error<MapperMethodModel>(validationError);
        }

        var explicitMappingError = ValidateExplicitPropertyMappings(ref model, sourceType, destinationType, containingType, compilation, syntax);
        if (explicitMappingError is not null)
        {
            return Results.Error<MapperMethodModel>(explicitMappingError);
        }

        model = BuildPropertyMappings(sourceType, destinationType, containingType, compilation, model);

        // Runs before the detection passes below so that mappings synthesized for constructor
        // parameters are analysed alongside the ones built from destination properties.
        var constructorError = BuildConstructorParameterMappings(ref model, destinationType, sourceType, containingType, compilation, syntax);
        if (constructorError is not null)
        {
            return Results.Error<MapperMethodModel>(constructorError);
        }

        var expressionTargetError = ValidateExpressionAssignedTargets(model, syntax);
        if (expressionTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(expressionTargetError);
        }

        var conditionTargetError = ValidateConditionTargetsMapped(model, syntax);
        if (conditionTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(conditionTargetError);
        }

        model = model with
        {
            PropertyMappings = AnalyzeConversions(
                model.PropertyMappings,
                symbol,
                sourceType,
                destinationType,
                GetEffectiveConstructor(model, destinationType),
                model.ConstructorParameters,
                model.MapConverterTypeName,
                model.MapConverterMethodName,
                compilation)
        };

        var converterError = ValidateConverterMethods(symbol, sourceType, destinationType, compilation, ref model, syntax);
        if (converterError is not null)
        {
            return Results.Error<MapperMethodModel>(converterError);
        }

        var valueConverterError = ValidateValueConverterMethods(symbol, compilation, ref model, syntax);
        if (valueConverterError is not null)
        {
            return Results.Error<MapperMethodModel>(valueConverterError);
        }

        var propertyConditionError = ValidatePropertyConditionMethods(symbol, sourceType, compilation, ref model, syntax);
        if (propertyConditionError is not null)
        {
            return Results.Error<MapperMethodModel>(propertyConditionError);
        }

        model = BuildConstantMappings(destinationType, containingType, compilation, model);

        var mapUsingError = ValidateAndBuildMapUsingMappings(symbol, compilation, ref model, sourceType, destinationType, syntax);
        if (mapUsingError is not null)
        {
            return Results.Error<MapperMethodModel>(mapUsingError);
        }

        // A feature mapping assigned in the object initializer (an init-only or required member, or a dotted path
        // only an initializer reaches) makes a return mapper construct through one, even when nothing else needs it
        if (model.ReturnsDestination &&
            (model.ConstantMappings.Any(static c => c.IsTargetInitOnly || c.IsTargetRequired) ||
             model.ExpressionMappings.Any(static e => e.IsTargetInitOnly || e.IsTargetRequired) ||
             model.MapUsingMappings.Any(static u => u.IsTargetInitOnly || u.IsTargetRequired)))
        {
            model = model with { UseConstructorMapping = true };
        }

        var mapFromError = ValidateAndBuildMapFromMappings(ref model, sourceType, destinationType, containingType, compilation, syntax);
        if (mapFromError is not null)
        {
            return Results.Error<MapperMethodModel>(mapFromError);
        }

        var assignedTargetError = ValidateAssignedTargets(model, destinationType, containingType, compilation, syntax);
        if (assignedTargetError is not null)
        {
            return Results.Error<MapperMethodModel>(assignedTargetError);
        }

        var mapCollectionError = ValidateAndBuildMapCollectionMappings(symbol, compilation, ref model, sourceType, destinationType, syntax);
        if (mapCollectionError is not null)
        {
            return Results.Error<MapperMethodModel>(mapCollectionError);
        }

        var mapNestedError = ValidateAndBuildMapNestedMappings(symbol, compilation, ref model, sourceType, destinationType, syntax);
        if (mapNestedError is not null)
        {
            return Results.Error<MapperMethodModel>(mapNestedError);
        }

        var warnings = new List<DiagnosticInfo>();
        if (model.Strict)
        {
            warnings.AddRange(CollectStrictModeWarnings(model, destinationType, containingType, compilation, syntax));
        }

        warnings.AddRange(CollectMapExpressionReflectionWarnings(model, syntax));

        model = model with { Warnings = new(warnings) };

        var voidInitOnlyError = ValidateVoidMapperInitOnlyTargets(model, syntax);
        if (voidInitOnlyError is not null)
        {
            return Results.Error<MapperMethodModel>(voidInitOnlyError);
        }

        var requiredMemberError = ValidateRequiredMembers(ref model, destinationType, containingType, compilation, syntax);
        if (requiredMemberError is not null)
        {
            return Results.Error<MapperMethodModel>(requiredMemberError);
        }

        var cultureFormatError = ValidateCultureAndFormat(model, syntax);
        if (cultureFormatError is not null)
        {
            return Results.Error<MapperMethodModel>(cultureFormatError);
        }

        var typeConverterError = ValidateNoTypeConverterFallback(ref model, sourceType, destinationType, containingType, compilation, syntax);
        if (typeConverterError is not null)
        {
            return Results.Error<MapperMethodModel>(typeConverterError);
        }

        // Runs last, once the conversions that decide where a NullValue is written are settled
        var constantValueError = ValidateConstantValues(model, context.SemanticModel, destinationType, containingType, syntax);
        if (constantValueError is not null)
        {
            return Results.Error<MapperMethodModel>(constantValueError);
        }

        return Results.Success(model);
    }

    // The type declaring the mapper method and the types containing it, outermost first.
    private static List<INamedTypeSymbol> GetContainingTypes(INamedTypeSymbol type)
    {
        var types = new List<INamedTypeSymbol>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            types.Insert(0, current);
        }

        return types;
    }

    // Whether every declaration of the type is partial, so that the generated code can add one.
    private static bool IsPartialType(INamedTypeSymbol type) =>
        (type.DeclaringSyntaxReferences.Length > 0) &&
        type.DeclaringSyntaxReferences.All(static r => r.GetSyntax() is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

    // The name of a type with its type parameters, as declared (Mappers<TKey, TValue>).
    private static string GetTypeName(INamedTypeSymbol type) =>
        type.TypeParameters.Length == 0 ? type.Name : type.Name + "<" + String.Join(", ", type.TypeParameters.Select(static p => p.Name)) + ">";

    // The same as the generated type declaration writes it, the names escaped.
    private static string GetTypeDeclarationName(INamedTypeSymbol type) =>
        type.TypeParameters.Length == 0
            ? IdentifierHelper.EscapeTypeName(type.Name)
            : IdentifierHelper.EscapeTypeName(type.Name) + "<" + String.Join(", ", type.TypeParameters.Select(static p => IdentifierHelper.EscapeTypeName(p.Name))) + ">";

    // The type parameters of a generic method as declared (<TKey, TValue>), the names escaped.
    private static string GetTypeParameterList(IMethodSymbol method) =>
        method.TypeParameters.Length == 0
            ? string.Empty
            : "<" + String.Join(", ", method.TypeParameters.Select(static p => IdentifierHelper.EscapeTypeName(p.Name))) + ">";

    // The constraints of the type parameters of a generic method, as the implementing declaration of a partial
    // method has to repeat them: the primary constraint first, then the types, new() and allows ref struct, with
    // the nullable annotations as declared (CS8667 otherwise).
    private static string GetConstraintClauses(IMethodSymbol method)
    {
        var clauses = new List<string>();
        foreach (var parameter in method.TypeParameters)
        {
            var constraints = new List<string>();
            if (parameter.HasReferenceTypeConstraint)
            {
                constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
            }
            else if (parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (parameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (parameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(parameter.ConstraintTypes.Select(static t => t.ToDisplayString(NullableQualifiedFormat)));
            if (parameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (parameter.AllowsRefLikeType)
            {
                constraints.Add("allows ref struct");
            }

            if (constraints.Count > 0)
            {
                clauses.Add(" where " + IdentifierHelper.EscapeTypeName(parameter.Name) + " : " + String.Join(", ", constraints));
            }
        }

        return String.Concat(clauses);
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
        _ => "none"
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

    // The type a property mapping assigns: the type of the target member, or for a constructor argument, of the
    // parameter, as a parameter the target names when no member has its name as well.
    private static ITypeSymbol? GetMappingTargetType(
        PropertyMappingModel mapping,
        MapperMethodModel model,
        ITypeSymbol destinationType,
        IMethodSymbol? constructor,
        INamedTypeSymbol within,
        Compilation compilation) =>
        (mapping.IsConstructorParameter ? FindArgumentParameter(constructor, model.ConstructorParameters, mapping.TargetPath)?.Type : null) ??
        PropertyPathHelper.ResolvePropertySymbol(destinationType, mapping.TargetPath.Split('.'), within, compilation)?.Type ??
        constructor?.Parameters.FirstOrDefault(p => p.Name == mapping.TargetPath)?.Type;

    // Whether the parameter of a converter or a condition taking the source value does not take null: a reference
    // annotated as not null without [AllowNull], or one with [DisallowNull], which a nullable source is not passed to
    // while it is null (CS8604 otherwise). One with nullable annotations disabled takes null. For a value going to it
    // by a user-defined conversion, the parameter of the conversion operator is taken the same way.
    private static bool RejectsNull(IMethodSymbol method, ITypeSymbol? valueType, Compilation compilation) =>
        !TakesNull(method.Parameters[0], method.Parameters[0].Type) ||
        ((valueType is not null) &&
         (compilation.ClassifyCommonConversion(valueType, method.Parameters[0].Type) is { IsUserDefined: true, MethodSymbol: { } conversion }) &&
         !TakesNull(conversion.Parameters[0], conversion.Parameters[0].Type));

    // The condition returns bool. The overloads of other return types are matched as well, as the call binds among them
    // all, and one it binds to that does not return bool does not match.
    private static DiagnosticInfo? ValidatePropertyConditionMethods(
        IMethodSymbol mapperMethod,
        ITypeSymbol sourceType,
        Compilation compilation,
        ref MapperMethodModel model,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var customTypes = GetCustomParameterTypes(mapperMethod, model);

        var resolved = new List<PropertyMappingModel>(model.PropertyMappings.Count);
        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.ConditionMethod))
            {
                resolved.Add(mapping);
                continue;
            }

            var valueType = GetSourceValueType(mapping, sourceType, containingType, compilation);
            var match = MatchValueMethod(
                LookupStaticMethods(containingType, mapping.ConditionMethod!, compilation),
                new ValueCall(valueType, mapping.SourceType, ArgumentKind.Value, customTypes),
                model.CustomParameters,
                static m => m.ReturnType.SpecialType == SpecialType.System_Boolean,
                compilation);
            if (match.Method is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidPropertyConditionSignature,
                    LocationOf(model, ConditionAttributeIndex(model, mapping.TargetPath), syntax),
                    mapperMethod.Name,
                    mapping.ConditionMethod!,
                    mapping.TargetPath);
            }

            var unwraps = match.Match is ValueMatch.Unwrap or ValueMatch.UnwrapConvert;
            resolved.Add(mapping with
            {
                ConditionAcceptsCustomParameters = match.Result == ConverterMatchResult.MatchWithCustomParams,
                ConditionParameterRefKinds = GetParameterRefKinds(match.Method),
                ConditionUnwrapsSource = unwraps,
                ConditionRejectsNull = unwraps || RejectsNull(match.Method, valueType, compilation)
            });
        }

        model = model with { PropertyMappings = new(resolved) };
        return null;
    }

    // The type of the source member a converter or a condition takes, from its symbol, as the conversions to the
    // parameter are classified with it; null when it is not found, which leaves only the exact match by name.
    private static ITypeSymbol? GetSourceValueType(PropertyMappingModel mapping, ITypeSymbol sourceType, INamedTypeSymbol within, Compilation compilation) =>
        PropertyPathHelper.ResolvePropertySymbol(sourceType, mapping.SourcePath.Split('.'), within, compilation, readable: true)?.Type;

    // The types of the custom parameters of the mapper, its last parameters
    private static List<ITypeSymbol> GetCustomParameterTypes(IMethodSymbol mapperMethod, MapperMethodModel model) =>
        mapperMethod.Parameters.Skip(mapperMethod.Parameters.Length - model.CustomParameters.Count).Select(static p => p.Type).ToList();

    // How the source value goes to the parameter of a method taking it: as its own type (the nullable annotations of
    // references aside), by an implicit conversion (Convert), or, from a nullable struct, as the value it holds
    // (Unwrap), or as a type the value it holds converts to implicitly (UnwrapConvert), which the generated code passes
    // after a null check.
    internal enum ValueMatch
    {
        None,
        Exact,
        Convert,
        Unwrap,
        UnwrapConvert
    }

    // The method a converter, a condition or a [MapUsing] method call binds to and how the value goes to it, or for a
    // return type mismatch, the method the call binds to all the same.
    internal readonly record struct ValueMethodMatch(ConverterMatchResult Result, IMethodSymbol? Method, ValueMatch Match, IMethodSymbol? Mismatched);

    // Matches the methods of the name taking the source value, then the mapper's custom parameters when they take them,
    // and returning what the target takes (returnsTarget), as the generated call (call) binds: one taking the custom
    // parameters over one without, as before, each arity bound on its own (BindValueCall). The custom parameters go to
    // parameters of their own types. The methods are the static methods the name is looked up as (LookupStaticMethods),
    // which the call binds among.
    private static ValueMethodMatch MatchValueMethod(
        IEnumerable<IMethodSymbol> methods,
        ValueCall call,
        EquatableArray<CustomParameterModel> customParams,
        Func<IMethodSymbol, bool> returnsTarget,
        Compilation compilation)
    {
        var candidates = methods
            .Select(static m => m.PartialDefinitionPart ?? m)
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .Where(static m => m.IsStatic && (m.Parameters.Length > 0))
            .Select(m => (Method: m, Match: MatchValueParameter(m.Parameters[0], call.ValueType, call.ValueTypeName, call.Argument, compilation)))
            .ToList();

        var (withMethod, withMatch, withMismatched) = customParams.Count > 0
            ? BindValueCall(candidates, call, 1 + customParams.Count, m => TakesCustomArguments(m, customParams, 1), returnsTarget, compilation)
            : default;
        if (withMethod is not null)
        {
            return new ValueMethodMatch(ConverterMatchResult.MatchWithCustomParams, withMethod, withMatch, null);
        }

        var (withoutMethod, withoutMatch, withoutMismatched) = BindValueCall(candidates, call, 1, static _ => true, returnsTarget, compilation);
        if (withoutMethod is not null)
        {
            return new ValueMethodMatch(ConverterMatchResult.MatchWithoutCustomParams, withoutMethod, withoutMatch, null);
        }

        var mismatched = withMismatched ?? withoutMismatched;
        return mismatched is not null
            ? new ValueMethodMatch(ConverterMatchResult.ReturnTypeMismatch, null, ValueMatch.None, mismatched)
            : new ValueMethodMatch(ConverterMatchResult.NoMatch, null, ValueMatch.None, null);
    }

    // The value parameter of a candidate: of the type of the value, when its modifier can take the argument (as
    // before); the struct a nullable struct holds, taken by value or by in; and, by value, a type the value converts to
    // implicitly, as the assignment of a result does (IsImplicitlyConvertible): a base class or an interface, boxing to
    // object or an interface, a wider number, a nullable struct, or a user-defined implicit conversion; or else a type
    // the value a nullable struct holds converts to that way (long or double for an int?). A parameter by in takes the
    // type of the value, or of the value a nullable struct holds, only.
    private static ValueMatch MatchValueParameter(IParameterSymbol parameter, ITypeSymbol? valueType, string valueTypeName, ArgumentKind argument, Compilation compilation)
    {
        if (parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == valueTypeName)
        {
            return CanTakeArgument(parameter.RefKind, argument) ? ValueMatch.Exact : ValueMatch.None;
        }

        if (valueType is null)
        {
            return ValueMatch.None;
        }

        var held = GetNullableValueType(valueType);
        if ((parameter.RefKind is RefKind.None or RefKind.In) && (held is not null) && IsSameType(held, parameter.Type))
        {
            return ValueMatch.Unwrap;
        }

        if (parameter.RefKind != RefKind.None)
        {
            return ValueMatch.None;
        }

        if (IsImplicitlyConvertible(valueType, parameter.Type, compilation))
        {
            return ValueMatch.Convert;
        }

        return (held is not null) && IsImplicitlyConvertible(held, parameter.Type, compilation) ? ValueMatch.UnwrapConvert : ValueMatch.None;
    }

    // Whether the parameters from the index take the mapper's custom parameters (the first count of them, all by
    // default), each as its own type.
    private static bool TakesCustomArguments(IMethodSymbol method, EquatableArray<CustomParameterModel> customParams, int firstIndex, int count = -1)
    {
        for (var i = 0; i < (count < 0 ? customParams.Count : count); i++)
        {
            if (!TakesArgument(method.Parameters[firstIndex + i], customParams[i].TypeName, GetVariableKind(customParams[i].RefKind)))
            {
                return false;
            }
        }

        return true;
    }

    // What the call of a converter, a condition or a [MapUsing] method passes: the value, of the type (its symbol, null
    // when it is not found, which leaves only the exact match by name) and how (a value, or a variable the call passes
    // as the method takes it), and the custom parameters, of their types.
    internal sealed record ValueCall(ITypeSymbol? ValueType, string ValueTypeName, ArgumentKind Argument, IReadOnlyList<ITypeSymbol> CustomTypes);

    // The method a call with that many arguments binds to. The candidates matched are the methods taking that many, not
    // generic ones, the custom parameters as their own types (takesCustomArguments). A candidate taking the value as
    // its own type goes first: of those, one callable returning what the target takes, taking every argument by value
    // first, as before, a mismatch when none returns it, and nothing when none is callable (obsolete as an error). The
    // call binds to the one chosen, as C# binds it among all the methods the name is looked up as (BindCall), or else
    // to another candidate, which is used instead (a method of a derived class hides those of its base classes, and one
    // taking by value goes before one taking by in). Otherwise the call passes the struct a nullable struct holds to
    // one taking it, or the value to one taking it by an implicit conversion, and the candidate is the one it binds to,
    // the custom parameters passed as the candidate takes them. A call binding to none of them, as an ambiguous one
    // (CS0121), one binding to a method not matched (a generic one, one taking the arguments with optional parameters
    // or params, one taking the value to a parameter by in with a conversion, or by a conversion obsolete as an error),
    // or to one obsolete as an error, matches nothing. A mismatch is the method the call binds to that does not return
    // what the target takes, which another candidate does not stand in for.
    private static (IMethodSymbol? Method, ValueMatch Match, IMethodSymbol? Mismatched) BindValueCall(
        List<(IMethodSymbol Method, ValueMatch Match)> candidates,
        ValueCall call,
        int count,
        Func<IMethodSymbol, bool> takesCustomArguments,
        Func<IMethodSymbol, bool> returnsTarget,
        Compilation compilation)
    {
        var applicable = candidates
            .Where(x => (x.Match != ValueMatch.None) && !x.Method.IsGenericMethod && (x.Method.Parameters.Length == count) && takesCustomArguments(x.Method))
            .ToList();
        var methods = candidates.Select(static x => x.Method).ToList();

        (IMethodSymbol? Method, ValueMatch Match, IMethodSymbol? Mismatched) Decide(IMethodSymbol method, ValueMatch match) =>
            method.GetObsoleteKind() == ObsoleteKind.Error
                ? (null, ValueMatch.None, null)
                : returnsTarget(method) ? (method, match, null) : (null, ValueMatch.None, method);

        bool BindsTo(IMethodSymbol method, ValueMatch match) =>
            SymbolEqualityComparer.Default.Equals(BindCall(methods, GetValueCallArguments(method, match, call, count), compilation), method);

        var exact = applicable.Where(static x => x.Match == ValueMatch.Exact).ToList();
        if (exact.Count > 0)
        {
            var callable = exact.Where(static x => x.Method.GetObsoleteKind() != ObsoleteKind.Error).ToList();
            if (callable.Count == 0)
            {
                return (null, ValueMatch.None, null);
            }

            IMethodSymbol? chosen = null;
            foreach (var (method, _) in callable)
            {
                if (returnsTarget(method))
                {
                    chosen = PreferByValue(chosen, method);
                }
            }

            if (chosen is null)
            {
                return (null, ValueMatch.None, callable[0].Method);
            }

            if ((call.ValueType is null) || BindsTo(chosen, ValueMatch.Exact))
            {
                return (chosen, ValueMatch.Exact, null);
            }

            var bound = BindCall(methods, GetValueCallArguments(chosen, ValueMatch.Exact, call, count), compilation);
            var (other, otherMatch) = applicable.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Method, bound));
            return (other is not null) && BindsTo(other, otherMatch) ? Decide(other, otherMatch) : (null, ValueMatch.None, null);
        }

        // The value a nullable struct holds to the struct first, then the value by a conversion, then the value a
        // nullable struct holds by a conversion
        foreach (var (method, match) in applicable.OrderBy(static x => x.Match switch { ValueMatch.Unwrap => 0, ValueMatch.Convert => 1, _ => 2 }))
        {
            if (BindsTo(method, match))
            {
                return Decide(method, match);
            }
        }

        return (null, ValueMatch.None, null);
    }

    // The arguments the call passes to a candidate: the value, or the value a nullable struct holds, as a value, or for
    // a variable with the modifier of the parameter taking it, and the custom parameters as the candidate takes them.
    private static List<CallArgument> GetValueCallArguments(IMethodSymbol method, ValueMatch match, ValueCall call, int count)
    {
        var unwraps = match is ValueMatch.Unwrap or ValueMatch.UnwrapConvert;
        var arguments = new List<CallArgument>
        {
            new(
                unwraps ? GetNullableValueType(call.ValueType!)! : call.ValueType!,
                unwraps || (call.Argument == ArgumentKind.Value) ? RefKind.None : GetArgumentModifierKind(method.Parameters[0].RefKind))
        };
        for (var i = 1; i < count; i++)
        {
            arguments.Add(new CallArgument(call.CustomTypes[i - 1], GetArgumentModifierKind(method.Parameters[i].RefKind)));
        }

        return arguments;
    }

    // An argument of a call the generated code makes: its type, and the modifier it goes with (None for a value, or for
    // a variable passed without one; In or Ref).
    internal readonly record struct CallArgument(ITypeSymbol Type, RefKind Modifier);

    // A method taking the arguments of a call: the type each argument goes to (for a generic method, the one the call
    // infers) and how, whether params takes the last ones (Expanded), and whether optional parameters are left to their
    // defaults.
    private sealed class CallCandidate
    {
        public CallCandidate(IMethodSymbol method, ITypeSymbol[] parameterTypes, RefKind[] refKinds, bool expanded, bool usesDefaults)
        {
            Method = method;
            ParameterTypes = parameterTypes;
            RefKinds = refKinds;
            Expanded = expanded;
            UsesDefaults = usesDefaults;
        }

        public IMethodSymbol Method { get; }

        public ITypeSymbol[] ParameterTypes { get; }

        public RefKind[] RefKinds { get; }

        public bool Expanded { get; }

        public bool UsesDefaults { get; }
    }

    // The modifier the generated code passes a variable to a parameter with, as GetArgumentModifier writes it.
    private static RefKind GetArgumentModifierKind(RefKind refKind) => refKind switch
    {
        RefKind.Ref => RefKind.Ref,
        RefKind.In or RefKind.RefReadOnlyParameter => RefKind.In,
        _ => RefKind.None
    };

    // The method a call of the static methods with the arguments binds to, as C# binds it, or null when
    // none takes them or the call is ambiguous (CS0121): the one taking them better than all the others (IsBetterCall).
    // The methods are those the name is looked up as (LookupStaticMethods), of which one of a derived class taking the
    // arguments leaves out those of its base classes. A method obsolete as an error takes part, as C# binds the call to
    // it and then reports it (CS0619). A generic method takes the arguments its type parameters are inferred from as
    // the types they are inferred as (InferParameterType), the constraints aside.
    private static IMethodSymbol? BindCall(IEnumerable<IMethodSymbol> methods, IReadOnlyList<CallArgument> arguments, Compilation compilation)
    {
        var taking = new List<CallCandidate>();
        foreach (var method in methods)
        {
            if (method.IsStatic &&
                ((ApplyCall(method, arguments, false, compilation) ?? ApplyCall(method, arguments, true, compilation)) is { } candidate))
            {
                taking.Add(candidate);
            }
        }

        var applicable = taking
            .Where(c => !taking.Any(o => DerivesFrom(o.Method.ContainingType, c.Method.ContainingType)))
            .ToList();
        var best = applicable
            .Where(c => applicable.All(o => ReferenceEquals(o, c) || IsBetterCall(c, o, arguments, compilation)))
            .ToList();
        return best.Count == 1 ? best[0].Method : null;
    }

    // Whether the type derives from the base type, through its base classes.
    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    // The static methods a call of the name in the mapper class binds among, as C# looks the simple name of a call up:
    // the members of the name the mapper class can access and the call can invoke, in it and its base classes, or, when
    // it has none, in the class containing it and its base classes, and so on outward, and last the methods the global
    // using static directives import (LookupGlobalStaticImports). The first of these classes having such a member is
    // the only one looked in, even when its methods do not take the arguments; a member the call cannot invoke (a
    // property or a field not of a delegate type, or a nested type) is passed over, as C# passes it over for a call. A
    // member of a derived class hides one of a base class of the same signature, and every member of a base class when
    // it is not a method. Instance methods are left out, as the static mapper cannot call them.
    internal static List<IMethodSymbol> LookupStaticMethods(INamedTypeSymbol mapperClass, string name, Compilation compilation)
    {
        for (var scope = mapperClass; scope is not null; scope = scope.ContainingType)
        {
            var members = new List<ISymbol>();
            for (var type = scope; type is not null; type = type.BaseType)
            {
                var declared = type.GetMembers(name)
                    .Where(m => IsInvocable(m) && compilation.IsSymbolAccessibleWithin(m, mapperClass) && !IsHiddenBy(m, members))
                    .ToList();
                members.AddRange(declared);
            }

            if (members.Count > 0)
            {
                return StaticMethodsOf(members);
            }
        }

        return LookupGlobalStaticImports(mapperClass, name, compilation);
    }

    // Whether a call can invoke the member: a method or an event, or a property or a field of a delegate type (or
    // dynamic).
    private static bool IsInvocable(ISymbol member) => member switch
    {
        IMethodSymbol or IEventSymbol => true,
        IPropertySymbol property => property.Type.TypeKind is TypeKind.Delegate or TypeKind.Dynamic,
        IFieldSymbol field => field.Type.TypeKind is TypeKind.Delegate or TypeKind.Dynamic,
        _ => false
    };

    private static List<IMethodSymbol> StaticMethodsOf(IEnumerable<ISymbol> members) =>
        members
            .OfType<IMethodSymbol>()
            .Where(static m => m.IsStatic)
            .Select(static m => m.PartialDefinitionPart ?? m)
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .ToList();

    // The static methods of the name the global using static directives import, which the generated file sees as well,
    // looked in after the classes: those declared in the types imported, not those they inherit, and not extension
    // methods, which C# does not import as static methods. A using static directive of one file is not seen from the
    // generated file.
    private static List<IMethodSymbol> LookupGlobalStaticImports(INamedTypeSymbol mapperClass, string name, Compilation compilation)
    {
        var members = new List<ISymbol>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var directives = tree.GetCompilationUnitRoot().Usings
                .Where(static u => u.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword) && u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                .ToList();
            if (directives.Count == 0)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(tree);
            foreach (var directive in directives)
            {
                if (semanticModel.GetSymbolInfo(directive.NamespaceOrType).Symbol is INamedTypeSymbol type)
                {
                    members.AddRange(type.GetMembers(name).Where(m =>
                        m.IsStatic && IsInvocable(m) && m is not IMethodSymbol { IsExtensionMethod: true } &&
                        compilation.IsSymbolAccessibleWithin(m, mapperClass)));
                }
            }
        }

        return StaticMethodsOf(members);
    }

    // Whether a member of a base class is hidden by those of the derived classes found before it: by one that is not a
    // method, or, for a method, by one of the same signature, and for another member, by any method.
    private static bool IsHiddenBy(ISymbol member, List<ISymbol> derived) =>
        derived.Any(d => (d is not IMethodSymbol) ||
                         (member is not IMethodSymbol) ||
                         HasSameSignature((IMethodSymbol)d, (IMethodSymbol)member));

    private static bool HasSameSignature(IMethodSymbol method, IMethodSymbol other) =>
        (method.Arity == other.Arity) &&
        (method.Parameters.Length == other.Parameters.Length) &&
        method.Parameters.Zip(other.Parameters, static (p, q) => IsSameType(p.Type, q.Type) && ((p.RefKind == RefKind.None) == (q.RefKind == RefKind.None))).All(static x => x);

    // The method taking the arguments in its normal form, the optional parameters after them left to their defaults, or
    // expanded, params taking the arguments after the other parameters; null when it does not take them, or, for a
    // generic method, when a type parameter is in none of the parameters taking them, which the call cannot infer
    // (CS0411).
    private static CallCandidate? ApplyCall(IMethodSymbol method, IReadOnlyList<CallArgument> arguments, bool expanded, Compilation compilation)
    {
        var parameters = method.Parameters;
        var fixedCount = expanded ? parameters.Length - 1 : parameters.Length;
        var takesCount = expanded
            ? (parameters.Length > 0) && parameters[parameters.Length - 1].IsParams && (fixedCount <= arguments.Count)
            : (parameters.Length >= arguments.Count) && parameters.Skip(arguments.Count).All(static p => p.IsOptional);
        if (!takesCount)
        {
            return null;
        }

        var types = new ITypeSymbol[arguments.Count];
        var refKinds = new RefKind[arguments.Count];
        var declaredTypes = new List<ITypeSymbol>(arguments.Count);
        for (var i = 0; i < arguments.Count; i++)
        {
            var declared = i < fixedCount ? parameters[i].Type : GetParamsElementType(parameters[fixedCount].Type);
            var refKind = i < fixedCount ? parameters[i].RefKind : RefKind.None;
            var type = declared is null ? null : InferParameterType(method, declared, arguments[i].Type);
            if ((type is null) || !CanPassArgument(arguments[i], type, refKind, compilation))
            {
                return null;
            }

            declaredTypes.Add(declared!);
            types[i] = type;
            refKinds[i] = refKind;
        }

        if (method.TypeParameters.Any(p => !declaredTypes.Any(t => HasTypeParameter(t, p))))
        {
            return null;
        }

        return new CallCandidate(method, types, refKinds, expanded, !expanded && (parameters.Length > arguments.Count));
    }

    // The type a parameter takes the argument as: its own, or, for one whose type has type parameters of the generic
    // method, the type of the argument for a type parameter, the array of the argument for an array of the same rank,
    // and the constructed type of the same definition the argument is, derives from or implements (IEnumerable<X> for
    // IEnumerable<T> and a List<X>), as the call infers them; null when they cannot be inferred so.
    private static ITypeSymbol? InferParameterType(IMethodSymbol method, ITypeSymbol parameterType, ITypeSymbol argumentType)
    {
        if (!method.IsGenericMethod || !HasMethodTypeParameter(parameterType))
        {
            return parameterType;
        }

        switch (parameterType)
        {
            case ITypeParameterSymbol:
                return argumentType;
            case IArrayTypeSymbol array:
                return (argumentType is IArrayTypeSymbol argumentArray) && (argumentArray.Rank == array.Rank) ? argumentType : null;
            case INamedTypeSymbol named:
                for (var type = argumentType; type is not null; type = type.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, named.OriginalDefinition))
                    {
                        return type;
                    }
                }

                return argumentType.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, named.OriginalDefinition));
            default:
                return null;
        }
    }

    // The element type of params: of an array, or of a params collection (a span or a collection type of one element
    // type)
    private static ITypeSymbol? GetParamsElementType(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => array.ElementType,
        INamedTypeSymbol { TypeArguments.Length: 1 } named => named.TypeArguments[0],
        _ => null
    };

    private static bool HasMethodTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol parameter => parameter.TypeParameterKind == TypeParameterKind.Method,
        IArrayTypeSymbol array => HasMethodTypeParameter(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(HasMethodTypeParameter),
        _ => false
    };

    private static bool HasTypeParameter(ITypeSymbol type, ITypeParameterSymbol typeParameter) => type switch
    {
        ITypeParameterSymbol parameter => SymbolEqualityComparer.Default.Equals(parameter, typeParameter),
        IArrayTypeSymbol array => HasTypeParameter(array.ElementType, typeParameter),
        INamedTypeSymbol named => named.TypeArguments.Any(t => HasTypeParameter(t, typeParameter)),
        _ => false
    };

    // Whether an argument goes to a parameter: a value, or a variable passed without a modifier, to one by value, in or
    // ref readonly of a type it converts to implicitly, and a variable passed with in or ref to one taking it so, of
    // its own type.
    private static bool CanPassArgument(CallArgument argument, ITypeSymbol parameterType, RefKind refKind, Compilation compilation) => argument.Modifier switch
    {
        RefKind.None => (refKind is RefKind.None or RefKind.In or RefKind.RefReadOnlyParameter) &&
                        (IsSameType(argument.Type, parameterType) || compilation.ClassifyCommonConversion(argument.Type, parameterType).IsImplicit),
        RefKind.In => (refKind is RefKind.In or RefKind.RefReadOnlyParameter) && IsSameType(argument.Type, parameterType),
        _ => (refKind is RefKind.Ref or RefKind.RefReadOnlyParameter) && IsSameType(argument.Type, parameterType)
    };

    // Whether the call prefers the candidate over the other: a better conversion for an argument and a worse one for
    // none, one to the type of the argument itself, or to a type converting to the other's and not back (the more
    // derived one); and with the same types for every argument, a method not generic, one not expanded, one leaving no
    // parameter to its default, then one taking an argument by value where the other takes it by in.
    private static bool IsBetterCall(CallCandidate candidate, CallCandidate other, IReadOnlyList<CallArgument> arguments, Compilation compilation)
    {
        var better = false;
        var worse = false;
        var sameTypes = true;
        var byValue = false;
        var byIn = false;
        for (var i = 0; i < arguments.Count; i++)
        {
            var type = candidate.ParameterTypes[i];
            var otherType = other.ParameterTypes[i];
            if (IsSameType(type, otherType))
            {
                byValue |= (candidate.RefKinds[i] == RefKind.None) && (other.RefKinds[i] == RefKind.In);
                byIn |= (candidate.RefKinds[i] == RefKind.In) && (other.RefKinds[i] == RefKind.None);
                continue;
            }

            sameTypes = false;
            var exact = IsSameType(arguments[i].Type, type);
            var otherExact = IsSameType(arguments[i].Type, otherType);
            if (exact != otherExact)
            {
                better |= exact;
                worse |= otherExact;
                continue;
            }

            var converts = compilation.ClassifyCommonConversion(type, otherType).IsImplicit;
            var convertsBack = compilation.ClassifyCommonConversion(otherType, type).IsImplicit;
            better |= converts && !convertsBack;
            worse |= convertsBack && !converts;
        }

        if (better || worse || !sameTypes)
        {
            return better && !worse;
        }

        if (candidate.Method.IsGenericMethod != other.Method.IsGenericMethod)
        {
            return !candidate.Method.IsGenericMethod;
        }

        if (candidate.Expanded != other.Expanded)
        {
            return !candidate.Expanded;
        }

        if (candidate.UsesDefaults != other.UsesDefaults)
        {
            return !candidate.UsesDefaults;
        }

        return byValue && !byIn;
    }

    private static DiagnosticInfo? ValidateConverterMethods(
        IMethodSymbol mapperMethod,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        Compilation compilation,
        ref MapperMethodModel model,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var constructor = GetEffectiveConstructor(model, destinationType);
        var customTypes = GetCustomParameterTypes(mapperMethod, model);

        var resolved = new List<PropertyMappingModel>(model.PropertyMappings.Count);
        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.ConverterMethod))
            {
                resolved.Add(mapping);
                continue;
            }

            // A converter returns the target type, or a type converting to it implicitly as the assignment of its
            // result does
            var targetType = GetMappingTargetType(mapping, model, destinationType, constructor, containingType, compilation);
            var valueType = GetSourceValueType(mapping, sourceType, containingType, compilation);
            var match = MatchValueMethod(
                LookupStaticMethods(containingType, mapping.ConverterMethod!, compilation),
                new ValueCall(valueType, mapping.SourceType, ArgumentKind.Value, customTypes),
                model.CustomParameters,
                m => (m.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == mapping.TargetType) ||
                     ((targetType is not null) && IsImplicitlyConvertible(m.ReturnType, targetType, compilation)),
                compilation);
            if (match.Result == ConverterMatchResult.ReturnTypeMismatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterReturnType,
                    LocationOf(model, mapping.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapping.ConverterMethod!,
                    mapping.TargetType,
                    match.Mismatched!.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            var matchedMethod = match.Method;
            if (matchedMethod is null)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterSignature,
                    LocationOf(model, mapping.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapping.ConverterMethod!,
                    mapping.TargetPath);
            }

            var unwraps = match.Match is ValueMatch.Unwrap or ValueMatch.UnwrapConvert;
            resolved.Add(mapping with
            {
                ConverterAcceptsCustomParameters = match.Result == ConverterMatchResult.MatchWithCustomParams,
                ConverterParameterRefKinds = GetParameterRefKinds(matchedMethod),
                ConverterUnwrapsSource = unwraps,
                ConverterRejectsNull = unwraps || RejectsNull(matchedMethod, valueType, compilation),
                ConverterForgivesNull = IsNullableReference(matchedMethod.ReturnType) && (targetType is not null) && !targetType.IsNullableType()
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

    internal static DiagnosticInfo? ValidateCallbackMethods(
        IMethodSymbol mapperMethod,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        Compilation compilation,
        ref MapperMethodModel model,
        MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;
        var customTypes = GetCustomParameterTypes(mapperMethod, model);

        if (!String.IsNullOrEmpty(model.BeforeMapMethod))
        {
            var (matchResult, matchedMethod) = FindMatchingCallbackMethod(
                LookupStaticMethods(containingType, model.BeforeMapMethod!, compilation),
                model,
                sourceType,
                destinationType,
                customTypes,
                compilation);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidBeforeMapSignature, LocationOf(model, model.BeforeMapAttributeIndex, syntax), mapperMethod.Name, model.BeforeMapMethod!);
            }
            model = model with
            {
                BeforeMapAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams,
                BeforeMapParameterRefKinds = GetParameterRefKinds(matchedMethod)
            };
        }

        if (!String.IsNullOrEmpty(model.AfterMapMethod))
        {
            var (matchResult, matchedMethod) = FindMatchingCallbackMethod(
                LookupStaticMethods(containingType, model.AfterMapMethod!, compilation),
                model,
                sourceType,
                destinationType,
                customTypes,
                compilation);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidAfterMapSignature, LocationOf(model, model.AfterMapAttributeIndex, syntax), mapperMethod.Name, model.AfterMapMethod!);
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

    // The callback taking the source, the destination, and the custom parameters when it takes them, over one without,
    // as before (BindCallbackCall).
    internal static (CallbackMatchResult Result, IMethodSymbol? Method) FindMatchingCallbackMethod(
        List<IMethodSymbol> methods,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        IReadOnlyList<ITypeSymbol> customTypes,
        Compilation compilation)
    {
        if ((model.CustomParameters.Count > 0) &&
            (BindCallbackCall(methods, model, sourceType, destinationType, customTypes, 2 + model.CustomParameters.Count, compilation) is { } withCustomParams))
        {
            return (CallbackMatchResult.MatchWithCustomParams, withCustomParams);
        }

        return BindCallbackCall(methods, model, sourceType, destinationType, customTypes, 2, compilation) is { } withoutCustomParams
            ? (CallbackMatchResult.MatchWithoutCustomParams, withoutCustomParams)
            : (CallbackMatchResult.NoMatch, null);
    }

    // The callback a call with that many arguments binds to. The source and the destination are variables: parameters
    // of the mapper, or the instance a return-type mapper builds, which the callback may write. Each goes to a
    // parameter of its type the modifier of which can take it, or, by value, of a base class or an interface it
    // converts to by an implicit reference conversion; a struct, which would be boxed into a copy the callback writes
    // in vain, goes to its own type only. The custom parameters go to parameters of their own types. Of the candidates
    // taking both as their own types, the one taking every argument by value, as before, which the call binds to, as C#
    // binds it among all the methods the name is looked up as (BindCall), or else to another candidate, which is used
    // instead; otherwise the candidate the call binds to. A call binding to none of them, or to one obsolete as an
    // error, matches nothing.
    private static IMethodSymbol? BindCallbackCall(
        List<IMethodSymbol> methods,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        IReadOnlyList<ITypeSymbol> customTypes,
        int count,
        Compilation compilation)
    {
        var sourceKind = GetVariableKind(model.SourceRefKind);
        var destinationKind = model.ReturnsDestination ? ArgumentKind.WritableVariable : GetVariableKind(model.DestinationRefKind);

        var candidates = new List<(IMethodSymbol Method, bool Exact)>();
        foreach (var method in methods)
        {
            if (method.IsGenericMethod || (method.Parameters.Length != count) || !TakesCustomArguments(method, model.CustomParameters, 2, count - 2))
            {
                continue;
            }

            var source = MatchCallbackParameter(method.Parameters[0], sourceType, model.SourceTypeName, sourceKind, compilation);
            var destination = MatchCallbackParameter(method.Parameters[1], destinationType, model.DestinationTypeName, destinationKind, compilation);
            if ((source != ValueMatch.None) && (destination != ValueMatch.None))
            {
                candidates.Add((method, (source == ValueMatch.Exact) && (destination == ValueMatch.Exact)));
            }
        }

        List<CallArgument> ArgumentsFor(IMethodSymbol method)
        {
            var arguments = new List<CallArgument>
            {
                new(sourceType, GetArgumentModifierKind(method.Parameters[0].RefKind)),
                new(destinationType, GetArgumentModifierKind(method.Parameters[1].RefKind))
            };
            for (var i = 2; i < count; i++)
            {
                arguments.Add(new CallArgument(customTypes[i - 2], GetArgumentModifierKind(method.Parameters[i].RefKind)));
            }

            return arguments;
        }

        bool BindsTo(IMethodSymbol method) =>
            SymbolEqualityComparer.Default.Equals(BindCall(methods, ArgumentsFor(method), compilation), method);

        var exact = candidates.Where(static x => x.Exact && (x.Method.GetObsoleteKind() != ObsoleteKind.Error)).ToList();
        if (exact.Count > 0)
        {
            IMethodSymbol? chosen = null;
            foreach (var (method, _) in exact)
            {
                chosen = PreferByValue(chosen, method);
            }

            if (BindsTo(chosen!))
            {
                return chosen;
            }

            var bound = BindCall(methods, ArgumentsFor(chosen!), compilation);
            var other = candidates.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Method, bound)).Method;
            return (other is not null) && (other.GetObsoleteKind() != ObsoleteKind.Error) && BindsTo(other) ? other : null;
        }

        if (candidates.Any(static x => x.Exact))
        {
            return null;
        }

        foreach (var (method, _) in candidates)
        {
            if (BindsTo(method))
            {
                return method.GetObsoleteKind() == ObsoleteKind.Error ? null : method;
            }
        }

        return null;
    }

    // The source or the destination parameter of a callback: of its type, when its modifier can take the variable, or,
    // by value, a base class or an interface it converts to by an implicit reference conversion (Convert).
    private static ValueMatch MatchCallbackParameter(IParameterSymbol parameter, ITypeSymbol type, string typeName, ArgumentKind argument, Compilation compilation)
    {
        if (parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == typeName)
        {
            return CanTakeArgument(parameter.RefKind, argument) ? ValueMatch.Exact : ValueMatch.None;
        }

        return (parameter.RefKind == RefKind.None) && IsImplicitReferenceConversion(type, parameter.Type, compilation) ? ValueMatch.Convert : ValueMatch.None;
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
        var ignoreTargets = new List<string>();
        var propertyConditions = new List<PropertyConditionModel>();
        var constantMappings = new List<ConstantMappingModel>();
        var expressionMappings = new List<ExpressionMappingModel>();
        var mapUsingMappings = new List<MapUsingModel>();
        var mapFromMappings = new List<MapFromModel>();
        var mapCollectionMappings = new List<MapCollectionModel>();
        var mapNestedMappings = new List<MapNestedModel>();

        // The location of every attribute, which the diagnostics about it point to, and the indexes of those the
        // method-level diagnostics are about
        var attributeLocations = new List<LocationInfo>();
        var ignoreAttributeIndices = new List<int>();
        var mapperAttributeIndex = -1;
        var beforeMapAttributeIndex = -1;
        var afterMapAttributeIndex = -1;
        var cultureAttributeIndex = -1;
        var formatAttributeIndex = -1;

        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            var attributeIndex = attributeLocations.Count;
            attributeLocations.Add(GetAttributeLocation(attribute, symbol));

            if (attributeName == Names.MapperAttribute)
            {
                mapperAttributeIndex = attributeIndex;
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
                        cultureAttributeIndex = attributeIndex;
                    }
                    else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string dtFmt))
                    {
                        dateTimeFormatOption = dtFmt;
                        formatAttributeIndex = attributeIndex;
                    }
                    else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string numFmt))
                    {
                        numberFormatOption = numFmt;
                        formatAttributeIndex = attributeIndex;
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
                    var nullValueUnsupported = false;
                    var nullValueHasNullElement = false;
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
                            nullValue = ConstantExpressionHelper.Format(namedArg.Value);
                            nullValueUnsupported = nullValue is null;
                            nullValueHasNullElement = ConstantExpressionHelper.HasNullElement(namedArg.Value);
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
                        IsNullValueUnsupported: nullValueUnsupported,
                        NullValueHasNullElement: nullValueHasNullElement,
                        EffectiveCulture: propCulture,
                        EffectiveDateTimeFormat: propDateTimeFormat,
                        EffectiveNumberFormat: propNumberFormat,
                        AttributeIndex: attributeIndex);

                    propertyMappings.Add(mapping);
                }
            }
            else if (attributeName == Names.MapIgnoreAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    ignoredProperties.Add(targetName);
                    ignoreTargets.Add(targetName);
                    ignoreAttributeIndices.Add(attributeIndex);
                }
            }
            else if ((attributeName == Names.MapConstantAttribute) ||
                     ((attributeName is not null) && attributeName.StartsWith(Names.MapConstantAttributeGenericPrefix, StringComparison.Ordinal)))
            {
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var value = attribute.ConstructorArguments[1];
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
                        Value: ConstantExpressionHelper.Format(value),
                        HasNullElement: ConstantExpressionHelper.HasNullElement(value),
                        Order: order,
                        DefinitionOrder: definitionOrder++,
                        AttributeIndex: attributeIndex);

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
                        DefinitionOrder: definitionOrder++,
                        AttributeIndex: attributeIndex));

                    ignoredProperties.Add(targetName);
                }
            }
            else if (attributeName == Names.BeforeMapAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    beforeMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                    beforeMapAttributeIndex = attributeIndex;
                }
            }
            else if (attributeName == Names.AfterMapAttribute)
            {
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    afterMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                    afterMapAttributeIndex = attributeIndex;
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
                        propertyConditions.Add(new PropertyConditionModel(targetName, conditionName, attributeIndex));
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
                        DefinitionOrder: definitionOrder++,
                        AttributeIndex: attributeIndex));

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
                        DefinitionOrder: definitionOrder++,
                        AttributeIndex: attributeIndex));

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
                        InPlace: inPlace,
                        AttributeIndex: attributeIndex));

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
                        DefinitionOrder: definitionOrder++,
                        AttributeIndex: attributeIndex));

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
            IgnoreTargets = new(ignoreTargets),
            PropertyConditions = new(propertyConditions),
            ConstantMappings = new(constantMappings),
            ExpressionMappings = new(expressionMappings),
            MapUsingMappings = new(mapUsingMappings),
            MapFromMappings = new(mapFromMappings),
            MapCollectionMappings = new(mapCollectionMappings),
            MapNestedMappings = new(mapNestedMappings),
            AttributeLocations = new(attributeLocations),
            IgnoreAttributeIndices = new(ignoreAttributeIndices),
            MapperAttributeIndex = mapperAttributeIndex,
            BeforeMapAttributeIndex = beforeMapAttributeIndex,
            AfterMapAttributeIndex = afterMapAttributeIndex,
            CultureAttributeIndex = cultureAttributeIndex,
            FormatAttributeIndex = formatAttributeIndex
        };
    }

    // The location of the attribute as applied in source, or else the symbol's
    private static LocationInfo GetAttributeLocation(AttributeData attribute, ISymbol symbol) =>
        (attribute.ApplicationSyntaxReference is { } reference ? LocationInfo.CreateFrom(reference.GetSyntax()) : null) ??
        LocationInfo.CreateFrom(symbol.Locations.FirstOrDefault() ?? Location.None) ??
        new LocationInfo(String.Empty, default, default);

    // The [MapIgnore] of the entry of IgnoreTargets, and the [MapCondition] of the target
    private static int IgnoreAttributeIndex(MapperMethodModel model, int index) =>
        (index >= 0) && (index < model.IgnoreAttributeIndices.Count) ? model.IgnoreAttributeIndices[index] : -1;

    private static int IgnoreAttributeIndex(MapperMethodModel model, string target)
    {
        for (var i = 0; i < model.IgnoreTargets.Count; i++)
        {
            if (String.Equals(model.IgnoreTargets[i], target, StringComparison.Ordinal))
            {
                return IgnoreAttributeIndex(model, i);
            }
        }

        return -1;
    }

    private static int ConditionAttributeIndex(MapperMethodModel model, string target) =>
        model.PropertyConditions.FirstOrDefault(c => String.Equals(c.TargetName, target, StringComparison.Ordinal))?.AttributeIndex ?? -1;

    // The location of the attribute a diagnostic is about, from its index in AttributeLocations, or else the
    // mapper method's, as for what the automatic mapping does and the construction of the destination
    private static LocationInfo LocationOf(MapperMethodModel model, int attributeIndex, MethodDeclarationSyntax syntax) =>
        ((attributeIndex >= 0) && (attributeIndex < model.AttributeLocations.Count) ? model.AttributeLocations[attributeIndex] : null) ??
        LocationInfo.CreateFrom(syntax) ??
        new LocationInfo(String.Empty, default, default);

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

        // The attributes of the method are at their index in AttributeLocations (ParseMappingAttributes); those of
        // the class the diagnostics are about are added after them
        var attributeLocations = new List<LocationInfo>(model.AttributeLocations);
        var valueConverterAttributeIndex = -1;
        var cultureAttributeIndex = model.CultureAttributeIndex;
        var formatAttributeIndex = model.FormatAttributeIndex;

        var methodAttributes = symbol.GetAttributes();
        for (var index = 0; index < methodAttributes.Length; index++)
        {
            var attribute = methodAttributes[index];
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == Names.ValueConverterAttribute)
            {
                if ((attribute.ConstructorArguments.Length >= 1) &&
                    (attribute.ConstructorArguments[0].Value is INamedTypeSymbol converterType))
                {
                    mapConverterTypeName = converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    valueConverterAttributeIndex = index;

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
                    valueConverterAttributeIndex = attributeLocations.Count;
                    attributeLocations.Add(GetAttributeLocation(attribute, containingType));

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
                // Each setting of the profile applies unless the mapper method sets it, Culture,
                // DateTimeFormat and NumberFormat each on its own
                var profileAttributeIndex = attributeLocations.Count;
                attributeLocations.Add(GetAttributeLocation(attribute, containingType));
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
                        cultureAttributeIndex = profileAttributeIndex;
                    }
                    else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string profileDtFmt) && (model.DateTimeFormat is null))
                    {
                        dateTimeFormatOption = profileDtFmt;
                        formatAttributeIndex = formatAttributeIndex < 0 ? profileAttributeIndex : formatAttributeIndex;
                    }
                    else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string profileNumFmt) && (model.NumberFormat is null))
                    {
                        numberFormatOption = profileNumFmt;
                        formatAttributeIndex = formatAttributeIndex < 0 ? profileAttributeIndex : formatAttributeIndex;
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
            NumberFormat = numberFormatOption,
            AttributeLocations = new(attributeLocations),
            ValueConverterAttributeIndex = valueConverterAttributeIndex,
            CultureAttributeIndex = cultureAttributeIndex,
            FormatAttributeIndex = formatAttributeIndex
        };
    }

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
                    TargetType = target.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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
    // a property or field of the destination, a dotted path of them (for [MapCondition]; SMP0223 for
    // [MapIgnore]), or a parameter of the constructor a return mapper calls (or, for [MapIgnore], of one it
    // could call, which the parameter without a value keeps from being chosen). The names are canonical here
    // (CanonicalizeTargetNames), so they are matched as declared, under the mapper's name comparison. Not
    // found, it is reported as a target that is not found (SMP0214); a [MapCondition] on a member no property
    // mapping assigns is reported later (SMP0221).
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
                return new DiagnosticInfo(Diagnostics.UnresolvedMapPropertyTargetProperty, LocationOf(model, index, syntax), model.MethodName, target);
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
    // whichever attribute the path is of (SMP0222). A void mapper never constructs, so it is not concerned.
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
    // which the condition does not guard), it would do nothing without a word, so it is reported (SMP0221).
    // The mappings of constructor arguments and initializer entries were reported before (SMP0215).
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

            // The method takes the source, as its own type or as a base class or an interface it converts to, and
            // returns the type of the target, or a type converting to it implicitly
            var match = MatchValueMethod(
                LookupStaticMethods(containingType, mapUsing.Method, compilation),
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

            var matchedMethod = match.Method!;
            resolved.Add(mapUsing with
            {
                TargetType = targetTypeName,
                TargetPathSegments = target.Segments,
                IsTargetInitOnly = target.IsInitOnly,
                IsTargetRequired = target.IsRequired,
                AcceptsCustomParameters = match.Result == ConverterMatchResult.MatchWithCustomParams,
                ParameterRefKinds = GetParameterRefKinds(matchedMethod),
                ForgivesNull = IsNullableReference(matchedMethod.ReturnType) && !target.Type.IsNullableType(),
                MethodReturnType = matchedMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
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
                TargetType = targetTypeName,
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
                        ReturnType = returnType,
                        ForgivesNull = IsNullableReference(sourceMethod.ReturnType) && !targetMemberType.IsNullableType(),
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

                resolved.Add(withTarget with
                {
                    IsMethodCall = false,
                    Member = member,
                    ReturnType = returnType,
                    NullCheckedPaths = GetNullableIntermediatePaths(sourceType, member, within, compilation),
                    ForgivesNull = IsNullableReference(resolvedType) && !targetMemberType.IsNullableType(),
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

            if (property.Type.IsNullableType())
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
    // segment by segment; the rest from a segment that is not a property (such as Length) is left as written.
    private static string CanonicalizeSourcePath(ITypeSymbol sourceType, string path, StringComparison comparison, INamedTypeSymbol within, Compilation compilation)
    {
        var parts = path.Split('.');
        var type = sourceType;
        for (var i = 0; i < parts.Length; i++)
        {
            var property = PropertyPathHelper.ResolveProperty(type, parts[i], comparison, within, compilation, readable: true);
            if (property is null)
            {
                break;
            }

            parts[i] = property.Name;
            type = property.Type;
        }

        return String.Join(".", parts);
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

                canCallMapper = (m, _) => converterMethods.Any(c => IsDelegateFor(c.Parameters[1].Type, m));
            }
            else
            {
                // The loop passes the element, or the value of a nullable struct one, and creates the instance a
                // void mapper fills with new T()
                var element = GetElementArgumentKind(sourceShape);
                var canCreateElement = CanCreateInstance(targetElementType, containingType, compilation);
                canCallMapper = (m, unwraps) => TakesMapperArguments(m, unwraps ? ArgumentKind.Value : element) && (!m.ReturnsVoid || canCreateElement);
            }

            var elementMatch = FindMapperMethod(
                containingType,
                mapCollection.Mapper!,
                sourceElementType,
                targetElementType,
                compilation,
                usesConverter ? null : GetElementArgumentKind(sourceShape),
                canCallMapper);
            if (elementMatch is not { } matchedElementMapper)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapCollectionMapperMethod,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapCollection.Mapper!,
                    mapCollection.TargetName);
            }

            var elementMapper = matchedElementMapper.Method;

            // A nullable reference element goes to a mapper whose parameter does not take null only when it has a
            // value, as the value of a nullable struct one does, and a null one gives default, as a null source of
            // [MapNested] does. The loop can pass it so to a parameter taking a value; a collection converter takes the
            // mapper as a delegate.
            var unwrapsReference = !usesConverter && IsNullableReference(sourceElementType) &&
                                   (elementMapper.Parameters[0].RefKind is RefKind.None or RefKind.In) &&
                                   !TakesNull(elementMapper.Parameters[0], elementMapper.Parameters[0].Type);
            resolvedCollections.Add(mapCollection with
            {
                IsInitializerEntry = inInitializer,
                SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                SourceElementType = sourceElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                SourceElementTypeArgument = sourceElementType.ToDisplayString(NullableQualifiedFormat),
                TargetType = GetCreatedTypeName(targetMemberType),
                TargetElementType = targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TargetElementTypeArgument = targetElementType.ToDisplayString(NullableQualifiedFormat),
                IsSourceNullable = sourceProp.Type.IsNullableType(),
                TargetIsArray = targetMemberType is IArrayTypeSymbol,
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
                ForgivesMapperResult = ReturnsNullableInto(elementMapper, targetElementType),
                UnwrapsSource = matchedElementMapper.UnwrapsSource || unwrapsReference,
                NullResult = GetNullResult(elementMapper, targetElementType),
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
            var nestedMatch = FindMapperMethod(
                containingType,
                mapNested.Mapper,
                sourceUnderlyingType,
                targetUnderlyingType,
                compilation,
                ArgumentKind.Value,
                (m, _) => TakesMapperArguments(m, ArgumentKind.Value) && (!m.ReturnsVoid || canCreateTarget));
            if (nestedMatch is not { } matchedNestedMapper)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidMapNestedMapperMethod,
                    LocationOf(model, declared.AttributeIndex, syntax),
                    mapperMethod.Name,
                    mapNested.Mapper,
                    mapNested.TargetName);
            }

            var nestedMapper = matchedNestedMapper.Method;

            resolvedNested.Add(mapNested with
            {
                IsInitializerEntry = inInitializer,
                SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TargetType = targetMemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ArgumentLocalType = ReturnsNullable(nestedMapper) && (targetMemberType.NullableAnnotation == NullableAnnotation.Annotated)
                    ? targetMemberType.ToDisplayString(NullableQualifiedFormat)
                    : targetMemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsSourceNullable = sourceProp.Type.IsNullableType(),
                MapperReturnsValue = !nestedMapper.ReturnsVoid,
                ForgivesMapperResult = ReturnsNullableInto(nestedMapper, targetMemberType),
                UnwrapsSource = matchedNestedMapper.UnwrapsSource,
                NullResult = GetNullResult(nestedMapper, targetMemberType),
                MapperParameterRefKinds = GetParameterRefKinds(nestedMapper)
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

    // The name of a collection type the generated code creates, or declares the local of a constructor argument
    // as: with the nullable annotations of its type arguments (List<Item?>), which the target has to get for its
    // type (CS8619 otherwise), and without its own, which new cannot take.
    private static string GetCreatedTypeName(ITypeSymbol type) =>
        (type.IsReferenceType ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : type).ToDisplayString(NullableQualifiedFormat);

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

    private static bool ReturnsNullable(IMethodSymbol mapper) =>
        !mapper.ReturnsVoid && mapper.ReturnType.IsReferenceType && (mapper.ReturnType.NullableAnnotation == NullableAnnotation.Annotated);

    // The mapper of [MapCollection] / [MapNested], and whether the value of a nullable struct source goes to it.
    internal readonly record struct MapperMatch(IMethodSymbol Method, bool UnwrapsSource);

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
    // error), as the call would bind to it.
    internal static MapperMatch? FindMapperMethod(
        INamedTypeSymbol containingType,
        string methodName,
        ITypeSymbol sourceElementType,
        ITypeSymbol targetElementType,
        Compilation compilation,
        ArgumentKind? sourceArgument,
        Func<IMethodSymbol, bool, bool> canCall)
    {
        var matches = new List<(IMethodSymbol Method, int Score, bool Unwraps)>();
        var methods = LookupStaticMethods(containingType, methodName, compilation);
        foreach (var method in methods.Where(IsCallableByName))
        {
            if (MatchesMapperShape(method, sourceElementType, targetElementType, compilation, sourceArgument is not null, out var score, out var unwraps) &&
                canCall(method, unwraps))
            {
                matches.Add((method, score, unwraps));
            }
        }

        if (matches.Count == 0)
        {
            return null;
        }

        var closestScore = matches.Min(static m => m.Score);
        var closest = matches.Where(m => m.Score == closestScore).ToList();
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
            var called = BindCall(methods, GetMapperCallArguments(chosen.Method, chosen.Unwraps, sourceElementType, targetElementType, argument), compilation);
            if (!SymbolEqualityComparer.Default.Equals(called, chosen.Method))
            {
                var other = matches.FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(m.Method, called) &&
                                                        (m.Method.ReturnsVoid == chosen.Method.ReturnsVoid) && (m.Unwraps == chosen.Unwraps));
                if ((other.Method is null) ||
                    !SymbolEqualityComparer.Default.Equals(
                        BindCall(methods, GetMapperCallArguments(other.Method, other.Unwraps, sourceElementType, targetElementType, argument), compilation),
                        other.Method))
                {
                    return null;
                }

                chosen = other;
            }
        }

        return new MapperMatch(chosen.Method, chosen.Unwraps);
    }

    // What the call of the mapper of [MapCollection] / [MapNested] passes: the source, or the value a nullable struct
    // holds, as a value, or as a variable with the modifier of the parameter taking it, and for a void mapper the
    // instance created for it, with the modifier of its parameter.
    private static List<CallArgument> GetMapperCallArguments(IMethodSymbol mapper, bool unwraps, ITypeSymbol sourceType, ITypeSymbol createdType, ArgumentKind sourceArgument)
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

        return arguments;
    }

    // Whether the call binds to the method rather than to the other: each parameter of the same type, or of one
    // converting to the other's.
    private static bool IsAtLeastAsSpecific(IMethodSymbol method, IMethodSymbol other, Compilation compilation) =>
        method.Parameters.Zip(other.Parameters, (p, q) => IsSameType(p.Type, q.Type) || compilation.ClassifyCommonConversion(p.Type, q.Type).IsImplicit)
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
        if ((method.Parameters.Length != (method.ReturnsVoid ? 2 : 1)) ||
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

    // A static method of the mapper class the generated code calls by name: a converter, a condition, a
    // [MapUsing] method, a callback, or the mapper of [MapCollection] / [MapNested]. A generic one is not, as the
    // call passes no type arguments, and the arguments of the shapes matched never let them be inferred
    // (CS0411).
    // One obsolete as an error cannot be called (CS0619), so it does not match; one obsolete as a warning is
    // called, as the attribute names it.
    private static bool IsCallableByName(IMethodSymbol method) =>
        method.IsStatic && !method.IsGenericMethod && (method.GetObsoleteKind() != ObsoleteKind.Error);

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
    // written as an expression (SMP0220 for one that cannot be, such as a file-local type) that converts to
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
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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

    // A dotted target path the generated code cannot assign: one it cannot reach or assign at all (SMP0214), or
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
            return new DiagnosticInfo(Diagnostics.UnresolvedMapPropertyTargetProperty, LocationOf(model, attributeIndex, syntax), model.MethodName, path);
        }

        if ((route == TargetRoute.Initializer) && !model.ReturnsDestination)
        {
            return new DiagnosticInfo(Diagnostics.InitOnlyDestinationRequiresReturnMapper, LocationOf(model, attributeIndex, syntax), model.MethodName, model.DestinationTypeName);
        }

        return null;
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

        if (String.IsNullOrEmpty(model.Culture) && (!String.IsNullOrEmpty(model.DateTimeFormat) || !String.IsNullOrEmpty(model.NumberFormat)))
        {
            return new DiagnosticInfo(Diagnostics.FormatWithoutCulture, LocationOf(model, model.FormatAttributeIndex, syntax), model.MethodName, "(method)");
        }

        foreach (var mapping in model.PropertyMappings)
        {
            if (String.IsNullOrEmpty(mapping.EffectiveCulture) &&
                (!String.IsNullOrEmpty(mapping.EffectiveDateTimeFormat) || !String.IsNullOrEmpty(mapping.EffectiveNumberFormat)))
            {
                return new DiagnosticInfo(Diagnostics.FormatWithoutCulture, LocationOf(model, mapping.AttributeIndex, syntax), model.MethodName, mapping.TargetPath);
            }
        }

        return null;
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
    // the object initializer; ignoring one would leave it unset (SMP0216, CS9035 otherwise). That is every one
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
                    creations.Add(new NestedPathSegment(name, type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), false));
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
                    !explicitMappings.Any(e => e.TargetPath == pm.TargetPath));
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
            if (explicitMapping is not null)
            {
                // ValidateExplicitPropertyMappings already resolved and canonicalized this path.
                sourcePath = explicitMapping.SourcePath;
                sourcePropertyType = PropertyPathHelper.ResolvePropertyType(sourceType, sourcePath, within, compilation)!;
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
    // (SMP0301, SMP0216).
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
        IReadOnlyList<IPropertySymbol> sourceProperties,
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
        string? converterMethod,
        string? conditionMethod,
        Compilation compilation)
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
        var nullValueUnsupported = false;
        var nullValueHasNullElement = false;
        string? propEffectiveCulture;
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

        // A reference declared without nullable annotations (nullable disabled) may hold null as well, so the null
        // handling an attribute asks for, NullValue or NullBehavior.Skip, applies to it
        if (!isSourceNullable && sourcePropertyType.IsReferenceType && (sourcePropertyType.NullableAnnotation == NullableAnnotation.None) &&
            ((nullValue is not null) || nullValueUnsupported || (nullBehavior == NullBehaviorType.Skip)))
        {
            isSourceNullable = true;
        }

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
            AttributeIndex: origMapping?.AttributeIndex ?? -1);

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
                var parameter = declared.TargetPath.Contains('.') ? null : FindParameter(declared.TargetPath);
                var mapping = ResolveNestedMapping(declared, sourceType, destinationType, within, compilation, parameter?.Type);

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
                sourcePropPath = customSourcePath;
                sourcePropertyType = PropertyPathHelper.ResolvePropertyType(sourceType, customSourcePath, within, compilation);

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
                    FindParameter(destProp.Name)?.Type ?? destProp.Type,
                    destProp.GetSetter()?.IsInitOnly == true,
                    destProp.IsRequired,
                    sourcePropPath,
                    sourcePropertyType,
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

            var finalSourceProp = PropertyPathHelper.GetProperties(currentType, within, compilation, readable: true).FirstOrDefault(p => p.Name == sourceParts[sourceParts.Length - 1]);
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
            var sourceProp = PropertyPathHelper.GetProperties(sourceType, within, compilation, readable: true).FirstOrDefault(p => p.Name == mapping.SourcePath);
            if (sourceProp is not null)
            {
                sourceTypeName = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                isSourceNullable = sourceProp.Type.IsNullableType();
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

        if (!String.IsNullOrEmpty(sourceUnderlyingTypeName) && !String.IsNullOrEmpty(targetUnderlyingTypeName))
        {
            var srcParts = mapping.SourcePath.Split('.');
            var dstParts = mapping.TargetPath.Split('.');
            var srcFinalProp = PropertyPathHelper.ResolvePropertySymbol(sourceType, srcParts, within, compilation, readable: true);
            var dstFinalProp = PropertyPathHelper.ResolvePropertySymbol(destinationType, dstParts, within, compilation);
            var srcUnderlying = srcFinalProp?.Type.GetUnderlyingType();
            var dstUnderlying = (targetTypeOverride ?? dstFinalProp?.Type)?.GetUnderlyingType();
            var assignable = (srcUnderlying is not null) && (dstUnderlying is not null) &&
                             (srcUnderlying.IsAssignableTo(dstUnderlying) || IsImplicitReferenceConversion(srcUnderlying, dstUnderlying, compilation));
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
            IsTargetInitOnly = isTargetInitOnly,
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
        EquatableArray<(string ParamName, string TargetPath, ConstructorArgumentKind Kind, bool IsNamed)> constructorArguments,
        string? mapConverterTypeName,
        string mapConverterMethodName,
        Compilation compilation)
    {
        var within = mapperMethod.ContainingType;
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
            .Where(static m => m.IsStatic && TakesArgumentCount(m, 3) && (m.GetObsoleteKind() != ObsoleteKind.Error));
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
