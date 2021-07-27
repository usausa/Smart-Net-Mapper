namespace Smart.Mapper.Expressions
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    using Smart.Mapper.Functions;

    public interface IMemberExpression<TSource, out TDestination, in TMember>
    {
        //--------------------------------------------------------------------------------
        // Info
        //--------------------------------------------------------------------------------

        PropertyInfo DestinationMember { get; }

        //--------------------------------------------------------------------------------
        // Ignore
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> Ignore();

        //--------------------------------------------------------------------------------
        // Nest
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> Nested();

        //--------------------------------------------------------------------------------
        // Order
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> Order(int order);

        //--------------------------------------------------------------------------------
        // Condition
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, bool> condition);

        IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, ResolutionContext, bool> condition);

        IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, ResolutionContext, bool> condition);

        IMemberExpression<TSource, TDestination, TMember> Condition(IMemberCondition<TSource, TDestination> condition);

        IMemberExpression<TSource, TDestination, TMember> Condition<TMemberCondition>()
            where TMemberCondition : IMemberCondition<TSource, TDestination>;

        //--------------------------------------------------------------------------------
        // MapFrom
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> expression);

        IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Expression<Func<TSource, ResolutionContext, TSourceMember>> expression);

        IMemberExpression<TSource, TDestination, TMember> MapFrom(IValueResolver<TSource, TDestination, TMember> resolver);

        IMemberExpression<TSource, TDestination, TMember> MapFrom<TValueResolver>()
            where TValueResolver : IValueResolver<TSource, TDestination, TMember>;

        IMemberExpression<TSource, TDestination, TMember> MapFrom(string sourcePath);

        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember>(Func<TSourceMember, TMember> converter);

        IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember>(Func<TSourceMember, ResolutionContext, TMember> converter);

        IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember>(IValueConverter<TSourceMember, TMember> converter);

        IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember, TValueConverter>()
            where TValueConverter : IValueConverter<TSourceMember, TMember>;

        //--------------------------------------------------------------------------------
        // Constant
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> Const(TMember value);

        //--------------------------------------------------------------------------------
        // Null
        //--------------------------------------------------------------------------------

        IMemberExpression<TSource, TDestination, TMember> NullIf(TMember value);

        IMemberExpression<TSource, TDestination, TMember> NullIgnore();
    }
}
