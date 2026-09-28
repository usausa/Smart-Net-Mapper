namespace Smart.Mapper.Generator.Tests;

using System.Globalization;

// A dotted target going through a nullable struct (Location.Lat for a GeoPoint? Location) is not written into: the path
// would write into the struct it holds, a copy read through Value that no setter takes back. It is reported as a target
// that cannot be assigned (SMP0214), with a message saying so, where a dotted source reads through one. The other
// targets that cannot be assigned keep the message they had.
public class NullableStructTargetTests
{
    private const string Message = "goes through a nullable struct";

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public struct Geo { public double Lat { get; set; } }
        public class Holder { public Geo? Location { get; set; } public Geo Home { get; } }
        public class Src { public double Lat { get; set; } public GeoSource? Point { get; set; } }
        public struct GeoSource { public double Lat { get; set; } }
        public class Dst { public Geo? Location { get; set; } public Holder Holder { get; set; } = new(); public double Lat { get; set; } }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static double Twice(Src src) => src.Lat * 2;
            private static bool IsSet(double value) => value > 0;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(\"Location.Lat\", nameof(Src.Lat))]", "Location.Lat")]
    [InlineData("[MapProperty(\"Location.Value.Lat\", nameof(Src.Lat))]", "Location.Value.Lat")]
    [InlineData("[MapProperty(\"Holder.Location.Lat\", nameof(Src.Lat))]", "Holder.Location.Lat")]
    [InlineData("[MapConstant(\"Location.Lat\", 1.5)]", "Location.Lat")]
    [InlineData("[MapUsing(\"Location.Lat\", nameof(Twice))]", "Location.Lat")]
    [InlineData("[MapProperty(nameof(Dst.Lat))] [MapCondition(\"Location.Lat\", nameof(IsSet))]", "Location.Lat")]
    public void NullableStructTargetIsReportedWithItsMessage(string attributes, string target)
    {
        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(Source(attributes)));
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);

        Assert.Equal("SMP0214", diagnostic.Id);
        Assert.Contains(Message, message, StringComparison.Ordinal);
        Assert.Contains($"target=[{target}]", message, StringComparison.Ordinal);
    }

    // A void mapper as well
    [Fact]
    public void NullableStructTargetOfVoidMapperIsReported()
    {
        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(Source(
            "[MapProperty(\"Location.Lat\", nameof(Src.Lat))]",
            "public static partial void Map(Src src, Dst dst);")));

        Assert.Equal("SMP0214", diagnostic.Id);
        Assert.Contains(Message, diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    // A target that cannot be assigned otherwise keeps its message, and a dotted source goes through one
    [Fact]
    public void OtherTargetKeepsMessage()
    {
        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(Source("[MapProperty(\"Holder.Home.Lat\", nameof(Src.Lat))]")));

        Assert.Equal("SMP0214", diagnostic.Id);
        Assert.DoesNotContain(Message, diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain(GeneratorTestHelper.GetDiagnostics(Source("[MapProperty(nameof(Dst.Lat), \"Point.Lat\")]")), static d => d.Id.StartsWith("SMP", StringComparison.Ordinal));
    }
}
