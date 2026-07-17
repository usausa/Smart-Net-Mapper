#pragma warning disable IDE0060
namespace Smart.Mapper.Mappers;

using Smart.Mapper.Models;

internal static partial class TestMappers
{
    // Basic mapping: same property names
    [Mapper]
    public static partial void Map(BasicSource source, BasicDestination destination);

    // Basic mapping with return type
    [Mapper]
    public static partial BasicDestination MapToNew(BasicSource source);

    // Different property names mapping
    [Mapper]
    [MapProperty(nameof(DifferentPropertyDestination.DestId), nameof(DifferentPropertySource.SourceId))]
    [MapProperty(nameof(DifferentPropertyDestination.DestName), nameof(DifferentPropertySource.SourceName))]
    public static partial void Map(DifferentPropertySource source, DifferentPropertyDestination destination);

    // Different property names mapping with return type
    [Mapper]
    [MapProperty(nameof(DifferentPropertyDestination.DestId), nameof(DifferentPropertySource.SourceId))]
    [MapProperty(nameof(DifferentPropertyDestination.DestName), nameof(DifferentPropertySource.SourceName))]
    public static partial DifferentPropertyDestination MapToNew(DifferentPropertySource source);

    // Ignore property mapping
    [Mapper]
    [MapIgnore(nameof(IgnoreDestination.Secret))]
    public static partial void Map(IgnoreSource source, IgnoreDestination destination);

    // Type conversion mapping
    [Mapper]
    public static partial void Map(TypeConversionSource source, TypeConversionDestination destination);

    // Multi-property mapping (for AutoMap = true test)
    [Mapper]
    public static partial void Map(MultiPropertySource source, MultiPropertyDestination destination);

    // Constant value mapping
    [Mapper]
    [MapConstant(nameof(ConstantDestination.Status), "Active")]
    [MapConstant(nameof(ConstantDestination.Version), 1)]
    [MapExpression(nameof(ConstantDestination.CreatedAt), "System.DateTime.Now")]
    public static partial void Map(ConstantSource source, ConstantDestination destination);

    // BeforeMap and AfterMap
    [Mapper]
    [BeforeMap(nameof(OnBeforeMap))]
    [AfterMap(nameof(OnAfterMap))]
    public static partial void Map(BeforeAfterSource source, BeforeAfterDestination destination);

    // ReSharper disable UnusedParameter.Local
    private static void OnBeforeMap(BeforeAfterSource source, BeforeAfterDestination destination)
    {
        destination.BeforeMapCalled = true;
    }
    // ReSharper restore UnusedParameter.Local

    // ReSharper disable UnusedParameter.Local
    private static void OnAfterMap(BeforeAfterSource source, BeforeAfterDestination destination)
    {
        destination.AfterMapCalled = true;
    }
    // ReSharper restore UnusedParameter.Local

    // Extended type conversions
    [Mapper]
    public static partial void Map(ExtendedTypeConversionSource source, ExtendedTypeConversionDestination destination);

    // Numeric conversions
    [Mapper]
    public static partial void Map(NumericConversionSource source, NumericConversionDestination destination);

    // Nested mapping: flat source to nested destination
    [Mapper]
    [MapProperty($"{nameof(NestedDestination.Child1)}.{nameof(DestinationChild.Value)}", nameof(FlatSource.Value1))]
    [MapProperty($"{nameof(NestedDestination.Child2)}.{nameof(DestinationChild.Value)}", nameof(FlatSource.Value2))]
    [MapProperty($"{nameof(NestedDestination.Child3)}.{nameof(DestinationChild.Value)}", nameof(FlatSource.Value3))]
    public static partial void Map(FlatSource source, NestedDestination destination);

    // Nested mapping: flat source to nested destination with return type
    [Mapper]
    [MapProperty($"{nameof(NestedDestination.Child1)}.{nameof(DestinationChild.Value)}", nameof(FlatSource.Value1))]
    [MapProperty($"{nameof(NestedDestination.Child2)}.{nameof(DestinationChild.Value)}", nameof(FlatSource.Value2))]
    [MapProperty($"{nameof(NestedDestination.Child3)}.{nameof(DestinationChild.Value)}", nameof(FlatSource.Value3))]
    public static partial NestedDestination MapToNew(FlatSource source);

