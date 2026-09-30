namespace Smart.Mapper.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Mapper.Generator.Helpers;
using Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

internal static partial class MapperModelBuilder
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

    // The name of the CultureInfo parameter giving the culture of the conversions when the mapper takes several
    private const string CultureParameterName = "culture";

    // The modifiers other than the accessibility the implementation repeats (GetDeclarationModifiers)
    private static readonly SyntaxKind[] RepeatedModifiers =
    [
        SyntaxKind.NewKeyword,
        SyntaxKind.VirtualKeyword,
        SyntaxKind.SealedKeyword,
        SyntaxKind.OverrideKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.UnsafeKeyword
    ];

    internal static Result<MapperMethodModel> BuildModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not { } symbol)
        {
            return Results.Errors<MapperMethodModel>();
        }

        // The generated code declares the containing types again, outermost first, so each of them has to be
        // partial, or that declaration would not compile (CS0260), and none of them file-local, as the declaration is
        // in another file (CS0759). The mapper is a static or an instance method.
        var typeChain = GetContainingTypes(symbol.ContainingType);
        if (!symbol.IsPartialDefinition || !typeChain.All(IsPartialType) || typeChain.Any(static t => t.IsFileLocal))
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodDefinition, syntax.Identifier.GetLocation(), symbol.Name));
        }

        // A mapper reported with an error gets an implementation throwing in place of none, so that the implementation
        // missing is not reported along with the error (CS8795). The one the checks above report could not have one.
        var result = BuildMethodModel(context, symbol, syntax, typeChain);
        return !result.HasValue && result.HasErrors
            ? new Result<MapperMethodModel>(CreatePlaceholder(symbol, syntax, typeChain), result.Diagnostics)
            : result;
    }

    private static Result<MapperMethodModel> BuildMethodModel(
        GeneratorAttributeSyntaxContext context,
        IMethodSymbol symbol,
        MethodDeclarationSyntax syntax,
        List<INamedTypeSymbol> typeChain)
    {
        // The mapper creates or fills the destination, which a reference it returned would have to point to
        if (symbol.ReturnsByRef || symbol.ReturnsByRefReadonly)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.RefReturnMapper, syntax.ReturnType.GetLocation(), symbol.Name));
        }

        if (symbol.Parameters.Length < 1)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.Identifier.GetLocation(), symbol.Name));
        }

        var containingType = symbol.ContainingType;
        var ns = GetNamespaceName(containingType);

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

        // A collection, an array or a tuple is not mapped as a whole: its members (Count, Capacity, Item1) are not what
        // it holds, and an array or a tuple cannot be created as the destination is
        if (IsWholeCollection(sourceParam.Type) || IsWholeCollection(destinationType))
        {
            var isSource = IsWholeCollection(sourceParam.Type);
            var type = isSource ? sourceParam.Type : destinationType;
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                Diagnostics.CollectionMapper,
                isSource ? syntax.ParameterList.Parameters[0].GetLocation() :
                returnsDestination ? syntax.ReturnType.GetLocation() : syntax.ParameterList.Parameters[1].GetLocation(),
                symbol.Name,
                type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        // The custom parameters, which may be of the same type: the methods the attributes name take them by type, or
        // by name for several of a type (MapCustomArguments)
        var customParameters = new List<CustomParameterModel>();
        for (var i = customParamStartIndex; i < symbol.Parameters.Length; i++)
        {
            var param = symbol.Parameters[i];
            customParameters.Add(new CustomParameterModel(
                IdentifierHelper.Escape(param.Name),
                param.Type.ToDisplayString(NullableQualifiedFormat),
                Modifiers: GetParameterModifiers(syntax, i),
                RefKind: param.RefKind));
        }

        // A custom parameter of type CultureInfo gives the culture of the conversions as well, and of several, the one
        // named culture, as the culture parameter of the converter is named
        var cultureParameters = Enumerable.Range(customParamStartIndex, symbol.Parameters.Length - customParamStartIndex)
            .Where(i => symbol.Parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == Names.QualifiedCultureInfo)
            .ToList();
        var cultureParameterIndex = cultureParameters.Count == 1
            ? cultureParameters[0]
            : cultureParameters.Where(i => symbol.Parameters[i].Name == CultureParameterName).DefaultIfEmpty(-1).First();
        if ((cultureParameters.Count > 1) && (cultureParameterIndex < 0))
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                Diagnostics.AmbiguousCultureParameter,
                syntax.GetLocation(),
                symbol.Name,
                CultureParameterName));
        }

        var cultureParameterName = cultureParameterIndex >= 0 ? IdentifierHelper.Escape(symbol.Parameters[cultureParameterIndex].Name) : null;
        var isCultureParameterNullable = (cultureParameterIndex >= 0) &&
                                         (symbol.Parameters[cultureParameterIndex].Type.NullableAnnotation != NullableAnnotation.NotAnnotated);

        // The destination as the method declares it: the return type, a nullable struct as it is, or the type of the
        // destination parameter. The destination is created under its name without its ?, which differs from the name
        // new takes for a type parameter declared T? only (GetCreatedTypeName).
        var declaredDestinationType = returnsDestination ? symbol.ReturnType : destinationType;
        var destinationNonNullableTypeName = GetNonNullableTypeName(destinationType);
        var model = new MapperMethodModel(
            Namespace: ns,
            ClassName: GetClassName(typeChain),
            TypeDeclarations: GetTypeDeclarations(typeChain),
            DeclarationModifiers: GetDeclarationModifiers(symbol, syntax),
            MethodName: symbol.Name,
            TypeParameterList: GetTypeParameterList(symbol),
            ConstraintClauses: GetConstraintClauses(symbol),
            SourceTypeName: sourceParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            SourceParameterName: IdentifierHelper.Escape(sourceParam.Name),
            SourceParameterModifiers: GetParameterModifiers(syntax, 0),
            SourceRefKind: sourceParam.RefKind,
            SourceDeclaredTypeName: sourceParam.Type.ToDisplayString(NullableQualifiedFormat),
            IsSourceParameterNullable: MayBeNullReference(sourceParam.Type),
            SourceNonNullableTypeName: GetNonNullableTypeName(sourceParam.Type),
            IsExtensionMethod: symbol.IsExtensionMethod,
            DestinationTypeName: destinationTypeName,
            DestinationParameterName: destinationParameterName,
            DestinationParameterModifiers: returnsDestination ? string.Empty : GetParameterModifiers(syntax, 1),
            DestinationRefKind: returnsDestination ? RefKind.None : symbol.Parameters[1].RefKind,
            DestinationDeclaredTypeName: declaredDestinationType.ToDisplayString(NullableQualifiedFormat),
            IsDestinationParameterNullable: !returnsDestination && MayBeNullReference(destinationType),
            DestinationNonNullableTypeName: destinationNonNullableTypeName,
            DestinationCreatedTypeName: destinationType is ITypeParameterSymbol ? GetCreatedTypeName(destinationType) : destinationNonNullableTypeName,
            DefaultReturnValue: declaredDestinationType.IsValueType || IsNullableReference(declaredDestinationType) ? "default" : "default!",
            ReturnsDestination: returnsDestination,
            ReturnNotNullIfNotNull: returnsDestination && MayBeNullReference(sourceParam.Type) && symbol.ReturnType.IsNullableType() &&
                                    CanApplyNotNullIfNotNull(context.SemanticModel.Compilation, containingType)
                ? sourceParam.Name
                : null,
            CultureParameterName: cultureParameterName,
            IsCultureParameterNullable: isCultureParameterNullable,
            IsInstance: !symbol.IsStatic,
            CustomParameters: new EquatableArray<CustomParameterModel>(customParameters));

        model = ParseMappingAttributes(symbol, model);

        // The profile gives the name comparison when the method does not, so it is read before the names
        // written in the attributes are resolved under that comparison
        model = ParseConverterAttributes(symbol, model);

        if (model.NameComparison is < (int)StringComparison.CurrentCulture or > (int)StringComparison.OrdinalIgnoreCase)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                Diagnostics.UndefinedNameComparison,
                syntax.Identifier.GetLocation(),
                symbol.Name,
                model.NameComparison.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        // The CultureInfo parameter that may be null goes to a parameter not taking null as the culture the conversions
        // go with, which the culture of the method gives for null
        if (model.IsCultureParameterNullable && (model.CultureParameterName is { } culture))
        {
            var nonNullArgument = GetNullableCultureArgument(model, culture);
            model = model with
            {
                CustomParameters = new EquatableArray<CustomParameterModel>(
                    model.CustomParameters.Select(p => p.Name == culture ? p with { NonNullArgument = nonNullArgument } : p).ToArray())
            };
        }

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

        var hiddenMethodError = ValidateHiddenMethods(symbol, model, syntax);
        if (hiddenMethodError is not null)
        {
            return Results.Error<MapperMethodModel>(hiddenMethodError);
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
        warnings.AddRange(CollectCultureParameterWarnings(model, syntax));

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

        // Strict mode reports the targets getting null or default without the mapping saying so, as the model is
        // complete by now
        if (model.Strict)
        {
            List<DiagnosticInfo> strictWarnings =
            [
                .. CollectNullableValueWarnings(model, symbol, sourceType, destinationType, containingType, compilation, syntax),
                .. CollectUnmatchedEnumMemberWarnings(model, syntax)
            ];
            if (strictWarnings.Count > 0)
            {
                model = model with { Warnings = new([.. model.Warnings, .. strictWarnings]) };
            }
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

    private static string GetNamespaceName(INamedTypeSymbol type) =>
        String.IsNullOrEmpty(type.ContainingNamespace.Name) ? string.Empty : type.ContainingNamespace.ToDisplayString(NamespaceNameFormat);

    private static string GetClassName(IEnumerable<INamedTypeSymbol> typeChain) =>
        String.Join(".", typeChain.Select(GetTypeName));

    private static EquatableArray<string> GetTypeDeclarations(IEnumerable<INamedTypeSymbol> typeChain) =>
        new(typeChain.Select(static t => t.GetDeclarationKeyword() + " " + GetTypeDeclarationName(t)).ToArray());

    // Whether the generated code can put [return: NotNullIfNotNull] on the mapper: the attribute is there to name, one
    // declaration of it the mapper class can access (of .NET Core 3.0 or later, or a copy of it), as a name declared
    // twice finds none.
    private static bool CanApplyNotNullIfNotNull(Compilation compilation, INamedTypeSymbol within) =>
        (compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute") is { } attribute) &&
        compilation.IsSymbolAccessibleWithin(attribute, within);

    // The implementation of a mapper reported with an error: its declaration repeated as the generated code repeats
    // it (the containing types, the modifiers, nullable annotations and type parameters with their constraints of the
    // defining declaration), with a body throwing. The build fails on the error, so the body never runs.
    private static MapperMethodModel CreatePlaceholder(IMethodSymbol symbol, MethodDeclarationSyntax syntax, List<INamedTypeSymbol> typeChain)
    {
        var parameters = symbol.Parameters.Select((parameter, i) =>
        {
            var modifiers = GetParameterModifiers(syntax, i);
            return ((i == 0) && symbol.IsExtensionMethod ? "this " : string.Empty) +
                   (modifiers.Length > 0 ? modifiers + " " : string.Empty) +
                   parameter.Type.ToDisplayString(NullableQualifiedFormat) + " " + IdentifierHelper.Escape(parameter.Name);
        });
        var returnType = symbol.ReturnsVoid
            ? "void"
            : (symbol.ReturnsByRefReadonly ? "ref readonly " : symbol.ReturnsByRef ? "ref " : string.Empty) + symbol.ReturnType.ToDisplayString(NullableQualifiedFormat);
        return new MapperMethodModel(
            Namespace: GetNamespaceName(symbol.ContainingType),
            ClassName: GetClassName(typeChain),
            TypeDeclarations: GetTypeDeclarations(typeChain),
            DeclarationModifiers: GetDeclarationModifiers(symbol, syntax),
            MethodName: symbol.Name,
            TypeParameterList: GetTypeParameterList(symbol),
            ConstraintClauses: GetConstraintClauses(symbol),
            IsInstance: !symbol.IsStatic,
            IsPlaceholder: true,
            PlaceholderReturnType: returnType,
            PlaceholderParameters: String.Join(", ", parameters));
    }

    // The modifiers of the defining declaration the implementation has to repeat: its accessibility modifiers (CS8799),
    // none for a declaration without one, which is implicitly private and one writing private would not match, new,
    // virtual, sealed and override (CS8800), readonly of a struct member (CS8663) and unsafe (CS0764).
    private static string GetDeclarationModifiers(IMethodSymbol symbol, MethodDeclarationSyntax syntax)
    {
        var hasAccessibility = false;
        foreach (var modifier in syntax.Modifiers)
        {
            hasAccessibility |= SyntaxFacts.IsAccessibilityModifier(modifier.Kind());
        }

        var modifiers = hasAccessibility ? symbol.DeclaredAccessibility.ToText() : string.Empty;
        foreach (var kind in RepeatedModifiers)
        {
            if (syntax.Modifiers.Any(kind))
            {
                modifiers = modifiers.Length == 0 ? SyntaxFacts.GetText(kind) : modifiers + " " + SyntaxFacts.GetText(kind);
            }
        }

        return modifiers;
    }

    // Whether the type is a collection that a mapper would map by its members (Count, Capacity) and not by its elements:
    // one of the framework (System.Collections and the namespaces under it, lists, sets and dictionaries, their
    // interfaces, the immutable, frozen and concurrent ones, and PriorityQueue<TElement, TPriority>, the one collection
    // there not implementing IEnumerable), a class deriving from one (class ItemList : List<Item>), which is as much a
    // collection, an array or a tuple, the views over the elements of an array or of memory (ArraySegment<T>, Memory<T>,
    // ReadOnlyMemory<T>, Span<T>, ReadOnlySpan<T>), and a type parameter one of whose constraints is one (where
    // T : List<Item>). A type of its own only implementing IEnumerable<T> (a page of items with its count), an interface
    // as well, is mapped by its members.
    private static bool IsWholeCollection(ITypeSymbol type)
    {
        if ((type is IArrayTypeSymbol) || type.IsTupleType ||
            (type is INamedTypeSymbol
            {
                Name: "Tuple" or "ValueTuple" or "ArraySegment" or "Memory" or "ReadOnlyMemory" or "Span" or "ReadOnlySpan",
                ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true }
            }))
        {
            return true;
        }

        // A type parameter has the members of its constraint types, as the mapper maps it (C# allows no circular
        // constraints, so this ends)
        if (type is ITypeParameterSymbol typeParameter)
        {
            return typeParameter.ConstraintTypes.Any(IsWholeCollection);
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (IsInCollectionsNamespace(current.ContainingNamespace) &&
                ((current.SpecialType == SpecialType.System_Collections_IEnumerable) ||
                 current.AllInterfaces.Any(static i => i.SpecialType == SpecialType.System_Collections_IEnumerable) ||
                 current is INamedTypeSymbol { MetadataName: "PriorityQueue`2", ContainingNamespace.Name: "Generic" }))
            {
                return true;
            }
        }

        return false;

        static bool IsInCollectionsNamespace(INamespaceSymbol? ns)
        {
            for (; ns is { IsGlobalNamespace: false }; ns = ns.ContainingNamespace)
            {
                if ((ns.Name == "Collections") && ns.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
                {
                    return true;
                }
            }

            return false;
        }
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

    // A reference type declared with ?, or with nullable annotations disabled (oblivious), which says nothing about
    // null: a value of it may be null. The values the generated code reads (the source, its members and the elements
    // of its collections) are taken so, and checked for null the way a nullable one is.
    private static bool MayBeNullReference(ITypeSymbol type) =>
        type.IsReferenceType && (type.NullableAnnotation != NullableAnnotation.NotAnnotated);

    // A value of the type may be null: a nullable struct, or a reference type that may be null (MayBeNullReference).
    private static bool MayBeNull(ITypeSymbol type) =>
        type.IsNullableType() || MayBeNullReference(type);

    // The value of a source property may be null: its type may be, or its getter says it may return null
    // (ReturnsMaybeNull).
    private static bool MayBeNullMember(IPropertySymbol property) =>
        MayBeNull(property.Type) || ReturnsMaybeNull(property);

    // A property of a reference type whose getter may return null all the same, as [MaybeNull] on the property, or on
    // the return of its getter, says. It is taken as a nullable one: null handling applies to it, and Strict mode reports
    // it as a value that may be null. The attributes of the property the path binds to count, an override's own.
    private static bool ReturnsMaybeNull(IPropertySymbol? property) =>
        (property is not null) && property.Type.IsReferenceType &&
        (HasMaybeNull(property.GetAttributes()) || ((property.GetMethod is { } getter) && HasMaybeNull(getter.GetReturnTypeAttributes())));

    // A method returning a reference type whose return [MaybeNull] says may be null: a method of the source, or one an
    // attribute names
    private static bool ReturnsMaybeNull(IMethodSymbol method) =>
        method.ReturnType.IsReferenceType && HasMaybeNull(method.GetReturnTypeAttributes());

    private static bool HasMaybeNull(ImmutableArray<AttributeData> attributes) =>
        attributes.Any(static a => a.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.MaybeNullAttribute");

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

            if (InstanceMethodOfStaticMapper(model, containingType, mapping.ConditionMethod!, ConditionAttributeIndex(model, mapping.TargetPath), compilation, syntax) is { } instanceError)
            {
                return instanceError;
            }

            var valueType = GetSourceValueType(mapping, sourceType, containingType, compilation);
            var match = MatchValueMethod(
                LookupMethods(containingType, mapping.ConditionMethod!, model.IsInstance, compilation),
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
                ConditionCustomArguments = match.CustomArguments,
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

    // The method a converter, a condition or a [MapUsing] method call binds to, how the value goes to it and the custom
    // parameters it takes (CustomArguments), or for a return type mismatch, the method the call binds to all the same.
    internal readonly record struct ValueMethodMatch(
        ConverterMatchResult Result,
        IMethodSymbol? Method,
        ValueMatch Match,
        IMethodSymbol? Mismatched,
        EquatableArray<int> CustomArguments = default);

    // A method a call may bind to, how the value goes to it, and the custom parameters its parameters after the value
    // take (MapCustomArguments), null when they do not all take one
    internal readonly record struct ValueCandidate(IMethodSymbol Method, ValueMatch Match, int[]? Custom);

    // Matches the methods of the name taking the source value, then the custom parameters of the mapper they declare,
    // and returning what the target takes (returnsTarget), as the generated call (call) binds: one taking more of the
    // custom parameters over one taking fewer, as one taking them went before one without, each number of them bound on
    // its own (BindValueCall), and none of several taking as many but other ones (TakeDifferentCustomParameters). The
    // methods are those the name is looked up as (LookupMethods), which the call binds among.
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
            .Where(static m => m.Parameters.Length > 0)
            .Select(m => new ValueCandidate(m, MatchValueParameter(m.Parameters[0], call.ValueType, call.ValueTypeName, call.Argument, compilation), MapCustomArguments(m, 1, customParams, call.CustomTypes, compilation)))
            .ToList();

        IMethodSymbol? mismatched = null;
        foreach (var count in candidates.Where(static c => c.Custom is not null).Select(static c => c.Custom!.Length).Distinct().OrderByDescending(static c => c))
        {
            // The candidates a call may bind to are looked at only when some take other custom parameters, which the one
            // candidate there usually is does not
            if (HasDifferentCustomParameters(candidates, count) &&
                TakeDifferentCustomParameters(candidates
                    .Where(c => IsValueCandidateOf(c, count) && (c.Method.GetObsoleteKind() != ObsoleteKind.Error) && returnsTarget(c.Method))
                    .Select(static c => c.Custom!)
                    .ToList()))
            {
                break;
            }

            var (method, match, custom, bindMismatched) = BindValueCall(candidates, call, count, returnsTarget, compilation);
            if (method is not null)
            {
                return new ValueMethodMatch(
                    count > 0 ? ConverterMatchResult.MatchWithCustomParams : ConverterMatchResult.MatchWithoutCustomParams,
                    method,
                    match,
                    null,
                    new EquatableArray<int>(custom!));
            }

            mismatched ??= bindMismatched;
        }

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

    // The custom parameters of the mapper the parameters of a method from the index take, as their indexes in the order
    // of the method's parameters: each parameter takes the custom parameter of its type, or, when the mapper or the
    // method has several parameters of that type, the one of its name, whatever the order, and the method declares the
    // ones it takes only. Null when a parameter takes none, or cannot take the one it gets (a ref parameter a read-only
    // one would go to), or two parameters would take the same one. A parameter taking a value that does not take null
    // gets the CultureInfo parameter that may be null as the culture the conversions go with (NonNullArgument), which
    // its index tells by its complement (CustomParameterIndex).
    internal static int[]? MapCustomArguments(
        IMethodSymbol method,
        int firstIndex,
        EquatableArray<CustomParameterModel> customParams,
        IReadOnlyList<ITypeSymbol> customTypes,
        Compilation compilation)
    {
        var count = method.Parameters.Length - firstIndex;
        if (count <= 0)
        {
            return count < 0 ? null : [];
        }

        var parameters = method.Parameters.Skip(firstIndex).ToList();
        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            var parameter = parameters[i];
            var ofType = Enumerable.Range(0, customParams.Count).Where(j => HasIdentityConversion(customTypes[j], parameter.Type, compilation)).ToList();
            var index = (ofType.Count == 1) && (parameters.Count(p => HasIdentityConversion(p.Type, parameter.Type, compilation)) == 1)
                ? ofType[0]
                : ofType.Where(j => customParams[j].Name == IdentifierHelper.Escape(parameter.Name)).DefaultIfEmpty(-1).First();
            if ((index < 0) ||
                !CanTakeArgument(parameter.RefKind, GetVariableKind(customParams[index].RefKind)) ||
                IsTakenBefore(result, i, index))
            {
                return null;
            }

            result[i] = (customParams[index].NonNullArgument is not null) &&
                        (parameter.RefKind is RefKind.None or RefKind.In) &&
                        (parameter.NullableAnnotation == NullableAnnotation.NotAnnotated)
                ? ~index
                : index;
        }

        return result;
    }

    // The custom parameter of an index MapCustomArguments gives, whose complement stands for the culture a parameter
    // not taking null gets instead of the parameter
    internal static int CustomParameterIndex(int index) => index < 0 ? ~index : index;

    // Whether a parameter before the one at the position takes the custom parameter of the index
    private static bool IsTakenBefore(int[] customArguments, int position, int index)
    {
        for (var i = 0; i < position; i++)
        {
            if (CustomParameterIndex(customArguments[i]) == index)
            {
                return true;
            }
        }

        return false;
    }

    // Whether the methods a call may bind to, taking as many of the custom parameters, take other ones: the call of
    // each passes other arguments, so nothing chooses among them, and none is taken, whatever the order they are
    // declared in
    internal static bool TakeDifferentCustomParameters(IReadOnlyList<int[]> customArguments) =>
        customArguments.Skip(1).Any(c => !c.SequenceEqual(customArguments[0]));

    // A candidate taking the value and that many of the custom parameters, not a generic one (BindValueCall)
    private static bool IsValueCandidateOf(ValueCandidate candidate, int count) =>
        (candidate.Match != ValueMatch.None) && (candidate.Custom?.Length == count) && !candidate.Method.IsGenericMethod;

    // Whether the value candidates taking that many of the custom parameters take other ones, whether a call may bind to
    // them or not, which looks at the one there usually is without allocating
    private static bool HasDifferentCustomParameters(List<ValueCandidate> candidates, int count)
    {
        int[]? first = null;
        foreach (var candidate in candidates)
        {
            if (!IsValueCandidateOf(candidate, count))
            {
                continue;
            }

            if (first is null)
            {
                first = candidate.Custom;
                continue;
            }

            for (var i = 0; i < count; i++)
            {
                if (first[i] != candidate.Custom![i])
                {
                    return true;
                }
            }
        }

        return false;
    }

    // The argument a call passes to the parameter for an index MapCustomArguments gives: the custom parameter, with the
    // modifier of the parameter, or the culture not null, a value
    private static CallArgument CustomCallArgument(int index, IReadOnlyList<ITypeSymbol> customTypes, IParameterSymbol parameter) =>
        new(customTypes[CustomParameterIndex(index)], index < 0 ? RefKind.None : GetArgumentModifierKind(parameter.RefKind));

    // Whether the types are the same type as C# converts between them by identity: whatever the nullable annotations and
    // the names of the tuple elements, and dynamic as object, nint as IntPtr. The conversion is classified only for types
    // that may be so (MayConvertByIdentity), as the types of the custom parameters differ from most they are matched with.
    private static bool HasIdentityConversion(ITypeSymbol type, ITypeSymbol other, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(type, other) ||
        (MayConvertByIdentity(type, other) && compilation.ClassifyCommonConversion(type, other).IsIdentity);

    // Whether types that are not the same symbol may be the same type: dynamic, a tuple or a native integer, two arrays,
    // or two types of the same definition, whose type arguments may be
    private static bool MayConvertByIdentity(ITypeSymbol type, ITypeSymbol other) =>
        (type.TypeKind == TypeKind.Dynamic) || (other.TypeKind == TypeKind.Dynamic) ||
        ((type.TypeKind == TypeKind.Array) && (other.TypeKind == TypeKind.Array)) ||
        ((type is INamedTypeSymbol named) && (other is INamedTypeSymbol otherNamed) &&
         (named.IsTupleType || otherNamed.IsTupleType || named.IsNativeIntegerType || otherNamed.IsNativeIntegerType ||
          SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, otherNamed.OriginalDefinition)));

    // What the call of a converter, a condition or a [MapUsing] method passes: the value, of the type (its symbol, null
    // when it is not found, which leaves only the exact match by name) and how (a value, or a variable the call passes
    // as the method takes it), and the custom parameters, of their types.
    internal sealed record ValueCall(ITypeSymbol? ValueType, string ValueTypeName, ArgumentKind Argument, IReadOnlyList<ITypeSymbol> CustomTypes);

    // The method a call taking that many of the custom parameters binds to. The candidates matched are the methods
    // taking that many (MapCustomArguments), not generic ones. A candidate taking the value as
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
    private static (IMethodSymbol? Method, ValueMatch Match, int[]? Custom, IMethodSymbol? Mismatched) BindValueCall(
        List<ValueCandidate> candidates,
        ValueCall call,
        int customCount,
        Func<IMethodSymbol, bool> returnsTarget,
        Compilation compilation)
    {
        var applicable = candidates
            .Where(x => (x.Match != ValueMatch.None) && !x.Method.IsGenericMethod && (x.Custom is { } custom) && (custom.Length == customCount))
            .ToList();
        var methods = candidates.Select(static x => x.Method).ToList();

        (IMethodSymbol? Method, ValueMatch Match, int[]? Custom, IMethodSymbol? Mismatched) Decide(ValueCandidate candidate) =>
            candidate.Method.GetObsoleteKind() == ObsoleteKind.Error
                ? (null, ValueMatch.None, null, null)
                : returnsTarget(candidate.Method) ? (candidate.Method, candidate.Match, candidate.Custom, null) : (null, ValueMatch.None, null, candidate.Method);

        bool BindsTo(ValueCandidate candidate) =>
            SymbolEqualityComparer.Default.Equals(BindCall(methods, GetValueCallArguments(candidate, call), compilation), candidate.Method);

        var exact = applicable.Where(static x => x.Match == ValueMatch.Exact).ToList();
        if (exact.Count > 0)
        {
            var callable = exact.Where(static x => x.Method.GetObsoleteKind() != ObsoleteKind.Error).ToList();
            if (callable.Count == 0)
            {
                return (null, ValueMatch.None, null, null);
            }

            ValueCandidate? chosen = null;
            foreach (var candidate in callable)
            {
                if (returnsTarget(candidate.Method))
                {
                    chosen = (chosen is null) || (!TakesAllByValue(chosen.Value.Method) && TakesAllByValue(candidate.Method)) ? candidate : chosen;
                }
            }

            if (chosen is not { } selected)
            {
                return (null, ValueMatch.None, null, callable[0].Method);
            }

            if ((call.ValueType is null) || BindsTo(selected))
            {
                return (selected.Method, ValueMatch.Exact, selected.Custom, null);
            }

            var bound = BindCall(methods, GetValueCallArguments(selected, call), compilation);
            var other = applicable.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Method, bound));
            return (other.Method is not null) && BindsTo(other) ? Decide(other) : (null, ValueMatch.None, null, null);
        }

        // The value a nullable struct holds to the struct first, then the value by a conversion, then the value a
        // nullable struct holds by a conversion
        foreach (var candidate in applicable.OrderBy(static x => x.Match switch { ValueMatch.Unwrap => 0, ValueMatch.Convert => 1, _ => 2 }))
        {
            if (BindsTo(candidate))
            {
                return Decide(candidate);
            }
        }

        return (null, ValueMatch.None, null, null);
    }

    // The arguments the call passes to a candidate: the value, or the value a nullable struct holds, as a value, or for
    // a variable with the modifier of the parameter taking it, and the custom parameters the candidate takes, in the
    // order of its parameters.
    private static List<CallArgument> GetValueCallArguments(ValueCandidate candidate, ValueCall call)
    {
        var method = candidate.Method;
        var unwraps = candidate.Match is ValueMatch.Unwrap or ValueMatch.UnwrapConvert;
        var arguments = new List<CallArgument>
        {
            new(
                unwraps ? GetNullableValueType(call.ValueType!)! : call.ValueType!,
                unwraps || (call.Argument == ArgumentKind.Value) ? RefKind.None : GetArgumentModifierKind(method.Parameters[0].RefKind))
        };
        for (var i = 0; i < candidate.Custom!.Length; i++)
        {
            arguments.Add(CustomCallArgument(candidate.Custom[i], call.CustomTypes, method.Parameters[1 + i]));
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

    // The method a call of the methods with the arguments binds to, as C# binds it, or null when none takes them or
    // the call is ambiguous (CS0121): the one taking them better than all the others (IsBetterCall). The methods are
    // those the name is looked up as (LookupMethods), of which one of a derived class taking the arguments leaves out
    // those of its base classes. A method obsolete as an error takes part, as C# binds the call to it and then reports
    // it (CS0619). A generic method takes the arguments its type parameters are inferred from as the types they are
    // inferred as (InferParameterType), the constraints aside.
    private static IMethodSymbol? BindCall(IEnumerable<IMethodSymbol> methods, IReadOnlyList<CallArgument> arguments, Compilation compilation)
    {
        var taking = new List<CallCandidate>();
        foreach (var method in methods)
        {
            if ((ApplyCall(method, arguments, false, compilation) ?? ApplyCall(method, arguments, true, compilation)) is { } candidate)
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

    // The methods a call of the name in the mapper binds among, as C# looks the simple name of a call up: the members
    // of the name the mapper class can access and the call can invoke, in it and its base classes, or, when it has
    // none, in the class containing it and its base classes, and so on outward, and last the methods the global using
    // static directives import (LookupGlobalStaticImports). The first of these classes having such a member is the
    // only one looked in, even when its methods do not take the arguments; a member the call cannot invoke (a property
    // or a field not of a delegate type, or a nested type) is passed over, as C# passes it over for a call. A member of
    // a derived class hides one of a base class of the same signature, and every member of a base class when it is not
    // a method. Instance methods are those of the mapper class and its base classes, which an instance mapper calls on
    // itself; a static mapper, and any mapper for the classes containing the mapper class, calls static methods only.
    internal static List<IMethodSymbol> LookupMethods(INamedTypeSymbol mapperClass, string name, bool instance, Compilation compilation)
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
                return MethodsOf(members, instance && SymbolEqualityComparer.Default.Equals(scope, mapperClass));
            }
        }

        return LookupGlobalStaticImports(mapperClass, name, compilation);
    }

    // A static mapper naming a method the mapper class and its base classes have only as instance methods, which it
    // cannot call, reported at the attribute naming it
    internal static DiagnosticInfo? InstanceMethodOfStaticMapper(
        MapperMethodModel model,
        INamedTypeSymbol mapperClass,
        string name,
        int attributeIndex,
        Compilation compilation,
        MethodDeclarationSyntax syntax) =>
        !model.IsInstance && NamesInstanceMethodsOnly(mapperClass, name, compilation)
            ? new DiagnosticInfo(Diagnostics.InstanceMethodOfStaticMapper, LocationOf(model, attributeIndex, syntax), model.MethodName, name)
            : null;

    // A parameter of the mapper of the name of a method its attributes name, which the generated code calls by the name,
    // so that the call would take the parameter (a delegate) or fail (CS0149), reported at the first attribute naming
    // one (SMP0104)
    private static DiagnosticInfo? ValidateHiddenMethods(IMethodSymbol symbol, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        string? hidden = null;
        var hiddenIndex = Int32.MaxValue;
        Check(model.BeforeMapMethod, model.BeforeMapAttributeIndex);
        Check(model.AfterMapMethod, model.AfterMapAttributeIndex);
        foreach (var mapping in model.PropertyMappings)
        {
            Check(mapping.ConverterMethod, mapping.AttributeIndex);
        }

        foreach (var condition in model.PropertyConditions)
        {
            Check(condition.ConditionMethod, condition.AttributeIndex);
        }

        foreach (var mapUsing in model.MapUsingMappings)
        {
            Check(mapUsing.Method, mapUsing.AttributeIndex);
        }

        foreach (var mapNested in model.MapNestedMappings)
        {
            Check(mapNested.Mapper, mapNested.AttributeIndex);
        }

        foreach (var mapCollection in model.MapCollectionMappings)
        {
            Check(mapCollection.Mapper, mapCollection.AttributeIndex);
        }

        return hidden is null ? null : new DiagnosticInfo(Diagnostics.ParameterHidesMethod, LocationOf(model, hiddenIndex, syntax), model.MethodName, hidden);

        // The name hidden by a parameter the attribute written first names
        void Check(string? name, int attributeIndex)
        {
            if ((name is null) || (attributeIndex >= hiddenIndex))
            {
                return;
            }

            foreach (var parameter in symbol.Parameters)
            {
                if (parameter.Name == name)
                {
                    hidden = name;
                    hiddenIndex = attributeIndex;
                    return;
                }
            }
        }
    }

    // Whether the name finds instance methods only in the mapper class and its base classes, which a static mapper
    // cannot call (SMP0105): those the lookup finds, as LookupMethods, one hidden by a method of a derived class passed
    // over
    private static bool NamesInstanceMethodsOnly(INamedTypeSymbol mapperClass, string name, Compilation compilation)
    {
        var members = new List<ISymbol>();
        for (var type = mapperClass; type is not null; type = type.BaseType)
        {
            var declared = type.GetMembers(name)
                .Where(m => IsInvocable(m) && compilation.IsSymbolAccessibleWithin(m, mapperClass) && !IsHiddenBy(m, members))
                .ToList();
            members.AddRange(declared);
        }

        var methods = members.OfType<IMethodSymbol>().ToList();
        return (methods.Count > 0) && methods.All(static m => !m.IsStatic);
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

    private static List<IMethodSymbol> MethodsOf(IEnumerable<ISymbol> members, bool instance) =>
        members
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic || instance)
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

        return MethodsOf(members, instance: false);
    }

    // Whether a member of a base class is hidden by those of the derived classes found before it: by one that is not a
    // method, or, for a method, by one of the same signature, and for another member, by any method.
    private static bool IsHiddenBy(ISymbol member, IEnumerable<ISymbol> derived) =>
        derived.Any(d => (d is not IMethodSymbol derivedMethod) ||
                         (member is not IMethodSymbol method) ||
                         HasSameSignature(derivedMethod, method));

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

            if (InstanceMethodOfStaticMapper(model, containingType, mapping.ConverterMethod!, mapping.AttributeIndex, compilation, syntax) is { } instanceError)
            {
                return instanceError;
            }

            // A converter returns the target type, or a type converting to it implicitly as the assignment of its
            // result does
            var targetType = GetMappingTargetType(mapping, model, destinationType, constructor, containingType, compilation);
            var valueType = GetSourceValueType(mapping, sourceType, containingType, compilation);
            var match = MatchValueMethod(
                LookupMethods(containingType, mapping.ConverterMethod!, model.IsInstance, compilation),
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

            // The converter gets a value that is not null when the source is not of a nullable type, or when the
            // generated code calls it for a value only: with NullValue, for a parameter not taking null, and with
            // NullBehavior.Skip where the assignment is a statement (not a constructor argument or an initializer entry)
            var unwraps = match.Match is ValueMatch.Unwrap or ValueMatch.UnwrapConvert;
            var rejectsNull = unwraps || RejectsNull(matchedMethod, valueType, compilation);
            var getsValue = !mapping.IsSourceNullable || rejectsNull || mapping.HasNullValue() ||
                            ((mapping.NullBehavior == NullBehaviorType.Skip) && !mapping.IsConstructorParameter &&
                             !(model.UseConstructorMapping && (mapping.IsTargetInitOnly || mapping.IsTargetRequired)));
            resolved.Add(mapping with
            {
                ConverterCustomArguments = match.CustomArguments,
                ConverterParameterRefKinds = GetParameterRefKinds(matchedMethod),
                ConverterUnwrapsSource = unwraps,
                ConverterRejectsNull = rejectsNull,
                ConverterForgivesNull = (IsNullableReference(matchedMethod.ReturnType) || ReturnsMaybeNull(matchedMethod)) && (targetType is not null) && !targetType.IsNullableType() &&
                                        !(getsValue && ReturnsNotNullForValue(matchedMethod, compilation))
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
            if (InstanceMethodOfStaticMapper(model, containingType, model.BeforeMapMethod!, model.BeforeMapAttributeIndex, compilation, syntax) is { } instanceError)
            {
                return instanceError;
            }

            var (matchResult, matchedMethod, customArguments) = FindMatchingCallbackMethod(
                LookupMethods(containingType, model.BeforeMapMethod!, model.IsInstance, compilation),
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
                BeforeMapCustomArguments = customArguments,
                BeforeMapParameterRefKinds = GetParameterRefKinds(matchedMethod)
            };
        }

        if (!String.IsNullOrEmpty(model.AfterMapMethod))
        {
            if (InstanceMethodOfStaticMapper(model, containingType, model.AfterMapMethod!, model.AfterMapAttributeIndex, compilation, syntax) is { } instanceError)
            {
                return instanceError;
            }

            var (matchResult, matchedMethod, customArguments) = FindMatchingCallbackMethod(
                LookupMethods(containingType, model.AfterMapMethod!, model.IsInstance, compilation),
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
                AfterMapCustomArguments = customArguments,
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

    // The callback taking the source, the destination, and the custom parameters of the mapper it declares, one taking
    // more of them over one taking fewer, as one taking them went before one without (BindCallbackCall), and the
    // custom parameters it takes.
    internal static (CallbackMatchResult Result, IMethodSymbol? Method, EquatableArray<int> CustomArguments) FindMatchingCallbackMethod(
        List<IMethodSymbol> methods,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        IReadOnlyList<ITypeSymbol> customTypes,
        Compilation compilation)
    {
        var counts = methods
            .Where(static m => m.Parameters.Length >= 2)
            .Select(m => MapCustomArguments(m, 2, model.CustomParameters, customTypes, compilation))
            .Where(static c => c is not null)
            .Select(static c => c!.Length)
            .Distinct()
            .OrderByDescending(static c => c);
        foreach (var count in counts)
        {
            if (BindCallbackCall(methods, model, sourceType, destinationType, customTypes, count, compilation, out var ambiguous) is { } matched)
            {
                return (count > 0 ? CallbackMatchResult.MatchWithCustomParams : CallbackMatchResult.MatchWithoutCustomParams, matched.Method, new EquatableArray<int>(matched.Custom));
            }

            if (ambiguous)
            {
                break;
            }
        }

        return (CallbackMatchResult.NoMatch, null, default);
    }

    // The callback a call with that many arguments binds to. The source and the destination are variables: parameters
    // of the mapper, or the instance a return-type mapper builds, which the callback may write. Each goes to a
    // parameter of its type the modifier of which can take it, or, by value, of a base class or an interface it
    // converts to by an implicit reference conversion; a struct, which would be boxed into a copy the callback writes
    // in vain, goes to its own type only. The custom parameters the callback declares go to them as MapCustomArguments
    // maps them, the candidates taking that many of them (customCount) matched. Of the candidates
    // taking both as their own types, the one taking every argument by value, as before, which the call binds to, as C#
    // binds it among all the methods the name is looked up as (BindCall), or else to another candidate, which is used
    // instead; otherwise the candidate the call binds to. A call binding to none of them, or to one obsolete as an
    // error, matches nothing, and candidates taking other custom parameters are ambiguous (TakeDifferentCustomParameters).
    private static (IMethodSymbol Method, int[] Custom)? BindCallbackCall(
        List<IMethodSymbol> methods,
        MapperMethodModel model,
        ITypeSymbol sourceType,
        ITypeSymbol destinationType,
        IReadOnlyList<ITypeSymbol> customTypes,
        int customCount,
        Compilation compilation,
        out bool ambiguous)
    {
        var sourceKind = GetVariableKind(model.SourceRefKind);
        var destinationKind = model.ReturnsDestination ? ArgumentKind.WritableVariable : GetVariableKind(model.DestinationRefKind);

        var candidates = new List<(IMethodSymbol Method, bool Exact)>();
        var customs = new Dictionary<IMethodSymbol, int[]>(SymbolEqualityComparer.Default);
        foreach (var method in methods)
        {
            if (method.IsGenericMethod || (method.Parameters.Length < 2) ||
                (MapCustomArguments(method, 2, model.CustomParameters, customTypes, compilation) is not { } custom) || (custom.Length != customCount))
            {
                continue;
            }

            customs[method] = custom;

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
            var custom = customs[method];
            for (var i = 0; i < custom.Length; i++)
            {
                arguments.Add(CustomCallArgument(custom[i], customTypes, method.Parameters[2 + i]));
            }

            return arguments;
        }

        bool BindsTo(IMethodSymbol method) =>
            SymbolEqualityComparer.Default.Equals(BindCall(methods, ArgumentsFor(method), compilation), method);

        ambiguous = (candidates.Count > 1) &&
                    TakeDifferentCustomParameters(candidates.Select(x => customs[x.Method]).ToList()) &&
                    TakeDifferentCustomParameters(candidates.Where(static x => x.Method.GetObsoleteKind() != ObsoleteKind.Error).Select(x => customs[x.Method]).ToList());
        if (ambiguous)
        {
            return null;
        }

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
                return (chosen!, customs[chosen!]);
            }

            var bound = BindCall(methods, ArgumentsFor(chosen!), compilation);
            var other = candidates.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Method, bound)).Method;
            return (other is not null) && (other.GetObsoleteKind() != ObsoleteKind.Error) && BindsTo(other) ? (other, customs[other]) : null;
        }

        if (candidates.Any(static x => x.Exact))
        {
            return null;
        }

        foreach (var (method, _) in candidates)
        {
            if (BindsTo(method))
            {
                return method.GetObsoleteKind() == ObsoleteKind.Error ? null : (method, customs[method]);
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
        var strictSet = model.StrictExplicitlySet;
        var nameComparisonOption = model.NameComparison;
        var nameComparisonSet = model.NameComparisonExplicitlySet;
        var cultureOption = model.Culture;
        var cultureSet = model.CultureExplicitlySet;
        var dateTimeFormatOption = model.DateTimeFormat;
        var numberFormatOption = model.NumberFormat;
        var useCurrentCulture = model.UseCurrentCulture;
        var defaultCultureSet = false;

        // The attributes of the method are at their index in AttributeLocations (ParseMappingAttributes); those of
        // the class and the assembly the diagnostics are about are added after them
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
                ApplyProfile(attribute, containingType, ofAssembly: false);
            }
        }

        // The profile of the assembly gives what neither the method nor the profile of the class sets
        foreach (var attribute in containingType.ContainingAssembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == Names.MapperProfileAttribute)
            {
                ApplyProfile(attribute, containingType, ofAssembly: true);
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
            UseCurrentCulture = useCurrentCulture,
            AttributeLocations = new(attributeLocations),
            ValueConverterAttributeIndex = valueConverterAttributeIndex,
            CultureAttributeIndex = cultureAttributeIndex,
            FormatAttributeIndex = formatAttributeIndex
        };

        // Each setting of a profile applies unless the mapper method or a profile closer to it sets it, Culture,
        // DateTimeFormat and NumberFormat each on its own. A culture of the profile of the assembly that is not a culture
        // name is reported once for the assembly (ValidateAssemblyProfile), so the mappers go without it.
        void ApplyProfile(AttributeData attribute, INamedTypeSymbol owner, bool ofAssembly)
        {
            var profileAttributeIndex = attributeLocations.Count;
            attributeLocations.Add(GetAttributeLocation(attribute, owner));
            foreach (var namedArg in attribute.NamedArguments)
            {
                if ((namedArg.Key == "Strict") && (namedArg.Value.Value is bool strict) && !strictSet)
                {
                    strictOption = strict;
                    strictSet = true;
                }
                else if ((namedArg.Key == "NameComparison") && (namedArg.Value.Value is int nc) && !nameComparisonSet)
                {
                    nameComparisonOption = nc;
                    nameComparisonSet = true;
                }
                else if ((namedArg.Key == "DefaultCulture") && (namedArg.Value.Value is int defaultCulture) && !defaultCultureSet)
                {
                    useCurrentCulture = defaultCulture == 1;
                    defaultCultureSet = true;
                }
                else if ((namedArg.Key == "Culture") && (namedArg.Value.Value is string profileCulture) && !cultureSet &&
                         !(ofAssembly && (profileCulture.Length > 0) && !IsValidCultureName(profileCulture)))
                {
                    cultureOption = profileCulture;
                    cultureAttributeIndex = profileAttributeIndex;
                    cultureSet = true;
                }
                else if ((namedArg.Key == "DateTimeFormat") && (namedArg.Value.Value is string profileDtFmt) && (dateTimeFormatOption is null))
                {
                    dateTimeFormatOption = profileDtFmt;
                    formatAttributeIndex = formatAttributeIndex < 0 ? profileAttributeIndex : formatAttributeIndex;
                }
                else if ((namedArg.Key == "NumberFormat") && (namedArg.Value.Value is string profileNumFmt) && (numberFormatOption is null))
                {
                    numberFormatOption = profileNumFmt;
                    formatAttributeIndex = formatAttributeIndex < 0 ? profileAttributeIndex : formatAttributeIndex;
                }
            }
        }
    }
}
