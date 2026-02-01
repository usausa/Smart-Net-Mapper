namespace Smart.Mapper;

#region Test Models

public class BasicSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BasicDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DifferentPropertySource
{
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
}

public class DifferentPropertyDestination
{
    public int DestId { get; set; }
    public string DestName { get; set; } = string.Empty;
}

public class IgnoreSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public class IgnoreDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public class TypeConversionSource
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
}

public class TypeConversionDestination
{
    public string IntValue { get; set; } = string.Empty;
    public int StringValue { get; set; }
}

// Phase 2 Test Models
public class ConstantSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ConstantDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BeforeAfterSource
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class BeforeAfterDestination
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool BeforeMapCalled { get; set; }
    public bool AfterMapCalled { get; set; }
}

public class ExtendedTypeConversionSource
{
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }
    public bool BoolValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public string GuidString { get; set; } = string.Empty;
}

public class ExtendedTypeConversionDestination
{
    public string LongValue { get; set; } = string.Empty;
    public string DoubleValue { get; set; } = string.Empty;
    public string DecimalValue { get; set; } = string.Empty;
    public string BoolValue { get; set; } = string.Empty;
    public string DateTimeValue { get; set; } = string.Empty;
    public Guid GuidString { get; set; }
}

public class NumericConversionSource
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
}

public class NumericConversionDestination
{
    public long IntValue { get; set; }
    public int LongValue { get; set; }
    public decimal DoubleValue { get; set; }
}

// Nested mapping test models
public class FlatSource
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
}

public class DestinationChild
{
    public int Value { get; set; }
}

public class NestedDestination
{
    public DestinationChild? Child1 { get; set; }
    public DestinationChild? Child2 { get; set; }
    public DestinationChild? Child3 { get; set; }
}

// Source with nested properties (for flatten test)
public class NestedSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NestedSource
{
    public NestedSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class FlatDestination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int DirectValue { get; set; }
}




// Deep nested test models
public class DeepNestedChild
{
    public int Value { get; set; }
}

public class DeepNestedParent
{
    public DeepNestedChild? Inner { get; set; }
}

public class DeepNestedDestination
{
    public DeepNestedParent? Outer { get; set; }
}

public class DeepSource
{
    public int DeepValue { get; set; }
}

// Deep nested source test models (for flatten from deep nested source)
public class DeepSourceInner
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DeepSourceOuter
{
    public DeepSourceInner? Inner { get; set; }
}

public class DeepNestedSource
{
    public DeepSourceOuter? Outer { get; set; }
    public int DirectValue { get; set; }
}

public class DeepFlatDestination
{
    public int OuterInnerValue { get; set; }
    public string OuterInnerName { get; set; } = string.Empty;
    public int DirectValue { get; set; }
}

// Null handling test models - Nested source with nullable child
public class NullableNestedSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NullableNestedSource
{
    public NullableNestedSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class NullableNestedFlatDestination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int DirectValue { get; set; }
}

// Null handling test models - Simple nullable properties
public class NullablePropertySource
{
    public string? NullableName { get; set; }
    public int? NullableInt { get; set; }
    public string NonNullableName { get; set; } = string.Empty;
}


public class NullablePropertyDestination
{
    public string? NullableName { get; set; }
    public int? NullableInt { get; set; }
    public string NonNullableName { get; set; } = "default";
}

// Null handling - nullable to non-nullable
public class NullableToNonNullableSource
{
    public string? Name { get; set; }
}

public class NullableToNonNullableDestination
{
    public string Name { get; set; } = "original";
}

// Null handling - nullable int to string
public class NullableIntToStringSource
{
    public int? IntValue { get; set; }
}

public class NullableIntToStringDestination
{
    public string IntValue { get; set; } = "original";
}

// Converter test models
public class ConverterSource
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class ConverterDestination
{
    public string ConvertedValue { get; set; } = string.Empty;
    public string FormattedText { get; set; } = string.Empty;
}

// MapFrom test models
public class MapFromSource
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class MapFromDestination
{
    public string FullName { get; set; } = string.Empty;
    public string UpperCaseName { get; set; } = string.Empty;
}

public class MapFromContext
{
    public string Separator { get; set; } = " ";
}

// MapFromMethod test models
public class MapFromMethodSource
{
    public int[] Items { get; set; } = [];

    public int GetItemCount() => Items.Length;
    public int GetItemSum() => Items.Sum();
}

public class MapFromMethodDestination
{
    public int ItemCount { get; set; }
    public int ItemSum { get; set; }
}

// AutoMap = false test models
public class AutoMapSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class AutoMapDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

// MapCollection test models
public class CollectionSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CollectionSource
{
    public CollectionSourceChild[]? Children { get; set; }
    public List<CollectionSourceChild>? Items { get; set; }
    public int DirectValue { get; set; }
}

public class CollectionDestinationChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CollectionDestination
{
    public List<CollectionDestinationChild>? Children { get; set; }
    public CollectionDestinationChild[]? Items { get; set; }
    public int DirectValue { get; set; }
}

// MapNested test models
public class NestedObjectSourceChild
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class NestedObjectSource
{
    public NestedObjectSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class NestedObjectDestinationChild
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class NestedObjectDestination
{
    public NestedObjectDestinationChild? Child { get; set; }
    public int DirectValue { get; set; }
}

// MapCollection with void mapper test models
public class VoidMapperSourceChild
{
    public int Id { get; set; }
}

public class VoidMapperSource
{
    public VoidMapperSourceChild[]? Children { get; set; }
}

public class VoidMapperDestinationChild
{
    public int Id { get; set; }
    public string Extra { get; set; } = string.Empty;
}

public class VoidMapperDestination
{
    public List<VoidMapperDestinationChild>? Children { get; set; }
}

// Custom converter test models
public class CustomConverterSource
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
}