    // Nested mapping: nested source to flat destination (flatten)
    [Mapper]
    [MapProperty(nameof(FlatDestination.ChildId), $"{nameof(NestedSource.Child)}.{nameof(NestedSourceChild.Id)}")]
    [MapProperty(nameof(FlatDestination.ChildName), $"{nameof(NestedSource.Child)}.{nameof(NestedSourceChild.Name)}")]
    public static partial void Map(NestedSource source, FlatDestination destination);

    // Deep nested mapping: flat source to deep nested destination
    [Mapper]
    [MapProperty($"{nameof(DeepNestedDestination.Outer)}.{nameof(DeepNestedParent.Inner)}.{nameof(DeepNestedChild.Value)}", nameof(DeepSource.DeepValue))]
    public static partial void Map(DeepSource source, DeepNestedDestination destination);

    // Deep nested mapping with return type
    [Mapper]
    [MapProperty($"{nameof(DeepNestedDestination.Outer)}.{nameof(DeepNestedParent.Inner)}.{nameof(DeepNestedChild.Value)}", nameof(DeepSource.DeepValue))]
    public static partial DeepNestedDestination MapToNew(DeepSource source);

    // Deep nested mapping: deep nested source to flat destination (multi-level flatten)
    [Mapper]
    [MapProperty(nameof(DeepFlatDestination.OuterInnerValue), $"{nameof(DeepNestedSource.Outer)}.{nameof(DeepSourceOuter.Inner)}.{nameof(DeepSourceInner.Value)}")]
    [MapProperty(nameof(DeepFlatDestination.OuterInnerName), $"{nameof(DeepNestedSource.Outer)}.{nameof(DeepSourceOuter.Inner)}.{nameof(DeepSourceInner.Name)}")]
    public static partial void Map(DeepNestedSource source, DeepFlatDestination destination);

    // Null handling: nested source to flat destination (with nullable source child)
    [Mapper]
    [MapProperty(nameof(NullableNestedFlatDestination.ChildId), $"{nameof(NullableNestedSource.Child)}.{nameof(NullableNestedSourceChild.Id)}")]
    [MapProperty(nameof(NullableNestedFlatDestination.ChildName), $"{nameof(NullableNestedSource.Child)}.{nameof(NullableNestedSourceChild.Name)}")]
    public static partial void Map(NullableNestedSource source, NullableNestedFlatDestination destination);

    // Null handling: simple nullable properties
    [Mapper]
    public static partial void Map(NullablePropertySource source, NullablePropertyDestination destination);

    // Null handling: nullable to non-nullable
    [Mapper]
    public static partial void Map(NullableToNonNullableSource source, NullableToNonNullableDestination destination);

    // Null handling: nullable int to non-nullable string
    [Mapper]
    public static partial void Map(NullableIntToStringSource source, NullableIntToStringDestination destination);

    // Custom parameter: with IServiceProvider
    [Mapper]
    [BeforeMap(nameof(OnBeforeMapWithContext))]
    [AfterMap(nameof(OnAfterMapWithContext))]
    public static partial void MapWithContext(BasicSource source, BasicDestination destination, CustomMappingContext context);

    // Custom parameter: BeforeMap without custom parameters (backward compatibility)
    [Mapper]
    [BeforeMap(nameof(OnBeforeMapBasic))]
    [AfterMap(nameof(OnAfterMapWithContext))]
    public static partial void MapWithContextMixed(BasicSource source, BasicDestination destination, CustomMappingContext context);

    // Custom parameter: return type pattern
    [Mapper]
    [AfterMap(nameof(OnAfterMapWithContextForReturn))]
    public static partial BasicDestination MapToNewWithContext(BasicSource source, CustomMappingContext context);

    // Converter: without custom parameters
    [Mapper]
    [MapProperty(nameof(ConverterDestination.ConvertedValue), nameof(ConverterSource.Value), Converter = nameof(ConvertIntToString))]
    public static partial void MapWithConverter(ConverterSource source, ConverterDestination destination);

    // Converter: with custom parameters
    [Mapper]
    [MapProperty(nameof(ConverterDestination.ConvertedValue), nameof(ConverterSource.Value), Converter = nameof(ConvertIntToStringWithContext))]
    [MapProperty(nameof(ConverterDestination.FormattedText), nameof(ConverterSource.Text), Converter = nameof(FormatTextWithContext))]
    public static partial void MapWithConverterAndContext(ConverterSource source, ConverterDestination destination, CustomMappingContext context);

