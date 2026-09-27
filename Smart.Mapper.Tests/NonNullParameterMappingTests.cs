namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// A null source is not passed to a converter or a condition whose parameter does not take null, nor a null element to
// such an element mapper, and a nullable reference a [MapUsing] method returns is taken as it is
public class NonNullParameterMappingTests
{
    [Fact]
    public void NullSourceIsNotPassed()
    {
        var destination = new NonNullDestination();

        TestMappers.MapNonNull(new NonNullSource(), destination);

        Assert.Equal("keep", destination.Name);
        Assert.Equal("none", destination.Note);
        Assert.Equal("keep", destination.Code);
        Assert.Null(destination.Found);
    }

    [Fact]
    public void ValueIsPassed()
    {
        var destination = new NonNullDestination();

        TestMappers.MapNonNull(new NonNullSource { Name = " a ", Note = " b ", Code = "c" }, destination);

        Assert.Equal("a", destination.Name);
        Assert.Equal("b", destination.Note);
        Assert.Equal("c", destination.Code);
        Assert.Equal("c", destination.Found);
    }

    [Fact]
    public void ConstructorArgumentTakesDefaultForNull()
    {
        var empty = TestMappers.MapNonNullRecord(new NonNullSource());
        var filled = TestMappers.MapNonNullRecord(new NonNullSource { Name = " a ", Code = " b " });

        Assert.Null(empty.Code);
        Assert.Equal("a", filled.Name);
        Assert.Equal("b", filled.Code);
    }

    [Fact]
    public void NullElementGivesDefault()
    {
        var destination = TestMappers.MapReferenceElements(new ReferenceElementSource { Items = [new ReferenceElementItem { Value = 1 }, null] });

        Assert.Collection(destination.Items, static x => Assert.Equal(1, x!.Value), static x => Assert.Null(x));
    }
}
