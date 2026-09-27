namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// The init-only and required targets of [MapNested] / [MapCollection] are made before construction and set in the
// object initializer, a null source giving default
public class InitializerTargetMappingTests
{
    [Fact]
    public void InitOnlyAndRequiredTargetsAreSet()
    {
        var destination = TestMappers.MapInitTarget(new InitTargetSource
        {
            Head = new ReferenceElementItem { Value = 1 },
            Items = [new ReferenceElementItem { Value = 2 }, new ReferenceElementItem { Value = 3 }]
        });

        Assert.Equal(1, destination.Head?.Value);
        Assert.Collection(destination.Items, static x => Assert.Equal(2, x.Value), static x => Assert.Equal(3, x.Value));
    }

    [Fact]
    public void NullSourceGivesDefault()
    {
        var destination = TestMappers.MapInitTarget(new InitTargetSource());

        Assert.Null(destination.Head);
        Assert.Null(destination.Items);
    }
}