    // ReSharper disable UnusedParameter.Local
    private static void OnBeforeMapWithContext(BasicSource source, BasicDestination destination, CustomMappingContext context)
    {
        context.BeforeMapCalled = true;
    }
    // ReSharper restore UnusedParameter.Local

    // ReSharper disable UnusedParameter.Local
    private static void OnAfterMapWithContext(BasicSource source, BasicDestination destination, CustomMappingContext context)
    {
        context.AfterMapCalled = true;
    }

    private static void OnBeforeMapBasic(BasicSource source, BasicDestination destination)
    {
        // Basic version without context
    }

    private static void OnAfterMapWithContextForReturn(BasicSource source, BasicDestination destination, CustomMappingContext context)
    {
        context.AfterMapCalled = true;
        destination.Description = $"Modified by AfterMap: {context.ContextValue}";
    }

    private static string ConvertIntToString(int value)
    {
        return $"Value: {value}";
    }

    private static string ConvertIntToStringWithContext(int value, CustomMappingContext context)
    {
        return $"Value: {value}, Context: {context.ContextValue}";
    }

    private static string FormatTextWithContext(string text, CustomMappingContext context)
    {
        return $"{text} (formatted with {context.ContextValue})";
    }

    // Condition: Property-level condition for Name
    [Mapper]
    [MapCondition(nameof(ConditionDestination.Name), nameof(ShouldMapName))]
    public static partial void MapWithPropertyCondition(ConditionSource source, ConditionDestination destination);

    // Condition: Generic MapConstant test
    [Mapper]
    [MapConstant<int>(nameof(ConstantDestination.Version), 2)]
    [MapConstant<string>(nameof(ConstantDestination.Status), "Pending")]
    public static partial void MapWithGenericConstant(ConstantSource source, ConstantDestination destination);

    private static bool ShouldMapName(string? name)
    {
        return !String.IsNullOrEmpty(name);
    }

    // MapUsing: basic usage
    [Mapper]
    [MapUsing(nameof(MapFromDestination.FullName), nameof(CombineFullName))]
    [MapUsing(nameof(MapFromDestination.UpperCaseName), nameof(GetUpperCaseName))]
    public static partial void Map(MapFromSource source, MapFromDestination destination);

    private static string CombineFullName(MapFromSource source)
    {
        return $"{source.FirstName} {source.LastName}";
    }

    private static string GetUpperCaseName(MapFromSource source)
    {
        return $"{source.FirstName} {source.LastName}".ToUpperInvariant();
    }

    // MapUsing: with custom parameters
    [Mapper]
    [MapUsing(nameof(MapFromDestination.FullName), nameof(CombineFullNameWithContext))]
    public static partial MapFromDestination MapWithContext(MapFromSource source, MapFromContext context);

    private static string CombineFullNameWithContext(MapFromSource source, MapFromContext context)
    {
        return $"{source.FirstName}{context.Separator}{source.LastName}";
    }

    // MapFrom: calling methods on source object
    [Mapper]
    [MapFrom(nameof(MapFromMethodDestination.ItemCount), nameof(MapFromMethodSource.GetItemCount))]
    [MapFrom(nameof(MapFromMethodDestination.ItemSum), nameof(MapFromMethodSource.GetItemSum))]
    public static partial void Map(MapFromMethodSource source, MapFromMethodDestination destination);

    // AutoMap = false: only explicitly mapped properties
    [Mapper(AutoMap = false)]
    [MapProperty(nameof(AutoMapDestination.Id), nameof(AutoMapSource.Id))]
    public static partial void MapExplicit(AutoMapSource source, AutoMapDestination destination);

    // AutoMap = false: with multiple MapProperty
    [Mapper(AutoMap = false)]
    [MapProperty(nameof(AutoMapDestination.Id), nameof(AutoMapSource.Id))]
    [MapProperty(nameof(AutoMapDestination.Name), nameof(AutoMapSource.Name))]
    public static partial AutoMapDestination MapExplicitToNew(AutoMapSource source);

    // MapCollection: child mapper (return value pattern)
    [Mapper]
    public static partial CollectionDestinationChild MapCollectionChild(CollectionSourceChild source);

