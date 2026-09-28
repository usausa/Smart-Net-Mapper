#pragma warning disable CA1815
namespace Smart.Mapper.Models;

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
