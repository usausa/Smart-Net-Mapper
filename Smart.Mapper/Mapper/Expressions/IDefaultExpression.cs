namespace Smart.Mapper.Expressions
{
    using System;

    using Smart.Mapper.Functions;

    public interface IDefaultExpression
    {
        //--------------------------------------------------------------------------------
        // Factory
        //--------------------------------------------------------------------------------

        // TODO
        //IDefaultExpression FactoryUsingServiceProvider();

        //IDefaultExpression FactoryUsing<TDestination>(Func<TDestination> factory);

        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        // TODO
        //IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> converter);

        //IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, ResolutionContext, TDestinationMember> converter);

        //IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> converter);

        //IDefaultExpression ConvertUsing<TSourceMember, TDestinationMember, TValueConverter>()
        //    where TValueConverter : IValueConverter<TSourceMember, TDestinationMember>;

        //--------------------------------------------------------------------------------
        // Constant
        //--------------------------------------------------------------------------------

        // TODO IDefaultExpression Const<TMember>(TMember value);

        //--------------------------------------------------------------------------------
        // Null
        //--------------------------------------------------------------------------------

        // TODO IDefaultExpression NullIf<TMember>(TMember value);

        // TODO IDefaultExpression NullIgnore(Type type);
    }
}