    // MapCollection: array to List and List to array
    [Mapper]
    [MapCollection(nameof(CollectionDestination.Children), nameof(CollectionSource.Children), Mapper = nameof(MapCollectionChild))]
    [MapCollection(nameof(CollectionDestination.Items), nameof(CollectionSource.Items), Mapper = nameof(MapCollectionChild))]
    public static partial void Map(CollectionSource source, CollectionDestination destination);

    // MapCollection: with return type
    [Mapper]
    [MapCollection(nameof(CollectionDestination.Children), nameof(CollectionSource.Children), Mapper = nameof(MapCollectionChild))]
    [MapIgnore(nameof(CollectionDestination.Items))]
    public static partial CollectionDestination MapToNew(CollectionSource source);

    // MapNested: child mapper (return value pattern)
    [Mapper]
    public static partial NestedObjectDestinationChild MapNestedChild(NestedObjectSourceChild source);

    // MapNested: basic usage
    [Mapper]
    [MapNested(nameof(NestedObjectDestination.Child), nameof(NestedObjectSource.Child), Mapper = nameof(MapNestedChild))]
    public static partial void Map(NestedObjectSource source, NestedObjectDestination destination);

    // MapNested: with return type
    [Mapper]
    [MapNested(nameof(NestedObjectDestination.Child), nameof(NestedObjectSource.Child), Mapper = nameof(MapNestedChild))]
    public static partial NestedObjectDestination MapToNew(NestedObjectSource source);

    // MapCollection with void mapper
    [Mapper]
    public static partial void MapVoidChild(VoidMapperSourceChild source, VoidMapperDestinationChild destination);

    [Mapper]
    [MapCollection(nameof(VoidMapperDestination.Children), nameof(VoidMapperSource.Children), Mapper = nameof(MapVoidChild))]
    public static partial void Map(VoidMapperSource source, VoidMapperDestination destination);

    // MapCollection with custom converter method
    [Mapper]
    public static partial CustomCollectionConverterDestChild MapCustomCollectionChild(CustomCollectionConverterSourceChild source);

    [Mapper]
    [CollectionConverter(typeof(TestCollectionConverter))]
    [MapCollection(nameof(CustomCollectionConverterDestination.Children), nameof(CustomCollectionConverterSource.Children), Mapper = nameof(MapCustomCollectionChild), Converter = nameof(TestCollectionConverter.ToReadOnlyList))]
    public static partial void MapWithCustomCollectionConverter2(CustomCollectionConverterSource source, CustomCollectionConverterDestination destination);

    // MapNested with void mapper
    [Mapper]
    public static partial void MapNestedChildVoid(NestedObjectSourceChild source, NestedObjectDestinationChild destination);

    [Mapper]
    [MapNested(nameof(NestedObjectDestination.Child), nameof(NestedObjectSource.Child), Mapper = nameof(MapNestedChildVoid))]
    public static partial void MapWithVoidNested(NestedObjectSource source, NestedObjectDestination destination);

    // Custom type converter test
    [Mapper]
    [ValueConverter(typeof(TestCustomConverter))]
    public static partial void MapWithCustomConverter(CustomConverterSource source, CustomConverterDestination destination);

    // Custom collection converter test
    [Mapper]
    [CollectionConverter(typeof(TestCustomCollectionConverter))]
    [MapCollection(nameof(CustomCollectionDestination.Numbers), nameof(CustomCollectionSource.Numbers), Mapper = nameof(MapCollectionChild))]
    public static partial void MapWithCustomCollectionConverter(CustomCollectionSource source, CustomCollectionDestination destination);

    // Order test mapper - properties are set in Order sequence
    [Mapper]
    [MapProperty(nameof(OrderTestDestination.Step1), nameof(OrderTestSource.Value), Order = 1)]
    [MapProperty(nameof(OrderTestDestination.Step2), nameof(OrderTestSource.Value), Order = 2)]
    [MapProperty(nameof(OrderTestDestination.Step3), nameof(OrderTestSource.Value), Order = 3)]
    public static partial void MapWithOrder(OrderTestSource source, OrderTestDestination destination);

    // Order test with reversed order to verify Order attribute is respected
    [Mapper]
    [MapProperty(nameof(OrderTestDestination.Step3), nameof(OrderTestSource.Value), Order = 1)]
    [MapProperty(nameof(OrderTestDestination.Step1), nameof(OrderTestSource.Value), Order = 2)]
    [MapProperty(nameof(OrderTestDestination.Step2), nameof(OrderTestSource.Value), Order = 3)]
    public static partial void MapWithReversedOrder(OrderTestSource source, OrderTestDestination destination);