public class CustomConverterDestination
{
    public string IntValue { get; set; } = string.Empty;  // int -> string with custom format
    public int StringValue { get; set; }  // string -> int with custom parsing
}

// Custom converter implementation
public static class TestCustomConverter
{
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        // Custom int -> string conversion (with prefix)
        if (typeof(TSource) == typeof(int) && typeof(TDestination) == typeof(string))
        {
            var value = (int)(object)source!;
            return (TDestination)(object)$"PREFIX_{value}";
        }

        // Custom string -> int conversion (removes prefix)
        if (typeof(TSource) == typeof(string) && typeof(TDestination) == typeof(int))
        {
            var value = (string)(object)source!;
            if (value.StartsWith("NUM_"))
            {
                return (TDestination)(object)int.Parse(value.Substring(4));
            }
            return (TDestination)(object)int.Parse(value);
        }

        // Fallback to default converter
        return DefaultMapConverter.Convert<TSource, TDestination>(source);
    }
}

// Custom collection converter test models
public class CustomCollectionSource
{
    public CollectionSourceChild[]? Numbers { get; set; }
}

public class CustomCollectionDestination
{
    public List<CollectionDestinationChild>? Numbers { get; set; }
}

// Custom collection converter implementation (doubles Id values)
public static class TestCustomCollectionConverter
{
    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null) return default;
        // Custom: doubles Id value if destination has Id property
        return source.Select(x =>
        {
            var mapped = mapper(x);
            if (mapped is CollectionDestinationChild child)
            {
                child.Id *= 2;
            }
            return mapped;
        }).ToArray();
    }

    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null) return default;
        // Custom: doubles Id value if destination has Id property
        return source.Select(x =>
        {
            var mapped = mapper(x);
            if (mapped is CollectionDestinationChild child)
            {
                child.Id *= 2;
            }
            return mapped;
        }).ToList();
    }
}

#endregion

