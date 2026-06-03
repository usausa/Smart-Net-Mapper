namespace Smart.Mapper.Generator.Tests;

using Xunit;

public class MapperSourceBuilderTests
{
    // Simple mapping generates correct property assignments
    [Fact]
    public void SimpleMappingGeneratesPropertyAssignments()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Source { public int Value { get; set; } public string Name { get; set; } = ""; }
            public class Dest { public int Value { get; set; } public string Name { get; set; } = ""; }
            public partial class Mapper
            {
                [Mapper]
                public static partial Dest Map(Source src);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("destination.Value = src.Value", generated, StringComparison.Ordinal);
        Assert.Contains("destination.Name = src.Name", generated, StringComparison.Ordinal);
    }

    // Constructor mapping (record) generates new Dest(src.Prop) pattern
    [Fact]
    public void RecordMappingGeneratesConstructorPattern()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class Source { public int Id { get; set; } public string Name { get; set; } = ""; }
            public record Dest(int Id, string Name);
            public partial class Mapper
            {
                [Mapper]
                public static partial Dest Map(Source src);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("new", generated, StringComparison.Ordinal);
        Assert.Contains("src.Id", generated, StringComparison.Ordinal);
        Assert.Contains("src.Name", generated, StringComparison.Ordinal);
    }

    // Collection mapping generates DefaultCollectionConverter.ToList(...) or ToArray(...) pattern
    [Fact]
    public void CollectionMappingGeneratesHelperPattern()
    {
        var source = """
            using Smart.Mapper;
            using System.Collections.Generic;
            namespace Test;
            public class SrcItem { public int X { get; set; } }
            public class DstItem { public int X { get; set; } }
            public class Source { public List<SrcItem> Items { get; set; } = new(); }
            public class Dest { public List<DstItem> Items { get; set; } = new(); }
            public partial class Mapper
            {
                [Mapper]
                [MapCollection("Items", Mapper = "MapItem")]
                public static partial Dest Map(Source src);
                [Mapper]
                public static partial DstItem MapItem(SrcItem s);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        // Inline collection path: uses CollectionsMarshal.AsSpan for List<T> source
        Assert.Contains("MapItem(", generated, StringComparison.Ordinal);
        // Assigns to destination.Items
        Assert.Contains("destination.Items", generated, StringComparison.Ordinal);
    }

    // Void nested mapping generates if (...) { var __nested = new T(); Mapper(...); dest.Prop = __nested; } pattern
    [Fact]
    public void VoidNestedMappingGeneratesLocalVariablePattern()
    {
        var source = """
            using Smart.Mapper;
            namespace Test;
            public class SrcNested { public int X { get; set; } }
            public class DstNested { public int X { get; set; } }
            public class Source { public SrcNested? Child { get; set; } }
            public class Dest { public DstNested Child { get; set; } = new(); }
            public partial class Mapper
            {
                [Mapper]
                [MapNested("Child", Mapper = "MapChild")]
                public static partial Dest Map(Source src);
                [Mapper]
                public static partial void MapChild(SrcNested s, DstNested d);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);

        var generated = GeneratorTestHelper.GetGeneratedSource(source);
        Assert.Contains("var __nested_Child", generated, StringComparison.Ordinal);
        Assert.Contains("new", generated, StringComparison.Ordinal);
        Assert.Contains("MapChild(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Func<", generated, StringComparison.Ordinal);
    }
}