    // MapUsing with custom parameters
    [Mapper]
    [MapUsing(nameof(MapUsingContextDestination.ComputedValue), nameof(ComputeWithContext))]
    public static partial void MapWithUsingContext(MapUsingContextSource source, MapUsingContextDestination destination, MapUsingContext context);

    private static string ComputeWithContext(MapUsingContextSource source, MapUsingContext context)
    {
        return $"{source.BaseValue}_{context.Suffix}";
    }

    // MapFrom with property path
    [Mapper]
    [MapFrom(nameof(MapFromPathDestination.ItemCount), "GetItemCount")]
    [MapFrom(nameof(MapFromPathDestination.NestedValue), "Nested.Value")]
    public static partial void MapWithPropertyPath(MapFromPathSource source, MapFromPathDestination destination);

    // Specialized converter test
    [Mapper]
    [ValueConverter(typeof(SpecializedConverter))]
    public static partial void MapWithSpecializedConverter(SpecializedConverterSource source, SpecializedConverterDestination destination);

    // A1: NullSubstitute – string and int with fallback values when source is null
    [Mapper]
    [MapProperty(nameof(NullSubstituteDestination.Name), nameof(NullSubstituteSource.Name), NullSubstitute = "(none)")]
    [MapProperty(nameof(NullSubstituteDestination.Count), nameof(NullSubstituteSource.Count), NullSubstitute = -1)]
    public static partial void MapWithNullSubstitute(NullSubstituteSource source, NullSubstituteDestination destination);

    // B1: DateOnly / TimeOnly / DateTimeOffset / TimeSpan -> string
    [Mapper]
    public static partial void MapDateTimeTypes(DateTimeTypeConversionSource source, DateTimeTypeToStringDestination destination);

    // B2: Half -> string, int -> Half
    [Mapper]
    public static partial void MapModernNumericTypes(ModernNumericConversionSource source, ModernNumericConversionDestination destination);

    // C3: ImmutableArray / ImmutableList / HashSet collection targets
    [Mapper]
    [MapCollection(nameof(ImmutableCollectionDestination.Items), nameof(ImmutableCollectionSource.Items), Mapper = nameof(MapImmutableChild))]
    [MapCollection(nameof(ImmutableCollectionDestination.ListItems), nameof(ImmutableCollectionSource.ListItems), Mapper = nameof(MapImmutableChild))]
    [MapCollection(nameof(ImmutableCollectionDestination.SetItems), nameof(ImmutableCollectionSource.SetItems), Mapper = nameof(MapImmutableChild))]
    public static partial void MapImmutableCollections(ImmutableCollectionSource source, ImmutableCollectionDestination destination);

    [Mapper]
    public static partial ImmutableCollectionDestinationChild MapImmutableChild(ImmutableCollectionSourceChild source);

