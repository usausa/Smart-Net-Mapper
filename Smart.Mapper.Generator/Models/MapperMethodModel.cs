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
    Accessibility MethodAccessibility = default,
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
    // Declared as a nullable reference type. The generated code reads the source, so when it is null
    // nothing is mapped; past that check the local functions of [MapExpression] take it as
    // SourceNonNullableTypeName.
    bool IsSourceParameterNullable = default,
    string SourceNonNullableTypeName = default!,
    string DestinationTypeName = default!,
    string? DestinationParameterName = default,
    // The same for the destination parameter of a void mapper.
    string DestinationParameterModifiers = "",
    RefKind DestinationRefKind = default,
    // The return type or the destination parameter type as declared. DestinationTypeName leaves the
    // annotations out, as the instance is created under it.
    string DestinationDeclaredTypeName = default!,
    // The same null check for the destination parameter of a void mapper, which the generated code writes.
    bool IsDestinationParameterNullable = default,
    string DestinationNonNullableTypeName = default!,
    // What a return-type mapper returns for a null source: default, or default! for a reference type
    // that is not nullable, so that the generated code does not warn.
    string DefaultReturnValue = "default",
    bool ReturnsDestination = default,
    bool AutoMap = true,
    bool Strict = default,
    bool StrictExplicitlySet = default,
    int NameComparison = default,
    bool NameComparisonExplicitlySet = default,
    string? Culture = default,
    bool CultureExplicitlySet = default,
    string? DateTimeFormat = default,
    string? NumberFormat = default,
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
    bool BeforeMapAcceptsCustomParameters = default,
    // RefKinds of the matched callback's parameters, which decide how each argument is passed.
    EquatableArray<RefKind> BeforeMapParameterRefKinds = default,
    string? AfterMapMethod = default,
    bool AfterMapAcceptsCustomParameters = default,
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
    // The indexes of the attributes the method-level diagnostics are about: [Mapper], [BeforeMap], [AfterMap],
    // the one giving Culture ([Mapper] or [MapperProfile]), the one giving DateTimeFormat / NumberFormat, and
    // [ValueConverter] (of the method or the class); -1 without one
    int MapperAttributeIndex = -1,
    int BeforeMapAttributeIndex = -1,
    int AfterMapAttributeIndex = -1,
    int CultureAttributeIndex = -1,
    int FormatAttributeIndex = -1,
    int ValueConverterAttributeIndex = -1,
    // The index of the [MapIgnore] of each of IgnoreTargets
    EquatableArray<int> IgnoreAttributeIndices = default);
