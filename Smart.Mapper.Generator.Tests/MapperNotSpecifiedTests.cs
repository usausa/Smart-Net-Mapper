namespace Smart.Mapper.Generator.Tests;

using System.Globalization;
using System.Linq;

// [MapCollection] / [MapNested] without Mapper is reported as the mapper not matching (SMP0210 / SMP0211) with a message
// saying that it is not specified, where it used to name an empty mapper (mapper=[]). Their source is a property of the
// source type, which the message of SMP0206 says for a dotted path.
public class MapperNotSpecifiedTests
{
    private static string Source(string attributes) =>
        $$"""
        #nullable enable
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public class Child { public int V { get; set; } }
        public class ChildDto { public int V { get; set; } }
        public class Parent { public Child Child { get; set; } = new(); }
        public class Src { public Child Child { get; set; } = new(); public List<Child> Items { get; set; } = []; public Parent Parent { get; set; } = new(); }
        public class Dst { public ChildDto Child { get; set; } = new(); public List<ChildDto> Items { get; set; } = []; }
        public static partial class M
        {
            [Mapper]
            public static partial ChildDto MapChild(Child source);

            [Mapper(AutoMap = false)]
            {{attributes}}
            public static partial Dst Map(Src source);
        }
        """;

    [Theory]
    [InlineData("[MapCollection(nameof(Dst.Items))]", "SMP0210", "[MapCollection] Mapper is not specified", "target=[Items]")]
    [InlineData("[MapNested(nameof(Dst.Child))]", "SMP0211", "[MapNested] Mapper is not specified", "target=[Child]")]
    [InlineData("[MapCollection(nameof(Dst.Items), Mapper = \"Missing\")]", "SMP0210", "element mapper method does not match", "mapper=[Missing]")]
    [InlineData("[MapNested(nameof(Dst.Child), Mapper = \"Missing\")]", "SMP0211", "mapper method does not match", "mapper=[Missing]")]
    [InlineData("[MapNested(nameof(Dst.Child), \"Parent.Child\", Mapper = nameof(MapChild))]", "SMP0206", "cannot be a dotted path", "source=[Parent.Child]")]
    public void MessageTellsCause(string attributes, string id, string cause, string argument)
    {
        var diagnostic = GeneratorTestHelper.GetDiagnostics(Source(attributes)).Single();
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);

        Assert.Equal(id, diagnostic.Id);
        Assert.Contains(cause, message, StringComparison.Ordinal);
        Assert.Contains(argument, message, StringComparison.Ordinal);
        Assert.DoesNotContain("mapper=[]", message, StringComparison.Ordinal);
    }
}