#region Mappers

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
    [MapProperty(nameof(DifferentPropertySource.SourceId), nameof(DifferentPropertyDestination.DestId))]
    [MapProperty(nameof(DifferentPropertySource.SourceName), nameof(DifferentPropertyDestination.DestName))]
    public static partial void Map(DifferentPropertySource source, DifferentPropertyDestination destination);

    // Different property names mapping with return type
    [Mapper]
    [MapProperty(nameof(DifferentPropertySource.SourceId), nameof(DifferentPropertyDestination.DestId))]
    [MapProperty(nameof(DifferentPropertySource.SourceName), nameof(DifferentPropertyDestination.DestName))]
    public static partial DifferentPropertyDestination MapToNew(DifferentPropertySource source);

    // Ignore property mapping
    [Mapper]
    [MapIgnore(nameof(IgnoreDestination.Secret))]
    public static partial void Map(IgnoreSource source, IgnoreDestination destination);

    // Type conversion mapping
    [Mapper]
    public static partial void Map(TypeConversionSource source, TypeConversionDestination destination);

    // Phase 2: Constant value mapping
    [Mapper]
    [MapConstant(nameof(ConstantDestination.Status), "Active")]
    [MapConstant(nameof(ConstantDestination.Version), 1)]
    [MapConstant(nameof(ConstantDestination.CreatedAt), null, Expression = "System.DateTime.Now")]
    public static partial void Map(ConstantSource source, ConstantDestination destination);

    // Phase 2: BeforeMap and AfterMap
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

    // Phase 2: Extended type conversions
    [Mapper]
    public static partial void Map(ExtendedTypeConversionSource source, ExtendedTypeConversionDestination destination);

    // Phase 2: Numeric conversions
    [Mapper]
    public static partial void Map(NumericConversionSource source, NumericConversionDestination destination);

    // Nested mapping: flat source to nested destination
    [Mapper]
    [MapProperty(nameof(FlatSource.Value1), $"{nameof(NestedDestination.Child1)}.{nameof(DestinationChild.Value)}")]
    [MapProperty(nameof(FlatSource.Value2), $"{nameof(NestedDestination.Child2)}.{nameof(DestinationChild.Value)}")]
    [MapProperty(nameof(FlatSource.Value3), $"{nameof(NestedDestination.Child3)}.{nameof(DestinationChild.Value)}")]
    public static partial void Map(FlatSource source, NestedDestination destination);

    // Nested mapping: flat source to nested destination with return type
    [Mapper]
    [MapProperty(nameof(FlatSource.Value1), $"{nameof(NestedDestination.Child1)}.{nameof(DestinationChild.Value)}")]
    [MapProperty(nameof(FlatSource.Value2), $"{nameof(NestedDestination.Child2)}.{nameof(DestinationChild.Value)}")]
    [MapProperty(nameof(FlatSource.Value3), $"{nameof(NestedDestination.Child3)}.{nameof(DestinationChild.Value)}")]
    public static partial NestedDestination MapToNew(FlatSource source);

    // Nested mapping: nested source to flat destination (flatten)
    [Mapper]
    [MapProperty($"{nameof(NestedSource.Child)}.{nameof(NestedSourceChild.Id)}", nameof(FlatDestination.ChildId))]
    [MapProperty($"{nameof(NestedSource.Child)}.{nameof(NestedSourceChild.Name)}", nameof(FlatDestination.ChildName))]
    public static partial void Map(NestedSource source, FlatDestination destination);

    // Deep nested mapping: flat source to deep nested destination
    [Mapper]
    [MapProperty(nameof(DeepSource.DeepValue), $"{nameof(DeepNestedDestination.Outer)}.{nameof(DeepNestedParent.Inner)}.{nameof(DeepNestedChild.Value)}")]
    public static partial void Map(DeepSource source, DeepNestedDestination destination);

    // Deep nested mapping with return type
    [Mapper]
    [MapProperty(nameof(DeepSource.DeepValue), $"{nameof(DeepNestedDestination.Outer)}.{nameof(DeepNestedParent.Inner)}.{nameof(DeepNestedChild.Value)}")]
    public static partial DeepNestedDestination MapToNew(DeepSource source);

    // Deep nested mapping: deep nested source to flat destination (multi-level flatten)
    [Mapper]
    [MapProperty($"{nameof(DeepNestedSource.Outer)}.{nameof(DeepSourceOuter.Inner)}.{nameof(DeepSourceInner.Value)}", nameof(DeepFlatDestination.OuterInnerValue))]
    [MapProperty($"{nameof(DeepNestedSource.Outer)}.{nameof(DeepSourceOuter.Inner)}.{nameof(DeepSourceInner.Name)}", nameof(DeepFlatDestination.OuterInnerName))]
    public static partial void Map(DeepNestedSource source, DeepFlatDestination destination);

    // Null handling: nested source to flat destination (with nullable source child)
    [Mapper]
    [MapProperty($"{nameof(NullableNestedSource.Child)}.{nameof(NullableNestedSourceChild.Id)}", nameof(NullableNestedFlatDestination.ChildId))]
    [MapProperty($"{nameof(NullableNestedSource.Child)}.{nameof(NullableNestedSourceChild.Name)}", nameof(NullableNestedFlatDestination.ChildName))]
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
    [MapProperty(nameof(ConverterSource.Value), nameof(ConverterDestination.ConvertedValue), Converter = nameof(ConvertIntToString))]
    public static partial void MapWithConverter(ConverterSource source, ConverterDestination destination);

    // Converter: with custom parameters
    [Mapper]
    [MapProperty(nameof(ConverterSource.Value), nameof(ConverterDestination.ConvertedValue), Converter = nameof(ConvertIntToStringWithContext))]
    [MapProperty(nameof(ConverterSource.Text), nameof(ConverterDestination.FormattedText), Converter = nameof(FormatTextWithContext))]
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

    // Converter methods
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

    // Condition: Global condition
    [Mapper]
    [MapCondition(nameof(ShouldMapCondition))]
    public static partial void MapWithCondition(ConditionSource source, ConditionDestination destination);

    // Condition: Property-level condition
    [Mapper]
    [MapPropertyCondition(nameof(ConditionDestination.Name), nameof(ShouldMapName))]
    public static partial void MapWithPropertyCondition(ConditionSource source, ConditionDestination destination);

    // Condition: Generic MapConstant test
    [Mapper]
    [MapConstant<int>(nameof(ConstantDestination.Version), 2)]
    [MapConstant<string>(nameof(ConstantDestination.Status), "Pending")]
    public static partial void MapWithGenericConstant(ConstantSource source, ConstantDestination destination);

    // Condition check methods
    private static bool ShouldMapCondition(ConditionSource source, ConditionDestination destination)
    {
        return source.IsActive;
    }

    private static bool ShouldMapName(string? name)
    {
        return !string.IsNullOrEmpty(name);
    }

    // MapFrom: basic usage
    [Mapper]
    [MapFrom(nameof(MapFromDestination.FullName), nameof(CombineFullName))]
    [MapFrom(nameof(MapFromDestination.UpperCaseName), nameof(GetUpperCaseName))]
    public static partial void Map(MapFromSource source, MapFromDestination destination);

    private static string CombineFullName(MapFromSource source)
    {
        return $"{source.FirstName} {source.LastName}";
    }

    private static string GetUpperCaseName(MapFromSource source)
    {
        return $"{source.FirstName} {source.LastName}".ToUpperInvariant();
    }

    // MapFrom: with custom parameters
    [Mapper]
    [MapFrom(nameof(MapFromDestination.FullName), nameof(CombineFullNameWithContext))]
    public static partial MapFromDestination MapWithContext(MapFromSource source, MapFromContext context);

    private static string CombineFullNameWithContext(MapFromSource source, MapFromContext context)
    {
        return $"{source.FirstName}{context.Separator}{source.LastName}";
    }

    // MapFromMethod: calling methods on source object
    [Mapper]
    [MapFromMethod(nameof(MapFromMethodDestination.ItemCount), nameof(MapFromMethodSource.GetItemCount))]
    [MapFromMethod(nameof(MapFromMethodDestination.ItemSum), nameof(MapFromMethodSource.GetItemSum))]
    public static partial void Map(MapFromMethodSource source, MapFromMethodDestination destination);

    // AutoMap = false: only explicitly mapped properties
    [Mapper(AutoMap = false)]
    [MapProperty(nameof(AutoMapSource.Id), nameof(AutoMapDestination.Id))]
    public static partial void MapExplicit(AutoMapSource source, AutoMapDestination destination);

    // AutoMap = false: with multiple MapProperty
    [Mapper(AutoMap = false)]
    [MapProperty(nameof(AutoMapSource.Id), nameof(AutoMapDestination.Id))]
    [MapProperty(nameof(AutoMapSource.Name), nameof(AutoMapDestination.Name))]
    public static partial AutoMapDestination MapExplicitToNew(AutoMapSource source);

    // MapCollection: child mapper (return value pattern)
    [Mapper]
    public static partial CollectionDestinationChild MapCollectionChild(CollectionSourceChild source);

    // MapCollection: array to List and List to array
    [Mapper]
    [MapCollection(nameof(CollectionSource.Children), nameof(CollectionDestination.Children), MapperMethod = nameof(MapCollectionChild))]
    [MapCollection(nameof(CollectionSource.Items), nameof(CollectionDestination.Items), MapperMethod = nameof(MapCollectionChild))]
    public static partial void Map(CollectionSource source, CollectionDestination destination);

    // MapCollection: with return type
    [Mapper]
    [MapCollection(nameof(CollectionSource.Children), nameof(CollectionDestination.Children), MapperMethod = nameof(MapCollectionChild))]
    [MapIgnore(nameof(CollectionDestination.Items))]  // Ignore Items to test single collection mapping
    public static partial CollectionDestination MapToNew(CollectionSource source);

    // MapNested: child mapper (return value pattern)
    [Mapper]
    public static partial NestedObjectDestinationChild MapNestedChild(NestedObjectSourceChild source);

    // MapNested: basic usage
    [Mapper]
    [MapNested(nameof(NestedObjectSource.Child), nameof(NestedObjectDestination.Child), MapperMethod = nameof(MapNestedChild))]
    public static partial void Map(NestedObjectSource source, NestedObjectDestination destination);

    // MapNested: with return type
    [Mapper]
    [MapNested(nameof(NestedObjectSource.Child), nameof(NestedObjectDestination.Child), MapperMethod = nameof(MapNestedChild))]
    public static partial NestedObjectDestination MapToNew(NestedObjectSource source);

    // MapCollection with void mapper
    [Mapper]
    public static partial void MapVoidChild(VoidMapperSourceChild source, VoidMapperDestinationChild destination);

    [Mapper]
    [MapCollection(nameof(VoidMapperSource.Children), nameof(VoidMapperDestination.Children), MapperMethod = nameof(MapVoidChild))]
    public static partial void Map(VoidMapperSource source, VoidMapperDestination destination);

    // MapNested with void mapper
    [Mapper]
    public static partial void MapNestedChildVoid(NestedObjectSourceChild source, NestedObjectDestinationChild destination);

    [Mapper]
    [MapNested(nameof(NestedObjectSource.Child), nameof(NestedObjectDestination.Child), MapperMethod = nameof(MapNestedChildVoid))]
    public static partial void MapWithVoidNested(NestedObjectSource source, NestedObjectDestination destination);

    // Custom type converter test
    [Mapper]
    [MapConverter(typeof(TestCustomConverter))]
    public static partial void MapWithCustomConverter(CustomConverterSource source, CustomConverterDestination destination);

    // Custom collection converter test - using CollectionSourceChild to CollectionDestinationChild
    [Mapper]
    [CollectionConverter(typeof(TestCustomCollectionConverter))]
    [MapCollection(nameof(CustomCollectionSource.Numbers), nameof(CustomCollectionDestination.Numbers), MapperMethod = nameof(MapCollectionChild))]
    public static partial void MapWithCustomCollectionConverter(CustomCollectionSource source, CustomCollectionDestination destination);
}

