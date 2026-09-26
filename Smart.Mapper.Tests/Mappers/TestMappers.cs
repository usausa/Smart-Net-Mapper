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

    // Extension method mappers (this is kept on the generated implementation)
    [Mapper]
    public static partial BasicDestination ToDestination(this BasicSource source);

    [Mapper]
    public static partial void CopyTo(this BasicSource source, BasicDestination destination);

    // Source parameter named destination (the generated instance is __d, so the names do not collide)
    [Mapper]
    public static partial BasicDestination MapNamedDestination(BasicSource destination);

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

    // A1: NullValue – string and int with fallback values when source is null
    [Mapper]
    [MapProperty(nameof(NullValueDestination.Name), nameof(NullValueSource.Name), NullValue = "(none)")]
    [MapProperty(nameof(NullValueDestination.Count), nameof(NullValueSource.Count), NullValue = -1)]
    public static partial void MapWithNullValue(NullValueSource source, NullValueDestination destination);

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

    // Explicit feature mappings targeting init-only / required members (object-initializer emit)
    [Mapper]
    [MapConstant(nameof(FeatureInitDestination.Fixed), "F")]
    [MapExpression(nameof(FeatureInitDestination.Doubled), "source.Id * 2")]
    [MapUsing(nameof(FeatureInitDestination.Upper), nameof(BuildUpper))]
    [MapFrom(nameof(FeatureInitDestination.FromName), nameof(FeatureInitSource.Name))]
    public static partial FeatureInitDestination MapFeatureInit(FeatureInitSource source);

    private static string BuildUpper(FeatureInitSource source) => source.Name.ToUpperInvariant();

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

    // コンストラクタ引数にも型変換・Converter・NullValue が適用される
    // Type conversion, Converter and NullValue apply to constructor arguments too
    [Mapper(Culture = "en-US")]
    [MapProperty(nameof(CtorConversionDestination.Raw), nameof(CtorConversionSource.Raw), Converter = nameof(FormatRaw))]
    [MapProperty<int>(nameof(CtorConversionDestination.Quantity), nameof(CtorConversionSource.Quantity), NullValue = 99)]
    public static partial CtorConversionDestination MapCtorConversion(CtorConversionSource source);

    private static string FormatRaw(int value) => $"#{value}";

    // セッターの無いプロパティへのリネーム + 型変換
    // Rename plus type conversion onto a get-only property
    [Mapper]
    [MapProperty(nameof(CtorGetOnlyDestination.Value), nameof(CtorGetOnlySource.Other))]
    public static partial CtorGetOnlyDestination MapCtorGetOnly(CtorGetOnlySource source);

    // null 許容ソース + 型変換。null はターゲット型の default になる
    // Nullable source plus conversion; null yields the destination type's default
    [Mapper(Culture = "en-US")]
    public static partial CtorNullableConversionDestination MapCtorNullableConversion(CtorNullableConversionSource source);

    // 対応する destination プロパティを持たない引数へのリネーム + 型変換
    // Rename plus type conversion onto a parameter with no backing destination property
    [Mapper]
    [MapProperty("value", nameof(CtorNoPropertySource.Other))]
    public static partial CtorNoPropertyDestination MapCtorNoProperty(CtorNoPropertySource source);

    // null 許容な中間セグメントを持つドット記法ソース + コンストラクタ引数
    // Dotted source with a nullable intermediate segment feeding a constructor argument
    [Mapper]
    [MapProperty(nameof(CtorNestedGuardDestination.Value), "Child.Val")]
    public static partial CtorNestedGuardDestination MapCtorNestedGuard(CtorNestedGuardSource source);
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

    // IReadOnlyCollection → ImmutableArray (Func) — presized builder + MoveToImmutable
    [Mapper]
    [MapCollection(nameof(MatrixToImmutableArrayDst.Items), nameof(MatrixReadOnlyCollectionSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapReadOnlyCollectionToImmutableArray(MatrixReadOnlyCollectionSource source, MatrixToImmutableArrayDst destination);

    // IReadOnlyCollection → HashSet (Func) — presized HashSet
    [Mapper]
    [MapCollection(nameof(MatrixToHashSetDst.Items), nameof(MatrixReadOnlyCollectionSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial void MapReadOnlyCollectionToHashSet(MatrixReadOnlyCollectionSource source, MatrixToHashSetDst destination);

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

// Regression: several [MapCollection] whose sources are never null in one mapper
internal static partial class TestMappers
{
    // Replace: four non-nullable collections, a nullable one and a nested object
    [Mapper]
    [MapCollection(nameof(MultiCollectionDestination.Lines), Mapper = nameof(MapMatrixItem))]
    [MapCollection(nameof(MultiCollectionDestination.Items), Mapper = nameof(MapMatrixItem))]
    [MapCollection(nameof(MultiCollectionDestination.Values), Mapper = nameof(MapMatrixItem))]
    [MapCollection(nameof(MultiCollectionDestination.Sequence), Mapper = nameof(MapMatrixItem))]
    [MapCollection(nameof(MultiCollectionDestination.Optional), Mapper = nameof(MapMatrixItem))]
    [MapNested(nameof(MultiCollectionDestination.Child), Mapper = nameof(MapNestedChild))]
    public static partial MultiCollectionDestination MapMultiCollection(MultiCollectionSource source);

    // InPlace mixed with Replace in the same mapper
    [Mapper]
    [MapCollection(nameof(MultiCollectionDestination.Lines), Mapper = nameof(MapMatrixItem))]
    [MapCollection(nameof(MultiCollectionDestination.Items), Mapper = nameof(MapMatrixItem), Strategy = CollectionStrategy.InPlace)]
    [MapCollection(nameof(MultiCollectionDestination.Values), Mapper = nameof(MapMatrixItem))]
    [MapCollection(nameof(MultiCollectionDestination.Sequence), Mapper = nameof(MapMatrixItem), Strategy = CollectionStrategy.InPlace)]
    [MapCollection(nameof(MultiCollectionDestination.Optional), Mapper = nameof(MapMatrixItem), Strategy = CollectionStrategy.InPlace)]
    [MapNested(nameof(MultiCollectionDestination.Child), Mapper = nameof(MapNestedChild))]
    public static partial void MapMultiCollectionInPlace(MultiCollectionSource source, MultiCollectionDestination destination);
}

// MapExpression: expressions declaring the same variable names in one mapper
internal static partial class TestMappers
{
    // Return type
    [Mapper]
    [MapExpression(nameof(ExpressionDestination.First), "int.TryParse(source.First, out var n) ? n : -1")]
    [MapExpression(nameof(ExpressionDestination.Second), "int.TryParse(source.Second, out var n) ? n : -1")]
    [MapExpression(nameof(ExpressionDestination.Boxed), "source.Boxed is int n ? n : -1")]
    [MapExpression(nameof(ExpressionDestination.OtherBoxed), "source.OtherBoxed is int n ? n : -1")]
    public static partial ExpressionDestination MapExpressionToNew(ExpressionSource source);

    // Void: the expressions can also read the destination parameter
    [Mapper]
    [MapExpression(nameof(ExpressionDestination.First), "int.TryParse(source.First, out var n) ? n : -1")]
    [MapExpression(nameof(ExpressionDestination.Second), "int.TryParse(source.Second, out var n) ? n : -1")]
    [MapExpression(nameof(ExpressionDestination.Boxed), "source.Boxed is int n ? n : -1")]
    [MapExpression(nameof(ExpressionDestination.OtherBoxed), "source.OtherBoxed is int n ? n : -1")]
    [MapExpression(nameof(ExpressionDestination.Label), "destination.Label ?? \"(none)\"")]
    public static partial void MapExpressionInto(ExpressionSource source, ExpressionDestination destination);

    // init-only / required targets
    [Mapper]
    [MapExpression(nameof(ExpressionInitDestination.First), "int.TryParse(source.First, out var n) ? n : -1")]
    [MapExpression(nameof(ExpressionInitDestination.Second), "int.TryParse(source.Second, out var n) ? n : -1")]
    [MapExpression(nameof(ExpressionInitDestination.Boxed), "source.Boxed is int n ? n : -1")]
    public static partial ExpressionInitDestination MapExpressionInit(ExpressionSource source);

    // in parameter and custom parameter; Order decides the evaluation order
    [Mapper]
    [MapExpression(nameof(ExpressionDestination.First), "int.TryParse(source.Name, out var n) ? n + context.Offset : -1")]
    [MapExpression(nameof(ExpressionDestination.Second), "source.Name is { Length: > 0 } n ? n.Length : 0")]
    [MapExpression(nameof(ExpressionDestination.Boxed), "context.Next()", Order = 2)]
    [MapExpression(nameof(ExpressionDestination.OtherBoxed), "context.Next()", Order = 1)]
    public static partial ExpressionDestination MapExpressionWithContext(in ReadOnlyStructSource source, ExpressionContext context);
}

// Parameter modifiers: the implementation repeats the modifier each parameter is declared with
internal static partial class TestMappers
{
    // readonly struct source passed by value
    [Mapper]
    public static partial ReadOnlyStructDestination MapReadOnlyStructByValue(ReadOnlyStructSource source);

    // Mutable struct source passed by in, read by an expression
    [Mapper]
    [MapExpression(nameof(ReadOnlyStructDestination.Name), "source.Name + \"!\"")]
    public static partial ReadOnlyStructDestination MapMutableStructIn(in MutableStructSource source);

    // Struct destination passed by ref, so the caller's instance is filled
    [Mapper]
    [MapExpression(nameof(MutableStructDestination.Total), "destination.Total + source.Id")]
    public static partial void MapIntoStruct(MutableStructSource source, ref MutableStructDestination destination);

    // Custom parameter passed by ref readonly, read by an expression and a MapUsing method
    [Mapper(AutoMap = false)]
    [MapExpression(nameof(ExpressionDestination.First), "int.TryParse(source.First, out var n) ? n + context.Offset : -1")]
    [MapUsing(nameof(ExpressionDestination.Label), nameof(BuildContextLabel))]
    public static partial ExpressionDestination MapWithReadOnlyContext(ExpressionSource source, ref readonly ExpressionContext context);

    private static string BuildContextLabel(ExpressionSource source, ExpressionContext context) => $"{source.Second}:{context.Offset}";
}

// Hook parameter modifiers: each argument is passed the way the hook's parameter takes it
internal static partial class TestMappers
{
    // AfterMap taking the struct destination by ref: the change reaches the returned instance
    [Mapper]
    [AfterMap(nameof(AddTotal))]
    public static partial MutableStructDestination MapStructWithAfterMap(MutableStructSource source);

    // The same for a destination parameter passed by ref: the change reaches the caller's instance
    [Mapper]
    [AfterMap(nameof(AddTotal))]
    public static partial void MapIntoStructWithAfterMap(MutableStructSource source, ref MutableStructDestination destination);

    private static void AddTotal(MutableStructSource source, ref MutableStructDestination destination) => destination.Total += source.Id * 10;

    // MapUsing taking the readonly struct source by in
    [Mapper]
    [MapUsing(nameof(ReadOnlyStructDestination.Name), nameof(DescribeReadOnlyStruct))]
    public static partial ReadOnlyStructDestination MapWithInHook(in ReadOnlyStructSource source);

    private static string DescribeReadOnlyStruct(in ReadOnlyStructSource source) => $"{source.Name}#{source.Id}";

    // Converter and condition taking the custom parameter by in and ref readonly
    [Mapper]
    [MapProperty(nameof(BasicDestination.Name), nameof(BasicSource.Name), Converter = nameof(DecorateName))]
    [MapCondition(nameof(BasicDestination.Description), nameof(ShouldCopyDescription))]
    public static partial BasicDestination MapWithHookContext(BasicSource source, in HookContext context);

    private static string DecorateName(string value, in HookContext context) => context.Prefix + value;

    private static bool ShouldCopyDescription(string value, ref readonly HookContext context) => context.CopyDescription;
}

// Element mappers, collection converters and culture overloads: each argument is passed the way the
// parameter takes it
internal static partial class TestMappers
{
    // Element and nested mappers taking the element by in: from a span, an array, a foreach variable and
    // an indexer, and the property value of the nested object
    [Mapper]
    [MapCollection(nameof(MultiCollectionDestination.Lines), Mapper = nameof(MapMatrixItemIn))]
    [MapCollection(nameof(MultiCollectionDestination.Items), Mapper = nameof(MapMatrixItemIn), Strategy = CollectionStrategy.InPlace)]
    [MapCollection(nameof(MultiCollectionDestination.Values), Mapper = nameof(MapMatrixItemIn))]
    [MapCollection(nameof(MultiCollectionDestination.Sequence), Mapper = nameof(MapMatrixItemIn))]
    [MapCollection(nameof(MultiCollectionDestination.Optional), Mapper = nameof(MapMatrixItemIn))]
    [MapNested(nameof(MultiCollectionDestination.Child), Mapper = nameof(MapNestedChildIn))]
    public static partial MultiCollectionDestination MapMultiCollectionIn(MultiCollectionSource source);

    private static MatrixDstItem MapMatrixItemIn(in MatrixSrcItem source) => new() { Value = source.Value + 100 };

    private static NestedObjectDestinationChild MapNestedChildIn(in NestedObjectSourceChild source) => new() { Value = source.Value + 100, Text = source.Text };

    // Void mappers filling the struct instance created for each element and for the nested object by ref
    [Mapper]
    [MapCollection(nameof(PathDestination.Points), Mapper = nameof(FillPoint))]
    [MapCollection(nameof(PathDestination.Route), Mapper = nameof(FillPoint))]
    [MapNested(nameof(PathDestination.Origin), Mapper = nameof(FillPoint))]
    public static partial PathDestination MapPath(PathSource source);

    private static void FillPoint(in PointSource source, ref PointDestination destination)
    {
        destination.X = source.X;
        destination.Y = source.Y;
    }

    // Collection converter taking its arguments by in
    [Mapper]
    [CollectionConverter(typeof(InCollectionConverter))]
    [MapCollection(nameof(MatrixToListDst.Items), nameof(MatrixListSource.Items), Mapper = nameof(MapMatrixItem))]
    public static partial MatrixToListDst MapListWithInConverter(MatrixListSource source);

    // Culture overload taking the culture by ref readonly
    [Mapper(Culture = "de-DE")]
    [ValueConverter(typeof(CultureReferenceConverter))]
    public static partial CultureReferenceDestination MapWithCultureReference(CultureReferenceSource source);
}

// Converter classes nested in another class, and a dotted path to a parsable type
internal static partial class TestMappers
{
    // Nested value converter: its specialized method converts the value
    [Mapper]
    [ValueConverter(typeof(ConverterHost.NestedValueConverter))]
    [MapIgnore(nameof(NestedConverterDestination.Items))]
    public static partial NestedConverterDestination MapWithNestedValueConverter(NestedConverterSource source);

    // Collection converter nested in another class and generic
    [Mapper]
    [CollectionConverter(typeof(ConverterHost.NestedCollectionConverter<string>))]
    [MapCollection(nameof(NestedConverterDestination.Items), Mapper = nameof(FormatNestedItem))]
    public static partial NestedConverterDestination MapWithNestedCollectionConverter(NestedConverterSource source);

    private static string FormatNestedItem(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // Dotted source path to a parsable type
    [Mapper]
    [MapProperty(nameof(ParsePathDestination.Id), "Child.Text")]
    public static partial ParsePathDestination MapParsePath(ParsePathSource source);
}

// Nullable annotations of the declaration are repeated on the implementation
internal static partial class TestMappers
{
    // Nullable source, custom parameter and return type
    [Mapper]
    [MapUsing(nameof(BasicDestination.Description), nameof(DescribeWithOptionalContext))]
    public static partial BasicDestination? MapNullableSignature(BasicSource? source, HookContext? context);

    private static string DescribeWithOptionalContext(BasicSource source, HookContext? context) => (context?.Prefix ?? "-") + source.Description;

    // Nullable source into a return type that is not nullable, and into a struct
    [Mapper]
    public static partial BasicDestination MapNullableSourceToDestination(BasicSource? source);

    [Mapper]
    public static partial MutableStructDestination MapNullableSourceToStruct(BasicSource? source);

    // Void mapper with a nullable source, and with a nullable destination
    [Mapper]
    public static partial void MapNullableSourceInto(BasicSource? source, BasicDestination destination);

    [Mapper]
    public static partial void MapIntoNullableDestination(BasicSource source, BasicDestination? destination);
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
