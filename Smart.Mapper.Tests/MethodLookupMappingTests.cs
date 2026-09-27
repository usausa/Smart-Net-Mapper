namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Methods of a base class of the mapper class, of the class containing it and of a global using static directive, a
// callback taking the source and the destination as interfaces, and converters taking the value, or the value a
// nullable struct holds, by boxing, a wider number or a user-defined conversion
public class MethodLookupMappingTests
{
    [Fact]
    public void MethodsOfBaseAndContainingClassesAreCalled()
    {
        var destination = LookupOuter.LookupMappers.Map(new LookupSource
        {
            Id = 7,
            Name = "ab",
            Count = 2,
            Stock = 3,
            Owner = new UserId { Value = 5 },
            Total = 9,
            Amount = 1.5m,
            Child = new LookupChild { Id = 4 },
            Reserve = 6,
            Keeper = new UserId { Value = 8 },
            Shared = 3
        });

        Assert.Equal(7, destination.Id);
        Assert.Equal("AB", destination.Name);
        Assert.Equal("<2>", destination.Count);
        Assert.Equal("<3>", destination.Stock);
        Assert.Equal("#5", destination.Owner);
        Assert.Equal("9L", destination.Total);
        Assert.Equal("1.50", destination.Amount);
        Assert.Equal(4, destination.Child.Id);
        Assert.Equal("stamped 7", destination.Stamp);
        Assert.Equal("6L", destination.Reserve);
        Assert.Equal("#8", destination.Keeper);
        Assert.Equal("shared 3", destination.Shared);
    }

    [Fact]
    public void NullIsNotBoxed()
    {
        var destination = LookupOuter.LookupMappers.Map(new LookupSource { Id = 1 });

        Assert.Equal("keep", destination.Stock);
        Assert.Equal("keep", destination.Reserve);
        Assert.Equal("keep", destination.Keeper);
        Assert.Equal("stamped 1", destination.Stamp);
    }
}