// Custom context for testing
public class CustomMappingContext
{
    public bool BeforeMapCalled { get; set; }
    public bool AfterMapCalled { get; set; }
    public string ContextValue { get; set; } = string.Empty;
}

// Condition test models
public class ConditionSource
{
    public int Value { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}

public class ConditionDestination
{
    public int Value { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}

#endregion

#region Tests

public class BasicMappingTests
{
    [Fact]
    public void Map_BasicProperties_CopiesAllProperties()
    {
        // Arrange
        var source = new BasicSource
        {
            Id = 42,
            Name = "Test Name",
            Description = "Test Description"
        };
        var destination = new BasicDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(42, destination.Id);
        Assert.Equal("Test Name", destination.Name);
        Assert.Equal("Test Description", destination.Description);
    }

    [Fact]
    public void MapToNew_BasicProperties_ReturnsNewObjectWithCopiedProperties()
    {
        // Arrange
        var source = new BasicSource
        {
            Id = 100,
            Name = "New Object",
            Description = "Created via MapToNew"
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.Equal(100, destination.Id);
        Assert.Equal("New Object", destination.Name);
        Assert.Equal("Created via MapToNew", destination.Description);
    }
}

public class DifferentPropertyMappingTests
{
    [Fact]
    public void Map_DifferentPropertyNames_MapsCorrectly()
    {
        // Arrange
        var source = new DifferentPropertySource
        {
            SourceId = 123,
            SourceName = "Different Name"
        };
        var destination = new DifferentPropertyDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(123, destination.DestId);
        Assert.Equal("Different Name", destination.DestName);
    }

