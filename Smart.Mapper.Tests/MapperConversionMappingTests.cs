namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// The mappers of [MapNested] / [MapCollection] matched through conversions: the result into an interface it
// implements, the source into a base class, and a nullable struct as the value it holds, a null one giving default
public class MapperConversionMappingTests
{
    [Fact]
    public void ConvertedMappersAreCalled()
    {
        var destination = TestMappers.MapConversion(new ConversionSource
        {
            Child = new ConvertedDerived { Value = 3 },
            Point = new ConvertedPoint(4),
            Children = [new ConvertedDerived { Value = 5 }],
            Points = [new ConvertedPoint(6), null]
        });

        Assert.Equal(3, Assert.IsType<ConvertedChild>(destination.Child).Value);
        Assert.Equal(4, destination.Point.X);
        Assert.Equal(5, Assert.Single(destination.Children).Value);
        Assert.Collection(destination.Points, static x => Assert.Equal(6, x!.Value.X), static x => Assert.Null(x));
    }

    [Fact]
    public void NullNullableStructGivesDefault()
    {
        var destination = TestMappers.MapConversion(new ConversionSource { Point = null });

        Assert.Equal(default, destination.Point);
    }
}
