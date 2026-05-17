namespace Smart.Mapper;

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

    private static void OnBeforeMap(BeforeAfterSource source, BeforeAfterDestination destination)
    {
        destination.BeforeMapCalled = true;
    }

    private static void OnAfterMap(BeforeAfterSource source, BeforeAfterDestination destination)
    {
        destination.AfterMapCalled = true;
    }

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

    private static void OnBeforeMapWithContext(BasicSource source, BasicDestination destination, CustomMappingContext context)
    {
        context.BeforeMapCalled = true;
    }

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
        return !string.IsNullOrEmpty(name);
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
    [MapConverter(typeof(TestCustomConverter))]
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
    [MapConverter(typeof(SpecializedConverter))]
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
    [Mapper]
    public static partial void MapPartialEnum(PartialEnumSource source, PartialEnumDestination destination);
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