    [Fact]
    public void MapToNew_DifferentPropertyNames_ReturnsCorrectlyMappedObject()
    {
        // Arrange
        var source = new DifferentPropertySource
        {
            SourceId = 456,
            SourceName = "Another Name"
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.Equal(456, destination.DestId);
        Assert.Equal("Another Name", destination.DestName);
    }
}

public class IgnorePropertyMappingTests
{
    [Fact]
    public void Map_IgnoredProperty_DoesNotCopyIgnoredProperty()
    {
        // Arrange
        var source = new IgnoreSource
        {
            Id = 1,
            Name = "Public",
            Secret = "TopSecret"
        };
        var destination = new IgnoreDestination
        {
            Secret = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(1, destination.Id);
        Assert.Equal("Public", destination.Name);
        Assert.Equal("Original", destination.Secret); // Should not be overwritten
    }
}

public class TypeConversionMappingTests
{
    [Fact]
    public void Map_TypeConversion_ConvertsTypes()
    {
        // Arrange
        var source = new TypeConversionSource
        {
            IntValue = 999,
            StringValue = "123"
        };
        var destination = new TypeConversionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("999", destination.IntValue);
        Assert.Equal(123, destination.StringValue);
    }
}

// Phase 2 Tests
public class ConstantMappingTests
{
    [Fact]
    public void Map_ConstantValues_SetsConstantsCorrectly()
    {
        // Arrange
        var source = new ConstantSource
        {
            Id = 1,
            Name = "Test"
        };
        var destination = new ConstantDestination();
        var beforeMap = DateTime.Now;

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(1, destination.Id);
        Assert.Equal("Test", destination.Name);
        Assert.Equal("Active", destination.Status);
        Assert.Equal(1, destination.Version);
        Assert.True(destination.CreatedAt >= beforeMap);
        Assert.True(destination.CreatedAt <= DateTime.Now);
    }
}

public class BeforeAfterMapTests
{
    [Fact]
    public void Map_BeforeAfterMap_CallsBothMethods()
    {
        // Arrange
        var source = new BeforeAfterSource
        {
            Value = 42,
            Text = "Hello"
        };
        var destination = new BeforeAfterDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(42, destination.Value);
        Assert.Equal("Hello", destination.Text);
        Assert.True(destination.BeforeMapCalled);
        Assert.True(destination.AfterMapCalled);
    }
}

public class ExtendedTypeConversionTests
{
    [Fact]
    public void Map_ExtendedTypeConversions_ConvertsCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0);
        var source = new ExtendedTypeConversionSource
        {
            LongValue = 1234567890L,
            DoubleValue = 3.14159,
            DecimalValue = 99.99m,
            BoolValue = true,
            DateTimeValue = dateTime,
            GuidString = guid.ToString()
        };
        var destination = new ExtendedTypeConversionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("1234567890", destination.LongValue);
        Assert.Contains("3.14159", destination.DoubleValue);
        Assert.Contains("99.99", destination.DecimalValue);
        Assert.Equal("True", destination.BoolValue);
        Assert.Equal(dateTime.ToString(), destination.DateTimeValue);
        Assert.Equal(guid, destination.GuidString);
    }
}

public class NumericConversionTests
{
    [Fact]
    public void Map_NumericConversions_ConvertsCorrectly()
    {
        // Arrange
        var source = new NumericConversionSource
        {
            IntValue = 100,
            LongValue = 200L,
            DoubleValue = 3.5
        };
        var destination = new NumericConversionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(100L, destination.IntValue);
        Assert.Equal(200, destination.LongValue);
        Assert.Equal(3.5m, destination.DoubleValue);
    }
}

// Nested mapping tests
public class NestedMappingTests
{
    [Fact]
    public void Map_FlatToNested_MapsToNestedProperties()
    {
        // Arrange
        var source = new FlatSource
        {
            Value1 = 10,
            Value2 = 20,
            Value3 = 30
        };
        var destination = new NestedDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Child1);
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
        Assert.Equal(10, destination.Child1.Value);
        Assert.Equal(20, destination.Child2.Value);
        Assert.Equal(30, destination.Child3.Value);
    }

    [Fact]
    public void MapToNew_FlatToNested_ReturnsNestedObject()
    {
        // Arrange
        var source = new FlatSource
        {
            Value1 = 100,
            Value2 = 200,
            Value3 = 300
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.NotNull(destination.Child1);
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
        Assert.Equal(100, destination.Child1.Value);
        Assert.Equal(200, destination.Child2.Value);
        Assert.Equal(300, destination.Child3.Value);
    }

    [Fact]
    public void Map_NestedToFlat_FlattensProperties()
    {
        // Arrange
        var source = new NestedSource
        {
            Child = new NestedSourceChild
            {
                Id = 42,
                Name = "Test"
            },
            DirectValue = 999
        };
        var destination = new FlatDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(42, destination.ChildId);
        Assert.Equal("Test", destination.ChildName);
        Assert.Equal(999, destination.DirectValue);
    }

    [Fact]
    public void Map_DeepNested_MapsToDeepNestedProperties()
    {
        // Arrange
        var source = new DeepSource
        {
            DeepValue = 12345
        };
        var destination = new DeepNestedDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Outer);
        Assert.NotNull(destination.Outer.Inner);
        Assert.Equal(12345, destination.Outer.Inner.Value);
    }


