namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Classifies the source collection type for optimized emit strategy selection.
internal enum CollectionSourceShape
{
    Enumerable = 0,
    ReadOnlyCollection,
    Array,
    List,
    ImmutableArray,
    ReadOnlyMemory,
    Memory,
    IndexedList
}

// Classifies the target collection type for optimized emit strategy selection.
internal enum CollectionTargetShape
{
    List = 0,
    Array,
    ImmutableArray,
    ImmutableList,
    HashSet,
    ImmutableHashSet,
    FrozenSet,
    // A collection class the loop creates with its own constructor and fills through ICollection<T>
    Custom,
    // A dictionary interface, for which the loop creates a Dictionary<TKey, TValue> it fills the same way
    Dictionary
}

// Represents a MapCollection mapping (collection property mapped using a mapper method).
internal sealed record MapCollectionModel(
    // Identity. TargetType is written with the nullable annotations of its type arguments (List<Item?>), which
    // the collection the generated code creates for the target repeats, and without its own, which new cannot
    // take, and TargetElementType the same, for the element a void mapper fills. The element types are written
    // with them as type arguments and array elements (ElementTypeArgument), so that the collections and the
    // elements created take what the target does (CS8619)
    string SourceName = default!,
    string SourceElementTypeArgument = default!,
    string TargetName = default!,
    string TargetType = default!,
    string TargetElementType = default!,
    string TargetElementTypeArgument = default!,
    // Mapper method applied to each element. The RefKinds of its parameters decide how the element and
    // the instance a void mapper fills are passed
    string? Mapper = default,
    EquatableArray<RefKind> MapperParameterRefKinds = default,
    // The custom parameters of the mapper the element mapper takes after the element (and instance), as the arguments
    // the generated code appends to its call (", culture")
    string MapperCustomArguments = "",
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Emit strategy. Shapes pick the optimized loop, UseHelperPath routes through a converter instead
    CollectionSourceShape SourceShape = CollectionSourceShape.Enumerable,
    CollectionTargetShape TargetShape = CollectionTargetShape.List,
    string TargetCollectionMethod = "ToList",
    bool MapperReturnsValue = default,
    // The mapper returns a nullable reference into elements not annotated as one, so its result is taken with !
    bool ForgivesMapperResult = default,
    // The elements are nullable structs the mapper takes the value of, which goes to it after a null check
    bool UnwrapsSource = default,
    // What a null element gives: default!, or null for nullable struct elements the result of the mapper, the
    // struct, goes into, as the type of the conditional is that of the result
    string NullResult = "default!",
    bool IsSourceNullable = default,
    bool UseHelperPath = default,
    // Optional per-mapping settings
    string? Converter = default,
    // Reuse of an existing target collection instead of building a new one. The fallback is created when
    // the target is null; without one (a target that cannot be assigned) a null target is left as it is
    bool InPlace = default,
    string? InPlaceFallbackTypeName = default,
    // The collection class the loop creates for a dictionary interface; one of its own is created as TargetType
    string? CreatedTypeName = default,
    // The value goes to the argument of the constructor a return mapper calls, as the parameter assigns the
    // member
    bool IsConstructorArgument = default,
    // The collection is made before construction and set in the object initializer of a return mapper, as the
    // member is init-only or required
    bool IsInitializerEntry = default,
    // The attribute this was declared by, as its index in MapperMethodModel.AttributeLocations, which the
    // diagnostics about it point to; -1 for one the automatic mapping made
    int AttributeIndex = -1);

internal static class MapCollectionModelExtensions
{
    public static bool HasCustomConverter(this MapCollectionModel m) => !String.IsNullOrEmpty(m.Converter);
}
