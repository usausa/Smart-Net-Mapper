#pragma warning disable SA1136
#pragma warning disable SA1502
#pragma warning disable CA1008
#pragma warning disable CA2227
namespace Smart.Mapper.AotTests;

// ---- Models ----

// Basic

public sealed class BasicSource { public int Id { get; set; } public string Name { get; set; } = default!; }

public sealed class BasicDest { public int Id { get; set; } public string Name { get; set; } = default!; }

// Type conversion

public sealed class TypeConvSource { public int IntVal { get; set; } public long LongVal { get; set; } public double DoubleVal { get; set; } }

public sealed class TypeConvDest { public string IntVal { get; set; } = default!; public int LongVal { get; set; } public float DoubleVal { get; set; } }

// Enum

public enum SrcStatus { Active = 1, Inactive = 2 }

public enum DstStatus { Active, Inactive }

public sealed class EnumSource { public SrcStatus Status { get; set; } }

public sealed class EnumDest { public DstStatus Status { get; set; } }

// Null handling

public sealed class NullSource { public string? Name { get; set; } public int? Count { get; set; } }

public sealed class NullDest { public string Name { get; set; } = default!; public int Count { get; set; } }

// Nested

public sealed class ChildSrc { public int Value { get; set; } }

public sealed class FlatSrc { public ChildSrc? Child { get; set; } public int DirectVal { get; set; } }

public sealed class FlatDst { public int ChildValue { get; set; } public int DirectVal { get; set; } }

// Collection

public sealed class ItemSrc { public int Id { get; set; } public string Label { get; set; } = default!; }

public sealed class ItemDst { public int Id { get; set; } public string Label { get; set; } = default!; }

public sealed class CollSrc { public List<ItemSrc>? Items { get; set; } }

public sealed class CollDst
{
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<ItemDst>? Items { get; set; }
}

// Custom value converter

public sealed class CvtSource { public int Value { get; set; } }

public sealed class CvtDest { public string Value { get; set; } = default!; }

public static class IntToStringConverter
{
    public static TDest Convert<TSrc, TDest>(TSrc source)
    {
        if (typeof(TSrc) == typeof(int) && typeof(TDest) == typeof(string))
        {
            var v = (int)(object)source!;
            return (TDest)(object)v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        throw new NotSupportedException($"Conversion from {typeof(TSrc)} to {typeof(TDest)} is not supported.");
    }
}

// ---- Mappers ----

internal static partial class AotMappers
{
    // Basic void and return

    [Mapper]
    public static partial void Map(BasicSource src, BasicDest dst);

    [Mapper]
    public static partial BasicDest MapToNew(BasicSource src);

    // Type conversion

    [Mapper]
    public static partial void Map(TypeConvSource src, TypeConvDest dst);

    // Enum

    [Mapper]
    public static partial void Map(EnumSource src, EnumDest dst);

    // Null handling with NullValue

    [Mapper]
    [MapProperty(nameof(NullDest.Name), nameof(NullSource.Name), NullValue = "")]
    [MapProperty(nameof(NullDest.Count), nameof(NullSource.Count), NullValue = 0)]
    public static partial void Map(NullSource src, NullDest dst);

    // Nested property

    [Mapper]
    [MapProperty(nameof(FlatDst.ChildValue), $"{nameof(FlatSrc.Child)}.{nameof(ChildSrc.Value)}")]
    public static partial void Map(FlatSrc src, FlatDst dst);

    // Collection child mapper

    [Mapper]
    public static partial ItemDst MapItem(ItemSrc src);

    // Collection

    [Mapper]
    [MapCollection(nameof(CollDst.Items), nameof(CollSrc.Items), Mapper = nameof(MapItem))]
    public static partial void Map(CollSrc src, CollDst dst);

    // Custom value converter

    [Mapper]
    [ValueConverter(typeof(IntToStringConverter))]
    public static partial void Map(CvtSource src, CvtDest dst);
}
