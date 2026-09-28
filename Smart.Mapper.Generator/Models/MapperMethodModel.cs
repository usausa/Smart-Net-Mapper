namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Represents a mapper method model.
internal sealed record MapperMethodModel(
    // The names of the namespace, the class and the method as declared, which name the generated file and
    // the diagnostics; the generated code writes them escaped (IdentifierHelper). ClassName is the chain of
    // the containing types (Outer.Inner), and TypeDeclarations declares each of them, outermost first, as
    // the generated code repeats it (class Outer<T>, record Inner)
    string Namespace = default!,
    string ClassName = default!,
    EquatableArray<string> TypeDeclarations = default,
    // The modifiers of the defining declaration the implementation repeats besides static (IsInstance) and partial: its
    // accessibility (public, protected internal; CS8799), none for a declaration without one (a void mapper, implicitly
    // private), new (CS8800) and unsafe (CS0764)
    string DeclarationModifiers = "",
    string MethodName = default!,
    // The type parameters of a generic mapper method (<T>) and their constraints ( where T : class), which
    // the implementing declaration repeats (CS0759 without the type parameters, CS0761 with other constraints)
    string TypeParameterList = "",
    string ConstraintClauses = "",
    string SourceTypeName = default!,
    // The parameter names as the generated code writes them, a keyword with its @
    string SourceParameterName = default!,
    // Modifiers the defining declaration puts on the parameter besides this (in, ref readonly, ref,
    // scoped, params), which the implementation repeats, and its RefKind, which decides how the
    // parameter is passed on to the local function of a [MapExpression].
    string SourceParameterModifiers = "",
    RefKind SourceRefKind = default,
    // The source type as declared, nullable annotations included, which the implementation repeats
    // (CS8611 otherwise). SourceTypeName leaves them out and is what types are compared by.
    string SourceDeclaredTypeName = default!,
    // Declared as a nullable reference type, or with nullable annotations disabled (oblivious), which may be
    // null as well. The generated code reads the source, so when it is null nothing is mapped; past that
    // check the local functions of [MapExpression] take it as SourceNonNullableTypeName.
    bool IsSourceParameterNullable = default,
    string SourceNonNullableTypeName = default!,
    string DestinationTypeName = default!,
    string? DestinationParameterName = default,
    // The same for the destination parameter of a void mapper.
    string DestinationParameterModifiers = "",
    RefKind DestinationRefKind = default,
    // The return type or the destination parameter type as declared. DestinationTypeName leaves the
    // annotations out, which types are compared by.
    string DestinationDeclaredTypeName = default!,
    // The same null check for the destination parameter of a void mapper, declared nullable or with nullable
    // annotations disabled, which the generated code writes. Custom parameters are passed on as they come.
    bool IsDestinationParameterNullable = default,
    string DestinationNonNullableTypeName = default!,
    // The type a return mapper creates the destination as: with the nullable annotations of its type arguments
    // (Box<string?>), so that it is the type the declaration returns (CS8619 otherwise), and without its own, which
    // new cannot take.
    string DestinationCreatedTypeName = default!,
    // What a return-type mapper returns for a null source: default, or default! for a reference type
    // that is not nullable, so that the generated code does not warn.
    string DefaultReturnValue = "default",
    bool ReturnsDestination = default,
    // The name of the source parameter (as declared, without the @ of a keyword) the return value is not null for
    // when it is not, which the implementation declares with [return: NotNullIfNotNull]: a return-type mapper whose
    // source may be null returning a nullable type, which gives null for a null source only. Null without.
    string? ReturnNotNullIfNotNull = default,
    bool AutoMap = true,
    bool Strict = default,
    bool StrictExplicitlySet = default,
    int NameComparison = default,
    bool NameComparisonExplicitlySet = default,
    string? Culture = default,
    bool CultureExplicitlySet = default,
    string? DateTimeFormat = default,
    string? NumberFormat = default,
    // DefaultCulture of the profiles is Current: the conversions without a culture name use the current culture
    bool UseCurrentCulture = default,
    // The custom parameter of type CultureInfo, which gives the culture of the conversions, as the generated code
    // writes it, and whether it may be null (annotated, or with nullable annotations disabled); null without one
    string? CultureParameterName = default,
    bool IsCultureParameterNullable = default,
    // An instance method, which calls the instance methods of its class as well
    bool IsInstance = default,
    // The defining declaration has the this modifier on the source parameter. The implementing
    // declaration must repeat it (CS0755), so the emitter carries it over.
    bool IsExtensionMethod = default,
    string? MapConverterTypeName = default,
    string MapConverterMethodName = "Convert",
    string? CollectionConverterTypeName = default,
    EquatableArray<CustomParameterModel> CustomParameters = default,
    EquatableArray<PropertyMappingModel> PropertyMappings = default,
    // Snapshot of the parsed [MapProperty] mappings, taken by ValidateExplicitPropertyMappings.
    // BuildPropertyMappings rebuilds PropertyMappings from the destination members and drops anything
    // with no matching property, so constructor resolution reads the renames and their options
    // (Converter, NullValue, Culture, Order) from here instead.
    EquatableArray<PropertyMappingModel> ExplicitPropertyMappings = default,
    EquatableArray<string> IgnoredProperties = default,
    // The targets of the [MapIgnore] attributes; IgnoredProperties also holds the targets the other
    // attributes assign, which the automatic mapping leaves out
    EquatableArray<string> IgnoreTargets = default,
    EquatableArray<PropertyConditionModel> PropertyConditions = default,
    EquatableArray<ConstantMappingModel> ConstantMappings = default,
    EquatableArray<ExpressionMappingModel> ExpressionMappings = default,
    EquatableArray<MapUsingModel> MapUsingMappings = default,
    EquatableArray<MapFromModel> MapFromMappings = default,
    EquatableArray<MapCollectionModel> MapCollectionMappings = default,
    EquatableArray<MapNestedModel> MapNestedMappings = default,
    string? BeforeMapMethod = default,
    // The custom parameters the matched callback takes after the source and the destination, as their indexes in the
    // order of its parameters, and the RefKinds of its parameters, which decide how each argument is passed.
    EquatableArray<int> BeforeMapCustomArguments = default,
    EquatableArray<RefKind> BeforeMapParameterRefKinds = default,
    string? AfterMapMethod = default,
    EquatableArray<int> AfterMapCustomArguments = default,
    EquatableArray<RefKind> AfterMapParameterRefKinds = default,
    bool UseConstructorMapping = default,
    // The constructor a return mapper calls with arguments, as its index in the instance constructors of the
    // destination type, or -1 when it constructs without arguments or never does (a void mapper). It is chosen
    // once, before anything reads it, so that every stage binds the same one (SelectConstructor).
    int ConstructorIndex = -1,
    // TargetPath names the entry that supplies the argument, of the collection Kind tells: a PropertyMappings
    // entry carrying its conversion metadata, or the attribute assigning the member, flagged as a constructor
    // argument. BuildConstructorParameterMappings guarantees the entry exists: it either flags an existing
    // mapping or synthesizes one under the parameter's own name. An optional parameter without a value is left
    // out, and the arguments after it are passed by name (IsNamed).
    EquatableArray<(string ParamName, string TargetPath, ConstructorArgumentKind Kind, bool IsNamed)> ConstructorParameters = default,
    // The required members the object initializer creates, as the dotted paths that write into them do so
    // after construction only: the member (Path) and the type it is created as (TypeName)
    EquatableArray<NestedPathSegment> RequiredMemberCreations = default,
    // The warnings of a model built without errors, located like the errors (the file path and the spans of the
    // method or the attribute), which keeps them comparable for the incremental pipeline
    EquatableArray<DiagnosticInfo> Warnings = default,
    // The locations of the attributes of the mapper method, and of the profile and converter attributes of its
    // class, which the diagnostics about them point to. The entries built from an attribute keep its index
    // (AttributeIndex), which does not change as the code moves; the source generation takes the models without
    // the locations, so that it is not run again for code that only moved.
    EquatableArray<LocationInfo> AttributeLocations = default,
    // The indexes of the attributes the method-level diagnostics are about: [BeforeMap], [AfterMap], the one giving
    // Culture ([Mapper] or [MapperProfile]), the one giving DateTimeFormat / NumberFormat, and [ValueConverter] (of
    // the method or the class); -1 without one
    int BeforeMapAttributeIndex = -1,
    int AfterMapAttributeIndex = -1,
    int CultureAttributeIndex = -1,
    int FormatAttributeIndex = -1,
    int ValueConverterAttributeIndex = -1,
    // The index of the [MapIgnore] of each of IgnoreTargets
    EquatableArray<int> IgnoreAttributeIndices = default,
    // A mapper reported with an error, which has no model but the declaration: the generated code implements it with
    // a body throwing, so that the implementation missing is not reported along with the error (CS8795). The return
    // type and the parameter list are as the defining declaration writes them, modifiers and nullable annotations
    // included, and the names and the type parameters of the model above name the rest.
    bool IsPlaceholder = default,
    string PlaceholderReturnType = "",
    string PlaceholderParameters = "");
