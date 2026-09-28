#pragma warning disable CA1815
namespace Smart.Mapper.Models;

using System.Diagnostics.CodeAnalysis;

// Enums at the end of a dotted path, converted as members the path does not go through are
public enum PathStatus
{
    Active,
    Suspended
}

public enum PathStatusDto
{
    Active,
    Suspended
}

public class PathEnumInner
{
    public PathStatus Status { get; set; }

    public string Text { get; set; } = "Active";

    public int Code { get; set; }
}

public class PathEnumSource
{
    public PathEnumInner Inner { get; set; } = new();

    public PathStatus Status { get; set; }
}

public class PathEnumOuter
{
    public string Text { get; set; } = string.Empty;
}

public class PathEnumDestination
{
    public string Text { get; set; } = string.Empty;

    public PathStatusDto Status { get; set; }

    public PathStatus FromText { get; set; }

    public int Number { get; set; }

    public PathStatus FromNumber { get; set; }

    public PathEnumOuter Outer { get; set; } = new();
}

// A reference type converting to string implicitly, whose operator does not take null
public sealed record EmailAddress(string Value)
{
    public static implicit operator string(EmailAddress address) => address.Value;
}

public sealed class EmailSource
{
    public EmailAddress? Primary { get; set; }

    public EmailAddress? Backup { get; set; }
}

public sealed class EmailDestination
{
    public string? Primary { get; set; } = "init";

    public string Backup { get; set; } = "init";
}

#nullable disable
public sealed class LegacyEmailSource
{
    public EmailAddress Primary { get; set; }
}
#nullable restore

public sealed class LegacyEmailDestination
{
    public string Primary { get; set; } = "init";
}

// DateTime and text by default, in the round-trip format
public sealed class DateTimeTextSource
{
    public DateTime At { get; set; }

    public DateTime? NullableAt { get; set; }

    public string AtText { get; set; } = string.Empty;

    public string? NullableAtText { get; set; }
}

public sealed class DateTimeTextDestination
{
    public string At { get; set; } = string.Empty;

    public string? NullableAt { get; set; }

    public DateTime AtText { get; set; }

    public DateTime? NullableAtText { get; set; }
}

// A dotted source path through a nullable struct
public struct GeoPoint
{
    public double Lat { get; set; }

    public string? Label { get; set; }
}

public sealed class GeoSource
{
    public GeoPoint? Location { get; set; }
}

public sealed class GeoDestination
{
    public double Lat { get; set; } = -1;

    public string Label { get; set; } = "init";

    public double? FromLat { get; set; } = -1;
}

public sealed record GeoRecord(double Lat, string Label);

// A value read through five members or more, the Value of a nullable struct counting as one, whose null state C# does not
// follow, so the generated code takes it into a variable after its null check
public struct DeepPathSpot
{
    public string? Name { get; set; }

    public string? Count { get; set; }

    public int? Level { get; set; }
}

public struct DeepPathArea
{
    public DeepPathSpot? Spot { get; set; }
}

public sealed class DeepPathSource
{
    public DeepPathArea? Area { get; set; }
}

public sealed class DeepPathDestination
{
    public string Name { get; set; } = "init";

    public int Count { get; set; } = -1;

    public string Level { get; set; } = "init";

    public int? Checked { get; set; } = -1;
}

public sealed record DeepPathRecord(string Name);

// Source members whose getter may return null by [MaybeNull], taken as nullable ones
public sealed class MaybeNullChild
{
    public string Value { get; set; } = string.Empty;
}

public sealed class MaybeNullSource
{
    [MaybeNull]
    public string Name { get; set; } = string.Empty;

    [MaybeNull]
    public string Note { get; set; } = string.Empty;

    [MaybeNull]
    public MaybeNullChild Child { get; set; } = new();
}

public sealed class MaybeNullDestination
{
    public string Name { get; set; } = "init";

    public string Upper { get; set; } = "init";

    public string Note { get; set; } = "init";

    public string ChildValue { get; set; } = "init";
}

// DateTime and text with the round-trip format (O) or the RFC 1123 format (R) given
public sealed class FormattedDateTimeSource
{
    public string At { get; set; } = string.Empty;

    public DateTime When { get; set; }
}

public sealed class FormattedDateTimeDestination
{
    public DateTime At { get; set; }

    public string When { get; set; } = string.Empty;
}
