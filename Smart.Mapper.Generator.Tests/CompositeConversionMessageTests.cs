namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

// A class, a struct or a collection going to one no conversion takes, most likely a nested member or a collection
// without its [MapNested] / [MapCollection], is reported with SMP0402 as before, with a message telling to map a nested
// member with [MapNested], a collection with [MapCollection], and other types with a converter. Another value no
// conversion takes keeps the message it had.
public class CompositeConversionMessageTests
{
    private static string Source(string destination) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Customer { public string Name { get; set; } = ""; }
        public class CustomerDto { public string Name { get; set; } = ""; }
        public struct Point { public int X { get; set; } }
        public struct PointDto { public int X { get; set; } }
        public class Line { public int V { get; set; } }
        public class LineDto { public int V { get; set; } }
        public class Src
        {
            public Customer Customer { get; set; } = new();
            public Point? Point { get; set; }
            public List<Line> Lines { get; set; } = [];
            public DateOnly Day { get; set; }
            public string Text { get; set; } = "";
        }
        public class Dst { {{destination}} }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            [MapProperty("Target", "Source")]
            public static partial Dst Map(Src src);
        }
        """;

    private static string Message(string destination, string source)
    {
        var text = Source(destination).Replace("\"Source\"", "\"" + source + "\"", StringComparison.Ordinal);
        var diagnostic = Assert.Single(GeneratorTestHelper.GetDiagnostics(text), static d => d.Id == "SMP0402");
        return diagnostic.GetMessage(CultureInfo.InvariantCulture);
    }

    [Theory]
    [InlineData("public CustomerDto Target { get; set; } = new();", "Customer")]
    [InlineData("public PointDto Target { get; set; }", "Point")]
    [InlineData("public List<LineDto> Target { get; set; } = [];", "Lines")]
    public void CompositeTypeMessageTellsTheAttributes(string destination, string source)
    {
        Assert.StartsWith(
            "Conversion falls back to a non-AOT-safe path; map a nested member with [MapNested], a collection with [MapCollection], and other types with a converter.",
            Message(destination, source),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public DateTime Target { get; set; }", "Day")]
    [InlineData("public Uri? Target { get; set; }", "Text")]
    public void OtherValueKeepsItsMessage(string destination, string source)
    {
        Assert.StartsWith("Conversion falls back to a non-AOT-safe path. method=[Map]", Message(destination, source), StringComparison.Ordinal);
    }
}
