namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// An update with NullBehavior.Skip into the members of a member creates the member only when it assigns through it
public class PatchUpdateMappingTests
{
    [Fact]
    public void MemberStaysNullWithoutValue()
    {
        var customer = new PatchCustomer();

        TestMappers.Patch(new PatchCustomerRequest(), customer);

        Assert.Null(customer.Address);
    }

    [Fact]
    public void MemberIsCreatedForValue()
    {
        var customer = new PatchCustomer();

        TestMappers.Patch(new PatchCustomerRequest { City = "Osaka" }, customer);

        Assert.NotNull(customer.Address);
        Assert.Equal("Osaka", customer.Address.City);
        Assert.Null(customer.Address.Zip);
    }

    [Fact]
    public void ExistingMemberIsKept()
    {
        var address = new PatchAddress { City = "Tokyo", Zip = "100" };
        var customer = new PatchCustomer { Address = address };

        TestMappers.Patch(new PatchCustomerRequest { Zip = "200" }, customer);

        Assert.Same(address, customer.Address);
        Assert.Equal("Tokyo", address.City);
        Assert.Equal("200", address.Zip);
    }
}
