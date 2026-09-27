namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// A generic mapper method maps the members of its type parameters, and creates a destination type parameter with
// new T(), with the members of its constraint type mapped. A nullable struct is returned as the struct it holds.
public class GenericMapperMethodMappingTests
{
    [Fact]
    public void GenericBoxIsMapped()
    {
        var destination = TestMappers.MapGenericBox(new GenericBox<string> { Value = "text", Count = 2 });

        Assert.Equal("text", destination.Value);
        Assert.Equal(2L, destination.Count);
    }

    // A nullable struct is returned as the struct it holds, and null for a null source
    [Fact]
    public void NullableStructIsReturnedFilled()
    {
        var destination = TestMappers.MapNullableStruct(new GenericEntitySource { Id = 3, Name = "name" });

        Assert.NotNull(destination);
        Assert.Equal(3, destination.Value.Id);
        Assert.Equal("name", destination.Value.Name);
        Assert.Null(TestMappers.MapNullableStruct(null));
    }

    [Fact]
    public void TypeParameterDestinationIsCreated()
    {
        var destination = TestMappers.CreateGenericEntity<GenericDerivedEntity>(new GenericEntitySource { Id = 7, Name = "name" });

        Assert.True(destination.Derived);
        Assert.Equal(7, destination.Id);
        Assert.Equal("name", destination.Name);
    }
}
