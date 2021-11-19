namespace Smart.Mapper.Expressions;

using System;

using Smart.Mapper.Functions;
using Smart.Mapper.Options;

internal class DefaultExpression : IDefaultExpression
{
    private readonly DefaultOption defaultOption;

    public DefaultExpression(DefaultOption defaultOption)
    {
        this.defaultOption = defaultOption;
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public IDefaultExpression FactoryUsingServiceProvider()
    {
        defaultOption.SetFactoryUseServiceProvider();
        return this;
    }

    public IDefaultExpression FactoryUsing<TDestination>(Func<TDestination> factory)
    {
        defaultOption.SetFactory(factory);
        return this;
    }

    //--------------------------------------------------------------------------------
    // Converter
    //--------------------------------------------------------------------------------

    public IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> converter)
    {
        defaultOption.SetConverter(converter);
        return this;
    }

    public IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, ResolutionContext, TDestinationMember> converter)
    {
        defaultOption.SetConverter(converter);
        return this;
    }

    public IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> converter)
    {
        defaultOption.SetConverter(converter);
        return this;
    }

    public IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember, TValueConverter>()
        where TValueConverter : IValueConverter<TSourceMember, TDestinationMember>
    {
        defaultOption.SetConverter<TSourceMember, TDestinationMember, TValueConverter>();
        return this;
    }

    //--------------------------------------------------------------------------------
    // Constant
    //--------------------------------------------------------------------------------

    public IDefaultExpression Const<TMember>(TMember value)
    {
        defaultOption.SetConstValue(value);
        return this;
    }

    //--------------------------------------------------------------------------------
    // Null
    //--------------------------------------------------------------------------------

    public IDefaultExpression NullIf<TMember>(TMember value)
    {
        defaultOption.SetNullIfValue(value);
        return this;
    }
}
