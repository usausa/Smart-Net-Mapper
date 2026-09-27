namespace Smart.Mapper;

using Smart.Mapper.Mappers;
using Smart.Mapper.Models;

// Constructor arguments of the parameter's type, parameters named by the attributes, a constructor the mapper
// class cannot call, and a name a member the mapping does not take hides
public class ConstructorParameterMappingTests
{
    // The int of the source is converted to the string the parameter takes
    [Fact]
    public void ArgumentIsConvertedToParameterType()
    {
        var destination = TestMappers.MapParameterType(new ConstructorSource { Code = 12 });

        Assert.Equal(12, destination.Code);
    }

    [Fact]
    public void AttributesTargetParameters()
    {
        var destination = TestMappers.MapParameterOnly(new ConstructorSource { Code = 5 });

        Assert.Equal("#53", destination.Text);
    }

    // The mapping naming the parameter is taken, through the constructor the mapper class can call
    [Fact]
    public void MappingNamingParameterIsTaken()
    {
        var destination = TestMappers.MapParameterName(new ConstructorSource { Code = 1, Other = 7 });

        Assert.Equal(7, destination.Value);
    }

    // The internal Level hides the public one of the base, so neither is mapped automatically, while an attribute
    // that takes it targets the hiding one
    [Fact]
    public void HiddenNameIsNotMapped()
    {
        var destination = TestMappers.MapHidingInternal(new ConstructorSource { Code = 2, Level = 9 });

        Assert.Equal(2, destination.Code);
        Assert.Equal("none", destination.Level);
        Assert.Equal(0, ((HidingInternalBase)destination).Level);
    }

    [Fact]
    public void ConstantTargetsHidingMember()
    {
        var destination = TestMappers.MapHidingInternalConstant(new ConstructorSource());

        Assert.Equal("set", destination.Level);
        Assert.Equal(0, ((HidingInternalBase)destination).Level);
    }
}