    // E2: Case-insensitive name comparison
    [Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
    public static partial void MapCaseInsensitive(CaseInsensitiveSource source, CaseInsensitiveDestination destination);

    // Regression G: NullBehavior.Skip with no conversion (nullable value -> non-nullable), void mapper
    [Mapper]
    [MapProperty(nameof(SkipNoConvDestination.Value), NullBehavior = NullBehavior.Skip)]
    public static partial void MapSkipNoConv(SkipNoConvSource source, SkipNoConvDestination destination);

    // Regression H: return-mapper to init-only destination without a parameterized constructor
    [Mapper]
    public static partial InitReturnDestination MapInitReturn(InitReturnSource source);

    // Regression I: return-mapper to required-member destination
    [Mapper]
    public static partial RequiredReturnDestination MapRequiredReturn(RequiredReturnSource source);

    // D2: required member – all required properties are mapped
    [Mapper]
    public static partial void MapRequiredMembers(RequiredMemberSource source, RequiredMemberDestination destination);

    // A2: Enum ↔ Enum (by name)
    [Mapper]
    public static partial void MapEnumToEnum(EnumToEnumSource source, EnumToEnumDestination destination);

    // A2: Nullable Enum ↔ Nullable Enum
    [Mapper]
    public static partial void MapNullableEnum(NullableEnumSource source, NullableEnumDestination destination);

    // A2: Enum → int
    [Mapper]
    public static partial void MapEnumToInt(EnumToIntSource source, EnumToIntDestination destination);

    // A2: int → Enum
    [Mapper]
    public static partial void MapIntToEnum(IntToEnumSource source, IntToEnumDestination destination);

    // A2: Enum → string
    [Mapper]
    public static partial void MapEnumToString(EnumToStringSource source, EnumToStringDestination destination);

    // A2: string → Enum
    [Mapper]
    public static partial void MapStringToEnum(StringToEnumSource source, StringToEnumDestination destination);

    // A2: 部分一致 (Pending → default)
    // A2: partial match (Pending → default)
    [Mapper]
    public static partial void MapPartialEnum(PartialEnumSource source, PartialEnumDestination destination);

    // A2: エイリアス値を持つ enum → string (switch 生成、値で重複排除)
    // A2: enum with alias values → string (switch emit, deduped by value)
    [Mapper]
    public static partial void MapAliasEnumToString(AliasEnumToStringSource source, AliasEnumToStringDestination destination);

    // A2: Flags enum → string (合成値は ToString フォールバック)
    // A2: flags enum → string (combined values fall back to ToString)
    [Mapper]
    public static partial void MapFlagsEnumToString(FlagsEnumToStringSource source, FlagsEnumToStringDestination destination);

    // B4: Method-level Culture + NumberFormat (double -> string)
    [Mapper(Culture = "fr-FR", NumberFormat = "N2")]
    public static partial CultureFormatDestination MapWithCultureFormat(CultureFormatSource source);

    // B4: Method-level Culture for parsing (string -> double, string -> DateTime)
    [Mapper(Culture = "fr-FR")]
    public static partial CultureParseDestination MapWithCultureParse(CultureParseSource source);

    // B4: Property-level Culture overrides method-level
    [Mapper(Culture = "en-US")]
    [MapProperty(nameof(CultureOverrideDestination.ValueB), Culture = "de-DE")]
    public static partial CultureOverrideDestination MapWithPropertyCultureOverride(CultureOverrideSource source);

    // Regression: Culture + bool/Guid/Half/Int128/UInt128/BigInteger (string -> type)
    [Mapper(Culture = "en-US")]
    public static partial CultureSpecialParseDestination MapCultureSpecialParse(CultureSpecialParseSource source);

    // Regression: Culture + bool/Guid/Half/Int128/UInt128/BigInteger (type -> string)
    [Mapper(Culture = "en-US")]
    public static partial CultureSpecialFormatDestination MapCultureSpecialFormat(CultureSpecialFormatSource source);
}

// D1/D3: Primary constructor and record mapping
internal static partial class PrimaryConstructorMappers
{
    // D1: record → record
    [Mapper]
    public static partial RecordDestination MapRecord(RecordSource source);

    // D1: record → record (partial params)
    [Mapper]
    public static partial RecordDestinationPartial MapRecordPartial(RecordSource source);

    // D3: class with primary constructor
    [Mapper]
    public static partial PrimaryCtorDestination MapPrimaryCtorClass(PrimaryCtorSource source);

    // D1: record with extra settable property
    [Mapper]
    public static partial RecordWithExtra MapRecordWithExtra(RecordWithExtraSource source);

    // D1: [MapProperty] overrides constructor argument
    [Mapper]
    [MapProperty(nameof(MapPropertyOverrideDestination.Id), nameof(MapPropertyOverrideSource.Identifier))]
    [MapProperty(nameof(MapPropertyOverrideDestination.Name), nameof(MapPropertyOverrideSource.FullName))]
    public static partial MapPropertyOverrideDestination MapWithPropertyOverride(MapPropertyOverrideSource source);
}

// C2: InPlace, D4: readonly struct
internal static partial class TestMappers
{
    // C2: InPlace collection update – child mapper
    [Mapper]
    public static partial InPlaceDestinationChild MapInPlaceChild(InPlaceSourceChild source);

    // C2: InPlace collection update
    [Mapper]
    [MapCollection(nameof(InPlaceDestination.Items), nameof(InPlaceSource.Items), Mapper = nameof(MapInPlaceChild), Strategy = CollectionStrategy.InPlace)]
    public static partial void MapInPlace(InPlaceSource source, InPlaceDestination destination);

    // D4: readonly struct
    [Mapper]
    public static partial ReadOnlyStructDestination MapReadOnlyStruct(in ReadOnlyStructSource source);
}

// C4-β/γ: Collection matrix tests
internal static partial class TestMappers
{
    [Mapper]
    public static partial MatrixDstItem MapMatrixItem(MatrixSrcItem source);

