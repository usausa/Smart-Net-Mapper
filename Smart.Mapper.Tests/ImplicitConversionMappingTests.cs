namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// A converter returning a type the target converts to implicitly (decimal to decimal?), and a value the target takes by
// an implicit reference conversion through variance (IReadOnlyList<WideningCircle> to IReadOnlyList<WideningShape>),
// assigned as it is
public class ImplicitConversionMappingTests
{
    [Fact]
    public void ImplicitConversionsAreAssigned()
    {
        var source = new WideningConversionSource { Price = "1.5", Circles = [new WideningCircle { Size = 2 }] };

        var destination = TestMappers.MapWideningConversion(source);

        Assert.Equal(1.5m, destination.Price);
        Assert.Same(source.Circles, destination.Circles);
    }
}
