namespace Smart.Mapper;

using System.Globalization;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

public class PathAndConversionMappingTests
{
    // An enum at the end of a dotted source or target path converts as a member the path does not go through does: by
    // member name to another enum and to and from text, and by number to and from an integer
    [Fact]
    public void DottedPathConvertsEnums()
    {
        var destination = TestMappers.MapPathEnum(new PathEnumSource
        {
            Inner = new PathEnumInner { Status = PathStatus.Suspended, Text = "Suspended", Code = 1 },
            Status = PathStatus.Active
        });

        Assert.Equal("Suspended", destination.Text);
        Assert.Equal(PathStatusDto.Suspended, destination.Status);
        Assert.Equal(PathStatus.Suspended, destination.FromText);
        Assert.Equal(1, destination.Number);
        Assert.Equal(PathStatus.Suspended, destination.FromNumber);
        Assert.Equal("Active", destination.Outer.Text);
    }

    // A reference type converting to the target implicitly is converted by its operator for a value only, as C# does not
    // lift the conversion of a reference type; a null source gives null, NullValue, or default for a target not taking null
    [Fact]
    public void ReferenceTypeConvertsImplicitlyForValueOnly()
    {
        var withValue = TestMappers.MapEmail(new EmailSource { Primary = new EmailAddress("a@example.com"), Backup = new EmailAddress("b@example.com") });
        Assert.Equal("a@example.com", withValue.Primary);
        Assert.Equal("b@example.com", withValue.Backup);

        var withNull = TestMappers.MapEmail(new EmailSource());
        Assert.Null(withNull.Primary);
        Assert.Equal("none", withNull.Backup);

        Assert.Equal("legacy@example.com", TestMappers.MapLegacyEmail(new LegacyEmailSource { Primary = new EmailAddress("legacy@example.com") }).Primary);
        Assert.Null(TestMappers.MapLegacyEmail(new LegacyEmailSource()).Primary);
    }

    // The destination parameter of a void mapper declared with nullable annotations disabled is checked as a nullable one
    // is: a null destination maps nothing
    [Fact]
    public void ObliviousDestinationParameterIsChecked()
    {
        Assert.Null(Record.Exception(static () => TestMappers.MapObliviousInto(new ObliviousSource { Name = "name" }, null)));

        var destination = new ObliviousDestination();
        TestMappers.MapObliviousInto(new ObliviousSource { Name = "name", Note = "note" }, destination);
        Assert.Equal("name", destination.Name);
        Assert.Equal("note", destination.Note);
    }

    // DateTime goes to text in the round-trip format (O), and text to DateTime with the kind it gives: UTC for a Z,
    // unspecified without an offset, and local for an offset
    [Fact]
    public void DateTimeConvertsInRoundTripFormat()
    {
        var utc = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc);
        var unspecified = new DateTime(2024, 1, 2, 3, 4, 5, 678, DateTimeKind.Unspecified);

        var destination = TestMappers.MapDateTimeText(new DateTimeTextSource
        {
            At = utc,
            NullableAt = unspecified,
            AtText = "2024-01-02T03:04:05.6780000Z",
            NullableAtText = "2024-01-02T03:04:05.6780000"
        });

        Assert.Equal("2024-01-02T03:04:05.6780000Z", destination.At);
        Assert.Equal("2024-01-02T03:04:05.6780000", destination.NullableAt);
        Assert.Equal(utc, destination.AtText);
        Assert.Equal(DateTimeKind.Utc, destination.AtText.Kind);
        Assert.Equal(unspecified, destination.NullableAtText);
        Assert.Equal(DateTimeKind.Unspecified, destination.NullableAtText!.Value.Kind);

        var withNull = TestMappers.MapDateTimeText(new DateTimeTextSource { AtText = "2024-01-02T03:04:05.0000000+09:00" });
        Assert.Null(withNull.NullableAt);
        Assert.Null(withNull.NullableAtText);
        Assert.Equal(DateTimeKind.Local, withNull.AtText.Kind);
        Assert.Equal(new DateTime(2024, 1, 1, 18, 4, 5, DateTimeKind.Utc), withNull.AtText.ToUniversalTime());
    }

    [Fact]
    public void DateTimeRoundTripsThroughText()
    {
        foreach (var value in new[]
                 {
                     new DateTime(2024, 5, 6, 7, 8, 9, 123, DateTimeKind.Utc).AddTicks(4567),
                     new DateTime(2024, 5, 6, 7, 8, 9, 123, DateTimeKind.Local),
                     new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Unspecified)
                 })
        {
            var text = TestMappers.MapDateTimeText(new DateTimeTextSource { At = value, AtText = "2000-01-01" }).At;
            var back = TestMappers.MapDateTimeText(new DateTimeTextSource { AtText = text }).AtText;

            Assert.Equal(value.ToString("O", CultureInfo.InvariantCulture), text);
            Assert.Equal(value, back);
            Assert.Equal(value.Kind, back.Kind);
        }
    }

    // A dotted source path through a nullable struct reads the struct it holds under its null check, as through a
    // nullable reference: a null one leaves the target as it is in an assignment, gives NullValue, and gives default to a
    // constructor argument
    [Fact]
    public void NullableStructPathIsReadUnderNullCheck()
    {
        var withValue = TestMappers.MapGeo(new GeoSource { Location = new GeoPoint { Lat = 1.5, Label = "here" } });
        Assert.Equal(1.5, withValue.Lat);
        Assert.Equal("here", withValue.Label);
        Assert.Equal(1.5, withValue.FromLat);

        var withNull = TestMappers.MapGeo(new GeoSource());
        Assert.Equal(-1, withNull.Lat);
        Assert.Equal("none", withNull.Label);
        Assert.Equal(-1, withNull.FromLat);

        var withNullLabel = TestMappers.MapGeo(new GeoSource { Location = new GeoPoint { Lat = 2 } });
        Assert.Equal(2, withNullLabel.Lat);
        Assert.Equal("none", withNullLabel.Label);

        Assert.Equal(new GeoRecord(0, "none"), TestMappers.MapGeoRecord(new GeoSource()));
        Assert.Equal(new GeoRecord(3, "there"), TestMappers.MapGeoRecord(new GeoSource { Location = new GeoPoint { Lat = 3, Label = "there" } }));
    }
}
