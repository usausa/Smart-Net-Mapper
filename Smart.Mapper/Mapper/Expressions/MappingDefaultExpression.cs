namespace Smart.Mapper.Expressions;

using System;

using Smart.Mapper.Functions;
using Smart.Mapper.Options;

internal class MappingDefaultExpression : IMappingDefaultExpression
{
    private readonly MappingOption mappingOption;

    public MappingDefaultExpression(MappingOption mappingOption)
    {
        this.mappingOption = mappingOption;
    }

    //--------------------------------------------------------------------------------
    // Converter
    //--------------------------------------------------------------------------------

    public IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> converter)
    {
        mappingOption.SetConverter(converter);
        return this;
    }

    public IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, ResolutionContext, TDestinationMember> converter)
    {
        mappingOption.SetConverter(converter);
        return this;
    }

    public IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> converter)
    {
        mappingOption.SetConverter(converter);
        return this;
    }

    public IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember, TValueConverter>()
        where TValueConverter : IValueConverter<TSourceMember, TDestinationMember>
    {
        mappingOption.SetConverter<TSourceMember, TDestinationMember, TValueConverter>();
        return this;
    }

    //--------------------------------------------------------------------------------
    // Constant
    //--------------------------------------------------------------------------------

    public IMappingDefaultExpression Const<TMember>(TMember value)
    {
        mappingOption.SetConstValue(value);
        return this;
    }

    //--------------------------------------------------------------------------------
    // Null
    //--------------------------------------------------------------------------------

    public IMappingDefaultExpression NullIf<TMember>(TMember value)
    {
        mappingOption.SetNullIfValue(value);
        return this;
    }
}
