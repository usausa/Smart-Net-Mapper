namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;

// Strict mode reports the members of a source enum that a mapping to another enum, which matches the members by name,
// finds no member of the same name for in the target enum, whose values give the target default, or null for a
// nullable one (SMP0503). The values of a [Flags] enum combining members are not known before they come, so only the
// members are looked at, and a converter given to the mapping takes over the conversion. It is reported at the
// attribute of the mapping, or at the method for the automatic mapping.
public class StrictEnumMemberTests
{
    private static List<Diagnostic> Warnings(string source) =>
        GeneratorTestHelper.GetDiagnostics(source).Where(static d => d.Id == "SMP0503").ToList();

    private static int MarkedLine(string source) =>
        Array.FindIndex(source.Split('\n'), static line => line.Contains("/*here*/", StringComparison.Ordinal));

    private static string Source(string attributes, bool strict = true, string members = "") =>
        $$"""
        #nullable enable
        using System;
        using Smart.Mapper;
        namespace Test;
        public enum Color { Red, Green, Blue }
        public enum Colour { Red, Green, Yellow }
        public enum Shade { Red, Green, Blue }
        [Flags] public enum Access { None = 0, Read = 1, Write = 2, Delete = 4 }
        [Flags] public enum Rights { None = 0, Read = 1, Write = 2 }
        public class Src { public Color Color { get; set; } public Color? Maybe { get; set; } public Access Access { get; set; } public Color Same { get; set; } }
        public class Dst { public Colour Color { get; set; } public Colour? Maybe { get; set; } public Rights Access { get; set; } public Shade Same { get; set; } }
        public static partial class M
        {
            [Mapper(Strict = {{(strict ? "true" : "false")}})] /*method*/
            {{attributes}}
            public static partial Dst Map(Src src);
        {{members}}
        }
        """;

    // The automatic mapping, at the method: a member of no counterpart, of a nullable enum as well, and a member of a
    // [Flags] enum, not the combinations
    [Fact]
    public void UnmatchedMembersAreReportedAtMethod()
    {
        var source = Source(string.Empty);

        var warnings = Warnings(source);
        Assert.Equal(
            ["target=[Color], members=[Blue]", "target=[Maybe], members=[Blue]", "target=[Access], members=[Delete]"],
            warnings.Select(static w => w.GetMessage(CultureInfo.InvariantCulture)).Select(static m => m[m.IndexOf("target=[", StringComparison.Ordinal)..]));
        var methodLine = Array.FindIndex(source.Split('\n'), static line => line.Contains("/*method*/", StringComparison.Ordinal));
        Assert.All(warnings, w => Assert.Equal(methodLine, w.Location.GetLineSpan().StartLinePosition.Line));
        Assert.All(warnings, static w => Assert.Equal(DiagnosticSeverity.Warning, w.Severity));
    }

    [Fact]
    public void UnmatchedMembersOfAttributeAreReportedAtAttribute()
    {
        var source = Source("[MapProperty(nameof(Dst.Color), nameof(Src.Color))] /*here*/");

        var warning = Assert.Single(Warnings(source), static w => w.GetMessage(CultureInfo.InvariantCulture).Contains("target=[Color]", StringComparison.Ordinal));
        Assert.Equal(MarkedLine(source), warning.Location.GetLineSpan().StartLinePosition.Line);
    }

    [Fact]
    public void ConverterIsNotReported()
    {
        var source = Source(
            "[MapProperty(nameof(Dst.Color), nameof(Src.Color), Converter = nameof(ToColour))]",
            members: "    private static Colour ToColour(Color value) => value == Color.Blue ? Colour.Yellow : (Colour)value;");

        Assert.DoesNotContain(Warnings(source), static w => w.GetMessage(CultureInfo.InvariantCulture).Contains("target=[Color]", StringComparison.Ordinal));
    }

    [Fact]
    public void NotReportedWithoutStrict()
    {
        Assert.Empty(Warnings(Source(string.Empty, strict: false)));
    }
}
