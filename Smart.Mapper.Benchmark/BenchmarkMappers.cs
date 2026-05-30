namespace Smart.Mapper.Benchmark;

// ベンチマーク用 Smart.Mapper マッパー定義。
// 各ベンチマークシナリオに対応した static partial メソッドを宣言する。
internal static partial class BenchmarkMappers
{
    // Scenario 1: Simple flat copy
    [Mapper]
    public static partial SimpleDestination MapSimple(SimpleSource source);

    // Scenario 2: Nested object
    [Mapper]
    [MapNested(nameof(NestedDestination.Address), nameof(NestedSource.Address), Mapper = nameof(MapAddress))]
    public static partial NestedDestination MapNested(NestedSource source);

    [Mapper]
    public static partial AddressDestination MapAddress(AddressSource source);

    // Scenario 3: Collection via wrapper object
    [Mapper]
    [MapCollection(nameof(CollectionWrapper.Items), nameof(CollectionSource.Items), Mapper = nameof(MapItem))]
    public static partial CollectionWrapper MapWrapper(CollectionSource source);

    [Mapper]
    public static partial CollectionItemDestination MapItem(CollectionItemSource source);

    // Scenario 4: Type conversion (numeric -> string)
    [Mapper]
    public static partial ConversionDestination MapConversion(ConversionSource source);

    // Scenario 5: Void-pattern nested (lambda-free after fix)
    [Mapper]
    public static partial void MapAddressVoid(AddressSource source, AddressDestination destination);

    [Mapper]
    [MapNested(nameof(NestedDestination.Address), nameof(NestedSource.Address), Mapper = nameof(MapAddressVoid))]
    public static partial NestedDestination MapNestedVoid(NestedSource source);
}