    [Fact]
    public void MapToNew_DeepNested_ReturnsDeepNestedObject()
    {
        // Arrange
        var source = new DeepSource
        {
            DeepValue = 67890
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.NotNull(destination.Outer);
        Assert.NotNull(destination.Outer.Inner);
        Assert.Equal(67890, destination.Outer.Inner.Value);
    }

    [Fact]
    public void Map_FlatToNested_PreservesExistingNestedObjects()
    {
        // Arrange
        var source = new FlatSource
        {
            Value1 = 10,
            Value2 = 20,
            Value3 = 30
        };
        var existingChild1 = new DestinationChild { Value = 999 };
        var destination = new NestedDestination
        {
            Child1 = existingChild1
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - Child1 should be the same instance, just with updated value
        Assert.Same(existingChild1, destination.Child1);
        Assert.Equal(10, destination.Child1.Value);
        // Child2 and Child3 should be created
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
    }

    [Fact]
    public void Map_DeepNestedSourceToFlat_FlattensMultipleLevels()
    {
        // Arrange
        var source = new DeepNestedSource
        {
            Outer = new DeepSourceOuter
            {
                Inner = new DeepSourceInner
                {
                    Value = 12345,
                    Name = "DeepName"
                }
            },
            DirectValue = 100
        };
        var destination = new DeepFlatDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(12345, destination.OuterInnerValue);
        Assert.Equal("DeepName", destination.OuterInnerName);
        Assert.Equal(100, destination.DirectValue);
    }

    [Fact]
    public void Map_DeepNestedSourceWithNullOuter_SkipsNestedProperties()
    {
        // Arrange
        var source = new DeepNestedSource
        {
            Outer = null,
            DirectValue = 200
        };
        var destination = new DeepFlatDestination
        {
            OuterInnerValue = 999,
            OuterInnerName = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - DirectValue should be copied, nested properties should be skipped
        Assert.Equal(200, destination.DirectValue);
        Assert.Equal(999, destination.OuterInnerValue);
        Assert.Equal("Original", destination.OuterInnerName);
    }

    [Fact]
    public void Map_DeepNestedSourceWithNullInner_SkipsNestedProperties()
    {
        // Arrange
        var source = new DeepNestedSource
        {
            Outer = new DeepSourceOuter
            {
                Inner = null
            },
            DirectValue = 300
        };
        var destination = new DeepFlatDestination
        {
            OuterInnerValue = 888,
            OuterInnerName = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - DirectValue should be copied, nested properties should be skipped
        Assert.Equal(300, destination.DirectValue);
        Assert.Equal(888, destination.OuterInnerValue);
        Assert.Equal("Original", destination.OuterInnerName);
    }
}

// Null handling tests
public class NullHandlingTests
{
    [Fact]
    public void Map_NestedSourceWithNullChild_SkipsCopyForNullSource()
    {
        // Arrange
        var source = new NullableNestedSource
        {
            Child = null,  // Child is null
            DirectValue = 100
        };
        var destination = new NullableNestedFlatDestination
        {
            ChildId = 999,       // Original values should be preserved
            ChildName = "Original",
            DirectValue = 0
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - DirectValue should be copied, but nested properties should be skipped
        Assert.Equal(100, destination.DirectValue);
        Assert.Equal(999, destination.ChildId);           // Should preserve original
        Assert.Equal("Original", destination.ChildName);  // Should preserve original
    }

    [Fact]
    public void Map_NestedSourceWithNonNullChild_CopiesNestedProperties()
    {
        // Arrange
        var source = new NullableNestedSource
        {
            Child = new NullableNestedSourceChild
            {
                Id = 42,
                Name = "Test"
            },
            DirectValue = 100
        };
        var destination = new NullableNestedFlatDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert - All properties should be copied
        Assert.Equal(100, destination.DirectValue);
        Assert.Equal(42, destination.ChildId);
        Assert.Equal("Test", destination.ChildName);
    }

    [Fact]
    public void Map_NullableProperties_CopiesNullValues()
    {
        // Arrange
        var source = new NullablePropertySource
        {
            NullableName = null,
            NullableInt = null,
            NonNullableName = "Test"
        };
        var destination = new NullablePropertyDestination
        {
            NullableName = "Original",
            NullableInt = 999
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - Nullable properties should be copied (including null)
        Assert.Null(destination.NullableName);
        Assert.Null(destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void Map_NullableProperties_CopiesNonNullValues()
    {
        // Arrange
        var source = new NullablePropertySource
        {
            NullableName = "NewName",
            NullableInt = 42,
            NonNullableName = "Test"
        };
        var destination = new NullablePropertyDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("NewName", destination.NullableName);
        Assert.Equal(42, destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void Map_NullableToNonNullable_WithNullSource_SetsDefault()
    {
        // Arrange
        var source = new NullableToNonNullableSource
        {
            Name = null
        };
        var destination = new NullableToNonNullableDestination
        {
            Name = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - Expected behavior: Set default! when source is null and target is non-nullable
        // For string, default! is null
        Assert.Null(destination.Name);
    }

    [Fact]
    public void Map_NullableToNonNullable_WithNonNullSource_CopiesValue()
    {
        // Arrange
        var source = new NullableToNonNullableSource
        {
            Name = "NewValue"
        };
        var destination = new NullableToNonNullableDestination
        {
            Name = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("NewValue", destination.Name);
    }

    [Fact]
    public void Map_NullableIntToString_WithNullSource_SetsDefault()
    {
        // Arrange
        var source = new NullableIntToStringSource
        {
            IntValue = null
        };
        var destination = new NullableIntToStringDestination
        {
            IntValue = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - int? null -> string should be default! (null)
        Assert.Null(destination.IntValue);
    }

    [Fact]
    public void Map_NullableIntToString_WithNonNullSource_ConvertsValue()
    {
        // Arrange
        var source = new NullableIntToStringSource
        {
            IntValue = 42
        };
        var destination = new NullableIntToStringDestination
        {
            IntValue = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("42", destination.IntValue);
    }
}

// Custom parameter tests
public class CustomParameterTests
{
    [Fact]
    public void MapWithContext_CallsBeforeMapAndAfterMapWithContext()
    {
        // Arrange
        var source = new BasicSource
        {
            Id = 1,
            Name = "Test",
            Description = "Desc"
        };
        var destination = new BasicDestination();
        var context = new CustomMappingContext { ContextValue = "TestContext" };

        // Act
        TestMappers.MapWithContext(source, destination, context);

        // Assert
        Assert.True(context.BeforeMapCalled);
        Assert.True(context.AfterMapCalled);
        Assert.Equal(1, destination.Id);
        Assert.Equal("Test", destination.Name);
    }

    [Fact]
    public void MapWithContextMixed_CallsBeforeMapWithoutContextAndAfterMapWithContext()
    {
        // Arrange
        var source = new BasicSource
        {
            Id = 2,
            Name = "Mixed",
            Description = "Desc"
        };
        var destination = new BasicDestination();
        var context = new CustomMappingContext();

        // Act
        TestMappers.MapWithContextMixed(source, destination, context);

        // Assert
        Assert.False(context.BeforeMapCalled); // Basic version doesn't set this
        Assert.True(context.AfterMapCalled);
        Assert.Equal(2, destination.Id);
    }

    [Fact]
    public void MapToNewWithContext_ReturnsNewObjectAndCallsAfterMapWithContext()
    {
        // Arrange
        var source = new BasicSource
        {
            Id = 3,
            Name = "Return",
            Description = "Original"
        };
        var context = new CustomMappingContext { ContextValue = "ReturnContext" };

        // Act
        var destination = TestMappers.MapToNewWithContext(source, context);

        // Assert
        Assert.True(context.AfterMapCalled);
        Assert.Equal(3, destination.Id);
        Assert.Equal("Return", destination.Name);
        Assert.Equal("Modified by AfterMap: ReturnContext", destination.Description);
    }
}

// Converter tests
public class ConverterTests
{
    [Fact]
    public void MapWithConverter_UsesCustomConverter()
    {
        // Arrange
        var source = new ConverterSource
        {
            Value = 42,
            Text = "Hello"
        };
        var destination = new ConverterDestination();

        // Act
        TestMappers.MapWithConverter(source, destination);

        // Assert
        Assert.Equal("Value: 42", destination.ConvertedValue);
    }

    [Fact]
    public void MapWithConverterAndContext_UsesCustomConverterWithContext()
    {
        // Arrange
        var source = new ConverterSource
        {
            Value = 100,
            Text = "Hello"
        };
        var destination = new ConverterDestination();
        var context = new CustomMappingContext { ContextValue = "TestContext" };

        // Act
        TestMappers.MapWithConverterAndContext(source, destination, context);

        // Assert
        Assert.Equal("Value: 100, Context: TestContext", destination.ConvertedValue);
        Assert.Equal("Hello (formatted with TestContext)", destination.FormattedText);
    }
}

// Condition tests
public class ConditionTests
{
    [Fact]
    public void MapWithCondition_WhenConditionTrue_MapsProperties()
    {
        // Arrange
        var source = new ConditionSource
        {
            Value = 42,
            Name = "Test",
            IsActive = true
        };
        var destination = new ConditionDestination();

        // Act
        TestMappers.MapWithCondition(source, destination);

        // Assert
        Assert.Equal(42, destination.Value);
        Assert.Equal("Test", destination.Name);
        Assert.True(destination.IsActive);
    }

    [Fact]
    public void MapWithCondition_WhenConditionFalse_DoesNotMapProperties()
    {
        // Arrange
        var source = new ConditionSource
        {
            Value = 42,
            Name = "Test",
            IsActive = false
        };
        var destination = new ConditionDestination
        {
            Value = 100,
            Name = "Original"
        };

        // Act
        TestMappers.MapWithCondition(source, destination);

        // Assert - Values should remain unchanged
        Assert.Equal(100, destination.Value);
        Assert.Equal("Original", destination.Name);
        Assert.False(destination.IsActive);
    }

    [Fact]
    public void MapWithPropertyCondition_WhenNameNotNull_MapsName()
    {
        // Arrange
        var source = new ConditionSource
        {
            Value = 42,
            Name = "Test",
            IsActive = true
        };
        var destination = new ConditionDestination();

        // Act
        TestMappers.MapWithPropertyCondition(source, destination);

        // Assert
        Assert.Equal(42, destination.Value);
        Assert.Equal("Test", destination.Name);
    }

    [Fact]
    public void MapWithPropertyCondition_WhenNameNull_DoesNotMapName()
    {
        // Arrange
        var source = new ConditionSource
        {
            Value = 42,
            Name = null,
            IsActive = true
        };
        var destination = new ConditionDestination
        {
            Name = "Original"
        };

        // Act
        TestMappers.MapWithPropertyCondition(source, destination);

        // Assert - Name should remain unchanged
        Assert.Equal(42, destination.Value);
        Assert.Equal("Original", destination.Name);
    }

    [Fact]
    public void MapWithGenericConstant_SetsGenericConstantValues()
    {
        // Arrange
        var source = new ConstantSource { Name = "Test" };
        var destination = new ConstantDestination();

        // Act
        TestMappers.MapWithGenericConstant(source, destination);

        // Assert
        Assert.Equal("Test", destination.Name);
        Assert.Equal(2, destination.Version);
        Assert.Equal("Pending", destination.Status);
    }
}

// MapFrom tests
public class MapFromTests
{
    [Fact]
    public void MapFrom_ComputesValueFromMethod()
    {
        // Arrange
        var source = new MapFromSource
        {
            FirstName = "John",
            LastName = "Doe"
        };
        var destination = new MapFromDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("John Doe", destination.FullName);
        Assert.Equal("JOHN DOE", destination.UpperCaseName);
    }

    [Fact]
    public void MapFrom_WithCustomParameters_UsesContext()
    {
        // Arrange
        var source = new MapFromSource
        {
            FirstName = "Jane",
            LastName = "Smith"
        };
        var context = new MapFromContext { Separator = " - " };

        // Act
        var destination = TestMappers.MapWithContext(source, context);

        // Assert
        Assert.Equal("Jane - Smith", destination.FullName);
    }
}

// MapFromMethod tests
public class MapFromMethodTests
{
    [Fact]
    public void MapFromMethod_CallsSourceMethod()
    {
        // Arrange
        var source = new MapFromMethodSource
        {
            Items = new[] { 1, 2, 3, 4, 5 }
        };
        var destination = new MapFromMethodDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(5, destination.ItemCount);
        Assert.Equal(15, destination.ItemSum);
    }
}

// AutoMap = false tests
public class AutoMapFalseTests
{
    [Fact]
    public void AutoMapFalse_OnlyMapsExplicitProperties()
    {
        // Arrange
        var source = new AutoMapSource
        {
            Id = 42,
            Name = "Test",
            Value = 100
        };
        var destination = new AutoMapDestination
        {
            Id = 0,
            Name = "Original",
            Value = 0
        };

        // Act
        TestMappers.MapExplicit(source, destination);

        // Assert
        Assert.Equal(42, destination.Id);  // Explicitly mapped
        Assert.Equal("Original", destination.Name);  // Not mapped (AutoMap = false)
        Assert.Equal(0, destination.Value);  // Not mapped (AutoMap = false)
    }

    [Fact]
    public void AutoMapFalse_WithMapProperty_OnlyMapsSpecified()
    {
        // Arrange
        var source = new AutoMapSource
        {
            Id = 10,
            Name = "Explicit",
            Value = 200
        };

        // Act
        var destination = TestMappers.MapExplicitToNew(source);

        // Assert
        Assert.Equal(10, destination.Id);  // Explicitly mapped
        Assert.Equal("Explicit", destination.Name);  // Explicitly mapped
        Assert.Equal(0, destination.Value);  // Not mapped
    }
}

// MapCollection tests
public class MapCollectionTests
{
    [Fact]
    public void MapCollection_ArrayToList_MapsElements()
    {
        // Arrange
        var source = new CollectionSource
        {
            Children =
            [
                new CollectionSourceChild { Id = 1, Name = "Child1" },
                new CollectionSourceChild { Id = 2, Name = "Child2" }
            ],
            DirectValue = 100
        };
        var destination = new CollectionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Children);
        Assert.Equal(2, destination.Children.Count);
        Assert.Equal(1, destination.Children[0].Id);
        Assert.Equal("Child1", destination.Children[0].Name);
        Assert.Equal(2, destination.Children[1].Id);
        Assert.Equal("Child2", destination.Children[1].Name);
        Assert.Equal(100, destination.DirectValue);
    }

    [Fact]
    public void MapCollection_ListToArray_MapsElements()
    {
        // Arrange
        var source = new CollectionSource
        {
            Items =
            [
                new CollectionSourceChild { Id = 10, Name = "Item1" },
                new CollectionSourceChild { Id = 20, Name = "Item2" },
                new CollectionSourceChild { Id = 30, Name = "Item3" }
            ]
        };
        var destination = new CollectionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Items);
        Assert.Equal(3, destination.Items.Length);
        Assert.Equal(10, destination.Items[0].Id);
        Assert.Equal(20, destination.Items[1].Id);
        Assert.Equal(30, destination.Items[2].Id);
    }

    [Fact]
    public void MapCollection_NullSource_SetsDefault()
    {
        // Arrange
        var source = new CollectionSource
        {
            Children = null,
            DirectValue = 50
        };
        var destination = new CollectionDestination
        {
            Children = [new CollectionDestinationChild { Id = 999 }]
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Null(destination.Children);  // default! assigns null
        Assert.Equal(50, destination.DirectValue);
    }

    [Fact]
    public void MapCollection_WithReturnType_ReturnsNewObject()
    {
        // Arrange
        var source = new CollectionSource
        {
            Children =
            [
                new CollectionSourceChild { Id = 5, Name = "Test" }
            ]
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.NotNull(destination.Children);
        Assert.Single(destination.Children);
        Assert.Equal(5, destination.Children[0].Id);
    }

    [Fact]
    public void MapCollection_WithVoidMapper_MapsElements()
    {
        // Arrange
        var source = new VoidMapperSource
        {
            Children =
            [
                new VoidMapperSourceChild { Id = 100 },
                new VoidMapperSourceChild { Id = 200 }
            ]
        };
        var destination = new VoidMapperDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Children);
        Assert.Equal(2, destination.Children.Count);
        Assert.Equal(100, destination.Children[0].Id);
        Assert.Equal(200, destination.Children[1].Id);
    }
}

// MapNested tests
public class MapNestedTests
{
    [Fact]
    public void MapNested_WithValue_MapsNestedObject()
    {
        // Arrange
        var source = new NestedObjectSource
        {
            Child = new NestedObjectSourceChild { Value = 42, Text = "Hello" },
            DirectValue = 100
        };
        var destination = new NestedObjectDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Child);
        Assert.Equal(42, destination.Child.Value);
        Assert.Equal("Hello", destination.Child.Text);
        Assert.Equal(100, destination.DirectValue);
    }

    [Fact]
    public void MapNested_NullSource_SetsDefault()
    {
        // Arrange
        var source = new NestedObjectSource
        {
            Child = null,
            DirectValue = 50
        };
        var destination = new NestedObjectDestination
        {
            Child = new NestedObjectDestinationChild { Value = 999 }
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Null(destination.Child);  // default! assigns null
        Assert.Equal(50, destination.DirectValue);
    }

    [Fact]
    public void MapNested_WithReturnType_ReturnsNewObject()
    {
        // Arrange
        var source = new NestedObjectSource
        {
            Child = new NestedObjectSourceChild { Value = 123, Text = "Test" },
            DirectValue = 456
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.NotNull(destination.Child);
        Assert.Equal(123, destination.Child.Value);
        Assert.Equal("Test", destination.Child.Text);
        Assert.Equal(456, destination.DirectValue);
    }

    [Fact]
    public void MapNested_WithVoidMapper_MapsNestedObject()
    {
        // Arrange
        var source = new NestedObjectSource
        {
            Child = new NestedObjectSourceChild { Value = 77, Text = "VoidTest" },
            DirectValue = 88
        };
        var destination = new NestedObjectDestination();

        // Act
        TestMappers.MapWithVoidNested(source, destination);

        // Assert
        Assert.NotNull(destination.Child);
        Assert.Equal(77, destination.Child.Value);
        Assert.Equal("VoidTest", destination.Child.Text);
        Assert.Equal(88, destination.DirectValue);
    }
}

// Custom converter tests
public class CustomConverterTests
{
    [Fact]
    public void MapWithCustomConverter_UsesCustomConversion()
    {
        // Arrange
        var source = new CustomConverterSource
        {
            IntValue = 42,
            StringValue = "NUM_100"
        };
        var destination = new CustomConverterDestination();

        // Act
        TestMappers.MapWithCustomConverter(source, destination);

        // Assert
        Assert.Equal("PREFIX_42", destination.IntValue);  // Custom int -> string conversion
        Assert.Equal(100, destination.StringValue);  // Custom string -> int conversion
    }

    [Fact]
    public void MapWithCustomCollectionConverter_UsesCustomCollectionConversion()
    {
        // Arrange
        var source = new CustomCollectionSource
        {
            Numbers =
            [
                new CollectionSourceChild { Id = 1, Name = "One" },
                new CollectionSourceChild { Id = 2, Name = "Two" },
                new CollectionSourceChild { Id = 3, Name = "Three" }
            ]
        };
        var destination = new CustomCollectionDestination();

        // Act
        TestMappers.MapWithCustomCollectionConverter(source, destination);

        // Assert
        Assert.NotNull(destination.Numbers);
        Assert.Equal(3, destination.Numbers.Count);
        // Custom converter doubles Id values
        Assert.Equal(2, destination.Numbers[0].Id);
        Assert.Equal(4, destination.Numbers[1].Id);
        Assert.Equal(6, destination.Numbers[2].Id);
    }
}

#endregion
