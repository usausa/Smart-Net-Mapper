namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The compiler follows the null state of a member read through four members from the parameter at most, so a value
// read through five or more, a nullable struct read through its Value counting as one (Location.In.V as
// source.Location.Value.In.Value.V), is not known to be not null after its null check. Where the generated code checks
// such a value and then uses it (a converter or a condition not taking null, the conversion of a nullable reference), it
// takes it into a variable with a pattern and uses the variable. It used to read the path again, which warned (CS8604,
// CS8629). A value read through fewer members is checked and read again as before.
public class CheckedValueCaptureTests
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
        public struct Inner { public int? V { get; set; } public string? Name { get; set; } }
        public struct Geo { public Inner? In { get; set; } public Owner? Owner { get; set; } }
        public class Owner { public string? Name { get; set; } }
        public class Holder { public Geo? Geo { get; set; } }
        public class D { public string? Name { get; set; } }
        public class C { public D? D { get; set; } }
        public class B { public C? C { get; set; } }
        public class A { public B? B { get; set; } }
        public class Src { public Geo? Location { get; set; } public Holder? Holder { get; set; } public A? A { get; set; } }
        public class Out { public string V { get; set; } = ""; }
        public class Dst
        {
            public string V { get; set; } = "";
            public string Name { get; set; } = "";
            public int Number { get; set; }
            public Out Out { get; set; } = new();
            public string My_V { get; set; } = "";
        }
        public record Rec(string V);
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attributes}}
            {{mapper}}

            private static string ToText(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            private static string Upper(string value) => value.ToUpperInvariant();
            private static bool IsSet(int value) => value > 0;
        }
        """;

    [Theory]
    [InlineData("[MapProperty(nameof(Dst.V), \"Location.In.V\", Converter = nameof(ToText))]", "if (src.Location.Value.In.Value.V is { } __value_V)", "__d.V = ToText(__value_V);")]
    [InlineData("[MapProperty(nameof(Dst.Name), \"Location.In.Name\", Converter = nameof(Upper))]", "if (src.Location.Value.In.Value.Name is { } __value_Name)", "__d.Name = Upper(__value_Name);")]
    [InlineData("[MapProperty(nameof(Dst.Name), \"Holder.Geo.Owner.Name\", Converter = nameof(Upper))]", "if (src.Holder.Geo.Value.Owner.Name is { } __value_Name)", "__d.Name = Upper(__value_Name);")]
    [InlineData("[MapProperty(nameof(Dst.Name), \"A.B.C.D.Name\", Converter = nameof(Upper))]", "if (src.A.B.C.D.Name is { } __value_Name)", "__d.Name = Upper(__value_Name);")]
    [InlineData("[MapProperty(nameof(Dst.Number), \"Location.In.Name\")]", "__d.Number = src.Location.Value.In.Value.Name is { } __value_Number ? global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(__value_Number) : default!;", "__value_Number")]
    [InlineData("[MapProperty(nameof(Dst.Number), \"Location.In.Name\", NullBehavior = NullBehavior.Skip)]", "if (src.Location.Value.In.Value.Name is { } __value_Number)", "__d.Number = global::Smart.Mapper.DefaultValueConverter.ConvertToInt32(__value_Number);")]
    [InlineData("[MapProperty(nameof(Dst.V), \"Location.In.V\")] [MapCondition(nameof(Dst.V), nameof(IsSet))]", "if (src.Location.Value.In.Value.V is { } __condition_V && IsSet(__condition_V))", "__d.V")]
    [InlineData("[MapProperty(\"Out.V\", \"Location.In.V\", Converter = nameof(ToText))]", "if (src.Location.Value.In.Value.V is { } __value_Out_dV)", "__d.Out.V = ToText(__value_Out_dV);")]
    [InlineData("[MapProperty(nameof(Dst.My_V), \"Location.In.V\", Converter = nameof(ToText))]", "if (src.Location.Value.In.Value.V is { } __value_My__V)", "__d.My_V = ToText(__value_My__V);")]
    public void CheckedValueIsTakenIntoVariable(string attributes, string check, string use)
    {
        var (generated, problems) = Build(Source(attributes));

        Assert.Empty(problems);
        Assert.Contains(check, generated, StringComparison.Ordinal);
        Assert.Contains(use, generated, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckedValueIsTakenIntoVariableInExpression()
    {
        var (generated, problems) = Build(Source(
            "[MapProperty(nameof(Rec.V), \"Location.In.V\", Converter = nameof(ToText))]",
            "public static partial Rec Map(Src src);").Replace("[Mapper(AutoMap = false)]", "[Mapper]", StringComparison.Ordinal));

        Assert.Empty(problems);
        Assert.Contains(
            "new global::Test.Rec(src.Location is not null && src.Location.Value.In is not null ? src.Location.Value.In.Value.V is { } __value_V ? ToText(__value_V) : default! : default!)",
            generated,
            StringComparison.Ordinal);
    }

    // Read through four members, the value is checked and read again as before
    [Fact]
    public void ValueReadThroughFewerMembersIsReadAgain()
    {
        var (generated, problems) = Build(Source("[MapProperty(nameof(Dst.Name), \"Location.Owner.Name\", Converter = nameof(Upper))]"));

        Assert.Empty(problems);
        Assert.Contains("if (src.Location.Value.Owner.Name is not null)", generated, StringComparison.Ordinal);
        Assert.Contains("__d.Name = Upper(src.Location.Value.Owner.Name);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("is { }", generated, StringComparison.Ordinal);
    }
}
