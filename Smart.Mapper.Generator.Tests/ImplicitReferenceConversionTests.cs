namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// A value that may be null (nullable, or declared with nullable annotations disabled) going to the target by a
// user-defined implicit conversion of a reference type, such as a value object with an implicit operator to string, is
// converted by the operator as a cast inside its null check, as C# does not lift the conversion of a reference type. It
// used to go through the generic Convert<TSource, TDestination> of the converter class, which casts the object and threw
// InvalidCastException at run time. A value that is not null is assigned as it is, C# converting it.
public class ImplicitReferenceConversionTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source), problems);
    }

    private static string Source(string attributes, string mapper = "public static partial Dst Map(Src src);") =>
        $$"""
        #nullable enable
        using Smart.Mapper;
        namespace Test;
        public sealed record Email(string Value)
        {
            public static implicit operator string(Email email) => email.Value;
        }
        public sealed record Code(int Value)
        {
            public static implicit operator Code(string value) => new(value.Length);
        }
        #nullable disable
        public class LegacySrc { public Email Email { get; set; } }
        #nullable enable
        public class Src { public Email Email { get; set; } = new("e"); public Email? Backup { get; set; } public string? Text { get; set; } }
        public class Dst { public string Email { get; set; } = ""; public string? Backup { get; set; } public Code? Text { get; set; } }
        public class StrictDst { public string Backup { get; set; } = ""; }
        public record DstRecord(string? Backup);
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.Email))]", "__d.Email = src.Email;")]
    [InlineData("[MapProperty(nameof(Dst.Backup))]", "__d.Backup = src.Backup is not null ? (string)src.Backup : null;")]
    [InlineData("[MapProperty(nameof(Dst.Text))]", "__d.Text = src.Text is not null ? (global::Test.Code)src.Text : null;")]
    [InlineData("[MapProperty(nameof(StrictDst.Backup), NullValue = \"none\")]", "__d.Backup = src.Backup is not null ? (string)src.Backup : \"none\";", "public static partial StrictDst Map(Src src);")]
    [InlineData("[MapProperty(nameof(StrictDst.Backup), NullBehavior = NullBehavior.Skip)]", "__d.Backup = (string)src.Backup;", "public static partial StrictDst Map(Src src);")]
    [InlineData("[MapProperty(nameof(DstRecord.Backup))]", "new global::Test.DstRecord(src.Backup is not null ? (string)src.Backup : null)", "public static partial DstRecord Map(Src src);")]
    [InlineData("[MapProperty(nameof(Dst.Email))]", "__d.Email = src.Email is not null ? (string)src.Email : default!;", "public static partial Dst Map(LegacySrc src);")]
    public void ConversionIsCalledForValueOnly(string attributes, string expected, string mapper = "public static partial Dst Map(Src src);")
    {
        var (generated, problems) = Build(Source(attributes, mapper));

        Assert.Empty(problems);
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Convert<", generated, StringComparison.Ordinal);
    }
}