    private static void MapMatrixItemVoid(MatrixSrcItem source, MatrixDstItem destination)
    {
        destination.Value = source.Value;
    }

    // Array → List (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToListDst.Items), nameof(MatrixArraySource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapArrayToList(MatrixArraySource source, MatrixToListDst destination);

    // Array → Array (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToArrayDst.Items), nameof(MatrixArraySource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapArrayToArray(MatrixArraySource source, MatrixToArrayDst destination);

    // Array → ImmutableArray (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToImmutableArrayDst.Items), nameof(MatrixArraySource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapArrayToImmutableArray(MatrixArraySource source, MatrixToImmutableArrayDst destination);

    // Array → HashSet (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToHashSetDst.Items), nameof(MatrixArraySource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapArrayToHashSet(MatrixArraySource source, MatrixToHashSetDst destination);

    // Array → FrozenSet (Func) — regression for bare .ToFrozenSet()
    [Mapper]
    [MapCollection(nameof(MatrixToFrozenSetDst.Items), nameof(MatrixArraySource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapArrayToFrozenSet(MatrixArraySource source, MatrixToFrozenSetDst destination);

    // Memory → List (Func) — regression for Memory<T> source being rejected as non-collection
    [Mapper]
    [MapCollection(nameof(MatrixToListDst.Items), nameof(MatrixMemorySource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapMemoryToList(MatrixMemorySource source, MatrixToListDst destination);

    // IReadOnlyList → List (Func) — indexer-based IndexedList shape
    [Mapper]
    [MapCollection(nameof(MatrixToListDst.Items), nameof(MatrixReadOnlyListSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapReadOnlyListToList(MatrixReadOnlyListSource source, MatrixToListDst destination);

    // Regression: char -> string and string -> char (scalar conversion matrix sweep)
    [Mapper]
    public static partial ScalarCharDestination MapScalarChar(ScalarCharSource source);

    // Regression: numeric -> Half with Culture (scalar conversion matrix sweep)
    [Mapper(Culture = "en-US")]
    public static partial ScalarHalfDestination MapScalarHalf(ScalarHalfSource source);

    // List → List (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToListDst.Items), nameof(MatrixListSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapListToList(MatrixListSource source, MatrixToListDst destination);

    // List → Array (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToArrayDst.Items), nameof(MatrixListSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapListToArray(MatrixListSource source, MatrixToArrayDst destination);

    // List → ImmutableArray (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToImmutableArrayDst.Items), nameof(MatrixListSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapListToImmutableArray(MatrixListSource source, MatrixToImmutableArrayDst destination);

    // List → HashSet (Func)
    [Mapper]
    [MapCollection(nameof(MatrixToHashSetDst.Items), nameof(MatrixListSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapListToHashSet(MatrixListSource source, MatrixToHashSetDst destination);

    // Array → List (Action/void mapper)
    [Mapper]
    [MapCollection(nameof(MatrixVoidDst.Items), nameof(MatrixArraySource.Items), Mapper = nameof(MapMatrixItemVoid))]
    public static partial void MapArrayToListVoid(MatrixArraySource source, MatrixVoidDst destination);

    // List → List (Action/void mapper)
    [Mapper]
    [MapCollection(nameof(MatrixVoidDst.Items), nameof(MatrixListSource.Items), Mapper = nameof(MapMatrixItemVoid))]
    public static partial void MapListToListVoid(MatrixListSource source, MatrixVoidDst destination);

    // IEnumerable source (EmitInlineTargetBuildFromEnumerable path)
    [Mapper]
    [MapCollection(nameof(MatrixToArrayDst.Items), nameof(MatrixEnumerableSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapEnumerableToArray(MatrixEnumerableSource source, MatrixToArrayDst destination);

    [Mapper]
    [MapCollection(nameof(MatrixToListDst.Items), nameof(MatrixEnumerableSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapEnumerableToList(MatrixEnumerableSource source, MatrixToListDst destination);

    [Mapper]
    [MapCollection(nameof(MatrixToImmutableArrayDst.Items), nameof(MatrixEnumerableSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapEnumerableToImmutableArray(MatrixEnumerableSource source, MatrixToImmutableArrayDst destination);

    [Mapper]
    [MapCollection(nameof(MatrixToHashSetDst.Items), nameof(MatrixEnumerableSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapEnumerableToHashSet(MatrixEnumerableSource source, MatrixToHashSetDst destination);

    // Custom CollectionConverter + array destination (DetermineCollectionMethod helper path)
    [Mapper]
    [CollectionConverter(typeof(TestCollectionConverter))]
    [MapCollection(nameof(MatrixConverterArrayDst.Items), nameof(MatrixListSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapListToArrayWithConverter(MatrixListSource source, MatrixConverterArrayDst destination);
}

// E3: MapperProfile – class-level defaults applied to all methods
[MapperProfile(Strict = true, NameComparison = StringComparison.OrdinalIgnoreCase)]
internal static partial class ProfileMappers
{
    // Inherits Strict=true and NameComparison=OrdinalIgnoreCase from MapperProfile
    [Mapper]
    public static partial void Map(ProfileSource source, ProfileDestination destination);

    // Method-level attribute overrides profile: Strict=false
    [Mapper(Strict = false)]
    public static partial void MapNoStrict(ProfileSource source, ProfileDestination destination);
}

// B3: IParsable<T> / ISpanParsable<T> mappings
internal static partial class TestMappers
{
    // T1: string → TestParsableId (IParsable only)
    [Mapper]
    public static partial void Map(ParsableSource source, ParsableDestination destination);

    // T2: string → TestSpanParsableId (ISpanParsable)
    [Mapper]
    public static partial void Map(SpanParsableSource source, SpanParsableDestination destination);

    // T3: Culture あり + IParsable
    // T3: with Culture + IParsable
    [Mapper(Culture = "en-US")]
    public static partial void MapCulture(ParsableCultureSource source, ParsableCultureDestination destination);

    // T4: Culture あり + ISpanParsable
    // T4: with Culture + ISpanParsable
    [Mapper(Culture = "en-US")]
    public static partial void MapCulture(SpanParsableCultureSource source, SpanParsableCultureDestination destination);

    // T7: string? → TestSpanParsableId (nullable source)
    [Mapper]
    public static partial void Map(NullableSpanParsableSource source, NullableSpanParsableDestination destination);
}

// User-defined conversion mappings (op_Implicit / op_Explicit / IFormattable)
internal static partial class TestMappers
{
    // op_Implicit: UserId (struct) → int
    [Mapper]
    public static partial void Map(ImplicitConversionSource source, ImplicitConversionDestination destination);

    // op_Explicit: Celsius (struct) → double
    [Mapper]
    public static partial void Map(ExplicitConversionSource source, ExplicitConversionDestination destination);

    // DualOp: both op_Implicit(→long) and op_Explicit(→int) exist; op_Implicit target wins
    [Mapper]
    public static partial void Map(DualOpSource source, DualOpImplicitDestination destination);

    // DualOp explicit path (op_Explicit only match for int target)
    [Mapper]
    public static partial void Map(DualOpSource source, DualOpExplicitDestination destination);

    // IFormattable: Money → string with Culture+NumberFormat
    [Mapper(Culture = "ja-JP", NumberFormat = "G")]
    public static partial void MapFormattable(FormattableSource source, FormattableDestination destination);
}

// =====================================================================
// __todo.md カバレッジ用マッパー
// __todo.md coverage mappers
// =====================================================================
internal static partial class TestMappers
{
    // §1 数値変換
    // §1 Numeric conversion
    [Mapper]
    public static partial void MapNumericCov(NumericCovSource source, NumericCovDestination destination);

    [Mapper]
    public static partial void MapNullableNumCov(NullableNumCovSource source, NullableNumCovDestination destination);

    // §2 Enum 変換
    // §2 Enum conversion
    [Mapper]
    public static partial void MapEnumCov(EnumCovSource source, EnumCovDestination destination);

    [Mapper]
    public static partial void MapNullableEnumCov(NullableEnumCovSource source, NullableEnumCovDestination destination);

    // §3 ユーザー定義変換演算子
    // §3 User-defined conversion operators
    [Mapper]
    public static partial void MapOperatorCov(OperatorCovSource source, OperatorCovDestination destination);

    // §4 カスタムコンバータ
    // §4 Custom converters
    [Mapper]
    [ValueConverter(typeof(CovConverter))]
    public static partial void MapConverterCov(ConverterCovSource source, ConverterCovDestination destination);
}
